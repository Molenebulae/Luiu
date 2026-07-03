
using Castle.Core.Logging;
using Luiu.Service.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Xunit.Abstractions;

namespace Luiu.Service.Tests.Implementations
{
    public class TokenServiceTests
    {
        private readonly ITestOutputHelper _output;
        public TokenServiceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateToken_ValidArguments_ReturnsValidJwtToken()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                {"JwtSettings:Secret", "b73de066-f234-433f-a1f1-fe2f3f69a8666e3cd12c-a4a9-466a-8af1-d6d07e5e4d08"},
                {"JwtSettings:Issuer", "LuiuBackend"},
                {"JwtSettings:Audience", "LuiuFrontend"},
                {"JwtSettings:DurationInMinutes", "60"}
            };
            IConfiguration realConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var mockLogger = new Mock<ILogger<TokenService>>();
            var tokenService = new TokenService(realConfig, mockLogger.Object);
            var expectedUserId = "Luiu_tem-member-01";

            // Act
            var tokenString = tokenService.CreateToken(expectedUserId);

            _output.WriteLine($"JWT Token: {tokenString}");
            _output.WriteLine($"UserId: {expectedUserId}");

            // Assert
            Assert.False(string.IsNullOrEmpty(tokenString));

            var tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(tokenString);

            Assert.Equal("LuiuBackend", jwtToken.Issuer);
            Assert.Equal("LuiuFrontend", jwtToken.Audiences.First());

            var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value;
            Assert.Equal(expectedUserId, nameIdClaim);
        }

        [Fact]
        public void CreateToken_MissingSecret_ThrowsInvalidOperationException()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string>
            {
                {"JwtSettings:Issuer", "LuiuBackend"},
                {"JwtSettings:Audience", "LuiuFrontend"},
                {"JwtSettings:DurationInMinutes", "60"}
            };
            IConfiguration realConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var mockLogger = new Mock<ILogger<TokenService>>();
            var tokenService = new TokenService(realConfig, mockLogger.Object);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken("any_user_id"));
        }
    }
}
