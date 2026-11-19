using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CookingBaby.Models
{
    public class RecipeHomeViewModel
    {
        public List<Category> Categories { get; set; }
        public List<RecipeCardViewModel> Recipes { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string Keyword { get; set; }
        public List<string> AvailableIngredients { get; set; } = new List<string>();
        public List<string> SelectedIngredients { get; set; } = new List<string>();
        public string SortBy { get; set; }

        // Phân trang chuẩn Buổi 06
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 6;
    }
}