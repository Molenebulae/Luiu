using Luiu.Models;
using Luiu.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Diagnostics;

namespace Luiu.Controllers
{
    public class MemberController : Controller
    {
        IWebHostEnvironment _envir = null;
        public MemberController(IWebHostEnvironment p)
        {
            // 取得用戶執行路徑
            _envir = p;
        }
        public IActionResult List(CMemberSearchViewModel vm)
        {
            Debug.WriteLine($"[MemberController - List] {vm}");

            LuiuDbContext db = new LuiuDbContext();
            // 確保有分類
            if (string.IsNullOrEmpty(vm.Category)) vm.Category = "Name";
            // 取得人
            var datas = db.TMembers.Select(m => m);
            // 搜尋
            if (!string.IsNullOrEmpty(vm.Search))
            {
                Debug.WriteLine($"[MemberController - List] In Search");
                switch (vm.Category)
                {
                    case "Email":
                        datas = datas.Where(x => x.Email.Contains(vm.Search));
                        break;
                    case "Phone":
                        datas = datas.Where(x => x.Phone.Contains(vm.Search));
                        break;
                    default:
                        datas = datas.Where(x => x.Name.Contains(vm.Search));
                        break;
                }
            }

            vm.TotalCount = datas.Count();
            var pagedData = datas
                .OrderBy(m => m.MemberId)
                .Skip((vm.Page - 1) * vm.PageSize)
                .Take(vm.PageSize)
                .ToList();

            // 轉換型態
            var memberList = pagedData
                .Select(m => new CMemberWrap
                {
                    member = m,
                    LastLoginTime = db.TLoginLogs
                        .Where(log => log.MemberId == m.MemberId) // 取得id一樣的資料
                        .OrderByDescending(log => log.LoginTime)  // 利用時間排序
                        .Select(log => (DateTime?)log.LoginTime)  // 選擇時間欄位
                        .FirstOrDefault() // 取得最新的一筆
                }).ToList();

            ViewBag.vm = vm;
            return View(memberList);
        }
        
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Create(CMemberWrap memberW)
        {
            LuiuDbContext db = new LuiuDbContext();
            db.TMembers.Add(memberW.member);
            if (memberW.Icon != null)
            {
                string extension = Path.GetExtension(memberW.Icon.FileName);
                string photoName = Guid.NewGuid().ToString() + extension;
                string path = _envir.WebRootPath + "/Images/" + photoName;
                memberW.Icon.CopyTo(new FileStream(path, FileMode.Create));
                memberW.IconUrl = photoName;
            }
            if (memberW.UserId == null)
                memberW.UserId = DateTime.Now.ToString();

            memberW.CreateDate = DateTime.Now;
            memberW.UpdateDate = DateTime.Now;
            
            db.SaveChanges();
            return RedirectToAction("List");
        }
        
        public IActionResult Edit(int? id)
        {
            Debug.WriteLine($"[MemberController - Edit] id: {id}");
            LuiuDbContext db = new LuiuDbContext();
            TMember member = db.TMembers.FirstOrDefault(m => m.MemberId == id);

            if (member == null) return RedirectToAction("List");

            CMemberWrap memberW = new CMemberWrap();
            memberW.member = member;
            return View(memberW);
        }
        [HttpPost]
        public IActionResult Edit(CMemberWrap uiMember)
        {
            Debug.WriteLine($"[MemberController - Edit] uiMember: {uiMember}");
            LuiuDbContext db = new LuiuDbContext();
            TMember dbMember = db.TMembers.FirstOrDefault(m => m.MemberId == uiMember.MemberId);
            if (dbMember != null)
            {
                if (uiMember.Icon != null)
                {
                    string extension = Path.GetExtension(uiMember.Icon.FileName);
                    string photoName = Guid.NewGuid().ToString() + extension;
                    string path = _envir.WebRootPath + "/Images/" + photoName;
                    uiMember.Icon.CopyTo(new FileStream(path, FileMode.Create));
                    dbMember.IconUrl = photoName;
                }
                dbMember.UserId = uiMember.UserId;
                dbMember.Name = uiMember.Name;
                dbMember.Gender = (byte)uiMember.Gender;
                dbMember.Email = uiMember.Email;
                dbMember.Phone = uiMember.Phone;
                dbMember.Birthday = uiMember.Birthday;
                dbMember.ProfileStatus = uiMember.ProfileStatus;
                dbMember.Status = (byte)uiMember.Status;
                dbMember.RoleId = uiMember.RoleId;
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
    
        public IActionResult Delete(int? id)
        {
            LuiuDbContext db = new LuiuDbContext();
            TMember x = db.TMembers.FirstOrDefault(m => m.MemberId == id);
            if (x != null)
            {
                x.IsDelete = true;
                x.DeleteTime = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("List");
        }
    }
}
