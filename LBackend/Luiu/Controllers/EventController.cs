using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luiu.Controllers
{
    public class EventController : Controller
    {
        public async Task<IActionResult> List(string txtkeyword)
        {
            var db = new LuiuDbContext();

            ViewBag.Keyword = txtkeyword;

            var events = db.TEvents.AsQueryable();
            if (!string.IsNullOrEmpty(txtkeyword))
            {
                events = events.Where(e => e.EventName.Contains(txtkeyword) ||
                                          (e.EventType != null && e.EventType.Contains(txtkeyword)));
            }
            var result = await events.OrderByDescending(e => e.CreateTime).ToListAsync();

            return View(result);
        }

        public IActionResult Create()
        {
            using var db = new LuiuDbContext();
            var regionMap = db.TRegions.ToDictionary(r => r.RegionId, r => r.RegionName);
            var spotList = db.TSpots.Select(s => new CSpotListViewModel
            {
                spot = s,
                RegionName = regionMap.ContainsKey(s.RegionId) ? regionMap[s.RegionId] : "未知地區"
            }).OrderBy(s => s.spot.RegionId).ToList();
            ViewBag.AllSpots = spotList;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TEvent ev, IFormFile imageFile, List<int> selectedSpotIds)
        {
            using var db = new LuiuDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string extension = Path.GetExtension(imageFile.FileName);
                    string fileName = Guid.NewGuid().ToString() + extension;
                    string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", fileName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    ev.ImageBg = fileName;
                }

                ev.CreateTime = DateTime.Now;
                db.TEvents.Add(ev);
                await db.SaveChangesAsync();

                if (selectedSpotIds != null && selectedSpotIds.Count > 0)
                {
                    var spotsInfo = await db.TSpots
                        .Where(s => selectedSpotIds.Contains(s.SpotId))
                        .Select(s => new { s.SpotId, s.RegionId })
                        .ToListAsync();

                    foreach (var info in spotsInfo)
                    {
                        db.TEventSpotRelations.Add(new TEventSpotRelation
                        {
                            EventId = ev.EventId,
                            SpotId = info.SpotId,
                            RegionId = info.RegionId
                        });
                    }
                    await db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Json(new { success = true, message = "活動新增成功！" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "失敗：" + ex.Message });
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
                return RedirectToAction("List");
            
            using var db = new LuiuDbContext();
            var ev = await db.TEvents.FindAsync(id);
            if (ev == null) return RedirectToAction("List");

            var regionMap = await db.TRegions.ToDictionaryAsync(r => r.RegionId, r => r.RegionName);

            var spots = await db.TSpots.ToListAsync();
            var spotViewModels = spots.Select(s => new CSpotListViewModel
            {
                spot = s,
                RegionName = regionMap.GetValueOrDefault(s.RegionId, "未知地區")
            }).OrderBy(s => s.spot.RegionId).ToList();

            var currentRelatedSpotIds = await db.TEventSpotRelations
                .Where(r => r.EventId == id)
                .Select(r => r.SpotId)
                .ToListAsync();

            ViewBag.AllSpots = spotViewModels;
            ViewBag.CurrentSpotIds = currentRelatedSpotIds;

            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TEvent ev, IFormFile? imageFile, List<int> selectedSpotIds)
        {
            using var db = new LuiuDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var existingEvent = await db.TEvents.FindAsync(ev.EventId);
                if (existingEvent == null) return Json(new { success = false, message = "找不到該活動" });

                existingEvent.EventName = ev.EventName;
                existingEvent.EventType = ev.EventType;
                existingEvent.Description = ev.Description;
                existingEvent.StartTime = ev.StartTime;
                existingEvent.EndTime = ev.EndTime;
                existingEvent.OfficialUrl = ev.OfficialUrl;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images/Events", fileName);
                    using (var stream = new FileStream(savePath, FileMode.Create)) { await imageFile.CopyToAsync(stream); }
                    existingEvent.ImageBg = fileName;
                }

                var oldRelations = await db.TEventSpotRelations.Where(r => r.EventId == ev.EventId).ToListAsync();

                var toRemove = oldRelations.Where(r => !selectedSpotIds.Contains(r.SpotId)).ToList();
                db.TEventSpotRelations.RemoveRange(toRemove);

                var existingIds = oldRelations.Select(r => r.SpotId).ToList();
                var toAddIds = selectedSpotIds.Except(existingIds).ToList();

                if (toAddIds.Count > 0)
                {
                    var newSpotsInfo = await db.TSpots.Where(s => toAddIds.Contains(s.SpotId))
                                                     .Select(s => new { s.SpotId, s.RegionId })
                                                     .ToListAsync();
                    foreach (var info in newSpotsInfo)
                    {
                        db.TEventSpotRelations.Add(new TEventSpotRelation
                        {
                            EventId = ev.EventId,
                            SpotId = info.SpotId,
                            RegionId = info.RegionId
                        });
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true, message = "活動資料更新成功" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "更新失敗：" + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new LuiuDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var ev = await db.TEvents.FindAsync(id);
                if (ev == null) return RedirectToAction("List");

                var relations = await db.TEventSpotRelations.Where(r => r.EventId == id).ToListAsync();
                db.TEventSpotRelations.RemoveRange(relations);

                db.TEvents.Remove(ev);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
            }
            return RedirectToAction("List");
        }
    }
}
