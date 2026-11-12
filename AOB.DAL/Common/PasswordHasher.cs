using System.Security.Cryptography;
using AOB.BAL.Interfaces.Common;

namespace AOB.DAL.Common
{
    /// <summary>
    /// PBKDF2 (HMAC-SHA256) password hasher. Stored format: iterations.salt.hash (Base64 parts).
    /// The iteration count travels with each hash, so old hashes keep verifying after it is raised
    /// and are upgraded the next time their owner signs in (<see cref="NeedsRehash"/>).
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 600_000;

        private static readonly byte[] DummySalt = new byte[SaltSize];

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations) || iterations <= 0)
            {
                return false;
            }

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            // Constant-time comparison prevents timing attacks.
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        public bool NeedsRehash(string storedHash)
            => !int.TryParse(storedHash.Split('.')[0], out var iterations) || iterations < Iterations;

        public void SpendVerifyTime(string password)
            => Rfc2898DeriveBytes.Pbkdf2(password, DummySalt, Iterations, HashAlgorithmName.SHA256, HashSize);
    }
}
