using Azure.Storage.Blobs;
using Luiu.Service.Implementations;
using Luiu.Service.Interfaces;
using Luiu.Service.Options;
using Luiu.Service.Strategies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Luiu.Service.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddLuiuCache(config);
            services.AddMappingConfigs();
            services.AddInternalServices(config);
            return services;
        }

        // 註冊 Services
        public static IServiceCollection AddInternalServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<DemoAccountOptions>(config.GetSection("DemoAccount"));

            // Scoped, transient, singleton
            services.AddTransient<IEmailService, EmailService>();
            // 第三方登入註冊
            services.AddScoped<IOAuthStrategy, GoogleOAuthStrategy>();

            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<AuthService>();
            services.AddScoped<DemoSessionService>();
            services.AddScoped<MemberService>();
            services.AddScoped<FollowService>();
            services.AddScoped<FavoriteService>();
            services.AddScoped<RecommendService>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<PlanService>();
            services.AddScoped<MemoryService>();
            services.AddScoped<PackingListService>();

            // cache
            services.AddScoped<IVerificationService, VerificationService>();

            // 註冊儲存圖片的service
            bool useAzureBlobStorage = config.GetValue<bool>("UseAzureBlobStorage");
            if (useAzureBlobStorage)
            {
                string blobConnectionString = config["AzureBlobConnectionString"]
                    ?? throw new InvalidOperationException("UseAzureBlobStorage=true，但找不到 AzureBlobConnectionString");

                // 註冊 Azure SDK 官方客戶端
                services.AddSingleton(x => new BlobServiceClient(blobConnectionString));
                services.AddScoped<IStorageService, AzureStorageService>();
                Console.WriteLine("使用 Azure Blob Storage，已掛載：AzureStorageService");
            }
            else
            {
                services.AddScoped<IStorageService, LocalStorageService>();
                Console.WriteLine("使用本地儲存模式，已掛載：LocalStorageService");
            }
            return services;
        }

        private static IServiceCollection AddLuiuCache(this IServiceCollection services, IConfiguration config)
        {
            var useRedis = config.GetValue<bool>("UseRedis");
            if (!useRedis)
            {
                // 不使用 Redis 時，走本地記憶體快取
                return services.AddDistributedMemoryCache();
            }

            // 使用Redis
            var redisType = config.GetValue<string>("RedisType");
            var connectionString = redisType == "Azure"
                ? config.GetConnectionString("AzureRedis")
                : config.GetConnectionString("LocalRedis");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"未設定 {redisType}Redis 的連線字串。");
            }

            // 加上密碼
            if (redisType == "Azure")
            {
                var password = config["AzureRedisPassword"];
                if (string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException("找不到 AzureRedisPassword 設定（請檢查 User Secrets 或環境變數）。");
                }

                connectionString = $"{connectionString},password={password}";
            }

            // 註冊
            var options = ConfigurationOptions.Parse(connectionString);
            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(options));

            services.AddStackExchangeRedisCache(setupOptions =>
            {
                setupOptions.ConnectionMultiplexerFactory = () =>
                {
                    var multiplexer = services.BuildServiceProvider().GetRequiredService<IConnectionMultiplexer>();
                    return Task.FromResult(multiplexer);
                };
                setupOptions.InstanceName = "Luiu_";
            });
            return services;
        }

        // 註冊 Mapping
        public static IServiceCollection AddMappingConfigs(this IServiceCollection services)
        {
            // 它會自動抓到該專案下所有的 Profile 類別
            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies());
            });

            return services;
        }
    }
}
