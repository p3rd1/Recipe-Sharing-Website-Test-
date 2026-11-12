using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using CookingBaby.Models;

namespace CookingBaby.Services
{
    /// <summary>
    /// Các truy vấn LINQ to Entities cho tìm kiếm, lọc danh mục, lọc nguyên liệu, sắp xếp và thống kê.
    ///
    /// Nguyên tắc tối ưu:
    ///   1. Lọc, sắp xếp, phân trang, đếm, tính trung bình ngay trong SQL (không ToList() cả bảng rồi lọc bằng C#).
    ///   2. Chọn đúng cột cần dùng (Select vào ViewModel) và AsNoTracking() cho dữ liệu chỉ để hiển thị.
    ///   3. Lấy "danh sách Id đã sắp xếp" trước, rồi mới nạp thông tin thẻ món cho đúng các Id đó
    ///      (2-3 câu SQL cố định, không bị N+1 và không phụ thuộc thứ tự join).
    ///   4. Ghép chuỗi nguyên liệu bằng C# sau khi lấy dữ liệu (LINQ to Entities không có STRING_AGG).
    ///   5. Thống kê dùng GROUP BY đơn giản, tận dụng index IX_Ratings_Recipe_Stars và IX_RecipeIngredients_Name.
    ///
    /// Không dùng thuộc tính điều hướng Recipe.User / Recipe.User1 (tên do designer tự đặt, dễ đổi),
    /// mà nối bảng Users bằng AuthorId nên code vẫn chạy dù bạn có đổi tên thành Author/Approver.
    /// </summary>
    public class RecipeQueryService
    {
        private readonly CookingBabyDBEntities _db;

        public RecipeQueryService(CookingBabyDBEntities db)
        {
            _db = db;
        }

        // =====================================================================
        //  1. TÌM KIẾM / LỌC DANH MỤC / LỌC NGUYÊN LIỆU / SẮP XẾP / PHÂN TRANG
        // =====================================================================
        public PagedResult<RecipeCardViewModel> Search(RecipeSearchCriteria c)
        {
            if (c == null) c = new RecipeSearchCriteria();

            List<string> statuses = (c.Statuses != null && c.Statuses.Count > 0)
                ? c.Statuses.ToList()
                : new List<string> { RecipeStatuses.Approved };

            IQueryable<Recipe> q = _db.Recipes.AsNoTracking().Where(r => statuses.Contains(r.Status));

            if (c.AuthorId.HasValue)
            {
                int authorId = c.AuthorId.Value;
                q = q.Where(r => r.AuthorId == authorId);
            }

            if (c.CategoryId.HasValue)
            {
                int categoryId = c.CategoryId.Value;
                q = q.Where(r => r.CategoryId == categoryId);
            }

            // Từ khóa: tên, mô tả hoặc một nguyên liệu. Contains -> LIKE '%từ khóa%'.
            // Cột dùng collation không phân biệt hoa thường nên KHÔNG cần ToLower()
            // (ToLower trên cột còn làm SQL tính toán thêm cho từng dòng).
            string keyword = (c.Keyword ?? "").Trim();
            if (keyword.Length > 0)
            {
                q = q.Where(r => r.Title.Contains(keyword)
                              || r.Description.Contains(keyword)
                              || r.RecipeIngredients.Any(i => i.IngredientName.Contains(keyword)));
            }

            // Lọc nhiều nguyên liệu: món phải có TẤT CẢ (khớp chính xác tên nguyên liệu).
            // Mỗi Where thêm một EXISTS, dùng được index IX_RecipeIngredients_Name.
            foreach (string tag in CleanTags(c.Ingredients))
            {
                string name = tag;
                q = q.Where(r => r.RecipeIngredients.Any(i => i.IngredientName == name));
            }

            int totalCount = q.Count();

            // Sắp xếp. Luôn có tiêu chí phụ RecipeId để thứ tự ổn định khi phân trang.
            IOrderedQueryable<Recipe> ordered;
            switch ((c.SortBy ?? "newest").ToLowerInvariant())
            {
                case "rating":
                    ordered = q.OrderByDescending(r => r.Ratings.Average(x => (double?)x.Stars))
                               .ThenByDescending(r => r.Ratings.Count())
                               .ThenByDescending(r => r.RecipeId);
                    break;
                case "time_asc":
                    ordered = q.OrderBy(r => r.CookTime).ThenByDescending(r => r.RecipeId);
                    break;
                case "name_asc":
                    ordered = q.OrderBy(r => r.Title).ThenBy(r => r.RecipeId);
                    break;
                default:
                    ordered = q.OrderByDescending(r => r.RecipeId);
                    break;
            }

            // Phân trang (Skip bắt buộc phải đứng sau OrderBy)
            int page = c.Page < 1 ? 1 : c.Page;
            IQueryable<Recipe> pageQuery = ordered;
            if (c.PageSize > 0)
            {
                pageQuery = ordered.Skip((page - 1) * c.PageSize).Take(c.PageSize);
            }

            List<int> ids = pageQuery.Select(r => r.RecipeId).ToList();

            return new PagedResult<RecipeCardViewModel>
            {
                Items = BuildCards(ids),
                TotalCount = totalCount,
                Page = page,
                PageSize = c.PageSize
            };
        }

        /// <summary>
        /// Nạp dữ liệu thẻ món cho đúng danh sách Id, GIỮ NGUYÊN thứ tự của danh sách Id.
        /// Tổng cộng 2 câu SQL, không phụ thuộc số món.
        /// </summary>
        public List<RecipeCardViewModel> BuildCards(IList<int> recipeIds)
        {
            var cards = new List<RecipeCardViewModel>();
            if (recipeIds == null || recipeIds.Count == 0) return cards;

            List<int> ids = recipeIds.ToList();

            var rows = _db.Recipes.AsNoTracking()
                .Where(r => ids.Contains(r.RecipeId))
                .Select(r => new
                {
                    r.RecipeId,
                    r.Title,
                    r.Description,
                    r.CookTime,
                    r.ImageUrl,
                    r.CategoryId,
                    r.Status,
                    r.CreatedAt,
                    CategoryName = r.Category.CategoryName,
                    AuthorName = _db.Users.Where(u => u.UserId == r.AuthorId)
                                          .Select(u => u.DisplayName).FirstOrDefault(),
                    AvgRating = r.Ratings.Average(x => (double?)x.Stars),
                    RatingCount = r.Ratings.Count()
                })
                .ToList();

            Dictionary<int, string> ingredientText = LoadIngredientText(ids);
            Dictionary<int, int> rowIndex = new Dictionary<int, int>();
            for (int i = 0; i < rows.Count; i++) rowIndex[rows[i].RecipeId] = i;

            foreach (int id in ids)
            {
                if (!rowIndex.ContainsKey(id)) continue;     // món vừa bị xóa
                var x = rows[rowIndex[id]];
                string text;
                cards.Add(new RecipeCardViewModel
                {
                    RecipeId = x.RecipeId,
                    Title = x.Title,
                    Description = x.Description,
                    CookTime = x.CookTime,
                    ImageUrl = x.ImageUrl,
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    AuthorName = x.AuthorName,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    AvgRating = ToRating(x.AvgRating),
                    RatingCount = x.RatingCount,
                    Ingredients = ingredientText.TryGetValue(id, out text) ? text : ""
                });
            }
            return cards;
        }

        // =====================================================================
        //  2. DANH SÁCH THEO NGƯỜI DÙNG
        // =====================================================================

        /// <summary>Tab "Quản Lý Cá Nhân": món của tác giả, trạng thái Đã duyệt và Chờ duyệt, mới nhất trước.</summary>
        public List<RecipeCardViewModel> GetMyRecipes(int authorId)
        {
            return Search(new RecipeSearchCriteria
            {
                AuthorId = authorId,
                Statuses = new List<string> { RecipeStatuses.Approved, RecipeStatuses.Pending },
                SortBy = "newest"
            }).Items;
        }

        /// <summary>Tab "Đã Lưu": món đã thả tim còn đang hiển thị, lưu gần nhất trước.</summary>
        public List<RecipeCardViewModel> GetSavedRecipes(int userId)
        {
            string approved = RecipeStatuses.Approved;
            List<int> ids = _db.SavedRecipes.AsNoTracking()
                .Where(s => s.UserId == userId && s.Recipe.Status == approved)
                .OrderByDescending(s => s.SavedAt)
                .Select(s => s.RecipeId)
                .ToList();
            return BuildCards(ids);
        }

        /// <summary>Chỉ lấy Id để tô màu nút tim, không cần nạp cả món.</summary>
        public int[] GetSavedRecipeIds(int userId)
        {
            return _db.SavedRecipes.AsNoTracking()
                .Where(s => s.UserId == userId)
                .Select(s => s.RecipeId)
                .ToArray();
        }

        // =====================================================================
        //  3. THỐNG KÊ
        // =====================================================================

        /// <summary>Nguyên liệu phổ biến nhất (theo số món đã duyệt dùng nó). Dùng cho các ô lọc nhanh.</summary>
        public List<IngredientStatViewModel> GetPopularIngredientStats(int take)
        {
            string approved = RecipeStatuses.Approved;
            return _db.RecipeIngredients.AsNoTracking()
                .Where(i => i.Recipe.Status == approved)
                .GroupBy(i => i.IngredientName)
                .Select(g => new IngredientStatViewModel { Name = g.Key, RecipeCount = g.Count() })
                .OrderByDescending(x => x.RecipeCount)
                .ThenBy(x => x.Name)
                .Take(take)
                .ToList();
        }

        public List<string> GetPopularIngredients(int take)
        {
            return GetPopularIngredientStats(take).Select(x => x.Name).ToList();
        }

        public OverviewStatsViewModel GetOverview()
        {
            var statusCounts = _db.Recipes.AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            Func<string, int> countOf = status =>
            {
                foreach (var s in statusCounts) if (s.Status == status) return s.Count;
                return 0;
            };

            return new OverviewStatsViewModel
            {
                TotalUsers = _db.Users.Count(u => u.IsActive),
                TotalRecipes = statusCounts.Sum(s => s.Count),
                ApprovedRecipes = countOf(RecipeStatuses.Approved),
                PendingRecipes = countOf(RecipeStatuses.Pending),
                HiddenRecipes = countOf(RecipeStatuses.Hidden),
                TotalRatings = _db.Ratings.Count(),
                TotalComments = _db.Comments.Count(),
                TotalSaves = _db.SavedRecipes.Count(),
                AvgRating = ToRating(_db.Ratings.Average(r => (double?)r.Stars))
            };
        }

        /// <summary>Mỗi danh mục (kể cả danh mục chưa có món): số món đã duyệt, số lượt chấm, điểm trung bình.</summary>
        public List<CategoryStatViewModel> GetCategoryStats()
        {
            string approved = RecipeStatuses.Approved;

            var categories = _db.Categories.AsNoTracking()
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.CategoryId)
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToList();

            var recipeCounts = _db.Recipes.AsNoTracking()
                .Where(r => r.Status == approved)
                .GroupBy(r => r.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToList();

            var ratingStats = _db.Ratings.AsNoTracking()
                .Where(rt => rt.Recipe.Status == approved)
                .GroupBy(rt => rt.Recipe.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count(), Avg = g.Average(x => (double?)x.Stars) })
                .ToList();

            var result = new List<CategoryStatViewModel>();
            foreach (var cat in categories)
            {
                var rc = recipeCounts.FirstOrDefault(x => x.CategoryId == cat.CategoryId);
                var rs = ratingStats.FirstOrDefault(x => x.CategoryId == cat.CategoryId);
                result.Add(new CategoryStatViewModel
                {
                    CategoryId = cat.CategoryId,
                    CategoryName = cat.CategoryName,
                    RecipeCount = rc == null ? 0 : rc.Count,
                    RatingCount = rs == null ? 0 : rs.Count,
                    AvgRating = rs == null ? null : ToRating(rs.Avg)
                });
            }
            return result;
        }

        /// <summary>Các món được chấm cao nhất (đủ số lượt chấm tối thiểu), đã có đủ thông tin thẻ món.</summary>
        public List<RecipeCardViewModel> GetTopRated(int take, int minRatings)
        {
            string approved = RecipeStatuses.Approved;
            int min = minRatings < 1 ? 1 : minRatings;

            List<int> ids = _db.Recipes.AsNoTracking()
                .Where(r => r.Status == approved && r.Ratings.Count() >= min)
                .OrderByDescending(r => r.Ratings.Average(x => (double?)x.Stars))
                .ThenByDescending(r => r.Ratings.Count())
                .ThenBy(r => r.RecipeId)
                .Take(take)
                .Select(r => r.RecipeId)
                .ToList();

            return BuildCards(ids);
        }

        /// <summary>Tác giả có nhiều món đã duyệt nhất, kèm tổng lượt chấm sao nhận được.</summary>
        public List<AuthorStatViewModel> GetTopAuthors(int take)
        {
            string approved = RecipeStatuses.Approved;

            var top = _db.Recipes.AsNoTracking()
                .Where(r => r.Status == approved)
                .GroupBy(r => r.AuthorId)
                .Select(g => new { AuthorId = g.Key, RecipeCount = g.Count() })
                .OrderByDescending(x => x.RecipeCount).ThenBy(x => x.AuthorId)
                .Take(take)
                .ToList();
            if (top.Count == 0) return new List<AuthorStatViewModel>();

            List<int> authorIds = top.Select(x => x.AuthorId).ToList();

            var names = _db.Users.AsNoTracking()
                .Where(u => authorIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.DisplayName })
                .ToList();

            var ratingCounts = _db.Ratings.AsNoTracking()
                .Where(rt => rt.Recipe.Status == approved && authorIds.Contains(rt.Recipe.AuthorId))
                .GroupBy(rt => rt.Recipe.AuthorId)
                .Select(g => new { AuthorId = g.Key, Count = g.Count() })
                .ToList();

            var result = new List<AuthorStatViewModel>();
            foreach (var t in top)
            {
                var n = names.FirstOrDefault(x => x.UserId == t.AuthorId);
                var rc = ratingCounts.FirstOrDefault(x => x.AuthorId == t.AuthorId);
                result.Add(new AuthorStatViewModel
                {
                    AuthorId = t.AuthorId,
                    DisplayName = n == null ? "" : n.DisplayName,
                    RecipeCount = t.RecipeCount,
                    RatingCount = rc == null ? 0 : rc.Count
                });
            }
            return result;
        }

        // =====================================================================
        //  Hỗ trợ
        // =====================================================================

        /// <summary>Làm sạch danh sách nguyên liệu người dùng chọn: bỏ rỗng, tách dấu phẩy, bỏ trùng, tối đa 10.</summary>
        public static List<string> CleanTags(IEnumerable<string> raw)
        {
            var result = new List<string>();
            if (raw == null) return result;

            foreach (string item in raw)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                foreach (string part in item.Split(','))
                {
                    string tag = part.Trim();
                    if (tag.Length == 0) continue;
                    if (!result.Any(x => string.Equals(x, tag, StringComparison.OrdinalIgnoreCase)))
                        result.Add(tag);
                    if (result.Count >= 10) return result;
                }
            }
            return result;
        }

        private Dictionary<int, string> LoadIngredientText(List<int> ids)
        {
            var items = _db.RecipeIngredients.AsNoTracking()
                .Where(i => ids.Contains(i.RecipeId))
                .OrderBy(i => i.SortOrder).ThenBy(i => i.IngredientId)
                .Select(i => new { i.RecipeId, i.IngredientName })
                .ToList();

            return items.GroupBy(i => i.RecipeId)
                        .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(i => i.IngredientName)));
        }

        private static decimal? ToRating(double? value)
        {
            if (!value.HasValue) return null;
            return Math.Round((decimal)value.Value, 1, MidpointRounding.AwayFromZero);
        }
    }
}
