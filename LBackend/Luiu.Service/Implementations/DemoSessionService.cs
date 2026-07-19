using Luiu.Domain.Exceptions;
using Luiu.Domain.Models;
using Luiu.Service.DTOs.V1.Client;
using Luiu.Service.Enums;
using Luiu.Service.Extensions;
using Luiu.Service.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Luiu.Service.Implementations
{
    public class DemoSessionService
    {
        public const string IsDemoClaim = "is_demo";
        public const string DemoSessionIdClaim = "demo_session_id";
        public const string DemoExpiresAtClaim = "demo_expires_at";

        private readonly LuiuDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DemoAccountOptions _options;
        private readonly ILogger<DemoSessionService> _logger;

        public DemoSessionService(
            LuiuDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IOptions<DemoAccountOptions> options,
            ILogger<DemoSessionService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _logger = logger;
        }

        public bool IsDemoRequest()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return string.Equals(user?.FindFirst(IsDemoClaim)?.Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        public string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public void EnsureRouteUserMatches(string routeUserId)
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId) || !string.Equals(currentUserId, routeUserId, StringComparison.Ordinal))
            {
                throw new AppForbiddenException("您沒有權限操作此使用者的資料");
            }
        }

        public async Task<TDemoSession?> GetCurrentSessionAsync(bool throwIfInvalid = true)
        {
            if (!IsDemoRequest())
            {
                return null;
            }

            var value = _httpContextAccessor.HttpContext?.User.FindFirst(DemoSessionIdClaim)?.Value;
            if (!Guid.TryParse(value, out var demoSessionId))
            {
                if (throwIfInvalid) throw new AppUnauthorizedException("Demo 憑證無效，請重新登入");
                return null;
            }

            var now = DateTime.UtcNow;
            var session = await _context.TDemoSessions.FirstOrDefaultAsync(s => s.DemoSessionId == demoSessionId);
            if (session == null || session.EndedAt.HasValue || session.ExpiresAt <= now)
            {
                if (throwIfInvalid) throw new AppUnauthorizedException("Demo 使用期限已結束，請重新登入");
                return null;
            }

            return session;
        }

        public async Task<TDemoSession> CreateSessionAsync(int memberId)
        {
            if (!_options.Enabled)
            {
                throw new AppForbiddenException("Demo 功能目前未開放");
            }

            var now = DateTime.UtcNow;
            var clientIpHash = HashValue(_httpContextAccessor.GetClientIp());
            var userAgentHash = HashValue(_httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown");

            var activeQuery = _context.TDemoSessions
                .Where(s => !s.EndedAt.HasValue && s.ExpiresAt > now);

            var activeCount = await activeQuery.CountAsync();
            if (activeCount >= _options.MaxActiveSessions)
            {
                throw new AppTooManyRequestsException("目前 Demo 使用人數較多，請稍後再試");
            }

            var activeIpCount = await activeQuery.CountAsync(s => s.ClientIpHash == clientIpHash);
            if (activeIpCount >= _options.MaxActiveSessionsPerIp)
            {
                throw new AppTooManyRequestsException("此網路環境已有 Demo 使用中，請稍後再試");
            }

            var session = new TDemoSession
            {
                DemoSessionId = Guid.NewGuid(),
                MemberId = memberId,
                ClientIpHash = clientIpHash,
                UserAgentHash = userAgentHash,
                StartedAt = now,
                ExpiresAt = now.AddMinutes(_options.SessionDurationMinutes)
            };

            _context.TDemoSessions.Add(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Demo session created. DemoSessionId={DemoSessionId}, MemberId={MemberId}, ExpiresAt={ExpiresAt}",
                session.DemoSessionId,
                memberId,
                session.ExpiresAt);

            return session;
        }

        public async Task<DemoQuotaDTO?> GetCurrentQuotaAsync()
        {
            var session = await GetCurrentSessionAsync(throwIfInvalid: false);
            return session == null ? null : MapQuota(session);
        }

        public async Task IncrementQuotaAsync(DemoQuotaType quotaType, int amount = 1)
        {
            var session = await GetCurrentSessionAsync(throwIfInvalid: true);
            if (session == null)
            {
                return;
            }

            switch (quotaType)
            {
                case DemoQuotaType.PlaceSearch:
                    EnsureLimit(session.PlaceSearchCount, amount, _options.PlaceSearchLimit, "Demo 景點搜尋次數已用完");
                    session.PlaceSearchCount += amount;
                    break;
                case DemoQuotaType.RouteCompute:
                    EnsureLimit(session.RouteComputeCount, amount, _options.RouteComputeLimit, "Demo 路線規劃次數已用完");
                    session.RouteComputeCount += amount;
                    break;
                case DemoQuotaType.RouteExternalLeg:
                    EnsureLimit(session.RouteExternalLegCount, amount, _options.RouteExternalLegLimit, "Demo 路線外部查詢次數已用完");
                    session.RouteExternalLegCount += amount;
                    break;
                case DemoQuotaType.CreatedTrip:
                    EnsureLimit(session.CreatedTripCount, amount, _options.CreatedTripLimit, "Demo 可建立行程數已用完");
                    session.CreatedTripCount += amount;
                    break;
                case DemoQuotaType.CreatedCollect:
                    EnsureLimit(session.CreatedCollectCount, amount, _options.CreatedCollectLimit, "Demo 可收藏次數已用完");
                    session.CreatedCollectCount += amount;
                    break;
                default:
                    throw new AppBadRequestException("未知的 Demo 配額類型");
            }

            await _context.SaveChangesAsync();
        }

        public DemoQuotaDTO MapQuota(TDemoSession session)
        {
            return new DemoQuotaDTO
            {
                PlaceSearchLimit = _options.PlaceSearchLimit,
                PlaceSearchUsed = session.PlaceSearchCount,
                RouteComputeLimit = _options.RouteComputeLimit,
                RouteComputeUsed = session.RouteComputeCount,
                RouteExternalLegLimit = _options.RouteExternalLegLimit,
                RouteExternalLegUsed = session.RouteExternalLegCount,
                CreatedTripLimit = _options.CreatedTripLimit,
                CreatedTripUsed = session.CreatedTripCount,
                CreatedCollectLimit = _options.CreatedCollectLimit,
                CreatedCollectUsed = session.CreatedCollectCount
            };
        }

        private static void EnsureLimit(int current, int amount, int limit, string message)
        {
            if (current + amount > limit)
            {
                throw new AppTooManyRequestsException(message);
            }
        }

        private static string HashValue(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}
