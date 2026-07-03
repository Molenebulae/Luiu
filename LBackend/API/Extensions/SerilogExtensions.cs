using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Templates;
using Serilog.Templates.Themes;
using System.Data;

namespace API.Extensions
{
    public static class SerilogExtensions
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

            string currentDeveloper = environment.IsDevelopment()
                ? devName
                : (devName ?? "Production_Server");

            host.UseSerilog((context, services, logger) =>
            {
                // 模板
                string template =
                    "[{@t:yyyy-MM-dd HH:mm:ss}] [{@l:u3}] [{Source}] [{Developer}] " +
                    "<{coalesce(Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1), SourceContext, 'System')}> " +
                    "{@m}\n{@x}";

                // CMD用
                var cmdTemplate = new ExpressionTemplate(template, theme: TemplateTheme.Code);
                // 檔案用
                var fileTemplate = new ExpressionTemplate(template);

                // 通用配置
                logger
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()     // 允許從程式碼上下文抓取額外資訊
                    .Enrich.WithProperty("Source", "API")  // 標記專案來源
                    .Enrich.WithProperty("Developer", currentDeveloper)  // 標記操作者

                    .WriteTo.Console(cmdTemplate)  // 同步到IDE
                    .WriteTo.Seq("http://localhost:5341");

                // 檔案儲存: 一般日誌
                logger.WriteTo.File(
                    fileTemplate,
                    path: "logs/all-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,  // 開啟大小新制分檔
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    retainedFileCountLimit: 31,
                    shared: true
                );

                // 檔案儲存: 錯誤日誌，篩選Error以上
                logger.WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                    .WriteTo.File(
                        fileTemplate,
                        path: "logs/only-errors-.txt",
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
                        },
                        columnOptions: columnOptions,
                        restrictedToMinimumLevel: LogEventLevel.Warning
                    );
                }


            });
        }
    }
}
