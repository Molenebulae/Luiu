using AutoMapper;
using Castle.Core.Logging;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Luiu.Service.Options;
using Luiu.Service.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        private AuthService CreateAuthService(
            LuiuDbContext dbContext,
            Mock<IEmailService> mockEmailService,
            Mock<ITokenService> mockTokenService,
            Mock<IVerificationService> mockVerificationService)
        {
            var mockLogger = new Mock<ILogger<AuthService>>();
            var mockDistributedCache = new Mock<IDistributedCache>();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockStorageService = new Mock<IStorageService>();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var demoOptions = Microsoft.Extensions.Options.Options.Create(new DemoAccountOptions());
            var demoSessionService = new DemoSessionService(
                dbContext,
                mockHttpContextAccessor.Object,
                demoOptions,
                new Mock<ILogger<DemoSessionService>>().Object);

            return new AuthService(
                dbContext,
                mockLogger.Object,
                GetMapper(),
                mockDistributedCache.Object,
                mockTokenService.Object,
                mockEmailService.Object,
                mockHttpContextAccessor.Object,
                mockStorageService.Object,
                mockHttpClientFactory.Object,
                Array.Empty<IOAuthStrategy>(),
                mockVerificationService.Object,
                demoSessionService,
                demoOptions);
        }

        [Fact]
        public async Task PrepareRegistrationAsync_ValidEmail_ShouldSaveToCacheAndSendCode()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var mockTokenService = new Mock<ITokenService>();
            var mockVerificationService = new Mock<IVerificationService>();
            mockVerificationService
                .Setup(x => x.CheckAndSetCooldownAsync(It.IsAny<string>(), "Register", 60))
                .ReturnsAsync(true);

            var authService = CreateAuthService(dbContext, mockEmailService, mockTokenService, mockVerificationService);

            var email = "chukps0410106@gmail.com";
            var password = "123456789";
            var request = new RegisterRequestDTO { Email = email, Password = password };

            // Act
            var result = await authService.PrepareRegistrationAsync(request);

            // Asssert
            Assert.True(result);

            mockVerificationService.Verify(
                x => x.SetRegistrationDataAsync(email, request, 600),
                Times.Once
            );
            mockVerificationService.Verify(
                x => x.SetCodeAsync(email, It.Is<string>(code => code.Length == 6), 600),
                Times.Once
            );

            mockEmailService.Verify(
                x => x.SendVerificationCodeAsync(email, It.Is<string>(code => code.Length == 6)),
                Times.Once
            );
        }

        [Fact]
        public async Task CompleteRegistrationAsync_CorrectCode_ShouldCreateAccountAndCleanCache()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var mockTokenService = new Mock<ITokenService>();
            var mockVerificationService = new Mock<IVerificationService>();

            var authService = CreateAuthService(dbContext, mockEmailService, mockTokenService, mockVerificationService);
            await Task.CompletedTask;

        }
    }
}
