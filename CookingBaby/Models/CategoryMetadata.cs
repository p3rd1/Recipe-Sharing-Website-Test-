using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace CookingBaby.Models
{
    [MetadataType(typeof(CategoryMetadata))]
    public partial class Category
    {
    }

    public class CategoryMetadata
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(
            100,
            ErrorMessage = "Tên danh mục tối đa 100 ký tự"
        )]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }


        [StringLength(7)]
        [RegularExpression(
            "^#[0-9A-Fa-f]{6}$",
            ErrorMessage = "Màu phải có dạng #RRGGBB, ví dụ #F59E0B"
        )]
        [Display(Name = "Màu chủ đạo")]
        public string ThemeColor { get; set; }


        [Range(
            1,
            100,
            ErrorMessage = "Thứ tự hiển thị phải từ 1 đến 100"
        )]
        [Display(Name = "Thứ tự hiển thị")]
        public byte DisplayOrder { get; set; }
    }
}