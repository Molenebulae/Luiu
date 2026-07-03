using API.Configurations;
using API.Extensions;
using Luiu.Service.Extensions;
using Luiu.Service.Implementations;
using Serilog;

// 啟動日誌紀錄 Program.cs 執行的狀況
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Luiu 系統啟動中...");
    var builder = WebApplication.CreateBuilder(args);
    Log.Information("取得builder成功");

    // 取得資料庫連線字串
    string connectionsString = ConnectionFactory.Create(builder.Configuration);
    Log.Information("連線字串連接取得成功");

    // 註冊基礎設施
    builder.Host.AddApplocationLogging(builder.Configuration, builder.Environment, connectionsString);
    builder.Services.AddLuiuDatabase(connectionsString);
    builder.Services.AddLuiuCors(builder.Configuration);
    builder.Services.AddLuiuVersioning();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddLuiuRateLimiter();  // 限制時間內呼叫的次數
    builder.Services.AddSMTP(builder.Configuration);
    builder.Services.AddHttpContextAccessor();  // 登入紀錄用
    Log.Information("註冊基礎設成功");

    //註冊 API 控制器和 Service
    builder.Services.AddMemoryCache();
    builder.Services.AddLuiuControllers();
    builder.Services.AddBusinessServices(builder.Configuration);
    builder.Services.Configure<GoogleMapsOptions>(
        builder.Configuration.GetSection("GoogleMaps"));

    builder.Services.AddHttpClient<GooglePlacesService>();
    builder.Services.AddHttpClient<GoogleRoutesService>();
    builder.Services.AddHostedService<RouteCacheCleanupHostedService>();
    //builder.Services.AddLuiuServices();
    //builder.Services.Add();

    builder.Services.AddOpenApi();

    Log.Information("剩餘設施註冊成功");
    var app = builder.Build();

    // 啟動限制次數
    app.UseRateLimiter();

    // 註冊CORS
    app.UseCors(LuiuConstants.CrosPolicyName);
    Log.Information("註冊CORS成功");

    // 註冊中間層
    app.UseLuiuPipeline();
    Log.Information("註冊中間層成功");

    // 允許存取 wwwroot 的檔案
    app.UseStaticFiles();

    // 驗證身份
    app.UseAuthentication();
    app.UseAuthorization();
    Log.Information("註冊驗證身份套件成功");

    app.MapLuiuApiDocuments();

    // 映射控制器
    app.MapControllers();

    //app.MapControllers().RequireRateLimiting("auth_me_policy");

    Log.Information("Luiu 系統已就緒，開始接收請求。");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Luiu API 服務啟動時發生未預期的崩潰！");
}
finally
{
    // 確保log都寫入了，在關閉 //←"再"關閉だよ
    Log.CloseAndFlush();
}
