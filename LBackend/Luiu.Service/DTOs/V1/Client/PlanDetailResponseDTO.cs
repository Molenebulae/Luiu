using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class PlanDetailResponseDTO
    {
        [JsonPropertyName("TripID")]
        public int TripId { get; set; }

        [JsonPropertyName("TripName")]
        public string TripName { get; set; } = string.Empty;

        [JsonPropertyName("TripDesc")]
        public string? TripDesc { get; set; }

        [JsonPropertyName("TripTag")]
        public string? TripTag { get; set; }

        [JsonPropertyName("StartDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("EndDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("PrivacyStatus")]
        public byte? PrivacyStatus { get; set; }

        [JsonPropertyName("IsSuggest")]
        public bool? IsSuggest { get; set; }

        [JsonPropertyName("OfficeOper")]
        public short? OfficeOper { get; set; }

        [JsonPropertyName("ShortCode")]
        public string? ShortCode { get; set; }

        [JsonPropertyName("ListID")]
        public int? ListId { get; set; }

        [JsonPropertyName("OwnerName")]
        public string OwnerName { get; set; } = string.Empty;

        [JsonPropertyName("PhotoURL")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("CreateAt")]
        public DateTime CreateAt { get; set; }

        [JsonPropertyName("UpdateAt")]
        public DateTime? UpdateAt { get; set; }

        [JsonPropertyName("TripDetails")]
        public List<PlanDetailItemDTO> TripDetails { get; set; } = new();

        [JsonPropertyName("TripComments")]
        public List<TripCommentResponseDTO> TripComments { get; set; } = new();
    }
}
