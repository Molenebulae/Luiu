using System;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace Luiu.Service.DTOs
{
    public class OAuthUserResult
    {
        public string ProviderKey { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}
