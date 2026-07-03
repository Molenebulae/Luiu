using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public class FavoriteAddDTO
    {
        public int TargetId { get; set; }
        public string Type { get; set; }
        public string? OwnerUserId { get; set; }
    }
}
