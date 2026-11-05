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
    }
}