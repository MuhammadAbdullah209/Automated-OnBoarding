using System.Security.Cryptography;
using AOB.DAL.Common;
using Xunit;

namespace AOB.Tests
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Hash_verifies_and_is_not_flagged_for_rehash()
        {
            var hash = _hasher.Hash("Correct#Horse9");

            Assert.True(_hasher.Verify("Correct#Horse9", hash));
            Assert.False(_hasher.Verify("Wrong#Horse9", hash));
            Assert.False(_hasher.NeedsRehash(hash));
        }

        [Fact]
        public void Hash_made_with_fewer_iterations_still_verifies_but_needs_rehash()
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var key = Rfc2898DeriveBytes.Pbkdf2("Correct#Horse9", salt, 100_000, HashAlgorithmName.SHA256, 32);
            var legacy = $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";

            Assert.True(_hasher.Verify("Correct#Horse9", legacy));
            Assert.True(_hasher.NeedsRehash(legacy));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-hash")]
        [InlineData("600000.!!!.???")]
        [InlineData("0.AAAA.AAAA")]
        public void Malformed_stored_hash_fails_verification_without_throwing(string stored)
        {
            Assert.False(_hasher.Verify("anything", stored));
        }
    }
}
