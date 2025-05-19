using System.Security.Cryptography;

namespace PwdGenerator
{
    public class PasswordGenerator
    {
        public byte[] GenerateSalt(int size = 16)
        {
            byte[] salt = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        public string GeneratePasswordHashUsingSalt(string passwordText, byte[] salt)
        {
            var iterate = 10000;
            var pbkdf2 = new Rfc2898DeriveBytes(passwordText, salt, iterate);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            var passwordHash = Convert.ToBase64String(hashBytes);
            return passwordHash;
        }

        public string GeneratePasswordHashUsingSaltOptimized(string passwordText, byte[] salt)
        {
            var iterate = 10000;
            using var pbkdf2 = new Rfc2898DeriveBytes(passwordText, salt, iterate, HashAlgorithmName.SHA1);
            byte[] hash = pbkdf2.GetBytes(20);
            Span<byte> hashBytes = stackalloc byte[36];
            salt.AsSpan(0, 16).CopyTo(hashBytes.Slice(0, 16));
            hash.AsSpan(0, 20).CopyTo(hashBytes.Slice(16, 20));
            return Convert.ToBase64String(hashBytes);
        }
    }

}
