namespace API.Configurations
{
    public static class ConnectionFactory
    {
        public static string Create(IConfiguration configuration)
        {
            bool useAzure = Environment.GetEnvironmentVariable("USE_AZURE_DB") == "true";
            if (useAzure)
            {
                var azureConnection = configuration.GetConnectionString("AzureConnection")
                    ?? throw new InvalidOperationException("啟動失敗: 找不到 AzureConnection 連接字串");

                if (azureConnection.Contains("Password=", StringComparison.OrdinalIgnoreCase))
                {
                    return azureConnection;
                }

                var dbPassword = configuration["DbPassword"]
                    ?? throw new InvalidOperationException("啟動失敗: AzureConnection 缺少 Password，且 secrets.json 或環境變數中缺少 DbPassword");

                return $"{azureConnection}Password={dbPassword};";
            }


            bool useCloud = Environment.GetEnvironmentVariable("USE_CLOUD_DB") == "true";

            if (useCloud)
            {
                var baseCloudConn = configuration.GetConnectionString("CloudConnection")
                    ?? throw new InvalidOperationException("啟動失敗: 找不到 CloudConnection 連接字串");
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
