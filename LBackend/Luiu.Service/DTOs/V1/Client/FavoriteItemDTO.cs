using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public class FavoriteItemDTO
    {
        public int CollectId { get; set; }           // 這是原始項目的 ID (ObjectID)
        public int TargetId { get; set; }
        public string UserId { get; set; }    // 前端需要的 string ID
        public string Type { get; set; }      // 給前端判斷的類型名稱 (例如 "Plan")
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }  // 若為景點則填入分類
        public decimal? Rating { get; set; }
        public int? UserRatingCount { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Latitude { get; set; }
    }
}
