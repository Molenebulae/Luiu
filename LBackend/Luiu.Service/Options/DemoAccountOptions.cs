namespace Luiu.Service.Options
{
    public class DemoAccountOptions
    {
        public bool Enabled { get; set; } = true;
        public int SessionDurationMinutes { get; set; } = 120;
        public int MaxActiveSessions { get; set; } = 10;
        public int MaxActiveSessionsPerIp { get; set; } = 2;
        public int LoginPermitLimit { get; set; } = 3;
        public int LoginWindowMinutes { get; set; } = 10;
        public int PlaceSearchLimit { get; set; } = 30;
        public int RouteComputeLimit { get; set; } = 10;
        public int RouteExternalLegLimit { get; set; } = 30;
        public int CreatedTripLimit { get; set; } = 5;
        public int CreatedCollectLimit { get; set; } = 30;
        public int CleanupIntervalMinutes { get; set; } = 10;
        public string DemoEmail { get; set; } = "demo@luiu.local";
        public string DemoName { get; set; } = "Demo 使用者";
    }
}
