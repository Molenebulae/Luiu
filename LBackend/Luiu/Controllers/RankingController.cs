using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Luiu.Controllers
{
    public class RankingController : Controller
    {
        public async Task<IActionResult> Dashboard()
        {
            using var db = new LuiuDbContext();
            var config = await db.TSystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "SpotRankWeight");
            CRankingWeight weights = null;
            if (config == null || string.IsNullOrEmpty(config.ConfigValue))
            {
                weights = new CRankingWeight();
            }
            else
            {
                try
                {
                    weights = JsonSerializer.Deserialize<CRankingWeight>(config.ConfigValue)
                              ?? new CRankingWeight();
                }
                catch
                {
                    weights = new CRankingWeight();
                }
            }

            var vm = new CRankingDashboardViewModel
            {
                Weights = weights,
                ConfigLastUpdated = config?.LastUpdated,
                LastSnapTime = db.TSpotMonthlySnaps.Max(s => (DateTime?)s.SnapTime),
                LastSpotRankDate = db.TSpotRanks.Max(r => (DateTime?)r.RankDate),
                LastEventRankDate = db.TEventRanks.Max(e => (DateTime?)e.RankDate)
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateWeights(CRankingWeight newWeights)
        {
            using var db = new LuiuDbContext();
            var config = await db.TSystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "SpotRankWeight");
            if (!ModelState.IsValid)
            {
                var vm = new CRankingDashboardViewModel
                {
                    Weights = newWeights,
                    ConfigLastUpdated = config?.LastUpdated,
                    LastSnapTime = db.TSpotMonthlySnaps.Max(s => (DateTime?)s.SnapTime),
                    LastSpotRankDate = db.TSpotRanks.Max(r => (DateTime?)r.RankDate),
                    LastEventRankDate = db.TEventRanks.Max(e => (DateTime?)e.RankDate)
                };
                return View("Index", vm);
            }

            var json = JsonSerializer.Serialize(newWeights);

            if (config == null)
            {
                db.TSystemConfigs.Add(new TSystemConfig
                {
                    ConfigKey = "SpotRankWeight",
                    ConfigValue = json,
                    LastUpdated = DateTime.Now
                });
            }
            else
            {
                config.ConfigValue = json;
                config.LastUpdated = DateTime.Now;
            }

            await db.SaveChangesAsync();
            return RedirectToAction("Dashboard");
        }

        public async Task<bool> CalculateRanks(int year, int month)
        {
            using var db = new LuiuDbContext();
            var config = await db.TSystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "SpotRankWeight");
            CRankingWeight w = null;
            if (config == null || string.IsNullOrEmpty(config.ConfigValue))
            {
                w = new CRankingWeight();
            }
            else
            {
                try
                {
                    w = JsonSerializer.Deserialize<CRankingWeight>(config.ConfigValue)
                              ?? new CRankingWeight();
                }
                catch
                {
                    w = new CRankingWeight();
                }
            }

            string currentRankType = $"{year}-{month:D2}";
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var currentSnaps = db.TSpotMonthlySnaps.Where(s => s.SnapYear == year && s.SnapMonth == month).ToList();
                var lastSnaps = db.TSpotMonthlySnaps.Where(
                    s => s.SnapYear == (month == 1 ? year - 1 : year)
                      && s.SnapMonth == (month == 1 ? 12 : month - 1)
                    ).ToList();

                var spotScores = currentSnaps.Select(curr => {
                    var prev = lastSnaps.FirstOrDefault(l => l.SpotId == curr.SpotId);
                    double score = ((curr.ViewCount - (prev?.ViewCount ?? 0)) * w.ViewCount) +
                                   ((curr.FavoriteCount - (prev?.FavoriteCount ?? 0)) * w.FavoriteCount) +
                                   ((curr.PlanCount - (prev?.PlanCount ?? 0)) * w.PlanCount) +
                                   ((curr.RecordCount - (prev?.RecordCount ?? 0)) * w.RecordCount) +
                                   ((curr.RefCount - (prev?.RefCount ?? 0)) * w.RefCount);
                    return new { curr.SpotId, curr.RegionId, Score = score };
                }).ToList();


                var oldSpotRanks = db.TSpotRanks.Where(r => r.RankType == currentRankType);
                db.TSpotRanks.RemoveRange(oldSpotRanks);

                var topSpots = spotScores.OrderByDescending(x => x.Score).Take(10).ToList();
                for (int i = 0; i < topSpots.Count; i++)
                {
                    var name = await db.TSpots.Where(s => s.SpotId == topSpots[i].SpotId).Select(s => s.SpotName).FirstOrDefaultAsync();
                    db.TSpotRanks.Add(new TSpotRank
                    {
                        SpotId = topSpots[i].SpotId,
                        SpotName = name ?? "未知景點",
                        RankType = currentRankType,
                        RankDate = DateTime.Now,
                        Rank = i + 1
                    });
                }


                var regionScores = spotScores.GroupBy(s => s.RegionId)
                                             .Select(g => new {
                                                 RegionId = g.Key,
                                                 AverageScore = g.Average(x => x.Score)
                                             })
                                             .OrderByDescending(x => x.AverageScore)
                                             .Take(10).ToList();

                var oldRegionRanks = db.TRegionRanks.Where(r => r.RankType == currentRankType);
                db.TRegionRanks.RemoveRange(oldRegionRanks);

                for (int i = 0; i < regionScores.Count; i++)
                {
                    var regionName = await db.TRegions
                                   .Where(reg => reg.RegionId == regionScores[i].RegionId)
                                   .Select(reg => reg.RegionName)
                                   .FirstOrDefaultAsync();
                    db.TRegionRanks.Add(new TRegionRank
                    {
                        RegionId = regionScores[i].RegionId,
                        RegionName = regionName,
                        RankType = currentRankType,
                        RankDate = DateTime.Now,
                        Rank = i + 1
                    });
                }

                var oneWeekAgo = DateTime.Now.AddDays(-7);
                var activeEvents = await db.TEvents.Where(e => e.EndTime >= oneWeekAgo).ToListAsync();
                var eventScores = activeEvents.Select(e => new {
                    e.EventId,
                    e.EventName,
                    Score = (e.ViewCount * w.ViewCount) +
                    (e.FavoriteCount * w.FavoriteCount) +
                    (e.PlanCount * w.PlanCount) +
                    (e.RecordCount * w.RecordCount) +
                    (e.RefCount * w.RefCount)
                }).OrderByDescending(x => x.Score).Take(5).ToList();

                var today = DateTime.Now.Date;
                var oldEventRanks = db.TEventRanks.Where(er => er.RankDate >= today);
                db.TEventRanks.RemoveRange(oldEventRanks);

                for (int i = 0; i < eventScores.Count; i++)
                {
                    db.TEventRanks.Add(new TEventRank
                    {
                        EventId = eventScores[i].EventId,
                        EventName = eventScores[i].EventName,
                        RankDate = today,
                        Rank = i + 1
                    });
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Recalculate()
        {
            try
            {
                var now = DateTime.Now;
                int year = now.Year;
                int month = now.Month;

                bool isSuccess = await CalculateRanks(year, month);

                if (isSuccess)
                {
                    return Json(new { success = true, message = $"{year}年{month}月 景點排名已重新計算完成！" });
                }
                else
                {
                    return Json(new { success = false, message = "計算過程中發生錯誤。" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "伺服器異常：" + ex.Message });
            }
        }


        public async Task<IActionResult> History(string rankType)
        {
            var db = new LuiuDbContext();
            var availableTypes = await db.TSpotRanks
                .Select(r => r.RankType)
                .Distinct()
                .OrderByDescending(t => t)
                .ToListAsync();

            if (string.IsNullOrEmpty(rankType))
            {
                rankType = availableTypes.FirstOrDefault() ?? DateTime.Now.ToString("yyyy-MM");
            }

            var spotRanks = await db.TSpotRanks
                .Where(r => r.RankType == rankType)
                .OrderBy(r => r.Rank)
                .ToListAsync();

            var regionRanks = await db.TRegionRanks
                .Where(r => r.RankType == rankType)
                .OrderBy(r => r.Rank)
                .ToListAsync();

            var latestEventDate = await db.TEventRanks.MaxAsync(e => (DateTime?)e.RankDate);
            var eventRanks = new List<TEventRank>();
            if (latestEventDate.HasValue)
            {
                eventRanks = await db.TEventRanks
                    .Where(e => e.RankDate == latestEventDate.Value)
                    .OrderBy(e => e.Rank)
                    .ToListAsync();
            }

            var vm = new CRankingHistoryViewModel
            {
                SelectedRankType = rankType,
                AvailableRankTypes = availableTypes,
                SpotRanks = spotRanks,
                RegionRanks = regionRanks,
                EventRanks = eventRanks
            };

            return View(vm);
        }
    }
}
