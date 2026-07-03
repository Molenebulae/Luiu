using Luiu.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Strategies
{
    public interface IOAuthStrategy
    {
        string AuthType { get; }
        string ProviderDisplayName { get; }
        Task<OAuthUserResult> GetUserProfileAsync(string code);
    }
}
