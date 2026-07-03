using System;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemoryListResponseDTO
    {
        public int MemoryId { get; set; }
        
        // 為了前端可以顯示是誰發佈的回憶
        public int UserId { get; set; }
        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public int FavoriteCount { get; set; }
        public int? SourceTripId { get; set; }
    }
}
