using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Luiu.Controllers
{
    public class DocumentReview : Controller
    {

        private LuiuDbContext db = new LuiuDbContext();

        // 1. 括號裡面加上 int? statusFilter，用來接收下拉選單傳來的數字
        public IActionResult DCReview(int? statusFilter)
        {
            // 2. 如果沒有選，預設就給 0 (待審核)
            int currentStatus = statusFilter ?? 0;

            // 3. 把目前的狀態存進 ViewBag，這樣等一下網頁才知道下拉選單要停在哪一格
            ViewBag.CurrentStatus = currentStatus;

            // 4. 去資料庫撈資料，把原本寫死的 0 換成 currentStatus 這個變數
            var datas = db.TMemories
                .Where(m => m.ReviewStatus == currentStatus && m.IsDelete == false)
                .Select(m => new ReviewViewModel
                {
                    MemoryID = m.MemoryId,
                    Title = m.Title,
                    AuthorName = "會員 " + m.UserId,
                    SubmitDate = m.StartDate.HasValue ? m.StartDate.Value.ToString("yyyy/MM/dd") : "無日期",
                    // 這裡順便幫你把狀態轉成好懂的中文
                    Status = m.ReviewStatus == 0 ? "待審核" : (m.ReviewStatus == 1 ? "已通過" : "已退回")
                })
                .ToList();

            return View(datas);
        }
        [HttpPost] // 加上這個標籤，代表這個動作只接受表單送出的資料，比較安全！
        public IActionResult UpdateStatus(int memoryId, int status)
        {
            // 1. 去資料庫找出那一筆被點擊的旅遊紀錄
            var memory = db.TMemories.FirstOrDefault(m => m.MemoryId == memoryId && m.IsDelete == false);

            // 2. 如果有找到這筆資料
            if (memory != null)
            {
                // 3. 把狀態更新成傳進來的數字 (1 代表通過，2 代表退回)
                memory.ReviewStatus = status;

                // 4. 最重要的一行：告訴 Entity Framework 把變更正式存入 SQL 資料庫！
                db.SaveChanges();
            }

            // 5. 處理完畢後，把畫面重新導向回原本的「審核列表」，資料就會自動刷新了！
            return RedirectToAction("DCReview");
        }
        // 負責顯示單筆旅遊紀錄的詳細內容
        // 負責顯示單筆旅遊紀錄的詳細內容
        public IActionResult MemoryDetails(int id)
        {
            // 1. 先找出這筆行程的基本資料
            var memory = db.TMemories.FirstOrDefault(m => m.MemoryId == id && m.IsDelete == false);
            if (memory == null)
            {
                return NotFound(); // 找不到就回傳 404 錯誤網頁
            }

            // 2. 找出這筆行程的所有「天數 (MemoryDays)」
            var days = db.TMemoryDays.Where(d => d.MemoryId == id).OrderBy(d => d.DayNumber).ToList();

            // 3. 抓出這些天數的 ID，然後去撈出所有的「景點 (MemoryStops)」
            var dayIds = days.Select(d => d.DayId).ToList();
            var stops = db.TMemoryStops.Where(s => dayIds.Contains(s.DayId)).ToList();

            // 4. 把資料裝進剛剛做好的 ViewModel 便當盒
            var vm = new MemoryDetailViewModel
            {
                MemoryID = memory.MemoryId,
                Title = memory.Title,
                AuthorName = "會員 " + memory.UserId,
                CoverImage = memory.CoverImage,
                // 組合日期字串
                DateRange = (memory.StartDate.HasValue ? memory.StartDate.Value.ToString("yyyy/MM/dd") : "未定") +
                            " ~ " +
                            (memory.EndDate.HasValue ? memory.EndDate.Value.ToString("yyyy/MM/dd") : "未定"),


            ReviewStatus = memory.ReviewStatus
            };


            // 5. 用迴圈把每天的景點串起來，變成漂亮的文字
            foreach (var day in days)
            {
                var dayStops = stops.Where(s => s.DayId == day.DayId).Select(s => s.PlaceName).ToList();
                // 如果有景點就用箭頭串起來，沒有就顯示無安排
                string stopString = dayStops.Any() ? string.Join(" ➡️ ", dayStops) : "自由活動 (無安排景點)";
                vm.Itinerary.Add($"第 {day.DayNumber} 天：{stopString}");
            }

            return View(vm);
        }
    }
}
    