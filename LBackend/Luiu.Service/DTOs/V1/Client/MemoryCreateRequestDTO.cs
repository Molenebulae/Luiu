using System;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemoryCreateRequestDTO
    {
        // 由後端從 JWT Token 解析，前端不需傳此欄位 (保留相容舊呼叫)
        public int? UserId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public int? SourceTripId { get; set; }

        public List<MemoryDayCreateDTO> Days { get; set; } = new();
    }

    public record class MemoryDayCreateDTO
    {
        public int DayNumber { get; set; }
        public List<MemoryStopCreateDTO> Stops { get; set; } = new();
    }

    public record class MemoryStopCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        // 使用字串格式接收時間，避免 System.Text.Json 無法反序列化 TimeSpan
        // 格式範例: "09:00:00" 或 "09:00"
        public string? Time { get; set; }
        public string? Location { get; set; }
        public string? Duration { get; set; }
        public string? Description { get; set; }
        public decimal? Expense { get; set; }
        public int? Rating { get; set; }
        public string? VideoUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
