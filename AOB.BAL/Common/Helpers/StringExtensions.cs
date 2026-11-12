namespace AOB.BAL.Common.Helpers
{
    /// <summary>
    /// Reusable string helpers shared across all layers.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Trims and upper-cases an email for UserAuth.NormalizedEmail so lookups and
        /// uniqueness checks are case-insensitive.
        /// </summary>
        public static string NormalizeEmail(this string? email)
            => (email ?? string.Empty).Trim().ToUpperInvariant();
    }
}
