using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationCodeAsync(string toEmail, string verificationToken);
    }
}
