using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using Xunit;

namespace AOB.Tests
{
    public class UserCsvParserTests
    {
        [Fact]
        public void Reads_rows_with_and_without_header()
        {
            var (rows, errors) = UserCsvParser.Parse("Name,Email,Role\nAda Lovelace,ada@x.com,Admin\r\nBob,bob@x.com,user\n");

            Assert.Empty(errors);
            Assert.Equal(2, rows.Count);
            Assert.Equal(CustomerUserRole.Admin, rows[0].Role);
            Assert.Equal(CustomerUserRole.User, rows[1].Role);

            var (noHeader, noHeaderErrors) = UserCsvParser.Parse("Ada,ada@x.com,admin");
            Assert.Empty(noHeaderErrors);
            Assert.Single(noHeader);
        }

        [Fact]
        public void Reads_quoted_names_with_commas()
        {
            var (rows, errors) = UserCsvParser.Parse("\"Lovelace, Ada\",ada@x.com,admin");

            Assert.Empty(errors);
            Assert.Equal("Lovelace, Ada", rows[0].Name);
        }

        [Theory]
        [InlineData("Ada,not-an-email,admin", "valid email")]
        [InlineData("Ada,ada@x.com,owner", "admin\" or \"user")]
        [InlineData("Ada,ada@x.com", "3 columns")]
        [InlineData(",ada@x.com,user", "name")]
        public void Reports_the_line_of_a_bad_row(string csv, string expected)
        {
            var (rows, errors) = UserCsvParser.Parse("name,email,role\n" + csv);

            Assert.Empty(rows);
            Assert.Contains(errors, e => e.StartsWith("Line 2") && e.Contains(expected));
        }

        [Fact]
        public void Rejects_duplicate_emails_and_empty_files()
        {
            var (_, duplicates) = UserCsvParser.Parse("A,a@x.com,user\nB,A@X.com,user");
            Assert.Contains(duplicates, e => e.Contains("more than once"));

            var (_, empty) = UserCsvParser.Parse("name,email,role\n");
            Assert.NotEmpty(empty);
        }
    }
}
