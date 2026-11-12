using Microsoft.AspNetCore.Http;

namespace AOB.WEB.Helpers
{
    /// <summary>Captures the current page for cancel/back links on multi-step flows.</summary>
    public static class NavigationReturnUrl
    {
        public static string Current(HttpRequest request)
        {
            var path = request.Path.Value ?? "/";
            var query = request.QueryString.Value ?? string.Empty;
            return path + query;
        }
    }
}
