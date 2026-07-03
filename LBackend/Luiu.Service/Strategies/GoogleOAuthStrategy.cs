using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Luiu.Domain.Exceptions;
using Luiu.Service.DTOs;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Strategies
{
    public class GoogleOAuthStrategy: IOAuthStrategy
    {
        private readonly IConfiguration _config;
        private readonly ILogger<GoogleOAuthStrategy> _logger;

        public string AuthType => "Google";
        public string ProviderDisplayName => "Google";
        public GoogleOAuthStrategy(IConfiguration config, ILogger<GoogleOAuthStrategy> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<OAuthUserResult> GetUserProfileAsync(string code)
        {
            var clinetId = _config["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("缺少 Google 登入的 ClientId ");
            var clientSecret = _config["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("secrets.json 缺少 Google 登入的 ClientSecret ");
            var redirectUri = _config["Authentication:Google:RedirectUri"] ?? throw new InvalidOperationException("缺少 Google 登入的 RedirectUri ");

            try
            {
                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = clinetId, ClientSecret = clientSecret }
                });

                var tokenResponse = await flow.ExchangeCodeForTokenAsync(
                    userId: "user",
                    code: code,
                    redirectUri: redirectUri,
                    CancellationToken.None
                );

                // TODO: 檢查Token的時間
                // 解密並取得用戶資料
                var payload = await GoogleJsonWebSignature.ValidateAsync(tokenResponse.IdToken);

                return new OAuthUserResult
                {
                    ProviderKey = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name ?? payload.Email.Split('@')[0],
                    AvatarUrl = payload.Picture
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Google 授權失敗] 原因: {ex.Message}");
                throw new AppBadRequestException("Google 認證失敗，請重新操作");
            }
        }
    }
}
