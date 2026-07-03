using System;
using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class PlanDetailItemDTO
    {
        [JsonPropertyName("DetailID")]
        public int DetailId { get; set; }

        [JsonPropertyName("DayNumber")]
        public int DayNumber { get; set; }

        [JsonPropertyName("SortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("ArrivalTime")]
        public TimeOnly? ArrivalTime { get; set; }

        [JsonPropertyName("StayDuration")]
        public int? StayDuration { get; set; }

        [JsonPropertyName("TransportMode")]
        public byte? TransportMode { get; set; }

        [JsonPropertyName("TransportTime")]
        public int? TransportTime { get; set; }

        [JsonPropertyName("Budget")]
        public decimal? Budget { get; set; }

        [JsonPropertyName("SpotAlias")]
        public string? SpotAlias { get; set; }

        [JsonPropertyName("SpotName")]
        public string SpotName { get; set; } = string.Empty;

        [JsonPropertyName("Notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("Address")]
        public string? Address { get; set; }

        [JsonPropertyName("IsDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("SpotID")]
        public int SpotId { get; set; }

        [JsonPropertyName("GoogleMapID")]
        public string? GoogleMapId { get; set; }

        [JsonPropertyName("OpeningHoursJson")]
        public string? OpeningHoursJson { get; set; }

        [JsonPropertyName("PhotoUrl")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("PhotoReference")]
        public string? PhotoReference { get; set; }

        [JsonPropertyName("Tel")]
        public string? Tel { get; set; }

        [JsonPropertyName("Rating")]
        public decimal? Rating { get; set; }

        [JsonPropertyName("UserRatingCount")]
        public int? UserRatingCount { get; set; }

        [JsonPropertyName("PriceLevel")]
        public string? PriceLevel { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public byte? VersionId { get; set; }
        public byte? PrivacyStatus { get; set; }
        public bool? IsSuggest { get; set; }
        public short? OfficeOper { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public string? PolylineEncoded { get; set; }
    }
}
