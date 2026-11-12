using System.ComponentModel.DataAnnotations;
using System.Text;
using AOB.BAL.Common.Enums;

namespace AOB.BAL.Common.Helpers
{
    /// <summary>One valid row of the users CSV.</summary>
    public sealed record UserCsvRow(string Name, string Email, CustomerUserRole Role);

    /// <summary>
    /// Reads the users CSV: three columns, <c>name, email, role</c>, where the role is <c>admin</c> or <c>user</c>.
    /// A header row is optional. Quoted values (with commas inside) are supported. Every problem is reported with its
    /// line number, and nothing is accepted unless the whole file is valid.
    /// </summary>
    public static class UserCsvParser
    {
        public const int MaxUsers = 500;
        private const int MaxErrorsReported = 10;

        public static (IReadOnlyList<UserCsvRow> Rows, IReadOnlyList<string> Errors) Parse(string csv)
        {
            var rows = new List<UserCsvRow>();
            var errors = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var emailCheck = new EmailAddressAttribute();

            var lines = (csv ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var first = true;

            for (var i = 0; i < lines.Length; i++)
            {
                var lineNumber = i + 1;
                var line = lines[i];
                if (line.Length > 0 && line[0] == '﻿')
                {
                    line = line[1..]; // a byte order mark at the start of the file
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var cells = SplitLine(line);

                // An optional header row.
                if (first)
                {
                    first = false;
                    if (cells.Count > 0 && cells[0].Equals("name", StringComparison.OrdinalIgnoreCase)
                        && cells.Count > 1 && cells[1].Equals("email", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                if (cells.Count != 3)
                {
                    AddError(errors, $"Line {lineNumber}: expected 3 columns (name, email, role) but found {cells.Count}.");
                    continue;
                }

                var name = cells[0].Trim();
                var email = cells[1].Trim();
                var roleText = cells[2].Trim();

                if (name.Length == 0 || name.Length > 200)
                {
                    AddError(errors, $"Line {lineNumber}: the name is missing or longer than 200 characters.");
                    continue;
                }

                if (email.Length > 320 || !emailCheck.IsValid(email))
                {
                    AddError(errors, $"Line {lineNumber}: \"{email}\" is not a valid email address.");
                    continue;
                }

                if (!TryParseRole(roleText, out var role))
                {
                    AddError(errors, $"Line {lineNumber}: the role must be \"admin\" or \"user\" (found \"{roleText}\").");
                    continue;
                }

                if (!seen.Add(email))
                {
                    AddError(errors, $"Line {lineNumber}: {email} appears more than once.");
                    continue;
                }

                rows.Add(new UserCsvRow(name, email, role));
            }

            if (rows.Count == 0 && errors.Count == 0)
            {
                errors.Add("The file has no users. Each line needs a name, an email and a role (admin or user).");
            }

            if (rows.Count > MaxUsers)
            {
                errors.Add($"The file has {rows.Count} users. The limit is {MaxUsers} per file.");
            }

            return errors.Count > 0 ? (Array.Empty<UserCsvRow>(), errors) : (rows, errors);
        }

        private static void AddError(List<string> errors, string message)
        {
            if (errors.Count < MaxErrorsReported)
            {
                errors.Add(message);
            }
            else if (errors.Count == MaxErrorsReported)
            {
                errors.Add("There are more problems after these. Fix these first.");
            }
        }

        private static bool TryParseRole(string text, out CustomerUserRole role)
        {
            switch (text.ToLowerInvariant())
            {
                case "admin": role = CustomerUserRole.Admin; return true;
                case "user": role = CustomerUserRole.User; return true;
                default: role = default; return false;
            }
        }

        /// <summary>Splits one line on commas, keeping commas and doubled quotes inside "quoted" values.</summary>
        private static List<string> SplitLine(string line)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    cells.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            cells.Add(current.ToString());
            return cells;
        }
    }
}
