using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class PackingListResponseDTO
    {
        [JsonPropertyName("ListID")]
        public int ListId { get; set; }

        [JsonPropertyName("ListName")]
        public string ListName { get; set; } = string.Empty;

        [JsonPropertyName("UserID")]
        public int UserId { get; set; }

        [JsonPropertyName("UserName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("IsDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("CreateAt")]
        public DateTime CreateAt { get; set; }

        [JsonPropertyName("UpdateAt")]
        public DateTime? UpdateAt { get; set; }

        [JsonPropertyName("Categories")]
        public List<PackingCategoryResponseDTO> Categories { get; set; } = new();
    }

    public record class PackingListSummaryResponseDTO
    {
        [JsonPropertyName("ListID")]
        public int ListId { get; set; }

        [JsonPropertyName("ListName")]
        public string ListName { get; set; } = string.Empty;
    }

    public record class PackingCategoryResponseDTO
    {
        [JsonPropertyName("CategoryID")]
        public int CategoryId { get; set; }

        [JsonPropertyName("CategoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("ListID")]
        public int ListId { get; set; }

        [JsonPropertyName("IsDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("CreateAt")]
        public DateTime CreateAt { get; set; }

        [JsonPropertyName("UpdateAt")]
        public DateTime? UpdateAt { get; set; }

        [JsonPropertyName("Items")]
        public List<PackingItemResponseDTO> Items { get; set; } = new();
    }

    public record class PackingItemResponseDTO
    {
        [JsonPropertyName("ItemID")]
        public int ItemId { get; set; }

        [JsonPropertyName("ItemName")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("IsCheck")]
        public bool IsCheck { get; set; }

        [JsonPropertyName("CategoryID")]
        public int CategoryId { get; set; }

        [JsonPropertyName("IsDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("CreateAt")]
        public DateTime CreateAt { get; set; }

        [JsonPropertyName("UpdateAt")]
        public DateTime? UpdateAt { get; set; }
    }

    public record class PackingListCreateRequestDTO
    {
        [JsonPropertyName("ListName")]
        public string? ListName { get; set; }
    }

    public record class PackingListUpdateRequestDTO
    {
        [JsonPropertyName("ListName")]
        public string? ListName { get; set; }
    }

    public record class PackingCategoryCreateRequestDTO
    {
        [JsonPropertyName("CategoryName")]
        public string? CategoryName { get; set; }
    }

    public record class PackingCategoryUpdateRequestDTO
    {
        [JsonPropertyName("CategoryName")]
        public string? CategoryName { get; set; }
    }

    public record class PackingItemCreateRequestDTO
    {
        [JsonPropertyName("ItemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("IsCheck")]
        public bool? IsCheck { get; set; }
    }

    public record class PackingItemUpdateRequestDTO
    {
        [JsonPropertyName("ItemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("IsCheck")]
        public bool? IsCheck { get; set; }
    }
}
