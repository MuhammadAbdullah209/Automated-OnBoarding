namespace AOB.BAL.Common.Helpers
{
    /// <summary>URL checks shared by the web layer (field message) and the service (final rule).</summary>
    public static class UrlValidator
    {
        /// <summary>
        /// A usable redirect URL: absolute, no embedded credentials, and https. Plain http is accepted
        /// only for localhost so it can be tried during development.
        /// </summary>
        public static bool IsValidRedirectUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)
                || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                || !string.IsNullOrEmpty(uri.UserInfo))
            {
                return false;
            }

            return uri.Scheme == Uri.UriSchemeHttps
                   || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
        }
    }
}
