using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CookingBaby.Models
{
    public class RecipeCardViewModel
    {
        public int RecipeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public short CookTime { get; set; }
        public string ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string AuthorName { get; set; }
        public string CategoryName { get; set; }
        public decimal? AvgRating { get; set; }
        public int RatingCount { get; set; }
        public string Ingredients { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }


    }
}