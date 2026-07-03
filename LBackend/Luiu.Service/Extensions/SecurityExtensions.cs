using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Extensions
{
    public static class SecurityExtensions
    {
        public static string ToSHA256(this string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}
