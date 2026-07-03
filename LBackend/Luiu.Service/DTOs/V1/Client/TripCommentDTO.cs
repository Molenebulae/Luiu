using System;
using System.Text.Json.Serialization;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class TripCommentResponseDTO
    {
        [JsonPropertyName("CommentID")]
        public int CommentId { get; set; }

        [JsonPropertyName("TripID")]
        public int TripId { get; set; }

        [JsonPropertyName("Content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("ParentID")]
        public int? ParentId { get; set; }

        [JsonPropertyName("UserID")]
        public int UserId { get; set; }

        [JsonPropertyName("UserName")]
        public string UserName { get; set; } = string.Empty;

        [JsonPropertyName("UserIcon")]
        public string? UserIcon { get; set; }

        [JsonPropertyName("CreateAt")]
        public DateTime CreateAt { get; set; }

        [JsonPropertyName("CanEdit")]
        public bool CanEdit { get; set; }

        [JsonPropertyName("CanDelete")]
        public bool CanDelete { get; set; }
    }

    public record class TripCommentCreateRequestDTO
    {
        [JsonPropertyName("Content")]
        public string? Content { get; set; }

        [JsonPropertyName("ParentID")]
        public int? ParentId { get; set; }
    }

    public record class TripCommentUpdateRequestDTO
    {
        [JsonPropertyName("Content")]
        public string? Content { get; set; }
    }
}
