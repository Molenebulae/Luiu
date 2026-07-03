using Microsoft.VisualBasic;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemberDTO
    {
        // 基本資訊
        public string UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        // 系統狀態
        public bool ProfileStatus { get; set; }
        public string? Phone { get; set; }
        public DateOnly? Birthday { get; set; }
        public byte Gender { get; set; } = 0;
        public string Role { get; set; }
    }
}
