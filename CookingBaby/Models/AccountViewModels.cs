using System;
using System.ComponentModel.DataAnnotations;

namespace CookingBaby.Models
{
    public class SignUpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Chỉ dùng chữ cái, số và dấu gạch dưới.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        private string _displayName;

        [Display(Name = "Họ tên / Tên hiển thị")]
        public string FullName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        [Display(Name = "Tên hiển thị")]
        [StringLength(100, ErrorMessage = "Tên hiển thị tối đa 100 ký tự.")]
        public string DisplayName
        {
            get { return _displayName; }
            set { _displayName = value; }
        }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 128 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }
    }

    public class UserAdminViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RecipeCount { get; set; }
    }
}
