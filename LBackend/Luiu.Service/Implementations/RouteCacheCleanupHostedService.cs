using Luiu.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Luiu.Service.Implementations
{
    public class RouteCacheCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RouteCacheCleanupHostedService> _logger;

        public RouteCacheCleanupHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<RouteCacheCleanupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = GetDelayUntilNextCleanup();
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested)
                    break;

                await CleanupExpiredRouteCachesAsync(stoppingToken);
            }
        }

        private static TimeSpan GetDelayUntilNextCleanup()
        {
            var now = DateTimeOffset.Now;
            var nextRun = new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                3,
                0,
                0,
                now.Offset);

            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            return nextRun - now;
        }

        private async Task CleanupExpiredRouteCachesAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<LuiuDbContext>();
                var now = DateTime.UtcNow;

                var expiredCaches = await db.TRouteCaches
                    .Where(cache => cache.ExpiredAt <= now)
                    .ToListAsync(stoppingToken);

                if (expiredCaches.Count == 0)
                {
                    _logger.LogInformation("Route cache cleanup finished. No expired cache found.");
                    return;
                }

                db.TRouteCaches.RemoveRange(expiredCaches);
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation(
                    "Route cache cleanup finished. Removed {Count} expired caches.",
                    expiredCaches.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Route cache cleanup failed.");
            }
        }
    }
}
