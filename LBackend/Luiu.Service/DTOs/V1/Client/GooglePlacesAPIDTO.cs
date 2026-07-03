using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    // 對應 Google Places API (New) 的回傳結構
    public record class GoogleTextSearchResponseDTO
    {
        public List<GooglePlaceResultDTO> Places { get; set; } = new();
    }

    public record class GoogleAutocompleteResponseDTO
    {
        public List<GoogleAutocompleteSuggestionDTO> Suggestions { get; set; } = new();
    }

    public class GoogleAutocompleteSuggestionDTO
    {
        public GooglePlacePredictionDTO? PlacePrediction { get; set; }
    }

    public class GooglePlacePredictionDTO
    {
        public string PlaceId { get; set; } = string.Empty;
        public GoogleAutocompleteTextDTO? Text { get; set; }
        public GoogleAutocompleteStructuredFormatDTO? StructuredFormat { get; set; }
    }

    public class GoogleAutocompleteStructuredFormatDTO
    {
        public GoogleAutocompleteTextDTO? MainText { get; set; }
        public GoogleAutocompleteTextDTO? SecondaryText { get; set; }
    }

    public class GoogleAutocompleteTextDTO
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GooglePlaceResultDTO
    {
        public string Id { get; set; } = string.Empty;
        public GoogleDisplayName DisplayName { get; set; } = new();
        public string FormattedAddress { get; set; } = string.Empty;
        public GoogleLocation Location { get; set; } = new();
        public double? Rating { get; set; }
        public int? UserRatingCount { get; set; }
        public string? NationalPhoneNumber { get; set; }
        public GoogleOpeningHours? CurrentOpeningHours { get; set; }
        public GoogleOpeningHours? RegularOpeningHours { get; set; }
        public string? GoogleMapsUri { get; set; }
        public List<GooglePlacePhotoDTO> Photos { get; set; } = new();
        public string? PriceLevel { get; set; }
    }

    public class GoogleDisplayName
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GoogleLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class GoogleOpeningHours
    {
        public bool OpenNow { get; set; }
        public List<string> WeekdayDescriptions { get; set; } = new();
    }

    public class GooglePlacePhotoDTO
    {
        public string Name { get; set; } = string.Empty;
        public int? WidthPx { get; set; }
        public int? HeightPx { get; set; }
    }
}
