namespace API.Configurations
{
    public static class ConnectionFactory
    {
        public static string Create(IConfiguration configuration)
        {
            bool useAzure = Environment.GetEnvironmentVariable("USE_AZURE_DB") == "true";
            if (useAzure)
            {
                var baseCloudConn = configuration.GetConnectionString("AzureConnection");
                var dbPassword = configuration["DbPassword"]
                    ?? throw new InvalidOperationException("啟動失敗: secrets.json 中缺少了 DbPassword");

                return $"{baseCloudConn}Password={dbPassword};";
            }


            bool useCloud = Environment.GetEnvironmentVariable("USE_CLOUD_DB") == "true";

            if (useCloud)
            {
                var baseCloudConn = configuration.GetConnectionString("CloudConnection");
                var dbPassword = configuration["DbPassword"]
                    ?? throw new InvalidOperationException("啟動失敗: secrets.json 中缺少了 DbPassword");

                return $"{baseCloudConn}Password={dbPassword};";
            }

            bool isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
            if (isProduction)
            {
                throw new InvalidOperationException("啟動失敗: 雲端環境 USE_AZURE_DB 跟 USE_CLOUD_DB 其中一個要為true");
            }
            return configuration.GetConnectionString("LocalConnection")
                ?? throw new InvalidOperationException("啟動失敗: 找不到 LocalConnection 連接字串");
        }
    }
}
