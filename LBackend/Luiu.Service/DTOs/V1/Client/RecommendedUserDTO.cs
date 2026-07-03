namespace Luiu.Service.DTOs.V1.Client
{
    public class RecommendedUserDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public bool IsFollowing { get; set; }
    }
}
