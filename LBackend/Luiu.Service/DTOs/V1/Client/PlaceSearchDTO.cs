using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    // 前端送來的搜尋請求
    public record class PlaceSearchRequestDTO
    {
        public string Query { get; set; } = string.Empty;
    }

    // 回傳給前端的單一地點資料
    public class PlaceResultDTO
    {
        public string GoogleMapID { get; set; } = string.Empty;
        public int? SpotID { get; set; }          // 若已存DB則有值
        public int? RegionID { get; set; }
        public string SpotName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Rating { get; set; }
        public int? UserRatingCount { get; set; }
        public string? Tel { get; set; }
        public string? OpeningHoursJson { get; set; }
        public string? GoogleMapURL { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoReference { get; set; }
        public string? PriceLevel { get; set; }
    }

    public class GoogleMapPlaceDetailDTO
    {
        [JsonPropertyName("spotId")]
        public int? SpotId { get; set; }

        [JsonPropertyName("placeId")]
        public string PlaceId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("openingHoursJson")]
        public string? OpeningHoursJson { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("userRatingCount")]
        public int? UserRatingCount { get; set; }

        [JsonPropertyName("googleMapUrl")]
        public string? GoogleMapUrl { get; set; }

        [JsonPropertyName("photoUrl")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("photoReference")]
        public string? PhotoReference { get; set; }

        [JsonPropertyName("priceLevel")]
        public string? PriceLevel { get; set; }
    }

    public class GoogleMapPlaceAutocompleteDTO
    {
        [JsonPropertyName("placeId")]
        public string PlaceId { get; set; } = string.Empty;

        [JsonPropertyName("mainText")]
        public string MainText { get; set; } = string.Empty;

        [JsonPropertyName("secondaryText")]
        public string? SecondaryText { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
