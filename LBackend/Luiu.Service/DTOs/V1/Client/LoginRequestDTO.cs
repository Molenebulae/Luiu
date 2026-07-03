using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.DTOs.V1.Client
{
    public record class LoginRequestDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
