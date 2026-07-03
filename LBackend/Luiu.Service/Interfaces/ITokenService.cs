using Luiu.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(string userId, int roleType);
    }
}
