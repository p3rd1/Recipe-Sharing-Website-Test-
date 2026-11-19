using System;
using System.Collections.Generic;
using System.Data;
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

        // 1. TRANG CHỦ KHÁM PHÁ CÔNG THỨC (LỌC DANH MỤC + TỪ KHÓA + NHIỀU TAG NGUYÊN LIỆU + SẮP XẾP + PHÂN TRANG)
        public ActionResult Index(int? categoryId, string keyword, string[] ingredients, string sortBy = "newest", int page = 1)
        {
            int? currentUserId = Session["UserID"] as int?;
            ViewBag.SavedRecipeIds = currentUserId.HasValue
                ? db.SavedRecipes.Where(s => s.UserId == currentUserId.Value).Select(s => s.RecipeId).ToArray()
                : new int[0];

            // Lấy dữ liệu từ view vw_RecipeCards (có fallback Entity Framework đảm bảo chạy ổn định khi mang qua máy mới)
            List<RecipeCardViewModel> listFromView;
            try
            {
                string query = "SELECT * FROM dbo.vw_RecipeCards WHERE Status = N'Đã duyệt'";
                listFromView = db.Database.SqlQuery<RecipeCardViewModel>(query).ToList();
            }
            catch
            {
                // Fallback Entity Framework LINQ: Hoạt động trơn tru trên mọi máy tính và phiên bản SQL Server
                var approvedRecipes = db.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.User1)
                    .Include(r => r.RecipeIngredients)
                    .Include(r => r.Ratings)
                    .Where(r => r.Status == "Đã duyệt")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();

                listFromView = approvedRecipes.Select(r => new RecipeCardViewModel
                {
                    RecipeId = r.RecipeId,
                    Title = r.Title,
                    Description = r.Description,
                    CookTime = r.CookTime,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    CategoryId = r.CategoryId,
                    CategoryName = r.Category != null ? r.Category.CategoryName : "",
                    AuthorId = r.AuthorId,
                    AuthorName = r.User1 != null ? r.User1.DisplayName : "Thành viên",
                    AvgRating = r.Ratings.Any() ? (decimal?)Math.Round((decimal)r.Ratings.Average(rt => (double)rt.Stars), 1) : null,
                    RatingCount = r.Ratings.Count(),
                    Ingredients = string.Join(", ", r.RecipeIngredients.OrderBy(ri => ri.SortOrder).Select(ri => ri.IngredientName))
                }).ToList();
            }

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

            // 5. Tính toán phân trang chuẩn Buổi 06 (Skip / Take)
            const int pageSize = 6;
            if (page < 1) page = 1;
            int totalItems = listFromView.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedRecipes = listFromView
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new RecipeHomeViewModel
            {
                Categories = db.Categories.OrderBy(c => c.DisplayOrder).ToList(),
                Recipes = pagedRecipes,
                SelectedCategoryId = categoryId,
                Keyword = keyword,
                AvailableIngredients = availableIngredients,
                SelectedIngredients = selectedIngs,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(model);
        }

        // 2. LẤY CHI TIẾT MÓN ĂN (NGUYÊN LIỆU & BƯỚC NẤU ĐỘC LẬP TỪNG MÓN)
        [HttpGet]
        public JsonResult GetRecipeDetail(int id)
        {
            RecipeCardViewModel card = null;
            try
            {
                card = db.Database.SqlQuery<RecipeCardViewModel>(
                    "SELECT * FROM dbo.vw_RecipeCards WHERE RecipeId = @RecipeId",
                    new SqlParameter("@RecipeId", id)
                ).FirstOrDefault();
            }
            catch { }

            if (card == null)
            {
                var r = db.Recipes.Include(x => x.Category).Include(x => x.User1).Include(x => x.Ratings).FirstOrDefault(x => x.RecipeId == id);
                if (r != null)
                {
                    card = new RecipeCardViewModel
                    {
                        RecipeId = r.RecipeId,
                        Title = r.Title,
                        Description = r.Description,
                        CookTime = r.CookTime,
                        ImageUrl = r.ImageUrl,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        CategoryId = r.CategoryId,
                        CategoryName = r.Category != null ? r.Category.CategoryName : "",
                        AuthorId = r.AuthorId,
                        AuthorName = r.User1 != null ? r.User1.DisplayName : "Thành viên",
                        AvgRating = r.Ratings.Any() ? (decimal?)Math.Round((decimal)r.Ratings.Average(rt => (double)rt.Stars), 1) : null,
                        RatingCount = r.Ratings.Count()
                    };
                }
            }

            if (card == null ||
                (card.Status != "Đã duyệt" &&
                 !(Session["Role"] as string == "Admin" ||
                   (Session["UserID"] is int && (int)Session["UserID"] == card.AuthorId))))
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            }

            var ingredients = db.RecipeIngredients
                .Where(ri => ri.RecipeId == id)
                .OrderBy(ri => ri.SortOrder)
                .Select(ri => ri.IngredientName)
                .ToList();

            var steps = db.RecipeSteps
                .Where(rs => rs.RecipeId == id)
                .OrderBy(rs => rs.StepNumber)
                .Select(rs => rs.Instruction)
                .ToList();

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
                    Content = r.CommentId == 0 ? null : r.Content,
                    CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                try
                {
                    var comments = db.Comments
                        .Include(c => c.User)
                        .Where(c => c.RecipeId == recipeId)
                        .OrderByDescending(c => c.CreatedAt)
                        .ToList();

                    var ratings = db.Ratings
                        .Include(r => r.User)
                        .Where(r => r.RecipeId == recipeId)
                        .ToList();

                    var result = comments.Select(c =>
                    {
                        var userRating = ratings.FirstOrDefault(r => r.UserId == c.UserId);
                        return new
                        {
                            CommentId = c.CommentId,
                            RecipeId = c.RecipeId,
                            UserId = c.UserId,
                            UserName = c.User != null ? c.User.DisplayName : "Thành viên",
                            Stars = userRating != null ? (int)userRating.Stars : 5,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                        };
                    }).ToList();

                    foreach (var rating in ratings.Where(r => !comments.Any(c => c.UserId == r.UserId)))
                    {
                        result.Add(new
                        {
                            CommentId = 0,
                            RecipeId = rating.RecipeId,
                            UserId = rating.UserId,
                            UserName = rating.User != null ? rating.User.DisplayName : "Thành viên",
                            Stars = (int)rating.Stars,
                            Content = (string)null,
                            CreatedAt = rating.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                        });
                    }

                    return Json(result, JsonRequestBehavior.AllowGet);
                }
                catch
                {
                    return Json(new List<object>(), JsonRequestBehavior.AllowGet);
                }
            }
        }

        // 4. GỬI ĐÁNH GIÁ (SAO & BÌNH LUẬN)
        [HttpPost]
        public JsonResult SubmitReview(int recipeId, int stars, string content)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        requireLogin = true,
                        message = "Vui lòng đăng nhập để đánh giá hoặc bình luận."
                    });
                }

                int userId = Session["UserID"] is int uid ? uid : Convert.ToInt32(Session["UserID"]);

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

                content = (content ?? string.Empty).Trim();
                if (content.Length > 1000)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bình luận không được vượt quá 1000 ký tự."
                    });
                }

                try
                {
                    db.Database.ExecuteSqlCommand(
                        @"EXEC dbo.sp_SubmitReview
                            @RecipeId,
                            @UserId,
                            @Stars,
                            @Content",
                        new SqlParameter("@RecipeId", recipeId),
                        new SqlParameter("@UserId", userId),
                        new SqlParameter("@Stars", stars),
                        new SqlParameter("@Content", SqlDbType.NVarChar, 1000)
                        {
                            Value = string.IsNullOrEmpty(content) ? (object)DBNull.Value : content
                        }
                    );
                }
                catch
                {
                    // Fallback nếu CSDL chưa có stored procedure sp_SubmitReview
                    var rating = db.Ratings.FirstOrDefault(r => r.RecipeId == recipeId && r.UserId == userId);
                    if (rating != null)
                    {
                        rating.Stars = (byte)stars;
                        rating.CreatedAt = DateTime.Now;
                    }
                    else
                    {
                        db.Ratings.Add(new Rating
                        {
                            RecipeId = recipeId,
                            UserId = userId,
                            Stars = (byte)stars,
                            CreatedAt = DateTime.Now
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        db.Comments.Add(new Comment
                        {
                            RecipeId = recipeId,
                            UserId = userId,
                            Content = content,
                            CreatedAt = DateTime.Now
                        });
                    }

                    db.SaveChanges();
                }

                return Json(new
                {
                    success = true,
                    message = "Đánh giá thành công!"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi gửi đánh giá/bình luận: " + ex.Message);
                return Json(new
                {
                    success = false,
                    message = "Chưa thể gửi đánh giá lúc này. Vui lòng thử lại."
                });
            }
        }

        // 5. LƯU HOẶC BỎ LƯU CÔNG THỨC (CHO NÚT TIM) - XỬ LÝ AN TOÀN, ĐÁP ỨNG CHUẨN EC09
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public JsonResult ToggleSaved(int? recipeId, int? id)
        {
            try
            {
                // Hỗ trợ cả param 'recipeId' và 'id'
                int targetId = recipeId ?? id ?? 0;

                // 1. Kiểm tra tham số recipeId <= 0 (DevTools test: recipeId = -1)
                if (targetId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Mã công thức không hợp lệ."
                    }, JsonRequestBehavior.AllowGet);
                }

                // 2. Kiểm tra công thức có tồn tại trong CSDL không (DevTools test: recipeId = 99999)
                var recipe = db.Recipes.Find(targetId);
                if (recipe == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Công thức không tồn tại hoặc đã bị xóa."
                    }, JsonRequestBehavior.AllowGet);
                }

                // 3. Kiểm tra đăng nhập (Bước 1: Khách bấm tim trên thẻ món hợp lệ ở Trang chủ)
                int currentUserId = 0;
                if (Session["UserID"] is int uid)
                {
                    currentUserId = uid;
                }
                else if (Session["UserID"] != null && int.TryParse(Session["UserID"].ToString(), out int parsedUid))
                {
                    currentUserId = parsedUid;
                }

                if (currentUserId <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        requireLogin = true,
                        message = "Vui lòng đăng nhập để lưu công thức này vào sổ tay!"
                    }, JsonRequestBehavior.AllowGet);
                }

                // 4. Tìm bản ghi trong bảng SavedRecipes theo (UserId, RecipeId) và thực hiện Lưu / Bỏ lưu
                var savedRecord = db.SavedRecipes.FirstOrDefault(s => s.RecipeId == targetId && s.UserId == currentUserId);

                bool isSaved = false;
                if (savedRecord != null)
                {
                    // Bỏ lưu: Xóa bản ghi
                    db.SavedRecipes.Remove(savedRecord);
                    isSaved = false;
                }
                else
                {
                    // Lưu mới: Gán đầy đủ các trường bắt buộc
                    var newSave = new SavedRecipe
                    {
                        UserId = currentUserId,
                        RecipeId = targetId,
                        SavedAt = DateTime.Now
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
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi lưu công thức yêu thích: " + ex.Message);
                return Json(new
                {
                    success = false,
                    message = "Chưa thể cập nhật sổ tay món ăn. Vui lòng thử lại."
                }, JsonRequestBehavior.AllowGet);
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
