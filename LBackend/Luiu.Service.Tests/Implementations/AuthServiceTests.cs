using AutoMapper;
using Castle.Core.Logging;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Resend;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Xunit.Abstractions;



namespace Luiu.Service.Tests.Implementations
{
    public class AuthServiceTests
    {
        private readonly ITestOutputHelper _output;
        public AuthServiceTests(ITestOutputHelper output)
        {
            _output = output;
        }
        private LuiuDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<LuiuDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new LuiuDbContext(options);
        }

        // 輔助方法：建立真實的 MemoryCache
        private IMemoryCache GetMemoryCache()
        {
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<IMemoryCache>();
        }

        private IMapper GetMapper()
        {
            var mockMapper = new Mock<IMapper>();

            // 設定它的行為：不管是誰 (It.IsAny<TMember>()) 叫你轉成 MemberDTO，
            // 都直接回傳一個我們宣告好的基本 DTO 物件。
            mockMapper.Setup(m => m.Map<MemberDTO>(It.IsAny<TMember>()))
                      .Returns((TMember source) => new MemberDTO
                      {
                          Email = source.Email,
                          // 這裡可以根據你測試需要用到的欄位手動對應
                      });

            return mockMapper.Object;
        }

        [Fact]
        public async void PrepareRegistrationAsync_ValidEmail_ShouldSaveToCacheAndSendCode()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var cache = GetMemoryCache();
            var mockEmailService = new Mock<IEmailService>();
            var mockTokenService = new Mock<ITokenService>();
            var mockLogger = new Mock<ILogger<AuthService>>();

            var authService = new AuthService(dbContext, mockLogger.Object, GetMapper(), cache, mockTokenService.Object, mockEmailService.Object);

            var email = "chukps0410106@gmail.com";
            var password = "123456789";
            var request = new RegisterRequestDTO { Email = email, Password = password };

            // Act
            var result = await authService.PrepareRegistrationAsync(request);

            // Asssert
            Assert.True(result);

            // 驗證快取資料
            Assert.True(cache.TryGetValue($"RegData_{email}", out RegisterRequestDTO cachedData));
            Assert.Equal(password, cachedData.Password);
            _output.WriteLine($"cache data: {cachedData}");

            // 驗證有沒有驗證碼
            Assert.True(cache.TryGetValue($"RegCode_{email}", out string cachedCode));
            Assert.Equal(6, cachedCode.Length);
            _output.WriteLine($"cache code: {cachedCode}");

            mockEmailService.Verify(
                x => x.SendVerificationCodeAsync(email, cachedCode),
                Times.Once
            );
        }

        [Fact]
        public async void CompleteRegistrationAsync_CorrectCode_ShouldCreateAccountAndCleanCache()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var cache = GetMemoryCache();
            var mockEmailService = new Mock<IEmailService>();
            var mockTokenService = new Mock<ITokenService>();
            var mockLogger = new Mock<ILogger<AuthService>>();

            var authService = new AuthService(dbContext, mockLogger.Object, GetMapper(), cache, mockTokenService.Object, mockEmailService.Object);

        }
    }
}
