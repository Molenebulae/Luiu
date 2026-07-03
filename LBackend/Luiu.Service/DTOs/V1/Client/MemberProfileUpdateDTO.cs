using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemberProfileUpdateDTO
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
