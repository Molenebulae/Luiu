namespace Luiu.Service.DTOs.V1.Client
{
    public class GoogleRouteResponse
    {
        public List<GoogleRoute> Routes { get; set; } = new();
    }

    public class GoogleRoute
    {
        public List<GoogleRouteLeg> Legs { get; set; } = new();

        public GoogleRoutePolyline Polyline { get; set; } = new();

        public int DistanceMeters { get; set; }

        public string Duration { get; set; } = string.Empty;
    }

    public class GoogleRouteLeg
    {
        public int DistanceMeters { get; set; }

        public string Duration { get; set; } = string.Empty;

        public GoogleRoutePolyline Polyline { get; set; } = new();

        public List<GoogleRouteLegStep> Steps { get; set; } = new();
    }

    public class GoogleRouteLegStep
    {
        public int DistanceMeters { get; set; }

        public string StaticDuration { get; set; } = string.Empty;

        public string TravelMode { get; set; } = string.Empty;

        public GoogleRoutePolyline Polyline { get; set; } = new();

        public GoogleNavigationInstruction? NavigationInstruction { get; set; }

        public GoogleTransitDetails? TransitDetails { get; set; }
    }

    public class GoogleRoutePolyline
    {
        public string EncodedPolyline { get; set; } = string.Empty;
    }

    public class GoogleNavigationInstruction
    {
        public string Instructions { get; set; } = string.Empty;
    }

    public class GoogleTransitDetails
    {
        public GoogleTransitStopDetails? StopDetails { get; set; }

        public GoogleTransitLine? TransitLine { get; set; }
    }

    public class GoogleTransitStopDetails
    {
        public GoogleTransitStop? DepartureStop { get; set; }

        public GoogleTransitStop? ArrivalStop { get; set; }
    }

    public class GoogleTransitStop
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GoogleTransitLine
    {
        public string Name { get; set; } = string.Empty;

        public string NameShort { get; set; } = string.Empty;

        public GoogleTransitVehicle? Vehicle { get; set; }
    }

    public class GoogleTransitVehicle
    {
        public string Type { get; set; } = string.Empty;
    }
}
