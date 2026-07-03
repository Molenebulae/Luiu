using System;
using System.Collections.Generic;
using System.Text;
using static Luiu.Domain.Enums.AppEnums;

namespace Luiu.Service.Interfaces
{
    public interface IVerificationService
    {
        Task SetCodeAsync(string email, string code, int durationSeconds);
        Task SetRegistrationDataAsync(string email, object data, int durationSeconds);
        Task<string?> GetCodeAsync(string email);
        Task<T?> GetRegistrationDataAsync<T>(string email) where T : class;
        Task RemoveCodeAsync(string email);
        Task LogVerificationAsync(int memberId, VerificationType type, string code, VerificationStatus status);
        Task<bool> CheckAndSetCooldownAsync(string email, string type, int seconds);
    }
}
