using API.Middlewares;
using Scalar.AspNetCore;
using Serilog;

namespace API.Extensions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// 統一管理 Luiu 字定義中間件管線
        /// </summary>
        public static IApplicationBuilder UseLuiuPipeline(this WebApplication app)
        {
            // 註冊Middleware
            app.UseMiddleware<ExceptionMiddleware>();

            // Serilog 請求紀錄
            app.UseSerilogRequestLogging(options =>
            {
                options.GetLevel = (httpContent, elapsed, ex) =>
                {
                    // 過濾 Scalar/OpenAPI
                    var path = httpContent.Request.Path.Value ?? "";
                    if (path.Contains("/scalar") || path.Contains("/openapi"))
                        return Serilog.Events.LogEventLevel.Verbose;

                    if (ex != null) return Serilog.Events.LogEventLevel.Debug;

                    if (httpContent.Response.StatusCode >= 500)
                    {
                        return Serilog.Events.LogEventLevel.Error;
                    }

                    if (httpContent.Response.StatusCode == 404)
                    {
                        return Serilog.Events.LogEventLevel.Warning;
                    }

                    return Serilog.Events.LogEventLevel.Information;
                };
            });

            // 基礎功能
            app.UseHttpsRedirection();
            app.UseRouting();

            return app;
        }

        public static void MapLuiuApiDocuments(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options
                        .WithTitle("Luiu API")
                        .WithTheme(ScalarTheme.Moon)
                        .WithDefaultHttpClient(ScalarTarget.JavaScript, ScalarClient.Axios);
                });
            }
        }
    }
}
