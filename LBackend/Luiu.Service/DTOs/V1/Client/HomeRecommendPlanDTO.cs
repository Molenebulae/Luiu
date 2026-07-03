using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public class HomeRecommendPlanDTO
    {
        public int TripId { get; set; }
        public string Title { get; set; }
        public string Tag { get; set; }
        public string Author { get; set; }  // 使用者Name
        public string AvatarUrl { get; set; }
        public int DurationDays { get; set; }
        public int spotCount { get; set; }
        public string CoverImage { get; set; }
        public string UserId { get; set; }
    }
}
