namespace API.Configurations
{
    public static class LuiuConstants
    {
        public const string CrosPolicyName = "LuiuCorsPolicy";

        public static class ApiVersioning
        {
            public const string GroupNameFormat = "'v'VVV";
            public const int DefaultMajorVersion = 1;
            public const int DefaultMinorVersion = 0;
        }

        public static class RateLimitPolicies
        {
            // 專門給 me API（身分確認、狀態對帳）使用的嚴格防線（需手動掛標籤）
            public const string IdentityCheck = "IdentityCheckPolicy";

            // 專門給一般功能（產品、購物車等業務）使用的中度防線（需手動掛標籤）
            public const string Business = "BusinessPolicy";

            // 全域預設保底防線（系統自動套用，寫在這裡作為備忘與架構紀錄）
            public const string GlobalDefault = "GlobalDefaultPolicy";
        }
    }
}
