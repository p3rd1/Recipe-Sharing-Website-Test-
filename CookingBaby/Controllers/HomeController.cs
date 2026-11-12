using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CookingBaby.Models;
using CookingBaby.Infrastructure;

namespace CookingBaby.Controllers
{
    public class HomeController : Controller
    {
        private CookingBabyDBEntities db = new CookingBabyDBEntities();

        // 1. TRANG CHỦ KHÁM PHÁ CÔNG THỨC (LỌC DANH MỤC + TỪ KHÓA + NHIỀU TAG NGUYÊN LIỆU + SẮP XẾP)
        public ActionResult Index(int? categoryId, string keyword, string[] ingredients, string sortBy = "newest")
        {
            int? currentUserId = Session["UserID"] as int?;
            ViewBag.SavedRecipeIds = currentUserId.HasValue
                ? db.SavedRecipes.Where(s => s.UserId == currentUserId.Value).Select(s => s.RecipeId).ToArray()
                : new int[0];

            // Lấy dữ liệu từ view vw_RecipeCards
            string query = "SELECT * FROM dbo.vw_RecipeCards WHERE Status = N'Đã duyệt'";
            var listFromView = db.Database.SqlQuery<RecipeCardViewModel>(query).ToList();

            // Lấy danh sách các Tag nguyên liệu phổ biến từ Database bằng LINQ để hiển thị lên trang chủ
            var availableIngredients = db.RecipeIngredients
                .Select(ri => ri.IngredientName.Trim())
                .Where(name => name != "")
                .Distinct()
                .OrderBy(name => name)
                .Take(15)
                .ToList();

            // 1. Lọc theo CategoryId bằng LINQ
            if (categoryId.HasValue)
            {
                listFromView = listFromView
                    .Where(r => r.CategoryId == categoryId.Value)
                    .ToList();
            }

            // 2. Tìm kiếm theo từ khóa (Title, Description hoặc Ingredients) bằng LINQ
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string kw = keyword.Trim().ToLower();
                listFromView = listFromView
                    .Where(r =>
                        (r.Title != null && r.Title.ToLower().Contains(kw)) ||
                        (r.Description != null && r.Description.ToLower().Contains(kw)) ||
                        (r.Ingredients != null && r.Ingredients.ToLower().Contains(kw))
                    )
                    .ToList();
            }

            // 3. Lọc theo NHIỀU Tag nguyên liệu được chọn
            var selectedIngs = new List<string>();
            if (ingredients != null && ingredients.Any())
            {
                selectedIngs = ingredients
                    .SelectMany(i => i.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (selectedIngs.Any())
                {
                    listFromView = listFromView
                        .Where(r => !string.IsNullOrWhiteSpace(r.Ingredients) &&
                                    selectedIngs.All(sel => r.Ingredients.ToLower().Contains(sel.ToLower())))
                        .ToList();
                }
            }

            // 4. Sắp xếp danh sách món ăn bằng LINQ
            switch (sortBy)
            {
                case "rating":
                    listFromView = listFromView
                        .OrderByDescending(r => r.AvgRating ?? 0)
                        .ThenByDescending(r => r.RatingCount)
                        .ToList();
                    break;
                case "time_asc":
                    listFromView = listFromView
                        .OrderBy(r => r.CookTime)
                        .ToList();
                    break;
                case "name_asc":
                    listFromView = listFromView
                        .OrderBy(r => r.Title)
                        .ToList();
                    break;
                default:
                    listFromView = listFromView
                        .OrderByDescending(r => r.RecipeId)
                        .ToList();
                    break;
            }

            var model = new RecipeHomeViewModel
            {
                Categories = db.Categories.OrderBy(c => c.DisplayOrder).ToList(),
                Recipes = listFromView,
                SelectedCategoryId = categoryId,
                Keyword = keyword,
                AvailableIngredients = availableIngredients,
                SelectedIngredients = selectedIngs,
                SortBy = sortBy
            };

            return View(model);
        }

        // 2. LẤY CHI TIẾT MÓN ĂN (NGUYÊN LIỆU & BƯỚC NẤU ĐỘC LẬP TỪNG MÓN)
        [HttpGet]
        public JsonResult GetRecipeDetail(int id)
        {
            var card = db.Database.SqlQuery<RecipeCardViewModel>(
                "SELECT * FROM dbo.vw_RecipeCards WHERE RecipeId = @RecipeId",
                new SqlParameter("@RecipeId", id)
            ).FirstOrDefault();

            if (card == null ||
                (card.Status != "Đã duyệt" &&
                 !(Session["Role"] as string == "Admin" ||
                   (Session["UserID"] is int && (int)Session["UserID"] == card.AuthorId))))
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var ingredients = db.Database.SqlQuery<string>(
                "SELECT IngredientName FROM dbo.RecipeIngredients WHERE RecipeId = @RecipeId ORDER BY SortOrder ASC",
                new SqlParameter("@RecipeId", id)
            ).ToList();

            var steps = db.Database.SqlQuery<string>(
                "SELECT Instruction FROM dbo.RecipeSteps WHERE RecipeId = @RecipeId ORDER BY StepNumber ASC",
                new SqlParameter("@RecipeId", id)
            ).ToList();

            if (!steps.Any() && !string.IsNullOrWhiteSpace(card.Description))
            {
                steps.Add(card.Description);
            }

            var ratingQuery = db.Ratings.Where(r => r.RecipeId == id);
            int reviewCount = ratingQuery.Count();
            double avgRating = ratingQuery
                .Select(r => (double?)r.Stars)
                .Average() ?? 0.0;
            avgRating = Math.Round(avgRating, 1);

            return Json(new
            {
                success = true,
                recipeId = card.RecipeId,
                title = card.Title,
                categoryName = card.CategoryName ?? "Món ngon",
                cookTime = card.CookTime,
                description = card.Description,
                imageUrl = string.IsNullOrEmpty(card.ImageUrl) ? "supbido.jpg" : card.ImageUrl,
                authorName = card.AuthorName,
                ingredients = ingredients,
                steps = steps,
                avgRating = avgRating,
                reviewCount = reviewCount
            }, JsonRequestBehavior.AllowGet);
        }

        // 3. LẤY DANH SÁCH BÌNH LUẬN & ĐÁNH GIÁ (CHO POPUP CHI TIẾT)
        [HttpGet]
        public JsonResult GetReviews(int recipeId)
        {
            try
            {
                string sql = @"
                    SELECT
                        CommentId,
                        RecipeId,
                        UserId,
                        UserName,
                        Stars,
                        Content,
                        CreatedAt
                    FROM dbo.vw_RecipeReviews
                    WHERE RecipeId = @RecipeId
                    ORDER BY CreatedAt DESC";

                var reviews = db.Database.SqlQuery<RecipeReviewViewModel>(
                    sql,
                    new SqlParameter("@RecipeId", recipeId)
                ).ToList();

                var result = reviews.Select(r => new
                {
                    r.CommentId,
                    r.RecipeId,
                    r.UserId,
                    r.UserName,
                    r.Stars,
                    r.Content,
                    CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        // 4. GỬI BÌNH LUẬN & ĐÁNH GIÁ SAO
        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public JsonResult SubmitReview(int recipeId, int stars, string content)
        {
            try
            {
                int userId = (int)Session["UserID"];

                if (recipeId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Công thức không hợp lệ."
                    });
                }

                if (stars < 1 || stars > 5)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng chọn từ 1 đến 5 sao."
                    });
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng nhập nội dung bình luận."
                    });
                }

                content = content.Trim();
                if (content.Length > 1000)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bình luận không được vượt quá 1000 ký tự."
                    });
                }

                db.Database.ExecuteSqlCommand(
                    @"EXEC dbo.sp_SubmitReview 
                        @RecipeId,
                        @UserId,
                        @Stars,
                        @Content",
                    new SqlParameter("@RecipeId", recipeId),
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@Stars", stars),
                    new SqlParameter("@Content", content)
                );

                return Json(new
                {
                    success = true,
                    message = "Đánh giá thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // 5. LƯU HOẶC BỎ LƯU CÔNG THỨC (CHO NÚT TIM) - XỬ LÝ AN TOÀN, KHÔNG VĂNG HTTP 500
        [HttpPost]
        public JsonResult ToggleSaved(int? recipeId)
        {
            try
            {
                // 1. Kiểm tra tham số recipeId linh hoạt
                if (!recipeId.HasValue || recipeId.Value <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã công thức không hợp lệ."
                    });
                }

                int currentUserId = 0;

                // 2. Kiểm tra an toàn Session["UserID"] (xử lý cả kiểu int lẫn string/object, không văng exception)
                if (Session["UserID"] is int uid)
                {
                    currentUserId = uid;
                }
                else if (Session["UserID"] != null && int.TryParse(Session["UserID"].ToString(), out int parsedUid))
                {
                    currentUserId = parsedUid;
                }

                // Nếu chưa đăng nhập, trả về JSON thông báo sạch sẽ để client chuyển hướng/báo lỗi
                if (currentUserId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        requireLogin = true,
                        message = "Vui lòng đăng nhập để lưu công thức này vào sổ tay!"
                    });
                }

                int targetRecipeId = recipeId.Value;

                // 3. Kiểm tra công thức có tồn tại trong CSDL không
                var recipeExists = db.Recipes.Any(r => r.RecipeId == targetRecipeId);
                if (!recipeExists)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Công thức không tồn tại hoặc đã bị xóa."
                    });
                }

                // 4. Tìm bản ghi trong bảng SavedRecipes theo khóa phức hợp (UserId, RecipeId)
                var savedRecord = db.SavedRecipes.FirstOrDefault(s => s.RecipeId == targetRecipeId && s.UserId == currentUserId);

                bool isSaved = false;
                if (savedRecord != null)
                {
                    // Bỏ lưu: Xóa bản ghi
                    db.SavedRecipes.Remove(savedRecord);
                    isSaved = false;
                }
                else
                {
                    // Lưu mới: Khởi tạo đầy đủ các trường (ĐẶC BIỆT là SavedAt kiểu DateTime NOT NULL)
                    var newSave = new SavedRecipe
                    {
                        UserId = currentUserId,
                        RecipeId = targetRecipeId,
                        SavedAt = DateTime.Now // Bắt buộc gán ngày giờ hiện tại để không bị lỗi DbEntityValidationException / SqlDateTime overflow
                    };

                    db.SavedRecipes.Add(newSave);
                    isSaved = true;
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    isSaved = isSaved,
                    message = isSaved ? "Đã lưu vào sổ tay thành công!" : "Đã bỏ lưu công thức."
                });
            }
            catch (Exception ex)
            {
                // Bắt toàn diện mọi ngoại lệ, lấy chi tiết InnerException để client/developer dễ debug
                string innerDetail = ex.InnerException != null ? ex.InnerException.Message : null;
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                {
                    innerDetail += " -> " + ex.InnerException.InnerException.Message;
                }

                return Json(new
                {
                    success = false,
                    message = "Lỗi hệ thống khi lưu công thức: " + ex.Message,
                    inner = innerDetail
                });
            }
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
