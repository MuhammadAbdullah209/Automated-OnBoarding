using System.Text.RegularExpressions;
using AOB.BAL.Common.Helpers;
using Xunit;

namespace AOB.Tests
{
    public class SecurityTokenHelperTests
    {
        [Fact]
        public void Temporary_password_meets_the_password_policy()
        {
            // Same rules as AOB.WEB.Validation.PasswordPolicyAttribute (8–30 chars, upper, lower, number, special).
            for (var i = 0; i < 500; i++)
            {
                var password = SecurityTokenHelper.GenerateTemporaryPassword();

                Assert.Equal(16, password.Length);
                Assert.Matches("[A-Z]", password);
                Assert.Matches("[a-z]", password);
                Assert.Matches("[0-9]", password);
                Assert.Matches(@"[^A-Za-z0-9\s]", password);
                Assert.DoesNotMatch(@"[\s<>&""']", password);
            }
        }

        [Fact]
        public void Temporary_passwords_are_not_repeated()
        {
            var passwords = Enumerable.Range(0, 1000).Select(_ => SecurityTokenHelper.GenerateTemporaryPassword()).ToHashSet();

            Assert.Equal(1000, passwords.Count);
        }

        [Fact]
        public void Temporary_password_leaves_out_look_alike_characters()
        {
            var all = string.Concat(Enumerable.Range(0, 200).Select(_ => SecurityTokenHelper.GenerateTemporaryPassword()));

            Assert.False(Regex.IsMatch(all, "[0O1lI]"));
        }

        [Fact]
        public void Temporary_password_rejects_lengths_too_short_for_the_rules()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SecurityTokenHelper.GenerateTemporaryPassword(3));
        }
    }
}
