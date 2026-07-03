using Luiu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Luiu.Controllers
{
    public class RegionController : Controller
    {
        public async Task<IActionResult> List()
        {
            LuiuDbContext db = new LuiuDbContext();
            var regions = await db.TRegions.OrderBy(r => r.RegionId).ToListAsync();
            return View(regions);
        }

        [HttpPost]
        public async Task<JsonResult> Create(string regionName)
        {
            if (string.IsNullOrWhiteSpace(regionName))
                return Json(new { success = false, message = "名稱不能為空" });
            regionName = regionName.Trim();
            using var db = new LuiuDbContext();

            if (await db.TRegions.AnyAsync(r => r.RegionName == regionName))
                return Json(new { success = false, message = $"地區「{regionName}」已經存在" });

            db.TRegions.Add(new TRegion { RegionName = regionName });
            await db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<JsonResult> Edit(int id, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return Json(new { success = false, message = "名稱不能為空" });
            newName = newName.Trim();
            using var db = new LuiuDbContext();

            if (await db.TRegions.AnyAsync(r => r.RegionName == newName && r.RegionId != id))
                return Json(new { success = false, message = $"地區「{newName}」已經存在，請勿重複新增" });

            var region = await db.TRegions.FindAsync(id);
            if (region == null) return Json(new { success = false, message = "找不到該地區" });

            region.RegionName = newName.Trim();
            await db.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            using var db = new LuiuDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var region = await db.TRegions.FindAsync(id);
                if (region == null) return Json(new { success = false });

                var spotIds = await db.TSpots.Where(s => s.RegionId == id).Select(s => s.SpotId).ToListAsync();
                var photos = await db.TSpotPhotos.Where(p => spotIds.Contains(p.SpotId)).ToListAsync();
                db.TSpotPhotos.RemoveRange(photos);

                var spots = await db.TSpots.Where(s => s.RegionId == id).ToListAsync();
                db.TSpots.RemoveRange(spots);

                db.TRegions.Remove(region);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "刪除失敗" });
            }
        }
    }
}
