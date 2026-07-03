using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Luiu.Models;
using Luiu.ViewModels;
using System.Reflection;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace Luiu.Controllers
{
    public class BackendPlanController : Controller
    {
        public IActionResult TripList(CKeywordViewModel vm)
        {
            string keyword = vm.txtKeyword;
            LuiuDbContext db = new LuiuDbContext();


            // 1. 建立Join查詢
            var datas = from t in db.TTrips
                        join m in db.TMembers on t.OwnerId equals m.MemberId
                        where t.IsDeleted == false
                        select new { t, m };

            // 2. 關鍵字過濾 (同時搜行程名或作者名)
            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {
                datas = datas.Where((x => x.t.TripName.Contains(vm.txtKeyword)
                              || x.m.Name.Contains(vm.txtKeyword)));
            }

            // 計算總筆數
            vm.TotalCount = datas.Count();

            // --- 分頁核心 ---
            var result = datas.OrderByDescending(x => x.t.TripId) // 必須先排序才能 Skip
                              .Skip((vm.Page - 1) * vm.PageSize)
                              .Take(vm.PageSize)
                              .ToList()
                              .Select(t => new CTripListViewModel
                              {
                                  TripID = t.t.TripId,
                                  TripName = t.t.TripName,
                                  OwnerName = t.m.Name,
                                  OwnerIcon = t.m.IconUrl,
                                  OwnerId = t.t.OwnerId,
                                  StartDate = t.t.StartDate.ToString("yyyy-MM-dd"),
                                  EndDate = t.t.EndDate.ToString("yyyy-MM-dd"),


                                  // --- 關鍵：將資料從資料庫物件 (t) 傳給 ViewModel ---
                                  PrivacyStatus = t.t.PrivacyStatus,
                                  IsSuggest = (bool)t.t.IsSuggest,
                                  CreateAt = t.t.CreateAt,
                                  UpdateAt = t.t.UpdateAt
                              }).ToList();

            ViewBag.vm = vm; // 把分頁資訊傳給 View 畫按鈕
            return View(result);
        }

        public IActionResult Create()
        {
            var vm = new CTripEditViewModel();

            // 如果網址有帶 ownerId (例如：/Trip/Create?ownerId=101)
            //if (OwnerID)
            //{
            //    PrepareViewModel(vm, ownerId.Value);
            //}
            return View();

        }
        [HttpPost]
        public IActionResult Create(CTripPlanWrap p)
        {
            LuiuDbContext db = new LuiuDbContext();

            {
                //------ShortCode 生成迴圈------
                bool isUnique = false;
                int startIndex = 0;
                string currentGuidStr = "";

                // 開始尋找唯一 ShortCode 的迴圈
                while (!isUnique)
                {
                    // 如果是第一次執行，或目前 GUID 已經移位到末端還沒找到
                    if (string.IsNullOrEmpty(currentGuidStr) || startIndex + 7 > currentGuidStr.Length)
                    {
                        p.TripGuid = Guid.NewGuid(); // 生成新的 GUID
                        currentGuidStr = p.TripGuid.ToString().Replace("-", ""); // 去除橫槓，增加可用字元位數
                        startIndex = 0;
                    }

                    // 擷取從 startIndex 開始的 7 位字元
                    string code = currentGuidStr.Substring(startIndex, 7);

                    // 檢查資料庫是否已存在
                    if (!db.TTrips.Any(t => t.ShortCode == code))
                    {
                        p.ShortCode = code;
                        isUnique = true; // 找到唯一的了，跳出迴圈
                    }
                    else
                    {
                        startIndex++; // 撞名了，往後順延一位
                    }
                }

                //檢查日期
                if (p.EndDate < p.StartDate)
                {
                    // 第一個參數是 Key (對應 View 的 asp-validation-for)，第二個是錯誤訊息
                    ModelState.AddModelError("EndDate", "結束日期不能早於開始日期");
                }

                if (ModelState.IsValid == false)
                {
                    // 如果驗證失敗，返回原本的頁面並帶入目前的資料，這時 View 會顯示錯誤訊息
                    return View(p);
                }

                db.TTrips.Add(p.Trip);
                db.SaveChanges();
            }


            return RedirectToAction("TripList");
        }


        public IActionResult Delete(int? id)
        {
            LuiuDbContext db = new LuiuDbContext();
            TTrip x = db.TTrips.FirstOrDefault(p => p.TripId == id);
            if (x != null)
            {
                x.IsDeleted = true;
                x.UpdateAt = DateTime.Now;
                db.TTrips.Update(x);
                db.SaveChanges();
            }
            return RedirectToAction("TripList");
        }

        public IActionResult Edit(int? id)
        {
            LuiuDbContext db = new LuiuDbContext();
            // 1. 抓出行程
            TTrip x = db.TTrips.FirstOrDefault(p => p.TripId == id);
            if (x == null)
                return RedirectToAction("TripList");
            // 2. 建立 Wrap 並把 Trip 塞進去(也叫封裝實體)
            CTripPlanWrap tpW = new CTripPlanWrap();
            tpW.Trip = x;
            // 3. Member 表撈出名字與頭像
            var member = db.TMembers.FirstOrDefault(m => m.MemberId == x.OwnerId);
            if (member != null)
            {
                tpW.OwnerName = member.Name;
                tpW.OwnerIcon = member.IconUrl;
                // 4. 根據該建立者的 MemberId 撈出他有的打包清單     
                var packLists = db.TUserPackingLists
                                  .Where(p => p.UserId == member.MemberId && !p.IsDeleted)
                                  .ToList();

                // 5. 將清單包裝成 SelectList 傳給前端，指定 Value 為 ListId，Text 為 ListName
                // 順便指定目前行程已選好的 tpW.ListId 作為預設選中項
                ViewBag.PackingListItems = new SelectList(packLists, "ListId", "ListName", tpW.ListId);
            }
            return View(tpW);
        }
        [HttpPost]
        public IActionResult Edit(CTripPlanWrap tpW)
        {
            //確認日期
            if (tpW.Trip.EndDate < tpW.Trip.StartDate)
            {
                ModelState.AddModelError("EndDate", "結束日期不可小於開始日期");
            }

            LuiuDbContext db = new LuiuDbContext();
            // 從 tpW.Trip 裡面抓 ID
            TTrip dbTripplan = db.TTrips.FirstOrDefault(p => p.TripId == tpW.Trip.TripId);


            if (ModelState.IsValid)
            {
                if (dbTripplan != null)
                {
                    // 從 tpW.Trip 取得前端傳回的值
                    dbTripplan.TripName = tpW.Trip.TripName;
                    dbTripplan.StartDate = tpW.Trip.StartDate;
                    dbTripplan.EndDate = tpW.Trip.EndDate;
                    dbTripplan.OwnerId = tpW.Trip.OwnerId;
                    dbTripplan.PrivacyStatus = tpW.Trip.PrivacyStatus;
                    dbTripplan.IsSuggest = tpW.Trip.IsSuggest; // 這裡就能正確接收到值
                    dbTripplan.IsDeleted = tpW.Trip.IsDeleted;
                    dbTripplan.ListId = tpW.ListId; // 確保有存到選中的清單 ID

                    db.SaveChanges();
                }
                return RedirectToAction("TripList");
            }

            // 驗證失敗，需要轉回符合 View 的 Model 型別 (CTripPlanWrap)
            CTripPlanWrap x = new CTripPlanWrap { Trip = tpW.Trip };
            // 重新抓取建立者與清單，確保 View 重新渲染時資料還在
            var member = db.TMembers.FirstOrDefault(m => m.MemberId == tpW.OwnerId);
            if (member != null)
            {
                tpW.OwnerName = member.Name;
                tpW.OwnerIcon = member.IconUrl;
                var packLists = db.TUserPackingLists.Where(p => p.UserId == member.MemberId && !p.IsDeleted).ToList();
                ViewBag.PackingListItems = new SelectList(packLists, "ListId", "ListName", tpW.ListId);
            }

            return View(tpW);
        }

        //行程細項區域
        public IActionResult TripDetailList(CTripDetailListViewModel vm)
        {
            LuiuDbContext db = new LuiuDbContext();
            // 取得該行程的所有景點 (假設 Trip 與 Spot 是 1:N 關係)
            var spots = db.TTripDetails
                          .Where(s => s.TripId == vm.TripID)
                          .ToList();

            // 也可以順便取得行程名稱傳給 View 顯示標題
            ViewBag.TripName = db.TTrips.Find(vm.TripID)?.TripName;
            return View(spots);

        }
    }
}
