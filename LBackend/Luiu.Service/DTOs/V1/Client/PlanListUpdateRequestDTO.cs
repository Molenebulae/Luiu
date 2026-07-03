using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Luiu.Service.DTOs.V1.Client
{
    // PlanList CRUD：PUT /PlanList/{tripId} 更新行程卡片基本資料時使用的前端輸入契約。
    public record class PlanListUpdateRequestDTO
    {
        public string TripName { get; set; } = string.Empty;
        public string? TripDesc { get; set; }
        [StringLength(10)]
        public string? TripTag { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public byte? PrivacyStatus { get; set; }
        public bool? IsSuggest { get; set; }
        public short? OfficeOper { get; set; }
        public string? ShortCode { get; set; }
        public int? ListId { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
