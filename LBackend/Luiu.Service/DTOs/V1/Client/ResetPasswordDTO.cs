using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class ResetPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Code { get; set; }
        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
