using System.Collections.Generic;

namespace CookingBaby.Models
{
    /// <summary>Các giá trị cột Recipes.Status (khớp CHECK constraint trong DB).</summary>
    public static class RecipeStatuses
    {
        public const string Pending = "Chờ duyệt";
        public const string Approved = "Đã duyệt";
        public const string Hidden = "Đã ẩn";
    }

    /// <summary>Điều kiện tìm kiếm / lọc / sắp xếp / phân trang món ăn.</summary>
    public class RecipeSearchCriteria
    {
        public int? CategoryId { get; set; }
        public string Keyword { get; set; }                 // tìm trong tên, mô tả, nguyên liệu
        public IList<string> Ingredients { get; set; }      // món phải chứa TẤT CẢ nguyên liệu này
        public IList<string> Statuses { get; set; }         // mặc định: chỉ "Đã duyệt"
        public int? AuthorId { get; set; }
        public string SortBy { get; set; }                  // newest | rating | time_asc | name_asc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; }                   // <= 0: không phân trang (trả về tất cả)
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages
        {
            get { return PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1; }
        }
    }

    // ---------------------------- Thống kê ----------------------------
    public class OverviewStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRecipes { get; set; }
        public int ApprovedRecipes { get; set; }
        public int PendingRecipes { get; set; }
        public int HiddenRecipes { get; set; }
        public int TotalRatings { get; set; }
        public int TotalComments { get; set; }
        public int TotalSaves { get; set; }
        public decimal? AvgRating { get; set; }
    }

    public class CategoryStatViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int RecipeCount { get; set; }                // chỉ tính món đã duyệt
        public int RatingCount { get; set; }
        public decimal? AvgRating { get; set; }
    }

    public class IngredientStatViewModel
    {
        public string Name { get; set; }
        public int RecipeCount { get; set; }
    }

    public class AuthorStatViewModel
    {
        public int AuthorId { get; set; }
        public string DisplayName { get; set; }
        public int RecipeCount { get; set; }
        public int RatingCount { get; set; }
    }
}
