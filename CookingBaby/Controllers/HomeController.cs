using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CookingBaby.Models;

namespace CookingBaby.Controllers
{
    public class HomeController : Controller
    {
        private CookingBabyDBEntities db = new CookingBabyDBEntities();

        // 1. TRANG CHỦ KHÁM PHÁ CÔNG THỨC (LỌC DANH MỤC + TỪ KHÓA + NHIỀU TAG NGUYÊN LIỆU + SẮP XẾP)
        public ActionResult Index(int? categoryId, string keyword, string[] ingredients, string sortBy = "newest")
        {
            int currentUserId = 1;

            ViewBag.SavedRecipeIds = db.SavedRecipes
                .Where(s => s.UserId == currentUserId)
                .Select(s => s.RecipeId)
                .ToArray();

            // Lấy dữ liệu từ view vw_RecipeCards
            string query = "SELECT * FROM dbo.vw_RecipeCards WHERE Status = N'Đã duyệt'";
            var listFromView = db.Database.SqlQuery<RecipeCardViewModel>(query).ToList();

            // Lấy danh sách các Tag nguyên liệu phổ biến từ Database bằng LINQ để hiển thị lên trang chủ
            var availableIngredients = db.RecipeIngredients
                .Select(ri => ri.IngredientName.Trim())
                .Where(name => name != "")
                .Distinct()
                .OrderBy(name => name)
                .Take(15) // Lấy tối đa 15 tag phổ biến
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

            // 3. Lọc theo NHIỀU Tag nguyên liệu được chọn (món ăn chứa các nguyên liệu đã chọn)
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
                case "rating": // Điểm đánh giá cao nhất
                    listFromView = listFromView
                        .OrderByDescending(r => r.AvgRating ?? 0)
                        .ThenByDescending(r => r.RatingCount)
                        .ToList();
                    break;
                case "time_asc": // Thời gian nấu nhanh nhất
                    listFromView = listFromView
                        .OrderBy(r => r.CookTime)
                        .ToList();
                    break;
                case "name_asc": // Tên món A -> Z
                    listFromView = listFromView
                        .OrderBy(r => r.Title)
                        .ToList();
                    break;
                default: // Mặc định: Mới nhất
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
            // 1. LẤY TRỰC TIẾP TỪ VIEW vw_RecipeCards (Đảm bảo 100% AuthorName khớp với kết quả SQL)
            var card = db.Database.SqlQuery<RecipeCardViewModel>(
                "SELECT * FROM dbo.vw_RecipeCards WHERE RecipeId = @RecipeId",
                new SqlParameter("@RecipeId", id)
            ).FirstOrDefault();

            if (card == null)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            // 2. Lấy danh sách nguyên liệu từ bảng RecipeIngredients
            var ingredients = db.Database.SqlQuery<string>(
                "SELECT IngredientName FROM dbo.RecipeIngredients WHERE RecipeId = @RecipeId ORDER BY SortOrder ASC",
                new SqlParameter("@RecipeId", id)
            ).ToList();

            // 3. Lấy danh sách các bước nấu từ bảng RecipeSteps
            var steps = db.Database.SqlQuery<string>(
                "SELECT Instruction FROM dbo.RecipeSteps WHERE RecipeId = @RecipeId ORDER BY StepNumber ASC",
                new SqlParameter("@RecipeId", id)
            ).ToList();

            if (!steps.Any() && !string.IsNullOrWhiteSpace(card.Description))
            {
                steps.Add(card.Description);
            }

            // 4. Tính điểm đánh giá trung bình & số lượt đánh giá bằng LINQ từ bảng Ratings
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
                authorName = card.AuthorName, // Lấy thẳng chữ "Quỳnh Như", "Thanh Bình", "Siêu đầu bếp nhí" từ SQL
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
        public JsonResult SubmitReview(int recipeId, int stars, string content)
        {
            try
            {
                int userId = 1;

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

        // 5. LƯU HOẶC BỎ LƯU CÔNG THỨC (CHO NÚT TIM)
        [HttpPost]
        public JsonResult ToggleSaved(int recipeId)
        {
            try
            {
                int userId = 1;

                if (recipeId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "RecipeId không hợp lệ."
                    });
                }

                bool recipeExists = db.Recipes.Any(r => r.RecipeId == recipeId);
                if (!recipeExists)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy công thức."
                    });
                }

                var savedRecipe = db.SavedRecipes
                    .FirstOrDefault(s => s.UserId == userId && s.RecipeId == recipeId);

                bool isSaved;

                if (savedRecipe == null)
                {
                    var newSavedRecipe = new SavedRecipe
                    {
                        UserId = userId,
                        RecipeId = recipeId,
                        SavedAt = DateTime.Now
                    };
                    db.SavedRecipes.Add(newSavedRecipe);
                    isSaved = true;
                }
                else
                {
                    db.SavedRecipes.Remove(savedRecipe);
                    isSaved = false;
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    isSaved = isSaved
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