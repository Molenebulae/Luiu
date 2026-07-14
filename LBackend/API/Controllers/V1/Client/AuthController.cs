using API.Configurations;
using Asp.Versioning;
using AutoMapper;
using Luiu.Domain.DTOs;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace API.Controllers.V1.Client
{
    [ApiVersion("1.0")]
    public class AuthController : BaseController<AuthController>
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        public AuthController(ILogger<AuthController> logger, AuthService authService, IConfiguration config, IMapper mapper) : base(logger)
        {
            _authService = authService;
            _config = config;
            _mapper = mapper;
        }

        // POST: api/v1/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<ResultDTO<MemberDTO>>> Login([FromBody] LoginRequestDTO request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email))
            {
                return BadRequestFail("請輸入帳號密碼");
            }

            var result = await _authService.LoginAsync(request);

            SetAuthCookie(result.Token);  // 設定Cookie
            var response = _mapper.Map<MemberDTO>(result);

            return Success(response, "登入成功");
        }

        // POST: api/v1/Auth/login/demo
        [HttpPost("login/demo")]
        [AllowAnonymous]
        [EnableRateLimiting(LuiuConstants.RateLimitPolicies.DemoLogin)]
        public async Task<ActionResult<ResultDTO<MemberDTO>>> DemoLogin()
        {
            var result = await _authService.DemoLoginAsync();

            SetAuthCookie(result.Token, result.DemoSessionExpiresAt);
            var response = _mapper.Map<MemberDTO>(result);

            return Success(response, "Demo 登入成功");
        }

        // POST: api/v1/Auth/login/google
        [HttpPost("login/google")]
        [AllowAnonymous]
        public async Task<ActionResult<ResultDTO<MemberDTO>>> GoogleLogin([FromBody] LoginGoogleRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequestFail("Google 授權碼不能為空");
            }

            var result = await _authService.OAuthLoginAsync("Google", request.Code);

            SetAuthCookie(result.Token);  // 設定Cookie
            var response = _mapper.Map<MemberDTO>(result);

            return Success(response, "登入成功");
        }

        private void SetAuthCookie(string token, DateTime? expiresAtUtc = null)
        {
            // Jwt Token放入Cookie
            //var expiry = _config.GetValue<int>("JwtSettings:DurationInHour");
            var expiry = _config.GetValue<int>("JwtSettings:DurationInMinute");

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                Path = "/",
                Expires = expiresAtUtc.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc.Value, DateTimeKind.Utc))
                    : DateTimeOffset.UtcNow.AddMinutes(expiry)
                //Expires = DateTimeOffset.UtcNow.AddHours(expiry)
            };

            Response.Cookies.Append("X-Access-Token", token, cookieOptions);
        }

        // GET: api/v1/Auth/me
        [HttpGet("me")]
        [Authorize]
        [EnableRateLimiting(LuiuConstants.RateLimitPolicies.IdentityCheck)]  // 刷新限制
        public async Task<ActionResult<ResultDTO<MemberDTO>>> GetCurrentUser()
        {
            _logger.LogInformation("me");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("{userId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                return UnauthorizedFail("憑證無效，請重新登入");
            }

            var member = await _authService.GetMemberByUserIdAsync(userId);

            return Success(member, "身份驗證成功");
        }

        // POST: api/v1/Auth/login
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ResultDTO<object>>> Logout()
        {
            _logger.LogInformation("執行登出");
            Response.Cookies.Delete("X-Access-Token", new CookieOptions
            {
                // 帶入跟登入相同的參數
                Path = "/",
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax
            });

            return Success<object>("登出成功");
        }


        // POST: api/v1/Auth/register/send-code
        /// <summary>
        /// 發送註冊驗證碼
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("register/send-code")]
        public async Task<ActionResult<ResultDTO<object>>> RegisterSendCode([FromBody] RegisterRequestDTO request)
        {
            // TODO: 攔截連續多次寄信
            _logger.LogInformation($"註冊資料: {request}");
            _logger.LogInformation("執行發送驗證碼");

            await _authService.PrepareRegistrationAsync(request);

            return Success<object>("請到信箱收取驗證碼並完成註冊");
        }


        // POST: api/v1/Auth/register/confirm-code
        /// <summary>
        /// 確認驗證碼
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("register/confirm-code")]
        public async Task<ActionResult<ResultDTO<MemberDTO>>> RegisterConfirm([FromBody] RegisterConfirmDTO request)
        {
            _logger.LogInformation("確認驗證碼");
            var result = await _authService.CompleteRegistrationAsync(request);
            _logger.LogInformation("new member: {member}", result);

            SetAuthCookie(result.Token);  // 設定Cookie
            var response = _mapper.Map<MemberDTO>(result);

            return Success(response);
        }

        // POST: api/v1/Auth/forgot-password/send-code
        [HttpPost("forgot-password/send-code")]
        public async Task<ActionResult<ResultDTO<object>>> SendResetCode([FromBody] ResetSendCodeDTO request)
        {
            // TODO: 攔截連續多次寄信

            await _authService.SendResetPasswordCodeAsync(request.Email);
            _logger.LogInformation("驗證碼已發送");
            return Success<object>("驗證碼已發送");
        }


        // POST: api/v1/Auth/forgot-password/confirm-code
        [HttpPost("forgot-password/confirm-code")]
        public async Task<ActionResult<ResultDTO<object>>> ResetConfirm([FromBody] ResetConfirmDTO request)
        {
            _logger.LogInformation("確認驗證碼");
            await _authService.ConfirmResetCodeAsync(request.Email, request.Code);

            _logger.LogInformation("驗證碼正確");
            return Success<object>("驗證碼正確");
        }

        // POST: api/v1/Auth/forgot-password/reset
        [HttpPost("forgot-password/reset")]
        public async Task<ActionResult<ResultDTO<object>>> ResetPassword([FromBody] ResetPasswordDTO request)
        {
            _logger.LogInformation("重設密碼");
            await _authService.CompleteResetPassword(request);

            _logger.LogInformation("密碼重設成功");
            return Success<object>("密碼重設成功");
        }
    }
}
