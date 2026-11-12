using System.Security.Cryptography;
using System.Text;

namespace AOB.BAL.Common.Helpers
{
    /// <summary>
    /// Generates and hashes one-time secrets (verification tokens and OTPs).
    /// All randomness comes from a cryptographically secure generator.
    /// </summary>
    public static class SecurityTokenHelper
    {
        /// <summary>Random URL-safe token (default 32 bytes = 256 bits) for links.</summary>
        public static string GenerateUrlSafeToken(int byteLength = 32)
            => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        /// <summary>Random numeric code of the given length (leading zeros preserved).</summary>
        public static string GenerateNumericOtp(int length = 6)
        {
            var digits = new char[length];
            for (var i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }
            return new string(digits);
        }

        // Look-alike characters (0/O, 1/l/I) are left out so the password can be typed from the email.
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnopqrstuvwxyz";
        private const string Digits = "23456789";
        private const string Specials = "!@#$%*?-_";

        /// <summary>
        /// Random temporary password that satisfies the password policy: at least one uppercase letter,
        /// lowercase letter, number and special character, in random positions.
        /// </summary>
        public static string GenerateTemporaryPassword(int length = 16)
        {
            if (length < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "A temporary password needs at least 4 characters.");
            }

            const string all = Upper + Lower + Digits + Specials;
            var chars = new char[length];
            chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
            chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
            chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            chars[3] = Specials[RandomNumberGenerator.GetInt32(Specials.Length)];
            for (var i = 4; i < length; i++)
            {
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            RandomNumberGenerator.Shuffle(chars.AsSpan());
            return new string(chars);
        }

        /// <summary>SHA-256 of the value as upper-case hex; this is what gets stored.</summary>
        public static string Hash(string value)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        /// <summary>Constant-time comparison of two hashes to avoid timing leaks.</summary>
        public static bool HashesMatch(string a, string b)
            => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
