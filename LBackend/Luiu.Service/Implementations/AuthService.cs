using AutoMapper;
using AutoMapper.Execution;
using AutoMapper.QueryableExtensions;
using Luiu.Domain.Conmon;
using Luiu.Domain.DTOs;
using Luiu.Domain.Enums;
using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Extensions;
using Luiu.Service.Interfaces;
using Luiu.Service.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class AuthService : BaseService<AuthService>
    {
        private readonly IDistributedCache _cache;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStorageService _storageService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEnumerable<IOAuthStrategy> _strategies;
        private readonly IVerificationService _verifyService;
        public AuthService(
            LuiuDbContext context,
            ILogger<AuthService> logger,
            IMapper mapper,
            IDistributedCache cache,
            ITokenService tokenService,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            IStorageService storageService,
            IHttpClientFactory httpClientFactory,
            IEnumerable<IOAuthStrategy> strategies,
            IVerificationService verifyService) : base(context, logger, mapper)
        {
            _cache = cache;
            _tokenService = tokenService;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _storageService = storageService;
            _httpClientFactory = httpClientFactory;
            _strategies = strategies;
            _verifyService = verifyService;
        }

        public async Task<LoginResultDTO> LoginAsync(LoginRequestDTO request)
        {

            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.Email == request.Email);
            _logger.LogInformation($"{member}");

            if (member == null)
            {
                // 帳號不存使用-1
                await RecordLoginLogAsync(-1, "Failure", "帳號不存在");
                _logger.LogWarning("登入失敗：Email {Email}", request.Email);
                throw new AppUnauthorizedException("帳號或密碼錯誤");
            }

            if (member.IsDelete)
            {
                _logger.LogWarning("登入失敗：嘗試登入已註銷的帳號 {Email}", request.Email);
                // 對外仍提示帳號或密碼錯誤，避免洩漏該 Email 已被註銷的事實
                await RecordLoginLogAsync(member.MemberId, "Failure", "帳號已註銷");
                throw new AppUnauthorizedException("帳號或密碼錯誤");
            }


            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, member.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("登入失敗: 密碼錯誤");
                await RecordLoginLogAsync(member.MemberId, "Failure", "密碼錯誤");
                throw new AppUnauthorizedException("帳號或密碼錯誤");
            }

            // 登入紀錄
            await RecordLoginLogAsync(member.MemberId, "Success", "None");

            var response = _mapper.Map<LoginResultDTO>(member);
            response.Token = _tokenService.CreateToken(member.UserId.ToString(), member.RoleId);

            _logger.LogInformation($"{response}");

            return response;
        }

        public async Task<LoginResultDTO> OAuthLoginAsync(string provider, string code)
        {
            // 判斷這個第三方登入有沒有存在
            var strategy = _strategies.FirstOrDefault(s => s.AuthType.Equals(provider, StringComparison.OrdinalIgnoreCase));
            if (strategy == null) throw new AppBadRequestException($"系統暫不支援 {provider} 登入方式");

            // 取得第三方用戶資料
            OAuthUserResult oauthUser = await strategy.GetUserProfileAsync(code);

            // 取得會員資料
            TMember member = await HandleMemberBindingAsync(provider, strategy.ProviderDisplayName, oauthUser);

            if (member.IsDelete)
            {
                _logger.LogWarning("第三方登入失敗：嘗試登入已註銷的帳號 {Email}", member.Email);
                await RecordLoginLogAsync(member.MemberId, "Failure", "帳號已註銷");
                throw new AppUnauthorizedException("登入失敗，帳號可能已停用或不存在");
            }

            // 登入紀錄
            await RecordLoginLogAsync(member.MemberId, "Success", $"{provider}Login");

            var response = _mapper.Map<LoginResultDTO>(member);
            response.Token = _tokenService.CreateToken(member.UserId, (int)AppEnums.RoleType.Member);

            _logger.LogInformation("MemberDTO: {dto}", response);
            return response;
        }

        /// <summary>
        /// 負責處理 tOAuthLogins 與 tMembers 的查詢跟建立關係
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="oauthUser"></param>
        /// <returns></returns>
        /// <exception cref="AppBadRequestException"></exception>
        private async Task<TMember> HandleMemberBindingAsync(string provider, string providerDisplayName, OAuthUserResult oauthUser)
        {
            var oauthLogin = await _context.TOauthLogins.FirstOrDefaultAsync(o => o.AuthType == provider && o.ProviderKey == oauthUser.ProviderKey);

            if (oauthLogin != null)
            {
                var existingMember = await _context.TMembers.FirstOrDefaultAsync(m => m.MemberId == oauthLogin.MemberId);
                if (existingMember == null) throw new AppBadRequestException("會員解析失敗");
                return existingMember;
            }

            _logger.LogInformation("[第三方登入] {email} 首次使用 {provider} 登入", oauthUser.Email, provider);

            // TODO: 確認Email大小寫的問題(都小寫可以解決)
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.Email == oauthUser.Email);
            if (member == null)
            {
                _logger.LogInformation("[第三方登入] {email} 註冊", oauthUser.Email);

                // 取得大頭貼
                string avatarUrl = await ProcessAvatarUploadAsync(oauthUser.AvatarUrl);

                member = new TMember
                {
                    UserId = "",
                    Email = oauthUser.Email,
                    AvatarUrl = avatarUrl,
                    // 使用Guid當作密碼並加密
                    Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    Name = oauthUser.Name,
                    RoleId = (int)AppEnums.RoleType.Member
                };

                // 儲存
                _context.TMembers.Add(member);
                await _context.SaveChangesAsync();

                // 取得ID
                member.UserId = $"User_{member.MemberId}";
                await _context.SaveChangesAsync();
            }

            // 紀錄第三方登入的綁定關係
            var newLink = new TOauthLogin
            {
                AuthType = provider,
                ProviderKey = oauthUser.ProviderKey,
                ProviderDisplayName = providerDisplayName,
                MemberId = member.MemberId,
                CreateTime = DateTime.UtcNow
            };
            _context.TOauthLogins.Add(newLink);
            await _context.SaveChangesAsync();

            _logger.LogInformation("綁定第三方登入");

            return member;
        }

        /// <summary>
        /// 儲存大頭貼
        /// </summary>
        /// <param name="avatarUrl"></param>
        /// <returns>大頭貼路徑</returns>
        private async Task<string> ProcessAvatarUploadAsync(string? avatarUrl)
        {
            const string defaultAvatar = "";
            if (string.IsNullOrEmpty(avatarUrl)) return defaultAvatar;

            try
            {
                // TODO: 抓取時間限制
                // 抓取二進制圖片
                var httpClient = _httpClientFactory.CreateClient();
                var imageByte = await httpClient.GetByteArrayAsync(avatarUrl);

                var avatarPolicy = CDictionary.UploadPolicies["avatar"];
                return await _storageService.SaveFileAsync(imageByte, ".jpg", avatarPolicy);
            }
            catch (Exception ex)
            {
                _logger.LogError("大頭貼下載失敗。原因: {ex}", ex.Message);
                return defaultAvatar;
            }
        }


        private async Task RecordLoginLogAsync(int memberId, string status, string failureReason = "")
        {
            // TODO: 防DOS攻擊，要做登入速率限制
            // TODO: 背景工作儲存登入紀錄
            // TODO: Azure Ip抓取可能需要使用 forwarded headers插件
            string ip = _httpContextAccessor.GetClientIp();

            var context = _httpContextAccessor.HttpContext;

            //// 取得ip位置
            //string ipAddress = context?.Request.Headers["X-Forwarded-For"].ToString() ?? "";
            //if (string.IsNullOrEmpty(ipAddress))
            //{
            //    ipAddress = context?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            //}

            // 取得瀏覽器資訊
            string deviceInfo = context?.Request.Headers["User-Agent"].ToString() ?? "Unknown";

            // TODO: 可以使用第三方套件取得位置資訊(ip-api.com, ipinfo.io)
            string location = "Taiwan";

            var log = new TLoginLog
            {
                MemberId = memberId,
                Ipaddress = ip,
                Location = location,
                DeviceInfo = deviceInfo,
                Status = status,
                FailureReason = failureReason,
                LoginTime = DateTime.UtcNow
            };

            _context.TLoginLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("登入紀錄: {log}", log);
        }

        public async Task<bool> PrepareRegistrationAsync(RegisterRequestDTO request)
        {
            bool isExist = await _context.TMembers.AnyAsync(m => m.Email == request.Email);
            if (isExist)
            {
                throw AppConflictException.EmailDuplicate(request.Email);
            }

            // 攔截頻繁發送
            bool isAllowed = await _verifyService.CheckAndSetCooldownAsync(request.Email, "Register", 60);
            if (!isAllowed) throw new AppBadRequestException("請勿頻繁發送，請稍後再試");

            // 驗證碼
            string verificationCode = new Random().Next(100000, 999999).ToString();

            // 設定10分鐘的快取
            var cachedOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            // 存入快取
            await _verifyService.SetRegistrationDataAsync(request.Email, request, 600);
            await _verifyService.SetCodeAsync(request.Email, verificationCode, 600);
            //_cache.Set($"RegData_{request.Email}", request, cachedOptions);
            //_cache.Set($"RegCode_{request.Email}", verificationCode, cachedOptions);

            // 寄信
            await _emailService.SendVerificationCodeAsync(request.Email, verificationCode);
            return true;
        }


        public async Task<LoginResultDTO> CompleteRegistrationAsync(RegisterConfirmDTO request)
        {
            _logger.LogInformation("開始執行註冊確認流程: {Email}", request.Email);

            string? savedCode = await _verifyService.GetCodeAsync(request.Email);
            if (savedCode == null)
            {
                await _verifyService.LogVerificationAsync(0, AppEnums.VerificationType.Register, request.Code, AppEnums.VerificationStatus.Expired);
                throw new AppBadRequestException("驗證碼已過期");
            }

            if (savedCode != request.Code)
            {
                await _verifyService.LogVerificationAsync(0, AppEnums.VerificationType.Register, request.Code, AppEnums.VerificationStatus.Failed);
                throw new AppBadRequestException("驗證碼錯誤");
            }

            var regData = await _verifyService.GetRegistrationDataAsync<RegisterRequestDTO>(request.Email);
            if (regData == null)
            {
                await _verifyService.LogVerificationAsync(0, AppEnums.VerificationType.Register, request.Code, AppEnums.VerificationStatus.Failed);
                throw new AppNotFoundException("找不到註冊資料，請重新註冊");
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(regData.Password);  // 加密
            var newMember = new TMember
            {
                UserId = "",
                Email = regData.Email,
                Password = hashedPassword,
                Name = regData.Email.Split('@')[0],
                RoleId = (int)AppEnums.RoleType.Member
            };

            // 儲存
            _context.TMembers.Add(newMember);
            await _context.SaveChangesAsync();

            // 取得ID
            newMember.UserId = $"User_{newMember.MemberId}";
            await _context.SaveChangesAsync();

            // 移除快取
            await _verifyService.RemoveCodeAsync(request.Email);

            // 紀錄驗證紀錄
            await _verifyService.LogVerificationAsync(
                newMember.MemberId,
                AppEnums.VerificationType.Register,
                request.Code,
                AppEnums.VerificationStatus.Success
            );

            // 登入紀錄
            await RecordLoginLogAsync(newMember.MemberId, "Success", "None");

            var response = _mapper.Map<LoginResultDTO>(newMember);
            response.Token = _tokenService.CreateToken(newMember.UserId, (int)AppEnums.RoleType.Member);

            _logger.LogInformation($"{response}");

            return response;
        }

        public async Task SendResetPasswordCodeAsync(string email)
        {
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.Email == email);
            if (member == null) throw new AppNotFoundException("找不到此帳號");

            // 攔截頻繁發送
            bool isAllowed = await _verifyService.CheckAndSetCooldownAsync(email, "ResetPassword", 60);
            if (!isAllowed) throw new AppBadRequestException("請勿頻繁發送，請稍後再試");

            // 取得驗證碼
            string verificationCode = new Random().Next(100000, 999999).ToString();
            _logger.LogInformation("驗證碼: {code}", verificationCode);

            // 儲存驗證碼
            await _verifyService.SetCodeAsync(email, verificationCode, 600);
            _logger.LogInformation("Reset Email: {email}, Code: {code}", email, verificationCode);

            // 寄信
            await _emailService.SendVerificationCodeAsync(email, verificationCode);
        }

        public async Task ConfirmResetCodeAsync(string email, string code)
        {
            // 取得 memberId 
            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.Email == email);
            int memberId = member?.MemberId ?? 0;

            string? savedCode = await _verifyService.GetCodeAsync(email);

            if (string.IsNullOrEmpty(savedCode))
            {
                await _verifyService.LogVerificationAsync(memberId, AppEnums.VerificationType.ResetPassword, code, AppEnums.VerificationStatus.Expired);
                throw new AppBadRequestException("驗證碼已過期");
            }

            if (savedCode != code)
            {
                await _verifyService.LogVerificationAsync(memberId, AppEnums.VerificationType.ResetPassword, code, AppEnums.VerificationStatus.Failed);
                throw new AppBadRequestException("驗證碼錯誤");
            }
        }

        public async Task CompleteResetPassword(ResetPasswordDTO request)
        {
            // 再做一次驗證
            await ConfirmResetCodeAsync(request.Email, request.Code);

            var member = await _context.TMembers.FirstOrDefaultAsync(m => m.Email == request.Email);
            if (member == null) throw new AppNotFoundException("帳號異常");

            // TODO: 密碼長度驗證
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);  // 加密
            member.Password = hashedPassword;
            member.UpdateDate = DateTime.Now;
            _logger.LogInformation("資料更新時間: {time}", member.UpdateDate);

            // 儲存變更
            _context.TMembers.Update(member);
            await _context.SaveChangesAsync();

            await _verifyService.LogVerificationAsync(
                member.MemberId,
                AppEnums.VerificationType.ResetPassword,
                request.Code,
                AppEnums.VerificationStatus.Success
            );

            await _verifyService.RemoveCodeAsync(request.Email);
        }

        public async Task<MemberDTO> GetMemberByUserIdAsync(string userId)
        {
            return await _context.TMembers
                .Where(m => m.UserId == userId)
                .ProjectTo<MemberDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }
    }
}
