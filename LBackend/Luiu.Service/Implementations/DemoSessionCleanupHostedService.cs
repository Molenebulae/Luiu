using Luiu.Domain.Models;
using Luiu.Service.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Luiu.Service.Implementations
{
    public class DemoSessionCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DemoAccountOptions _options;
        private readonly ILogger<DemoSessionCleanupHostedService> _logger;

        public DemoSessionCleanupHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<DemoAccountOptions> options,
            ILogger<DemoSessionCleanupHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalMinutes = Math.Max(1, _options.CleanupIntervalMinutes);
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
        }

        private async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LuiuDbContext>();
            var now = DateTime.UtcNow;

            var sessions = await context.TDemoSessions
                .Where(s => s.ExpiresAt < now && !s.EndedAt.HasValue)
                .OrderBy(s => s.ExpiresAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                var tripIds = await context.TTrips
                    .Where(t => t.DemoSessionId == session.DemoSessionId)
                    .Select(t => t.TripId)
                    .ToListAsync(cancellationToken);

                if (tripIds.Count > 0)
                {
                    var comments = context.TTripComments.Where(c => tripIds.Contains(c.TripId));
                    var details = context.TTripDetails.Where(d => tripIds.Contains(d.TripId));
                    context.TTripComments.RemoveRange(comments);
                    context.TTripDetails.RemoveRange(details);
                }

                var collects = context.TCollects.Where(c => c.DemoSessionId == session.DemoSessionId);
                var trips = context.TTrips.Where(t => t.DemoSessionId == session.DemoSessionId);
                context.TCollects.RemoveRange(collects);
                context.TTrips.RemoveRange(trips);

                session.EndedAt = now;
                session.EndReason = "ExpiredCleanup";
            }

            if (sessions.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Expired demo sessions cleaned. Count={Count}", sessions.Count);
            }
        }
    }
}
