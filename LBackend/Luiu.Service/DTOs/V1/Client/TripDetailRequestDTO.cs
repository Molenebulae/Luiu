using System;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class TripDetailCreateRequestDTO
    {
        public string? SpotAlias { get; set; }
        public string? Notes { get; set; }
        public byte DayNumber { get; set; }
        public byte SortOrder { get; set; }
        public TimeOnly? ArrivalTime { get; set; }
        public short? StayDuration { get; set; }
        public byte? TransportMode { get; set; }
        public short? TransportTime { get; set; }
        public decimal? Budget { get; set; }
        public int SpotId { get; set; }
        public byte? VersionId { get; set; }
        public bool? IsMaster { get; set; }
        public int? SuggestBy { get; set; }
        public TripDetailSpotRequestDTO? Spot { get; set; }
    }

    public record class TripDetailUpdateRequestDTO : TripDetailCreateRequestDTO
    {
    }

    public record class TripDetailSyncRequestDTO
    {
        public List<TripDetailCreateRequestDTO> Created { get; set; } = new();

        public List<TripDetailSyncUpdateRequestDTO> Updated { get; set; } = new();

        public List<int> DeletedDetailIds { get; set; } = new();
    }

    public record class TripDetailSyncUpdateRequestDTO : TripDetailUpdateRequestDTO
    {
        public int DetailId { get; set; }
    }

    public record class TripDetailSpotRequestDTO
    {
        public string? GoogleMapID { get; set; }
        public int? RegionID { get; set; }
        public int? MemberID { get; set; }
        public string? SpotName { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
        public string? Tel { get; set; }
        public string? Address { get; set; }
        public string? OfficialURL { get; set; }
        public string? OpeningHoursJson { get; set; }
        public decimal? Rating { get; set; }
        public int? UserRatingCount { get; set; }
        public string? GoogleMapURL { get; set; }
        public string? PriceLevel { get; set; }
        public string? PhotoUrl { get; set; }
        public string? PhotoReference { get; set; }
    }
}
