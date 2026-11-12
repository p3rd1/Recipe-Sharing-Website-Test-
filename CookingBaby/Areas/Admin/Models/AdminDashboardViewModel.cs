using System;
using System.Collections.Generic;

namespace CookingBaby.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        // Thống kê trạng thái công thức
        public int VisibleCount { get; set; }
        public int PendingCount { get; set; }
        public int HiddenCount { get; set; }

        // Thống kê tổng quan hệ thống
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public int TotalRatings { get; set; }

        // Dữ liệu biểu đồ Danh mục (Doughnut / Pie Chart)
        public List<string> CategoryChartLabels { get; set; }
        public List<int> CategoryChartData { get; set; }

        // Dữ liệu biểu đồ Xu hướng theo tháng (Bar / Line Chart)
        public List<string> MonthlyChartLabels { get; set; }
        public List<int> MonthlyChartData { get; set; }

        // Danh sách công thức kiểm duyệt
        public List<AdminRecipeRowViewModel> Recipes { get; set; }

        public AdminDashboardViewModel()
        {
            Recipes = new List<AdminRecipeRowViewModel>();
            CategoryChartLabels = new List<string>();
            CategoryChartData = new List<int>();
            MonthlyChartLabels = new List<string>();
            MonthlyChartData = new List<int>();
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
