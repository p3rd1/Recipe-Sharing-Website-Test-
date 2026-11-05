using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CookingBaby.Models
{
    public class RecipeReviewViewModel
    {
        public int CommentId { get; set; }

        public int RecipeId { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

        public byte? Stars { get; set; }

        public string Content { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}