using System.Globalization;

namespace AOB.BAL.Common.Constants
{
    /// <summary>One selectable value in a dropdown: a stable code that is stored, and a label shown to the user.</summary>
    public record SelectOption(string Code, string Label);

    /// <summary>
    /// Dropdown values for the customer Organisation &amp; Contacts form. Stored values are stable
    /// codes, so labels can change without touching data.
    /// </summary>
    public static class CustomerFormOptions
    {
        /// <summary>All countries as ISO 3166 two-letter codes (e.g. GB = United Kingdom), A-Z by name.</summary>
        public static IReadOnlyList<SelectOption> Countries { get; } = BuildCountries();

        /// <summary>Industry sectors. The label itself is stored in Customer.IndustrySector.</summary>
        public static IReadOnlyList<SelectOption> IndustrySectors { get; } = new[]
        {
            "Financial Services", "Insurance", "Retail & E-commerce", "Healthcare", "Telecommunications",
            "Utilities", "Travel & Hospitality", "Public Sector", "Technology", "Other"
        }.Select(s => new SelectOption(s, s)).ToList();

        public static IReadOnlyList<SelectOption> OperatingPatterns { get; } = new[]
        {
            new SelectOption("business_hours", "Business hours (Mon–Fri, 9:00–17:00)"),
            new SelectOption("extended_hours", "Extended hours (Mon–Fri, 8:00–20:00)"),
            new SelectOption("twenty_four_seven", "24/7"),
            new SelectOption("custom", "Custom / other")
        };

        /// <summary>IANA time zone ids (not fixed offsets, so daylight saving is handled).</summary>
        public static IReadOnlyList<SelectOption> TimeZones { get; } = new[]
        {
            new SelectOption("Europe/London", "Europe/London (GMT/BST)"),
            new SelectOption("Europe/Dublin", "Europe/Dublin (GMT/IST)"),
            new SelectOption("Europe/Paris", "Europe/Paris (CET/CEST)"),
            new SelectOption("Europe/Berlin", "Europe/Berlin (CET/CEST)"),
            new SelectOption("Africa/Cairo", "Africa/Cairo (EET/EEST)"),
            new SelectOption("Africa/Johannesburg", "Africa/Johannesburg (SAST)"),
            new SelectOption("America/New_York", "America/New_York (ET)"),
            new SelectOption("America/Chicago", "America/Chicago (CT)"),
            new SelectOption("America/Denver", "America/Denver (MT)"),
            new SelectOption("America/Los_Angeles", "America/Los_Angeles (PT)"),
            new SelectOption("America/Sao_Paulo", "America/Sao Paulo (BRT)"),
            new SelectOption("Pacific/Honolulu", "Pacific/Honolulu (HST)"),
            new SelectOption("Asia/Dubai", "Asia/Dubai (GST)"),
            new SelectOption("Asia/Karachi", "Asia/Karachi (PKT)"),
            new SelectOption("Asia/Kolkata", "Asia/Kolkata (IST)"),
            new SelectOption("Asia/Dhaka", "Asia/Dhaka (BST)"),
            new SelectOption("Asia/Kathmandu", "Asia/Kathmandu (NPT)"),
            new SelectOption("Asia/Bangkok", "Asia/Bangkok (ICT)"),
            new SelectOption("Asia/Shanghai", "Asia/Shanghai (CST)"),
            new SelectOption("Asia/Hong_Kong", "Asia/Hong Kong (HKT)"),
            new SelectOption("Asia/Singapore", "Asia/Singapore (SGT)"),
            new SelectOption("Asia/Seoul", "Asia/Seoul (KST)"),
            new SelectOption("Asia/Tokyo", "Asia/Tokyo (JST)"),
            new SelectOption("Australia/Perth", "Australia/Perth (AWST)"),
            new SelectOption("Australia/Adelaide", "Australia/Adelaide (ACST/ACDT)"),
            new SelectOption("Australia/Sydney", "Australia/Sydney (AEST/AEDT)"),
            new SelectOption("Pacific/Auckland", "Pacific/Auckland (NZST/NZDT)"),
            new SelectOption("UTC", "UTC")
        };

        public static bool IsValid(IReadOnlyList<SelectOption> options, string? code)
            => code is not null && options.Any(o => o.Code == code);

        public static string? LabelFor(IReadOnlyList<SelectOption> options, string? code)
            => options.FirstOrDefault(o => o.Code == code)?.Label ?? code;

        private static IReadOnlyList<SelectOption> BuildCountries()
        {
            var countries = new Dictionary<string, string>();

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var region = new RegionInfo(culture.Name);
                    // Keep real two-letter ISO codes; skips numeric regions such as "001" (World).
                    if (region.TwoLetterISORegionName.Length == 2 && region.TwoLetterISORegionName.All(char.IsLetter))
                    {
                        countries[region.TwoLetterISORegionName.ToUpperInvariant()] = region.EnglishName;
                    }
                }
                catch (ArgumentException)
                {
                    // Culture without a usable region; ignore.
                }
            }

            return countries
                .OrderBy(c => c.Value)
                .Select(c => new SelectOption(c.Key, c.Value))
                .ToList();
        }
    }
}
