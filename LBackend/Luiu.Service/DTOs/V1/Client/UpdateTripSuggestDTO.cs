using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class UpdateTripSuggestRequestDTO
    {
        [JsonPropertyName("IsSuggest")]
        public bool? IsSuggest { get; set; }

        [JsonPropertyName("OfficeOper")]
        public short? OfficeOper { get; set; }
    }

    public record class UpdateTripSuggestResponseDTO
    {
        [JsonPropertyName("TripID")]
        public int TripId { get; set; }

        [JsonPropertyName("IsSuggest")]
        public bool? IsSuggest { get; set; }

        [JsonPropertyName("OfficeOper")]
        public short? OfficeOper { get; set; }
    }
}
