namespace AOB.BAL.Common.Helpers
{
    /// <summary>
    /// Reads the "portal URLs" text box of the Discovery screen: one URL per line. Blank lines and repeats
    /// are ignored; every URL must be an absolute http(s) address.
    /// </summary>
    public static class PortalUrlParser
    {
        public const int MaxUrls = 50;

        /// <summary>Returns the cleaned URLs, or an error message when something is wrong.</summary>
        public static (IReadOnlyList<string> Urls, string? Error) Parse(string? text)
        {
            var urls = new List<string>();
            var lines = (text ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (!Uri.TryCreate(line, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                    || string.IsNullOrEmpty(uri.Host))
                {
                    return (Array.Empty<string>(), $"\"{Shorten(line)}\" is not a valid URL. Enter each URL on its own line, starting with https://.");
                }

                if (!urls.Contains(line, StringComparer.OrdinalIgnoreCase))
                {
                    urls.Add(line);
                }
            }

            return urls.Count > MaxUrls
                ? (Array.Empty<string>(), $"Enter no more than {MaxUrls} URLs.")
                : (urls, null);
        }

        private static string Shorten(string value) => value.Length > 60 ? value[..60] + "…" : value;
    }
}
