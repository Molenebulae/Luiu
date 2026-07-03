using System;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemoryQueryParameters
    {
        // 如果未來有需要關鍵字搜尋或分頁，可以加在這裡
        public string? Keyword { get; set; }
        public int? UserId { get; set; }
    }
}
