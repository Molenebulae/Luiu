using API.Configurations;
using Asp.Versioning;
using Luiu.Domain.DTOs;
using Luiu.Domain.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace API.Extensions
{
    public static class ServiceExtensions
    {
        // 註冊資料庫
        public static IServiceCollection AddLuiuDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<LuiuDbContext>(options =>
                options.UseSqlServer(connectionString));
            return services;
        }

        // CORS註冊
        public static IServiceCollection AddLuiuCors(this IServiceCollection services, IConfiguration configuration)
        {
            // 判斷是否為雲端環境
            bool isProduct = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();

            // 3. 陣列防呆與過渡期處理
            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                // 如果在雲端且完全沒設定，給予預設本地與 Postman 網址，確保不閃退且 Postman 可測
                allowedOrigins = new[] { "http://localhost:3030", "https://oauth.pstmn.io" };
            }

            // 4. 資安安全性檢查：判定是否包含萬用字元 "*"
            if (allowedOrigins.Contains("*"))
            {
                if (isProduct)
                {
                    // 雲端產線環境嚴格禁止使用 "*"，避免點火崩潰或資安漏洞
                    throw new InvalidOperationException("啟動失敗: 正式環境禁止將 AllowedOrigins 設定為 '*'！");
                }

                // 本機環境若設定為 "*"，微軟限制此時「不能」呼叫 AllowCredentials()
                services.AddCors(options =>
                {
                    options.AddPolicy(name: LuiuConstants.CrosPolicyName, policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });
                return services;
            }

            // 設定 CORS 政策
            services.AddCors(options =>
            {
                options.AddPolicy(name: LuiuConstants.CrosPolicyName,
                    policy =>
                    {
                        policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                    });
            });
            return services;
        }

        // Controller版本
        public static IServiceCollection AddLuiuVersioning(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(
                    LuiuConstants.ApiVersioning.DefaultMajorVersion,
                    LuiuConstants.ApiVersioning.DefaultMinorVersion);   // 預設版本
                options.AssumeDefaultVersionWhenUnspecified = true; // 用戶端沒有指定版本用預設
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();  // 接受使用URL取得版本
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = LuiuConstants.ApiVersioning.GroupNameFormat;       // 版本顯示方式
                options.SubstituteApiVersionInUrl = true; // 讓Controlller的Route可以自動讀取版本號
            });

            return services;
        }

        // 錯誤統一格式
        public static IServiceCollection AddLuiuControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = actionContext =>
                    {
                        // 抓取錯誤
                        var errors = actionContext.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList();

                        // 統一回傳格式
                        var response = new ResultDTO<object>
                        {
                            Success = false,
                            Message = string.Join("; ", errors),
                            Data = null
                        };

                        return new BadRequestObjectResult(response);
                    };
                });
            return services;
        }

        // 註冊JWT
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("啟動失敗: secrets.json 中缺少了 'JwtSettings/Secret");
            var issuer = jwtSettings["Issuer"] ?? "LuiuBackend";
            var audience = jwtSettings["Audience"] ?? "LuiuFrontend";

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;  // 檢查方式
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;     // 驗證失敗的處理方式
            })
            .AddJwtBearer(options =>
            {
                // 設定驗證機制
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // 檢查是否為後端發出
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    // 檢查是否為前端發出
                    ValidateAudience = true,
                    ValidAudience = audience,

                    // 檢查效期
                    ValidateLifetime = true,

                    // 確認私鑰沒有被篡改
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

                    // 取消時差緩衝
                    ClockSkew = TimeSpan.Zero
                };

                // 設定事件
                options.Events = new JwtBearerEvents
                {
                    // 設定驗證失敗回傳的內容
                    OnChallenge = async context =>
                    {
                        // 跳過預設處理處理
                        context.HandleResponse();

                        // 回傳格式
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var response = new ResultDTO<object>
                        {
                            Success = false,
                            Message = "您尚未登入，或憑證已過期，請重新登入",
                            Data = null
                        };

                        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        await context.Response.WriteAsync(json);
                    },
                    
                    OnMessageReceived = context =>
                    {
                        // 先嘗試從 HTTP Header 抓取 Token 
                        var token = context.Request.Headers["Authorization"]
                            .FirstOrDefault()?.Split(" ").Last();

                        // 2. 如果 Header 沒帶，再嘗試從 Cookie 抓取
                        if (string.IsNullOrEmpty(token))
                        {
                            token = context.Request.Cookies["X-Access-Token"];
                        }

                        context.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });


            return services;
        }

        // 註冊SMTP
        public static IServiceCollection AddSMTP(this IServiceCollection services, IConfiguration configuration)
        {
            var ResendSettings = configuration.GetSection("Resend");
            var apiKey = ResendSettings["ApiKey"] ?? throw new InvalidOperationException("啟動失敗: secrets.json 中缺少了 'Resend/ApiKey");

            services.AddOptions();
            services.AddHttpClient<ResendClient>();
            services.Configure<ResendClientOptions>(o =>
            {
                o.ApiToken = apiKey;
            });
            services.AddTransient<IResend, ResendClient>();

            return services;
        }

        public static IServiceCollection AddLuiuRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    // 設定回傳的 Content-Type 為 JSON
                    context.HttpContext.Response.ContentType = "application/json";

                    // 設定狀態碼（確保也是 429）
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    // 設計你想要的 JSON 結構
                    var response = new ResultDTO<object>
                    {
                        Success = false,
                        Message = "您的請求太過頻繁，請在 1 分鐘後再試一次。",
                        Data = null
                    };

                    // 將物件序列化成 JSON 字串並寫入 Response
                    var jsonString = JsonSerializer.Serialize(response);
                    await context.HttpContext.Response.WriteAsync(jsonString, cancellationToken);
                };

                // 使用 AddPolicy 才能正確拿到 HttpContext context
                // 身份認證用
                options.AddPolicy(LuiuConstants.RateLimitPolicies.IdentityCheck, context =>
                {
                    // 取得分組依據 (優先使用登入名稱，其次是 IP)
                    string partitionKey = context.User?.Identity?.Name
                                          ?? context.Connection.RemoteIpAddress?.ToString()
                                          ?? "anonymous";

                    // 回傳一個固定視窗的限制器
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: key => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // 專門給一般功能
                options.AddPolicy(LuiuConstants.RateLimitPolicies.Business, context =>
                {
                    string partitionKey = context.User?.Identity?.Name
                                          ?? context.Connection.RemoteIpAddress?.ToString()
                                          ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: key => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 1000, // 一般業務防線 250 次 / 1 分鐘
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(5)
                        });
                });

                // 全域1分鐘1千次
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    string partitionKey = context.User?.Identity?.Name
                                          ?? context.Connection.RemoteIpAddress?.ToString()
                                          ?? "anonymous";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: key => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 2000, // 全域寬鬆保底
                            QueueLimit = 0,
                            Window = TimeSpan.FromMinutes(10)
                        });
                });
            });

            return services;
        }
    }
}
