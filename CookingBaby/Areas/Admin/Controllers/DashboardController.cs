using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CookingBaby.Areas.Admin.Models;
using CookingBaby.Models;

namespace CookingBaby.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        private readonly CookingBabyDBEntities db = new CookingBabyDBEntities();

        // GET: Admin
        public ActionResult Index()
        {
            var recipes = db.Recipes
                .Include(r => r.Category)
                .Include(r => r.User1)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                VisibleCount = db.Recipes.Count(r => r.Status == "Đã duyệt"),
                PendingCount = db.Recipes.Count(r => r.Status == "Chờ duyệt"),
                HiddenCount = db.Recipes.Count(r => r.Status == "Đã ẩn"),
                Recipes = recipes.Select(r => new AdminRecipeRowViewModel
                {
                    RecipeId = r.RecipeId,
                    Title = r.Title,
                    ImageUrl = r.ImageUrl,
                    CategoryName = r.Category != null ? r.Category.CategoryName : "Chưa có danh mục",
                    AuthorName = r.User1 != null ? r.User1.DisplayName : "Không xác định",
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    CookTime = r.CookTime
                }).ToList()
            };

            ViewBag.IsAdminPage = true;
            ViewBag.ActiveTab = "Admin";
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            var recipe = db.Recipes.Find(id);
            if (recipe == null)
            {
                TempData["AdminError"] = "Không tìm thấy công thức cần duyệt.";
                return RedirectToAction("Index");
            }

            db.Configuration.ValidateOnSaveEnabled = false;

            recipe.Status = "Đã duyệt";
            recipe.ApprovedAt = DateTime.Now;
            recipe.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            db.Configuration.ValidateOnSaveEnabled = true;

            TempData["AdminSuccess"] = "Đã duyệt món: " + recipe.Title;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Hide(int id)
        {
            var recipe = db.Recipes.Find(id);
            if (recipe == null)
            {
                TempData["AdminError"] = "Không tìm thấy công thức cần ẩn.";
                return RedirectToAction("Index");
            }

            // Tắt validate khi lưu để tránh lỗi EntityValidationException khi chỉ update Status/UpdatedAt
            db.Configuration.ValidateOnSaveEnabled = false;

            recipe.Status = "Đã ẩn";
            recipe.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            // Bật lại cấu hình validate
            db.Configuration.ValidateOnSaveEnabled = true;

            TempData["AdminSuccess"] = "Đã ẩn món: " + recipe.Title;
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}