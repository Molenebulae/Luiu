using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemberSettingUpdateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateOnly? Birthday { get; set; }
        public int Gender { get; set; } = 0;
        public string? AvatarUrl { get; set; } 
    }
}
