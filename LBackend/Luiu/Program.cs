using Luiu.Extensions;
using Luiu.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// 讀取Properties/lauchSettings.json 設定的環境變數
var useCloud = Environment.GetEnvironmentVariable("USE_CLOUD_DB") == "true";

string connectionsString;
if (useCloud)
{
    // 讀取雲端DB連接字串
    var baseCloudConn = builder.Configuration.GetConnectionString("CloudConnection");
    // 取得密碼
    var dbPassword = builder.Configuration["DbPassword"];
    connectionsString = $"{baseCloudConn}Password={dbPassword};";
    Debug.WriteLine("[Mode] Cloud DB (Tailscale)");
}
else
{
    // 讀取本地DB連接字串
    connectionsString = builder.Configuration.GetConnectionString("LocalConnection");
    Debug.WriteLine("[Mode] Local DB (Windows Auth)");
}

// 儲存連接字串
LuiuDbContext.ConnectionString = connectionsString;

// 註冊資料庫
builder.Services.AddDbContext<LuiuDbContext>(options =>
    options.UseSqlServer(connectionsString)
);

// 註冊 Serilog 日誌系統
builder.Host.AddApplocationLogging(builder.Configuration, builder.Environment, connectionsString);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<Luiu.Services.SpotMonthlySnapService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContent, elapsed, ex) =>
    {
        if (ex != null || httpContent.Response.StatusCode >= 500)
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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
