using Luiu.Domain.DTOs;
using Luiu.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            } 
            catch (Exception ex)
            {
                // 抓取更精準的來源資訊
                var stackTrace = new System.Diagnostics.StackTrace(ex);
                // 找到第一行屬於 API 命名空間的 Frame
                var frame = stackTrace.GetFrames()
                    ?.FirstOrDefault(f => f.GetMethod()?.DeclaringType?.FullName?.StartsWith("API") == true);

                var method = frame?.GetMethod();
                var typeName = method?.DeclaringType?.Name ?? "UnknownType";

                // 處理 async 方法產生的 <MethodName>d__1 雜訊
                if (typeName.Contains(">d__"))
                {
                    typeName = method?.DeclaringType?.DeclaringType?.Name ?? typeName;
                }

                var methodName = method?.Name ?? "UnknownMethod";
                // 再次檢查如果是 MoveNext，則嘗試抓取原方法名
                if (methodName == "MoveNext" && method?.DeclaringType?.Name.Contains("<") == true)
                {
                    methodName = method.DeclaringType.Name.Split('>')[0].TrimStart('<');
                }

                var statusCode = ex is BaseAppException appEx ? appEx.StatusCode : 500;

                // 結構化紀錄
                _logger.LogError(ex,
                    "[系統異常] 狀態: {StatusCode} | 來源: {LuiuSource} | 路徑: {Method} {Path} | 訊息: {ErrorMessage}",
                    statusCode,
                    $"{typeName}.{methodName}", // 這會顯示如 MemberService.trytrysee
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            // 預設狀態碼500
            int statusCode = (int)HttpStatusCode.InternalServerError;
            string message = "伺服器發生意外：" + ex.Message;
            object? data = null;

            if (ex is BaseAppException appEx)
            {
                statusCode = appEx.StatusCode;
                message = appEx.Message;
                data = appEx.ExtraData;
            }

            // 回應格式
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            var response = new ResultDTO<object>
            {
                Success = false,
                Message = message,
                Data = data,
            };

            // 轉換成JSON
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
