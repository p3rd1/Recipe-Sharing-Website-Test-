using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CookingBaby.Models;

namespace CookingBaby.Services
{
    public class RegisterResult
    {
        public bool Success { get; set; }
        public int UserId { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public bool IsLockedOut { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
    }

    /// <summary>
    /// Đăng ký / đăng nhập / đổi mật khẩu, dùng các cột mới của bảng Users
    /// (PasswordHash, FailedLoginCount, LockoutEnd, LastLoginAt, PasswordChangedAt).
    /// Cần chạy 02_Users_Hoan_Thien.sql và "Update Model from Database" trong .edmx trước.
    /// </summary>
    public class AccountService
    {
        public const int MaxFailedAttempts = 5;
        public const int LockoutMinutes = 15;
        private const string GenericFailure = "Tên đăng nhập hoặc mật khẩu không đúng.";

        // Hash giả để vẫn tốn thời gian băm khi tài khoản không tồn tại (tránh dò tên đăng nhập qua thời gian phản hồi)
        private static readonly string DummyHash = PasswordHasher.Hash("dummy-password-for-timing");

        private static readonly Regex UsernameRule = new Regex(@"^[A-Za-z0-9_.]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex EmailRule = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly CookingBabyDBEntities _db;

        public AccountService(CookingBabyDBEntities db)
        {
            _db = db;
        }

        // ------------------------------------------------------------------ ĐĂNG KÝ
        public RegisterResult Register(string username, string displayName, string email, string password)
        {
            var result = new RegisterResult();
            username = (username ?? "").Trim();
            displayName = (displayName ?? "").Trim();
            email = (email ?? "").Trim();

            if (!UsernameRule.IsMatch(username))
                result.Errors.Add("Tên đăng nhập 3-50 ký tự, chỉ gồm chữ không dấu, số, dấu gạch dưới và dấu chấm.");
            if (displayName.Length == 0 || displayName.Length > 100)
                result.Errors.Add("Tên hiển thị là bắt buộc (tối đa 100 ký tự).");
            if (email.Length > 150 || !EmailRule.IsMatch(email))
                result.Errors.Add("Email không đúng định dạng (tối đa 150 ký tự).");
            if (!IsStrongPassword(password))
                result.Errors.Add("Mật khẩu từ 6 ký tự trở lên.");

            if (result.Errors.Count > 0) return result;

            // Cột Username/Email có ràng buộc UNIQUE trong DB (so sánh không phân biệt hoa thường)
            if (_db.Users.Any(u => u.Username == username))
                result.Errors.Add("Tên đăng nhập đã tồn tại.");
            if (_db.Users.Any(u => u.Email == email))
                result.Errors.Add("Email đã được sử dụng.");
            if (result.Errors.Count > 0) return result;

            DateTime now = DateTime.Now;
            var user = new User
            {
                Username = username,
                DisplayName = displayName,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Role = "User",                 // tài khoản Admin chỉ tạo thủ công trong DB
                IsActive = true,
                CreatedAt = now,
                PasswordChangedAt = now,
                FailedLoginCount = 0
            };
            _db.Users.Add(user);
            _db.SaveChanges();

            result.Success = true;
            result.UserId = user.UserId;
            return result;
        }

        // ------------------------------------------------------------------ ĐĂNG NHẬP
        public LoginResult Login(string usernameOrEmail, string password)
        {
            var result = new LoginResult { Message = GenericFailure };
            string key = (usernameOrEmail ?? "").Trim();
            if (key.Length == 0 || string.IsNullOrEmpty(password)) return result;

            DateTime now = DateTime.Now;
            User user = _db.Users.FirstOrDefault(u => u.Username == key || u.Email == key);

            if (user == null)
            {
                PasswordHasher.Verify(password, DummyHash);
                return result;
            }

            if (!user.IsActive)
            {
                result.Message = "Tài khoản đã bị vô hiệu hóa.";
                return result;
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
            {
                int minutes = (int)Math.Ceiling((user.LockoutEnd.Value - now).TotalMinutes);
                result.IsLockedOut = true;
                result.Message = "Tài khoản tạm khóa do nhập sai nhiều lần. Thử lại sau " + minutes + " phút.";
                return result;
            }

            if (!PasswordHasher.Verify(password, user.PasswordHash))
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.LockoutEnd = now.AddMinutes(LockoutMinutes);
                    user.FailedLoginCount = 0;
                }
                _db.SaveChanges();
                return result;
            }

            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = now;
            if (PasswordHasher.NeedsRehash(user.PasswordHash))
            {
                user.PasswordHash = PasswordHasher.Hash(password);
                user.PasswordChangedAt = now;
            }
            _db.SaveChanges();

            result.Success = true;
            result.Message = "Đăng nhập thành công.";
            result.UserId = user.UserId;
            result.Username = user.Username;
            result.DisplayName = user.DisplayName;
            result.Role = user.Role;
            return result;
        }

        // ------------------------------------------------------------------ ĐỔI MẬT KHẨU
        public bool ChangePassword(int userId, string oldPassword, string newPassword, out string error)
        {
            error = null;
            User user = _db.Users.FirstOrDefault(u => u.UserId == userId && u.IsActive);
            if (user == null || !PasswordHasher.Verify(oldPassword, user.PasswordHash))
            {
                error = "Mật khẩu hiện tại không đúng.";
                return false;
            }
            if (!IsStrongPassword(newPassword))
            {
                error = "Mật khẩu mới từ 6 ký tự trở lên.";
                return false;
            }

            user.PasswordHash = PasswordHasher.Hash(newPassword);
            user.PasswordChangedAt = DateTime.Now;
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            _db.SaveChanges();
            return true;
        }

        public static bool IsStrongPassword(string password)
        {
            return !string.IsNullOrEmpty(password)
                && password.Length >= 6;
        }
    }
}
