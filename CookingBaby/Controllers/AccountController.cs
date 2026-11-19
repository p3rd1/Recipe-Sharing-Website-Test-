using CookingBaby.Infrastructure;
using CookingBaby.Models;
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;

namespace CookingBaby.Controllers
{
    public class AccountController : Controller
    {
        private readonly CookingBabyDBEntities db = new CookingBabyDBEntities();

        // 1. ĐĂNG KÝ (SIGNUP / REGISTER)
        [HttpGet]
        public ActionResult SignUp()
        {
            if (Session["UserID"] is int) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var username = model.UserName.Trim();
            var email = model.Email.Trim();
            var displayName = !string.IsNullOrWhiteSpace(model.DisplayName)
                ? model.DisplayName.Trim()
                : (!string.IsNullOrWhiteSpace(model.FullName) ? model.FullName.Trim() : username);

            if (db.Users.Any(u => u.Username.ToLower() == username.ToLower()))
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại trên hệ thống.");
                return View(model);
            }
            if (db.Users.Any(u => u.Email.ToLower() == email.ToLower()))
            {
                ModelState.AddModelError("Email", "Địa chỉ email này đã được sử dụng.");
                return View(model);
            }

            // Gán đúng cấu trúc bảng dbo.Users
            var user = new User
            {
                Username = username,
                DisplayName = displayName,
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.Now,
                FailedLoginCount = 0,
                LastLoginAt = null,
                LockoutEnd = null,
                PasswordChangedAt = null
            };

            try
            {
                db.Users.Add(user);
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Không thể tạo tài khoản. Tên đăng nhập hoặc email có thể đã tồn tại.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã có lỗi xảy ra trong quá trình đăng ký: " + ex.Message);
                return View(model);
            }

            TempData["SignUpSuccess"] = "Đăng ký tài khoản thành công! Bạn có thể đăng nhập ngay bây giờ.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Register()
        {
            return RedirectToAction("SignUp");
        }

        // 2. ĐĂNG NHẬP (LOGIN)
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (Session["UserID"] is int) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            string name = model.UserName.Trim();
            var user = db.Users.FirstOrDefault(u => u.Username.ToLower() == name.ToLower());

            if (user == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra khóa tạm thời do nhập sai nhiều lần
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
                ModelState.AddModelError("", $"Tài khoản của bạn đang bị tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút.");
                return View(model);
            }

            // Kiểm tra tài khoản bị khóa vĩnh viễn bởi Admin
            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa bởi Quản trị viên hệ thống.");
                return View(model);
            }

            // Kiểm tra mật khẩu
            if (!PasswordHelper.Verify(model.Password, user.PasswordHash))
            {
                user.FailedLoginCount += 1;
                if (user.FailedLoginCount >= 5)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(15);
                }
                db.SaveChanges();

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Đăng nhập thành công -> Reset số lần đăng nhập sai, cập nhật LastLoginAt
            user.LastLoginAt = DateTime.Now;
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            db.SaveChanges();

            // Lưu đầy đủ Session chuẩn cho toàn hệ thống
            Session.Clear();
            Session["UserId"] = user.UserId;
            Session["UserID"] = user.UserId;
            Session["Username"] = user.Username;
            Session["UserName"] = user.Username;
            Session["DisplayName"] = user.DisplayName;
            Session["FullName"] = user.DisplayName;
            Session["Role"] = user.Role == "Admin" ? "Admin" : "User";

            FormsAuthentication.SetAuthCookie(user.Username, false);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (user.Role == "Admin")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            return RedirectToAction("Index", "Home");
        }

        // 3. ĐĂNG XUẤT (LOGOUT)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        // 4. TRANG TỪ CHỐI TRUY CẬP (ACCESS DENIED - 403)
        [HttpGet]
        public ActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
