using System.ComponentModel.DataAnnotations;

namespace CookingBaby.Models
{
    // ============================================================
    // USERS
    // ============================================================

    [MetadataType(typeof(UserMetadata))]
    public partial class User
    {
    }
    public class UserMetadata
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Tên hiển thị là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên hiển thị tối đa 100 ký tự")]
        [Display(Name = "Tên hiển thị")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [StringLength(150, ErrorMessage = "Email tối đa 150 ký tự")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }


    // ============================================================
    // RECIPES
    // ============================================================

    [MetadataType(typeof(RecipeMetadata))]
    public partial class Recipe
    {
    }

    public class RecipeMetadata
    {
        [Required(ErrorMessage = "Vui lòng nhập tên món ăn")]
        [StringLength(
            200,
            ErrorMessage = "Tên món ăn tối đa 200 ký tự"
        )]
        [Display(Name = "Tên món ăn")]
        public string Title { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập mô tả ngắn")]
        [StringLength(
            500,
            ErrorMessage = "Mô tả tối đa 500 ký tự"
        )]
        [Display(Name = "Mô tả ngắn hấp dẫn")]
        public string Description { get; set; }


        [Required(ErrorMessage = "Vui lòng nhập thời gian nấu")]
        [Range(
            1,
            1440,
            ErrorMessage = "Thời gian nấu phải từ 1 đến 1440 phút"
        )]
        [Display(Name = "Thời gian (phút)")]
        public short CookTime { get; set; }


        [StringLength(
            500,
            ErrorMessage = "URL ảnh tối đa 500 ký tự"
        )]
        [RegularExpression(
            @"^(https?://\S+|~?/\S+)$",
            ErrorMessage = "URL hình ảnh phải bắt đầu bằng http://, https:// hoặc /"
        )]
        [Display(Name = "URL hình ảnh món ăn")]
        public string ImageUrl { get; set; }


        [Required(
            ErrorMessage = "Vui lòng chọn Đầu Bếp Nhí (danh mục)"
        )]
        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Vui lòng chọn Đầu Bếp Nhí (danh mục)"
        )]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
    }


    // ============================================================
    // RECIPE INGREDIENTS
    // ============================================================

    [MetadataType(typeof(RecipeIngredientMetadata))]
    public partial class RecipeIngredient
    {
    }

    public class RecipeIngredientMetadata
    {
        [Required(ErrorMessage = "Tên nguyên liệu là bắt buộc")]
        [StringLength(
            100,
            ErrorMessage = "Tên nguyên liệu tối đa 100 ký tự"
        )]
        [Display(Name = "Nguyên liệu")]
        public string IngredientName { get; set; }


        [Range(
            0,
            255,
            ErrorMessage = "Thứ tự từ 0 đến 255"
        )]
        [Display(Name = "Thứ tự")]
        public byte SortOrder { get; set; }
    }


    // ============================================================
    // RECIPE STEPS
    // ============================================================

    [MetadataType(typeof(RecipeStepMetadata))]
    public partial class RecipeStep
    {
    }

    public class RecipeStepMetadata
    {
        [Range(
            1,
            255,
            ErrorMessage = "Số thứ tự bước từ 1 đến 255"
        )]
        [Display(Name = "Bước")]
        public byte StepNumber { get; set; }


        [Required(
            ErrorMessage = "Nội dung bước thực hiện là bắt buộc"
        )]
        [StringLength(
            1000,
            ErrorMessage = "Mỗi bước tối đa 1000 ký tự"
        )]
        [Display(Name = "Nội dung bước")]
        public string Instruction { get; set; }
    }


    // ============================================================
    // RATINGS
    // ============================================================

    [MetadataType(typeof(RatingMetadata))]
    public partial class Rating
    {
    }

    public class RatingMetadata
    {
        [Required(ErrorMessage = "Vui lòng chọn số sao")]
        [Range(
            1,
            5,
            ErrorMessage = "Số sao phải từ 1 đến 5"
        )]
        [Display(Name = "Số sao")]
        public byte Stars { get; set; }
    }


    // ============================================================
    // COMMENTS
    // ============================================================

    [MetadataType(typeof(CommentMetadata))]
    public partial class Comment
    {
    }

    public class CommentMetadata
    {
        [Required(
            ErrorMessage = "Vui lòng nhập nội dung bình luận"
        )]
        [StringLength(
            1000,
            ErrorMessage = "Bình luận tối đa 1000 ký tự"
        )]
        [Display(Name = "Bình luận")]
        public string Content { get; set; }
    }
}