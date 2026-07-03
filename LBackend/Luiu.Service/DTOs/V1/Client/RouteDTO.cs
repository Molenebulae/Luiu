using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    public class RouteRequestDTO
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;

        [JsonIgnore]
        public int TripId { get; set; }

        public byte DayNumber { get; set; }

        public List<string> TravelMode { get; set; } = new();

        public List<RouteStopDTO>? Stops { get; set; }
    }

    public class RouteStopDTO
    {
        public int? DetailId { get; set; }

        public int? SpotId { get; set; }

        public string? GoogleMapId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public byte SortOrder { get; set; }
    }

    public class RouteResultDTO
    {
        public int TotalDistanceMeters { get; set; }

        public int TotalDurationSeconds { get; set; }

        public int TotalDurationMinutes => (int)Math.Ceiling(TotalDurationSeconds / 60.0);

        public string OverviewPolyline { get; set; } = string.Empty;

        public List<RouteLegDTO> Legs { get; set; } = new();
    }

    public class RouteLegDTO
    {
        public int? FromSpotId { get; set; }

        public int? ToSpotId { get; set; }

        public int? FromDetailId { get; set; }

        public int? ToDetailId { get; set; }

        public string? FromGoogleMapId { get; set; }

        public string? ToGoogleMapId { get; set; }

        public string FromName { get; set; } = string.Empty;

        public string ToName { get; set; } = string.Empty;

        public string TravelMode { get; set; } = string.Empty;

        public int DistanceMeters { get; set; }

        public int DurationSeconds { get; set; }

        public int DurationMinutes => (int)Math.Ceiling(DurationSeconds / 60.0);

        public string Polyline { get; set; } = string.Empty;

        public List<RouteStepDTO> Steps { get; set; } = new();
    }

    public class RouteStepDTO
    {
        public string Type { get; set; } = string.Empty;

        public string? TransitMode { get; set; }

        public string? LineName { get; set; }

        public string? DepartureStop { get; set; }

        public string? ArrivalStop { get; set; }

        public string Instruction { get; set; } = string.Empty;

        public int DurationSeconds { get; set; }

        public int DurationMinutes => (int)Math.Ceiling(DurationSeconds / 60.0);

        public int DistanceMeters { get; set; }
    }
}
