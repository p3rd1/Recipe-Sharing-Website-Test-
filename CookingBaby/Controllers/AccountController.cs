using CookingBaby.Models;
using System;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web.Security;
using System.Web.Mvc;

namespace CookingBaby.Controllers
{
    public class AccountController : Controller
    {
        private readonly CookingBabyDBEntities db = new CookingBabyDBEntities();

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
            if (db.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
                return View(model);
            }
            if (db.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
                return View(model);
            }

            // Khách tự đăng ký luôn nhận role User; KHÔNG nhận Role từ form gửi lên.
            var user = new User
            {
                Username = username,
                DisplayName = model.FullName.Trim(),
                Email = email,
                PasswordHash = PasswordHelper.HashPassword(model.Password),
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            try
            {
                db.Users.Add(user);
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Ràng buộc UNIQUE ở SQL Server vẫn bảo vệ khi 2 người đăng ký đồng thời.
                ModelState.AddModelError("", "Không thể tạo tài khoản. Tên đăng nhập hoặc email có thể đã tồn tại.");
                return View(model);
            }
            TempData["SignUpSuccess"] = "Đăng ký thành công! Hãy đăng nhập bằng tài khoản vừa tạo.";
            return RedirectToAction("Login");
        }

        // URL /Account/Register cũng dẫn về trang đăng ký.
        [HttpGet]
        public ActionResult Register() { return RedirectToAction("SignUp"); }

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
            var user = db.Users.FirstOrDefault(u => u.Username == name && u.IsActive);
            if (user == null || !PasswordHelper.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
                return View(model);
            }

            Session.Clear();
            Session["UserID"] = user.UserId;
            Session["UserName"] = user.Username;
            Session["FullName"] = user.DisplayName;
            Session["Role"] = user.Role == "Admin" ? "Admin" : "User";
            // Cookie xác thực chỉ tồn tại trong phiên trình duyệt; Session giữ role ở máy chủ.
            FormsAuthentication.SetAuthCookie(user.Username, false);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

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
