using System;
using System.Collections.Generic;

namespace CookingBaby.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int VisibleCount { get; set; }
        public int PendingCount { get; set; }
        public int HiddenCount { get; set; }
        public List<AdminRecipeRowViewModel> Recipes { get; set; }

        public AdminDashboardViewModel()
        {
            Recipes = new List<AdminRecipeRowViewModel>();
        }
    }

    public class AdminRecipeRowViewModel
    {
        public int RecipeId { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public string AuthorName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public short CookTime { get; set; }
    }
}
