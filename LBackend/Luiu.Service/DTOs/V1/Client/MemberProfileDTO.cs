using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class MemberProfileDTO
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string AvatarUrl { get; set; }
        public string Bio { get; set; }

        // 統計資料
        public int TripCount { get; set; }      // 行程規劃數量
        public int MemoryCount { get; set; }    // 回憶紀錄數量
        public int CollectCount { get; set; }   // 搜藏數量
        public int FollowerCount { get; set; }  // 粉絲數
        public int FollowingCount { get; set; } // 追蹤數


    }
}
