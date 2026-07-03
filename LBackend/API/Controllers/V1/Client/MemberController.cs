using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers.V1.Client
{
    public class MemberController : BaseController<MemberController>
    {
        private readonly MemberService _memberService;
        private readonly IStorageService _storageService;
        public MemberController(ILogger<MemberController> logger, MemberService memberService, IStorageService storageService) : base(logger)
        {
            _memberService = memberService;
            _storageService = storageService;
        }

        // GET: api/v1/Member/profile/user-01
        [HttpGet("profile/{userId}")]
        public async Task<ActionResult<ResultDTO<MemberProfileDTO>>> GetProfile(string userId)
        {
            _logger.LogInformation("取得個人檔案資料");
            var profile = await _memberService.GetProfileAsync(userId);

            _logger.LogInformation("成功取得資料: {profile}", profile);
            return Success(profile);
        }

        // PUT: api/v1/Member/profile/user-01
        [Authorize]
        [HttpPut("profile/{userId}")]
        public async Task<ActionResult<ResultDTO<MemberProfileDTO>>> UpdateProfile(string userId, [FromBody] MemberProfileUpdateDTO request)
        {
            _logger.LogInformation("更新個人檔案");
            _logger.LogInformation("取得資料: {data}", request);
            var nowUserId = await _memberService.UpdateMemberAsync(userId, request);
            _logger.LogInformation("更新成功");

            var profile = await _memberService.GetProfileAsync(nowUserId);

            return Success(profile, "更新成功");
        }

        // PUT: api/v1/Member/settings
        [Authorize]
        [HttpPut("settings")]
        public async Task<ActionResult<ResultDTO<MemberSettingUpdateDTO>>> UpdateSettings([FromBody] MemberSettingUpdateDTO dto)
        {
            // 取得登入者
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogInformation("使用者未登入");
                return UnauthorizedFail("使用者未登入");
            }

            _logger.LogInformation("使用資料: {dto}", dto);
            _logger.LogInformation("更新隱私資料中...");
            // 更新
            var result = await _memberService.UpdateSettingsAsync(currentUserId, dto);

            _logger.LogInformation("帳戶隱私設定已成功更新");
            return Success(result, "帳戶隱私設定已成功更新");
        }

        // PUT: api/v1/Member/password
        [Authorize]
        [HttpPut("password")]
        public async Task<ActionResult<ResultDTO<object>>> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogInformation("使用者未登入");
                return UnauthorizedFail("使用者未登入");
            }

            _logger.LogInformation("進行變更密碼");
            // 呼叫邏輯層進行密碼變更
            await _memberService.ChangePasswordAsync(userIdClaim, dto);

            return Success<object>("密碼已變更成功");
        }

        // DELETE: api/v1/Member/account
        [Authorize]
        [HttpDelete("account")]
        public async Task<ActionResult<ResultDTO<object>>> DeleteAccount()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogInformation("使用者未登入");
                return UnauthorizedFail("使用者未登入");
            }

            await _memberService.SoftDeleteMemberAsync(userIdClaim);
            return Success<object>("帳號已成功註銷");
        }
    }
}
