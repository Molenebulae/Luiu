using System.ComponentModel.DataAnnotations;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class RegisterRequestDTO
    {
        [Required(ErrorMessage = "Email 必填")]
        [EmailAddress(ErrorMessage = "Email 格式不正確")]
        public string Email { get; set; }
        [MinLength(6, ErrorMessage = " 密碼最少 6 位")]
        public string Password { get; set; }
    }
}
