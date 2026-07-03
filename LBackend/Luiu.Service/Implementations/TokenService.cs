using Luiu.Service.Interfaces;
using Luiu.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Luiu.Service.Implementations
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TokenService> _logger;

        public TokenService (IConfiguration config, ILogger<TokenService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string CreateToken(string userId, int roleType)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("製造 Token 時發現 Secret 遺失！");
            var issuer = jwtSettings["Issuer"] ?? "LuiuBackend";
            var audience = jwtSettings["Audience"] ?? "LuiuFrontend";

            // 設定pyload 的內容
            // 放入userId用於驗證
            var roleName = ((AppEnums.RoleType)roleType).ToString();
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // 唯一ID
                new Claim(ClaimTypes.Role, roleName), // 放入權限
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // 放入發行時間
                
            };
            _logger.LogInformation("claims資料: {claims}", claims);

            // 加密
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenOptions = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinute"] ?? "10")),
                //Expires = DateTime.UtcNow.AddHours(double.Parse(jwtSettings["DurationInHour"] ?? "2")),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };
            _logger.LogInformation("tokenOptions: {options}", tokenOptions);

            // 轉換成字串
            var tokenHeader = new JwtSecurityTokenHandler();
            var token = tokenHeader.CreateToken(tokenOptions);
            _logger.LogInformation("jwt: {jwt}", token);

            return tokenHeader.WriteToken(token);
        }
    }
}
