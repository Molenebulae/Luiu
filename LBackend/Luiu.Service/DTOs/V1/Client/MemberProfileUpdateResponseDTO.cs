using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public class MemberProfileUpdateResponseDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; }
    }
}
