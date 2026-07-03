using Microsoft.AspNetCore.Mvc;
using Luiu.Models;
using Luiu.ViewModels;

namespace Luiu.Controllers
{
    public class BackendPlanCommentController : Controller
    {
        LuiuDbContext db = new LuiuDbContext();
        //行程留言區(嘗試2)
        // 顯示某行程的留言列表 (對應網址: /TripComment/Index/2)
        public IActionResult CommentList(int id)
        {
            // 1. 先用傳入的 id 去 Trips 表查出該行程的完整資料
            var trip = db.TTrips.FirstOrDefault(t => t.TripId == id);
            // 2. 如果找不到行程，直接返回列表或錯誤，避免後面程式崩潰
            if (trip == null) return RedirectToAction("TripList", "BackendPlan");
            // 3. 將抓到的行程名稱存入 ViewBag，供 View 的 <h1> 標題使用
            ViewBag.TripName = trip.TripName;
            ViewBag.TripId = id; // 傳給 View 方便做「返回行程」按鈕

            var comments = (from c in db.TTripComments
                            join m in db.TMembers on c.UserId equals m.MemberId
                            where c.TripId == id
                            select new CTripCommentViewModel
                            {
                    TripID = c.TripId,
                    TripName = trip.TripName, // 這裡直接用上面抓到的 trip 變數
                    CommentID = c.CommentId,
                    Content = c.Content,
                    UserID = c.UserId,
                    UserIcon = m.IconUrl,
                    UserName = m.Name,
                    CreateAt = c.CreateAt,
                    ParentID = c.ParentId,
                    IsDeleted = c.IsDeleted
                            }).ToList();

            return View(comments);
        }


        // 刪除功能
        public IActionResult Delete(int id)
        {
            var comment = db.TTripComments.Find(id);
            if (comment != null)
            {
                int tid = comment.TripId;
                comment.IsDeleted = true;
                db.SaveChanges();
                return RedirectToAction("CommentList", new { id = tid }); // 刪完回到該行程留言區
            }
            return RedirectToAction("TripList", "BackendPlan");
        }

        // 復原功能
        public IActionResult Restore(int id)
        {
            var comment = db.TTripComments.Find(id);
            if (comment != null)
            {
                comment.IsDeleted = false; // 恢復正常狀態
                db.SaveChanges();
                return RedirectToAction("CommentList", new { id = comment.TripId });
            }
            return RedirectToAction("TripList", "BackendPlan");
        }
    }
}
