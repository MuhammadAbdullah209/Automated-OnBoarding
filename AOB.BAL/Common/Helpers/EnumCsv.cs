namespace AOB.BAL.Common.Helpers
{
    /// <summary>
    /// Reads and writes the multiple-choice answers that are stored as comma-separated enum
    /// names (e.g. "ClickToPay,SpeakToPay").
    /// </summary>
    public static class EnumCsv
    {
        /// <summary>Turns selected values into the stored text; null when nothing is selected.</summary>
        public static string? Join<T>(IEnumerable<T>? values) where T : struct, Enum
        {
            var names = (values ?? Enumerable.Empty<T>()).Distinct().Select(v => v.ToString()).ToList();
            return names.Count == 0 ? null : string.Join(',', names);
        }

        /// <summary>Turns stored text back into values; names that no longer exist are skipped.</summary>
        public static List<T> Parse<T>(string? csv) where T : struct, Enum
        {
            var result = new List<T>();
            if (string.IsNullOrWhiteSpace(csv))
            {
                return result;
            }

            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<T>(part, ignoreCase: false, out var value) && Enum.IsDefined(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }
    }
}
