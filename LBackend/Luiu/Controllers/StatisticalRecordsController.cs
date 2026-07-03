using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Luiu.Controllers
{
    public class StatisticalRecordsController : Controller
    {
        private LuiuDbContext db = new LuiuDbContext();

        // 1. 括號多加一個 DateTime? searchDate 來接收網頁傳來的日期
        public IActionResult stat(string txtKeyword, string sortOrder, DateTime? searchDate)
        {
            var query = db.TMemories.Where(x => x.IsDelete == false).AsQueryable();

            // 2. 關鍵字過濾
            if (!string.IsNullOrEmpty(txtKeyword))
            {
                string searchWord = txtKeyword.Trim();
                query = query.Where(m => m.Title.Contains(searchWord));
                ViewBag.Keyword = searchWord;
            }

            // 3.  新增：日期過濾 
            if (searchDate.HasValue)
            {
                // 先把網頁傳過來的 DateTime (包含時間) 轉換成單純的 DateOnly
                DateOnly targetDate = DateOnly.FromDateTime(searchDate.Value);

                // 這裡直接比對就好！不用再寫 .Value.Date 了
                query = query.Where(m => m.StartDate == targetDate);

                // 維持原樣，傳給畫面顯示
                ViewBag.SearchDate = searchDate.Value.ToString("yyyy-MM-dd");
            }

            // 4. 排序方式 (維持不變)
            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "views":
                    query = query.OrderByDescending(m => m.ViewCount);
                    break;
                case "likes":
                    query = query.OrderByDescending(m => m.LikeCount);
                    break;
                case "favorites":
                    query = query.OrderByDescending(m => m.FavoriteCount);
                    break;
                default:
                    query = query.OrderByDescending(m => m.MemoryId);
                    break;
            }

            // 5. 資料轉換 (維持不變)
            var datas = query.Select(m => new StatViewModel
            {
                MemoryID = m.MemoryId,
                Title = m.Title,
                AuthorName = "會員 " + m.UserId,
                SubmitDate = m.StartDate.HasValue ? m.StartDate.Value.ToString("yyyy/MM/dd") : "無日期",
                ViewCount = m.ViewCount,
                LikeCount = m.LikeCount,
                FavoriteCount = m.FavoriteCount
            }).ToList();

            return View(datas);
        }
    }
    }
    
    


