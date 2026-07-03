using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Luiu.Controllers
{
    public class ForumController : Controller
    {
        public IActionResult Forumview(FKeywordViewModel vm, bool? isDelete)
        {
            LuiuDbContext db = new LuiuDbContext();

            bool showDeleted = isDelete ?? false;

            // ⭐ 下拉選單資料
            ViewBag.Categories = db.TCategories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToList();

            var datas = db.TPosts
                          .Include(p => p.Category) 
                          .Include(p => p.Member)
                          .Where(p => p.IsDelete == showDeleted);

            if (!string.IsNullOrEmpty(vm.txtKeyword))
            {
                datas = datas.Where(p =>
                    p.PostTitle.Contains(vm.txtKeyword) ||
                    p.PostContent.Contains(vm.txtKeyword));
            }
            // ⭐ 分類篩選（重點）
            if (vm.CategoryId.HasValue)
            {
                datas = datas.Where(p => p.CategoryId == vm.CategoryId.Value);
            }

            ViewBag.IsDelete = showDeleted;

            return View(datas.ToList());
        }

        public IActionResult Detail(int? id)
        {
            LuiuDbContext db = new LuiuDbContext();
            var x = db.TPosts
       .Include(p => p.Category)   // ⭐ 分類
       .Include(p => p.Member)     // ⭐ 會員
       .Include(p => p.Comments)   // ⭐ 留言
       .ThenInclude(c => c.Member) // ⭐ 留言者
       .FirstOrDefault(p => p.PostId == id);

            if (x == null)
                return RedirectToAction("Forumview");

            // ⭐ 下拉選單資料（給編輯用）
            ViewBag.Categories = db.TCategories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToList();

            return View(x);
        }
        [HttpPost]
        public IActionResult Detail (TPost uiPost)
        {
            LuiuDbContext db = new LuiuDbContext();
            TPost dbPost = db.TPosts.FirstOrDefault(p => p.PostId == uiPost.PostId);
            if (dbPost != null)
            {
                dbPost.PostTitle = uiPost.PostTitle;
                dbPost.PostContent = uiPost.PostContent;
                dbPost.CategoryId = uiPost.CategoryId;
                db.SaveChanges();
            }
            return RedirectToAction("Detail", new { id = uiPost.PostId });
            
        }
        [HttpPost]
        public IActionResult Delete(int id)
        {
            LuiuDbContext db = new LuiuDbContext();

            var post = db.TPosts.FirstOrDefault(p => p.PostId == id);
            if (post != null)
            {
                // 🔥 切換 true / false
                post.IsDelete = !post.IsDelete;
                db.SaveChanges();
            }

            return RedirectToAction("Detail", new { id = id });
        }
        public IActionResult Create()
        {
            LuiuDbContext db = new LuiuDbContext();

            ViewBag.Categories = db.TCategories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToList();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TPost post)
        {
            LuiuDbContext db = new LuiuDbContext();

            post.MemberId = 1; // ⭐ 先固定（測試用）
            post.PostTime = DateTime.Now;
            post.ViewCount = 0;
            post.IsDelete = false;

            db.TPosts.Add(post);
            db.SaveChanges();

            return RedirectToAction("Forumview");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(int PostId, string CommentContent)
        {
            LuiuDbContext db = new LuiuDbContext();

            TComment c = new TComment
            {
                PostId = PostId,
                CommentContent = CommentContent,
                CommentTime = DateTime.Now,
                MemberId = 1 // ⭐ 先固定
            };

            db.TComments.Add(c);
            db.SaveChanges();

            return RedirectToAction("Detail", new { id = PostId });
        }
        [HttpPost]
        public IActionResult ToggleCommentDelete(int id, int postId)
        {
            LuiuDbContext db = new LuiuDbContext();

            var comment = db.TComments.FirstOrDefault(c => c.CommentId == id);

            if (comment != null)
            {
                comment.IsDelete = !comment.IsDelete; // ⭐ 切換 true/false
                db.SaveChanges();
            }

            return RedirectToAction("Detail", new { id = postId });
        }
    }
}
