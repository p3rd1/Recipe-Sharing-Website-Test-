using System;
using System.Security.Cryptography;

namespace CookingBaby.Models
{
    // Lưu cả thuật toán, số vòng lặp và salt trong duy nhất cột Users.PasswordHash (NVARCHAR(255)).
    // Không đòi hỏi thêm cột PasswordSalt, phù hợp EDMX/SQL hiện tại.
    public static class PasswordHelper
    {
        private const int Iterations = 150000;
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const string Prefix = "PBKDF2-SHA256";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("Mật khẩu không được rỗng.", "password");
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                hash = pbkdf2.GetBytes(KeySize);
            return Prefix + "$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash)) return false;
            var parts = storedHash.Split('$');
            int iterations;
            if (parts.Length != 4 || parts[0] != Prefix ||
                !int.TryParse(parts[1], out iterations) || iterations < 10000 || iterations > 1000000)
                return false;
            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                if (salt.Length != SaltSize || expected.Length != KeySize) return false;
                byte[] actual;
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                    actual = pbkdf2.GetBytes(expected.Length);
                int difference = 0;
                for (int i = 0; i < expected.Length; i++) difference |= expected[i] ^ actual[i];
                return difference == 0;
            }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
        }
    }
}
