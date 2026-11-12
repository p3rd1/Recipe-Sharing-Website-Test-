using System;
using System.Globalization;
using System.Security.Cryptography;

namespace CookingBaby.Services
{
    /// <summary>
    /// Băm mật khẩu bằng PBKDF2-HMAC-SHA256 (có salt ngẫu nhiên riêng cho mỗi tài khoản).
    /// Chuỗi lưu trong cột Users.PasswordHash:
    ///     PBKDF2$SHA256$&lt;số vòng lặp&gt;$&lt;salt base64&gt;$&lt;hash base64&gt;
    /// Số vòng lặp nằm ngay trong chuỗi nên sau này tăng CurrentIterations vẫn đăng nhập được
    /// với hash cũ, và NeedsRehash() cho biết khi nào nên băm lại.
    /// </summary>
    public static class PasswordHasher
    {
        public const int CurrentIterations = 100000;

        private const string Prefix = "PBKDF2";
        private const string Algorithm = "SHA256";
        private const int SaltSize = 16;      // byte
        private const int KeySize = 32;       // byte (256 bit)
        private const int MinIterations = 10000;
        private const int MaxIterations = 5000000;

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException("password");

            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            byte[] key = Derive(password, salt, CurrentIterations, KeySize);

            return string.Join("$", new string[]
            {
                Prefix,
                Algorithm,
                CurrentIterations.ToString(CultureInfo.InvariantCulture),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key)
            });
        }

        public static bool Verify(string password, string stored)
        {
            if (password == null || string.IsNullOrEmpty(stored)) return false;

            string[] parts = stored.Split('$');
            if (parts.Length != 5 || parts[0] != Prefix || parts[1] != Algorithm) return false;

            int iterations;
            if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)) return false;
            if (iterations < MinIterations || iterations > MaxIterations) return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[3]);
                expected = Convert.FromBase64String(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }
            if (salt.Length < 8 || expected.Length < 16) return false;

            byte[] actual = Derive(password, salt, iterations, expected.Length);
            return FixedTimeEquals(actual, expected);
        }

        /// <summary>True nếu hash được tạo với số vòng lặp thấp hơn mức hiện hành.</summary>
        public static bool NeedsRehash(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return true;
            string[] parts = stored.Split('$');
            int iterations;
            if (parts.Length != 5 ||
                !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out iterations))
                return true;
            return iterations < CurrentIterations;
        }

        private static byte[] Derive(string password, byte[] salt, int iterations, int length)
        {
            // Rfc2898DeriveBytes có tham số HashAlgorithmName cần .NET Framework 4.7.2 trở lên (project dùng 4.8)
            using (var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return kdf.GetBytes(length);
            }
        }

        // So sánh thời gian không đổi (CryptographicOperations.FixedTimeEquals không có trên .NET Framework)
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
