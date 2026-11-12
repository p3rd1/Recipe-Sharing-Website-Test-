using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CookingBaby.Models;

namespace CookingBaby.Infrastructure
{
    // Phân quyền thực sự trên Controller/Action, không chỉ ẩn nút bằng Razor.
    // Session do máy chủ tạo sau khi xác thực mật khẩu; UserId/Role không đến từ query string/form.
    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase context)
        {
            if (context.User == null || !context.User.Identity.IsAuthenticated ||
                context.Session == null || !(context.Session["UserID"] is int))
                return false;
            int id = (int)context.Session["UserID"];
            if (id <= 0) return false;
            // Kiểm tra trực tiếp database: khóa tài khoản/thu hồi quyền Admin có hiệu lực ngay.
            using (var db = new CookingBabyDBEntities())
            {
                var account = db.Users.Where(u => u.UserId == id && u.IsActive)
                    .Select(u => new { u.Username, u.Role }).FirstOrDefault();
                if (account == null || !string.Equals(account.Username, context.User.Identity.Name, StringComparison.OrdinalIgnoreCase))
                    return false;
                string role = account.Role == "Admin" ? "Admin" : "User";
                context.Session["Role"] = role;
                if (string.IsNullOrWhiteSpace(Roles)) return true;
                return Roles.Split(',').Any(r => string.Equals(r.Trim(), role, StringComparison.OrdinalIgnoreCase));
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext context)
        {
            bool loggedIn = context.HttpContext.User.Identity.IsAuthenticated &&
                            context.HttpContext.Session != null &&
                            (context.HttpContext.Session["UserID"] is int);
            if (context.HttpContext.Request.IsAjaxRequest())
            {
                context.HttpContext.Response.StatusCode = loggedIn ? 403 : 401;
                context.HttpContext.Response.TrySkipIisCustomErrors = true;
                context.Result = new JsonResult
                {
                    Data = new { success = false, message = loggedIn ? "Bạn không có quyền thực hiện thao tác." : "Vui lòng đăng nhập để tiếp tục." },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return;
            }
            if (!loggedIn)
            {
                var requestedPath = context.HttpContext.Request.RawUrl;
                context.Result = new RedirectResult("~/Account/Login?returnUrl=" + HttpUtility.UrlEncode(requestedPath));
                return;
            }
            context.Result = new RedirectResult("~/Account/AccessDenied");
        }
    }
}
