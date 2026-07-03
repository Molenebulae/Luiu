using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Luiu.Service.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetClientIp(this IHttpContextAccessor accessor)
        {
            var context = accessor.HttpContext;
            if (context == null) return "0.0.0.0";

            var ip = context.Request.Headers["X-Forwarded-For"].ToString();
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            else
            {
                ip = ip.Split('.')[0].Trim();
            }

            return ip;
        }
    }
}
