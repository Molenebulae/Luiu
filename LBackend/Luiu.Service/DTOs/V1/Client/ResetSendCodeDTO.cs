using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class ResetSendCodeDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
