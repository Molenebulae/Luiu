using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public class HomeHotMemoryDTO
    {
        public int MemoryId { get; set; }
        public string Title { get; set; }
        public string Tag { get; set; }
        public string Author { get; set; }  // 使用者Name
        public string AvatarUrl { get; set; } 
        public string CoverImage { get; set; }
        public int LikeCount { get; set; } 
    }
}
