using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using System.Drawing;

namespace Luiu.Controllers
{
    public class SpotController : Controller
    {
        private readonly IConfiguration _configuration;

        public SpotController(IConfiguration configuration)
        {
            _configuration = configuration;
        }



        public IActionResult List(int? RegionId, string txtkeyword)
        {
            string keyword = txtkeyword;
            LuiuDbContext db = new LuiuDbContext();

            var regionDict = db.TRegions.ToDictionary(r => r.RegionId, r => r.RegionName);
            var regionList = regionDict.Select(kvp => new SelectListItem
            {
                Value = kvp.Key.ToString(),
                Text = kvp.Value,
                Selected = (RegionId.HasValue && kvp.Key == RegionId.Value)
            }).ToList();
            regionList.Insert(0, new SelectListItem { Value = "0", Text = "--- 請選擇區域 ---" });
            ViewBag.RegionList = regionList;
            ViewBag.SelectedRegionId = RegionId;

            var query = db.TSpots.AsQueryable();
            if (RegionId.HasValue && RegionId.Value > 0)
            {
                query = query.Where(s => s.RegionId == RegionId.Value);
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(s => s.SpotName.Contains(keyword)
                                       || s.Address.Contains(keyword));
            }
            var datas = query.Select(s => new CSpotListViewModel
            {
                RegionName = regionDict.ContainsKey(s.RegionId) ? regionDict[s.RegionId] : "未知地區",
                spot = s
            }).ToList();
            ViewBag.Regions = regionDict;
            return View(datas);
        }



        public IActionResult Create()
        {
            var db = new LuiuDbContext();
            ViewBag.RegionList = new SelectList(db.TRegions, "RegionId", "RegionName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TSpot spot, List<string> googleTypes, List<string> selectedPhotoNames, bool forceUpdate = false)
        {
            using var db = new LuiuDbContext();

            var existingSpot = await db.TSpots.FirstOrDefaultAsync(s => s.GoogleMapId == spot.GoogleMapId);
            if (existingSpot != null && !forceUpdate)
            {
                // 詢問是否更新
                return Json(new { success = false, isDuplicate = true, message = "此景點已存在，是否要更新資訊？" });
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                TSpot targetSpot = existingSpot ?? spot;

                // 更新現有景點資料 或 新增景點
                if (existingSpot != null)
                {
                    existingSpot.SpotName = spot.SpotName;
                    existingSpot.RegionId = spot.RegionId;
                    existingSpot.Longitude = spot.Longitude;
                    existingSpot.Latitude = spot.Latitude;
                    existingSpot.Tel = spot.Tel;
                    existingSpot.Address = spot.Address;
                    existingSpot.OfficialUrl = spot.OfficialUrl;
                    existingSpot.OpeningHoursJson = spot.OpeningHoursJson;
                    existingSpot.Rating = spot.Rating;
                    existingSpot.UserRatingCount = spot.UserRatingCount;
                    existingSpot.GoogleMapUrl = spot.GoogleMapUrl;
                    existingSpot.PriceLevel = spot.PriceLevel;
                    existingSpot.LastUpdated = DateTime.Now;
                }
                else
                {
                    targetSpot.MemberId = null;
                    targetSpot.LastUpdated = DateTime.Now;
                    db.TSpots.Add(targetSpot);
                }

                await db.SaveChangesAsync();

                // 處理地點類別 (Types) 多對多關係
                if (googleTypes != null && googleTypes.Any())
                {
                    if (existingSpot != null)
                    {
                        var oldRelations = db.TSpotTypeRelations.Where(r => r.SpotId == targetSpot.SpotId);
                        db.TSpotTypeRelations.RemoveRange(oldRelations);
                    }

                    foreach (var typeName in googleTypes)
                    {
                        var typeRecord = await db.TSpotTypes.FirstOrDefaultAsync(t => t.TypeName == typeName);
                        if (typeRecord == null)
                        {
                            typeRecord = new TSpotType { TypeName = typeName };
                            db.TSpotTypes.Add(typeRecord);
                            await db.SaveChangesAsync();
                        }

                        db.TSpotTypeRelations.Add(new TSpotTypeRelation
                        {
                            SpotId = targetSpot.SpotId,
                            TypeId = typeRecord.TypeId
                        });
                    }
                    await db.SaveChangesAsync();
                }

                if (selectedPhotoNames != null && selectedPhotoNames.Any())
                {
                    string apiKey = _configuration["GoogleMaps:ApiKey"];
                    string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    using var client = new HttpClient();
                    foreach (var photoResourceName in selectedPhotoNames)
                    {
                        // 1. 從 Google GetPhoto 取得原始圖片
                        string photoUrl = $"https://places.googleapis.com/v1/{photoResourceName}/media?key={apiKey}&maxWidthPx=1000";
                        byte[] imageBytes = await client.GetByteArrayAsync(photoUrl);

                        string fileName = $"{Guid.NewGuid()}.jpg";
                        string filePath = Path.Combine(uploadFolder, fileName);

                        using (MemoryStream ms = new MemoryStream(imageBytes))
                        {
                            using (Image originalImage = Image.FromStream(ms))
                            {
                                db.TSpotPhotos.Add(new TSpotPhoto
                                {
                                    SpotId = targetSpot.SpotId,
                                    PhotoUrl = fileName,
                                    Width = originalImage.Width,
                                    Height = originalImage.Height,
                                    Approved = true,
                                    UploadTime = DateTime.Now
                                });
                            }
                        }
                        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                    }

                    await db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Json(new { success = true, message = existingSpot != null ? "景點資料已更新！" : "景點新增成功！" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "失敗：" + ex.Message });
            }
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return RedirectToAction("List");

            using var db = new LuiuDbContext();

            var spot = await db.TSpots.FirstOrDefaultAsync(m => m.SpotId == id);
            if (spot == null) return RedirectToAction("List");

            var photos = await db.TSpotPhotos.Where(p => p.SpotId == id).ToListAsync();
            ViewBag.Photos = photos;

            var regions = db.TRegions.ToList();
            ViewBag.RegionList = new SelectList(regions, "RegionId", "RegionName", spot.RegionId);

            return View(spot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TSpot spot, List<int> deletePhotoIds)
        {
            using var db = new LuiuDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var existingSpot = await db.TSpots.FirstOrDefaultAsync(s => s.SpotId == spot.SpotId);

                if (existingSpot == null) return Json(new { success = false, message = "找不到該景點" });

                // 更新欄位
                existingSpot.SpotName = spot.SpotName;
                existingSpot.Address = spot.Address;
                existingSpot.Tel = spot.Tel;
                existingSpot.RegionId = spot.RegionId;
                existingSpot.OpeningHoursJson = spot.OpeningHoursJson;
                existingSpot.Latitude = spot.Latitude;
                existingSpot.Longitude = spot.Longitude;
                existingSpot.LastUpdated = DateTime.Now;

                // 處理圖片刪除
                if (deletePhotoIds != null && deletePhotoIds.Any())
                {
                    var photosToRemove = await db.TSpotPhotos
                        .Where(p => deletePhotoIds.Contains(p.PhotoId) && p.SpotId == spot.SpotId)
                        .ToListAsync();

                    db.TSpotPhotos.RemoveRange(photosToRemove);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "資料已成功更新" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "更新失敗：" + ex.Message });
            }
        }


        public IActionResult Delete(int? id)
        {
            LuiuDbContext db = new LuiuDbContext();
            TSpot x = db.TSpots.FirstOrDefault(p => p.SpotId == id);
            if (x != null)
            {
                db.TSpots.Remove(x);
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }




        [HttpGet]
        public IActionResult GetMapsApiKey()
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return NotFound("未設定 GoogleMapsApiKey");
            }
            return Content(apiKey, "text/plain");
        }


        [HttpGet]
        public async Task<IActionResult> GetPlaceDetails(string placeId)
        {
            var apiKey = _configuration["GoogleMaps:ApiKey"];
            using var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://places.googleapis.com/v1/places/{placeId}?languageCode=zh-TW");
            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Headers.Add("X-Goog-FieldMask", "id,displayName,formattedAddress,location,types,regularOpeningHours,rating,userRatingCount,websiteUri,nationalPhoneNumber,googleMapsLinks,priceLevel,photos");

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            return BadRequest("Google API 請求失敗");
        }



    }
}

