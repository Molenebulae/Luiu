using Luiu.Domain.Models;
using Luiu.Service.Extensions;
using Luiu.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Resend;
using System.Text.Json;
using static Luiu.Domain.Enums.AppEnums;

namespace Luiu.Service.Implementations
{
    public class VerificationService : IVerificationService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<VerificationService> _logger;
        protected readonly LuiuDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VerificationService(
            IDistributedCache cache,
            ILogger<VerificationService> logger, 
            LuiuDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SetCodeAsync(string email, string code, int durationSeconds)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(durationSeconds)
            };

            // 在寫入前檢查一下
            _logger.LogInformation("即將寫入快取，Key: RegCode_{Email}, 過期時間: {Time} 秒", email, durationSeconds);

            await _cache.SetStringAsync($"RegCode_{email}", code, options);
        }

        public async Task SetRegistrationDataAsync(string email, object data, int durationSeconds)
        {
            var jsonData = JsonSerializer.Serialize(data);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(durationSeconds)
            };
            await _cache.SetStringAsync($"RegData_{email}", jsonData, options);
            _logger.LogInformation("註冊資料已寫入快取: {Email}", email);
        }

        public async Task<string?> GetCodeAsync(string email)
            => await _cache.GetStringAsync($"RegCode_{email}");

        public async Task<T?> GetRegistrationDataAsync<T>(string email) where T : class
        {
            var jsonData = await _cache.GetStringAsync($"RegData_{email}");
            if (string.IsNullOrEmpty(jsonData)) return null;

            return JsonSerializer.Deserialize<T>(jsonData);
        }

        // 修改 Remove 方法：確保兩個 Key 都被清除
        public async Task RemoveCodeAsync(string email)
        {
            await _cache.RemoveAsync($"RegCode_{email}");
            await _cache.RemoveAsync($"RegData_{email}");
            _logger.LogInformation("已移除快取資料: {Email}", email);
        }

        public async Task LogVerificationAsync(int memberId, VerificationType type, string code, VerificationStatus status)
        {
            string ip = _httpContextAccessor.GetClientIp();

            var record = new TVerification
            {
                MemberId = memberId,
                RequestIp = ip,
                Type = (byte)type,
                TokenHash = code.ToSHA256(),
                Status = (byte)status,
                CreatedAt = DateTime.Now
            };

            _context.TVerifications.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckAndSetCooldownAsync(string email, string type, int seconds)
        {
            string cooldownKey = $"Cooldown_{type}_{email}";

            string? isCoolingDown = await _cache.GetStringAsync(cooldownKey);
            if (isCoolingDown != null) return false;  // 還在冷卻中..

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(seconds)
            };
            await _cache.SetStringAsync(cooldownKey, "locked", options);

            return true;  // 繼續發送
        }
    }
}
