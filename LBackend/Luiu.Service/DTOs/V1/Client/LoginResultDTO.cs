using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class LoginResultDTO
    {
        // 基本資訊
        public string UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public bool ProfileStatus { get; set; }
        public string? Phone { get; set; }
        public DateOnly? Birthday { get; set; }
        public byte Gender { get; set; } = 0;
        public string Role { get; set; }

        // JWT
        public string? Token { get; set; }
    }
}
