using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using CookingBaby.Areas.Admin.Models;
using CookingBaby.Models;
using CookingBaby.Infrastructure;

namespace CookingBaby.Areas.Admin.Controllers
{
    [SessionAuthorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly CookingBabyDBEntities db = new CookingBabyDBEntities();

        // GET: Admin/Dashboard
        public ActionResult Index()
        {
            try
            {
                var recipes = db.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.User1)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

                // 1. LINQ đếm động theo trạng thái & hệ thống
                int visibleCount = db.Recipes.Count(r => r.Status == "Đã duyệt");
                int pendingCount = db.Recipes.Count(r => r.Status == "Chờ duyệt");
                int hiddenCount = db.Recipes.Count(r => r.Status == "Đã ẩn");
                int totalUsers = db.Users.Count();
                int totalComments = db.Comments.Count();
                int totalRatings = db.Ratings.Count();

                // 2. LINQ Gom nhóm theo danh mục ẩm thực (Category Distribution)
                var catStats = db.Categories
                    .Select(c => new
                    {
                        CategoryName = c.CategoryName,
                        Count = c.Recipes.Count(r => r.Status != "Đã ẩn")
                    })
                    .OrderByDescending(c => c.Count)
                    .ToList();

                var categoryLabels = catStats.Select(c => c.CategoryName).ToList();
                var categoryData = catStats.Select(c => c.Count).ToList();

                // 3. LINQ Gom nhóm công thức theo 6 tháng gần nhất (Monthly Trend)
                var sixMonthsAgo = DateTime.Now.AddMonths(-5);
                var startOfFirstMonth = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

                var monthlyRaw = db.Recipes
                    .Where(r => r.CreatedAt >= startOfFirstMonth)
                    .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .ToList();

                var monthlyLabels = new List<string>();
                var monthlyData = new List<int>();

                for (int i = 5; i >= 0; i--)
                {
                    var targetDate = DateTime.Now.AddMonths(-i);
                    monthlyLabels.Add("T" + targetDate.Month + "/" + targetDate.Year);

                    var match = monthlyRaw.FirstOrDefault(m => m.Year == targetDate.Year && m.Month == targetDate.Month);
                    monthlyData.Add(match != null ? match.Count : 0);
                }

                var model = new AdminDashboardViewModel
                {
                    VisibleCount = visibleCount,
                    PendingCount = pendingCount,
                    HiddenCount = hiddenCount,
                    TotalUsers = totalUsers,
                    TotalComments = totalComments,
                    TotalRatings = totalRatings,
                    CategoryChartLabels = categoryLabels,
                    CategoryChartData = categoryData,
                    MonthlyChartLabels = monthlyLabels,
                    MonthlyChartData = monthlyData,
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
            catch (Exception ex)
            {
                TempData["AdminError"] = "Lỗi tải dữ liệu bảng điều khiển: " + ex.Message;
                return View(new AdminDashboardViewModel());
            }
        }

        // API Endpoint: Trả dữ liệu JSON phục vụ AJAX vẽ lại biểu đồ Chart.js nếu cần
        [HttpGet]
        public JsonResult GetChartData()
        {
            try
            {
                var catStats = db.Categories
                    .Select(c => new
                    {
                        CategoryName = c.CategoryName,
                        Count = c.Recipes.Count(r => r.Status != "Đã ẩn")
                    })
                    .OrderByDescending(c => c.Count)
                    .ToList();

                var sixMonthsAgo = DateTime.Now.AddMonths(-5);
                var startOfFirstMonth = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

                var monthlyRaw = db.Recipes
                    .Where(r => r.CreatedAt >= startOfFirstMonth)
                    .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .ToList();

                var monthlyLabels = new List<string>();
                var monthlyData = new List<int>();

                for (int i = 5; i >= 0; i--)
                {
                    var targetDate = DateTime.Now.AddMonths(-i);
                    monthlyLabels.Add("T" + targetDate.Month + "/" + targetDate.Year);
                    var match = monthlyRaw.FirstOrDefault(m => m.Year == targetDate.Year && m.Month == targetDate.Month);
                    monthlyData.Add(match != null ? match.Count : 0);
                }

                return Json(new
                {
                    success = true,
                    categories = new
                    {
                        labels = catStats.Select(c => c.CategoryName),
                        data = catStats.Select(c => c.Count)
                    },
                    monthly = new
                    {
                        labels = monthlyLabels,
                        data = monthlyData
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            try
            {
                var recipe = db.Recipes.Find(id);
                if (recipe == null)
                {
                    TempData["AdminError"] = "Không tìm thấy công thức cần duyệt.";
                    return RedirectToAction("Index");
                }

                db.Configuration.ValidateOnSaveEnabled = false;

                recipe.Status = "Đã duyệt";
                recipe.ApprovedBy = (int)Session["UserID"];
                recipe.ApprovedAt = DateTime.Now;
                recipe.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                db.Configuration.ValidateOnSaveEnabled = true;

                TempData["AdminSuccess"] = "Đã duyệt món: " + recipe.Title;
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Lỗi khi duyệt món: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Hide(int id)
        {
            try
            {
                var recipe = db.Recipes.Find(id);
                if (recipe == null)
                {
                    TempData["AdminError"] = "Không tìm thấy công thức cần ẩn.";
                    return RedirectToAction("Index");
                }

                db.Configuration.ValidateOnSaveEnabled = false;

                recipe.Status = "Đã ẩn";
                recipe.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                db.Configuration.ValidateOnSaveEnabled = true;

                TempData["AdminSuccess"] = "Đã ẩn món: " + recipe.Title;
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Lỗi khi ẩn món: " + ex.Message;
            }
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
