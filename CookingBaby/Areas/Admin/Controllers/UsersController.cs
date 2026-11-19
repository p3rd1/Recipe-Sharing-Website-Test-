using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CookingBaby.Infrastructure;
using CookingBaby.Models;

namespace CookingBaby.Areas.Admin.Controllers
{
    [SessionAuthorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly CookingBabyDBEntities db = new CookingBabyDBEntities();

        // 1. DANH SÁCH TÀI KHOẢN (TÌM KIẾM & BỘ LỌC ROLE/ISACTIVE)
        // GET: Admin/Users
        public ActionResult Index(string search, string role, bool? isActive)
        {
            var query = db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(kw) ||
                    u.DisplayName.ToLower().Contains(kw) ||
                    u.Email.ToLower().Contains(kw)
                );
            }

            if (!string.IsNullOrWhiteSpace(role) && (role == "Admin" || role == "User"))
            {
                query = query.Where(u => u.Role == role);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var users = query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserAdminViewModel
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    RecipeCount = db.Recipes.Count(r => r.AuthorId == u.UserId)
                })
                .ToList();

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.IsActive = isActive;
            ViewBag.IsAdminPage = true;
            ViewBag.ActiveTab = "Users";

            return View(users);
        }

        // 2. KHÓA / MỞ KHÓA TÀI KHOẢN (TOGGLEACTIVE)
        // POST: Admin/Users/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id)
        {
            try
            {
                int currentAdminId = Session["UserID"] is int ? (int)Session["UserID"] : 0;
                if (id == currentAdminId)
                {
                    TempData["ErrorMessage"] = "Bạn không thể khóa chính tài khoản Admin đang đăng nhập của mình!";
                    return RedirectToAction("Index");
                }

                var user = db.Users.Find(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản cần thao tác.";
                    return RedirectToAction("Index");
                }

                user.IsActive = !user.IsActive;
                db.SaveChanges();

                string actionName = user.IsActive ? "mở khóa" : "khóa";
                TempData["SuccessMessage"] = $"Đã {actionName} tài khoản \"{user.DisplayName}\" ({user.Username}) thành công!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật trạng thái tài khoản: " + ex.Message);
                TempData["ErrorMessage"] = "Chưa thể cập nhật trạng thái tài khoản lúc này. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }

        // 3. PHÂN QUYỀN TÀI KHOẢN (CHANGEROLE)
        // POST: Admin/Users/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeRole(int id, string newRole)
        {
            try
            {
                int currentAdminId = Session["UserID"] is int ? (int)Session["UserID"] : 0;
                if (id == currentAdminId)
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự hạ quyền hoặc thay đổi quyền của chính tài khoản mình đang đăng nhập!";
                    return RedirectToAction("Index");
                }

                var user = db.Users.Find(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng cần phân quyền.";
                    return RedirectToAction("Index");
                }

                if (newRole != "Admin" && newRole != "User")
                {
                    TempData["ErrorMessage"] = "Vai trò không hợp lệ (chỉ chấp nhận 'Admin' hoặc 'User').";
                    return RedirectToAction("Index");
                }

                user.Role = newRole;
                db.SaveChanges();

                TempData["SuccessMessage"] = $"Đã cập nhật vai trò của tài khoản \"{user.DisplayName}\" thành {newRole} thành công!";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật quyền tài khoản: " + ex.Message);
                TempData["ErrorMessage"] = "Chưa thể cập nhật quyền tài khoản lúc này. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
