using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    // PlanList CRUD：回傳格式改為前端可直接讀取的扁平行程卡片資料。
    public record class PlanListResponseDTO
    {
        [JsonPropertyName("TripID")]
        public int TripId { get; set; }

        [JsonPropertyName("TripName")]
        public string TripName { get; set; } = string.Empty;

        [JsonPropertyName("TripDesc")]
        public string? TripDesc { get; set; }

        [JsonPropertyName("TripTag")]
        public string? TripTag { get; set; }

        [JsonPropertyName("StartDate")]
        public DateOnly StartDate { get; set; }

        [JsonPropertyName("EndDate")]
        public DateOnly EndDate { get; set; }

        [JsonPropertyName("PrivacyStatus")]
        public byte? PrivacyStatus { get; set; }

        [JsonPropertyName("IsSuggest")]
        public bool? IsSuggest { get; set; }

        [JsonPropertyName("OfficeOper")]
        public short? OfficeOper { get; set; }

        [JsonPropertyName("ShortCode")]
        public string? ShortCode { get; set; }

        [JsonPropertyName("ListID")]
        public int? ListId { get; set; }

        [JsonPropertyName("PhotoURL")]
        public string? PhotoUrl { get; set; }

        [JsonPropertyName("OwnerName")]
        public string OwnerName { get; set; } = string.Empty;

        [JsonPropertyName("OwnerIconURL")]
        public string? OwnerIconUrl { get; set; }

        [JsonPropertyName("Collaborators")]
        public List<PlanListCollaboratorDTO> Collaborators { get; set; } = new();

        [JsonPropertyName("CreateAt")]
        public DateTime CreateAt { get; set; }

        [JsonPropertyName("UpdateAt")]
        public DateTime? UpdateAt { get; set; }
    }

    // PlanList CRUD：保留共同編輯者輸出位置，本階段先回傳空陣列。
    public record class PlanListCollaboratorDTO
    {
        [JsonPropertyName("CollaboratorID")]
        public int CollaboratorId { get; set; }

        [JsonPropertyName("Role")]
        public byte? Role { get; set; }

        [JsonPropertyName("UserID")]
        public int UserId { get; set; }

        [JsonPropertyName("MemberID")]
        public int? MemberId { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Avatar")]
        public string? Avatar { get; set; }
    }
}
