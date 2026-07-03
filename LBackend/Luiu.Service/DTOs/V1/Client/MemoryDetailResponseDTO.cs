using System;
using System.Collections.Generic;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemoryDetailResponseDTO
    {
        public int MemoryId { get; set; }
        
        public int UserId { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        
        public int ReviewStatus { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int FavoriteCount { get; set; }
        public int? SourceTripId { get; set; }

        // 巢狀結構：回憶的天數與景點
        public List<MemoryDayDTO> Days { get; set; } = new();
    }

    public record class MemoryDayDTO
    {
        public int DayId { get; set; }
        public int DayNumber { get; set; }
        public DateOnly? DayDate { get; set; }
        
        // 該天的所有景點
        public List<MemoryStopDTO> Stops { get; set; } = new();
    }

    public record class MemoryStopDTO
    {
        public int StopId { get; set; }
        public string PlaceName { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public string? MemoryText { get; set; }
        public string? VideoEmbedUrl { get; set; }
        public string? Duration { get; set; }
        public decimal? Expense { get; set; }
        public int? Rating { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
