using Microsoft.EntityFrameworkCore;
using Luiu.Models;

namespace Luiu.Services
{
    public class SpotMonthlySnapService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SpotMonthlySnapService> _logger;

        public SpotMonthlySnapService(IServiceProvider services, ILogger<SpotMonthlySnapService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("景點快照背景服務已啟動。");
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now.Day == 1 && now.Hour == 0)
                {
                    try
                    {
                        using (var scope = _services.CreateScope())
                        {
                            var db = scope.ServiceProvider.GetRequiredService<LuiuDbContext>();
                            await CreateSnapshot(db);
                            _logger.LogInformation($"{now:yyyy-MM} 快照紀錄已成功建立。");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "執行每月快照時發生錯誤。");
                    }
                }
                // 每小時檢查一次
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CreateSnapshot(LuiuDbContext db)
        {
            var lastMonth = DateTime.Now.AddMonths(-1);

            var stats = await db.TSpots.Select(s => new TSpotMonthlySnap
            {
                SpotId = s.SpotId,
                RegionId = s.RegionId,
                SnapYear = lastMonth.Year,
                SnapMonth = lastMonth.Month,
                ViewCount = s.ViewCount,
                FavoriteCount = s.FavoriteCount,
                PlanCount = s.PlanCount,
                RecordCount = s.RecordCount,
                RefCount = s.RefCount,
                SnapTime = DateTime.Now
            }).ToListAsync();

            db.TSpotMonthlySnaps.AddRange(stats);
            await db.SaveChangesAsync();
        }
    }
}
