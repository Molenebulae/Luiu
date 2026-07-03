using Microsoft.EntityFrameworkCore.Metadata;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Composition;
using System.Data;

namespace Luiu.Extensions
{
    public static class CSerilogExtensions
    {
        public static void AddApplocationLogging(
            this IHostBuilder host, 
            IConfiguration configuration,
            IWebHostEnvironment environment,
            string dbConnectString)
        {
            var devName = configuration["DeveloperName"];

            if (environment.IsDevelopment() && string.IsNullOrEmpty(devName))
                throw new InvalidOperationException("啟動失敗: secrets.json 中缺少了 'DeveloperName");

            host.UseSerilog((context, services, logger) =>
            {
                // log輸出格式
                const string logTemplate = "{Timestamp:yyy-MM-dd HH:mm:ss} [{Level:u3}] [{Source}] [{Developer}] {Message:lj}{NewLine}{Exception}";

                // 通用配置
                logger
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()     // 允許從程式碼上下文抓取額外資訊
                    .Enrich.WithProperty("Source", "MVC")  // 標記專案來源
                    .Enrich.WithProperty("Developer", devName ?? "Production_Server")  // 標記操作者
                    .WriteTo.Console(outputTemplate: logTemplate)  // 同步到IDE
                    .WriteTo.Seq("http://localhost:5341");

                // 檔案儲存: 一般日誌
                logger.WriteTo.File(
                    path: "logs/all-log-.txt",
                    outputTemplate: logTemplate,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,  // 開啟大小新制分檔
                    fileSizeLimitBytes: 10 * 1024 * 1024,  // 10MB
                    retainedFileCountLimit: 31,  // 保留最近31個檔案
                    shared: true
                );

                // 檔案儲存: 錯誤日誌，篩選Error以上
                logger.WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                    .WriteTo.File(
                        path: "logs/only-errors-.txt",
                        outputTemplate: logTemplate,
                        rollingInterval: RollingInterval.Day,
                        rollOnFileSizeLimit: true,
                        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                        retainedFileCountLimit: null  // 不限制數量
                    )
                );

                var columnOptions = new ColumnOptions();
                columnOptions.AdditionalColumns = new List<SqlColumn>
                {
                    new SqlColumn { ColumnName = "Source", DataType = SqlDbType.NVarChar, DataLength = 50},
                    new SqlColumn { ColumnName = "Developer", DataType = SqlDbType.NVarChar, DataLength = 50},
                   
                };
                // 資料庫儲存: 只存Warning以上
                if (!string.IsNullOrEmpty(dbConnectString))
                {
                    logger.WriteTo.MSSqlServer(
                        connectionString: dbConnectString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "tSystemLogs",
                            AutoCreateSqlTable = true // 如果資料表不存在自動建立
                        },
                        columnOptions: columnOptions,
                        restrictedToMinimumLevel: LogEventLevel.Warning
                    );
                }


            });
        }
    }
}
