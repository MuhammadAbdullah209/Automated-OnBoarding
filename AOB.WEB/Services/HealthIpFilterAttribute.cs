using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace AOB.WEB.Services
{
    /// <summary>
    /// Lets a request through only when the caller address is in HealthCheck:AllowedIps. Everyone else gets a 404,
    /// so the existence of the page is not revealed.
    /// </summary>
    public class HealthIpFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var settings = context.HttpContext.RequestServices
                .GetRequiredService<IOptionsMonitor<HealthCheckSettings>>().CurrentValue;

            var caller = context.HttpContext.Connection.RemoteIpAddress;
            if (!settings.Enabled || caller is null || !IsAllowed(caller, settings.AllowedIps))
            {
                context.Result = new NotFoundResult();
            }
        }

        public static bool IsAllowed(IPAddress caller, IEnumerable<string> allowed)
        {
            if (caller.IsIPv4MappedToIPv6)
            {
                caller = caller.MapToIPv4();
            }

            foreach (var entry in allowed)
            {
                var text = entry?.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                if (text.Contains('/'))
                {
                    if (IPNetwork.TryParse(text, out var network) && network.Contains(caller))
                    {
                        return true;
                    }
                }
                else if (IPAddress.TryParse(text, out var ip))
                {
                    if (ip.IsIPv4MappedToIPv6)
                    {
                        ip = ip.MapToIPv4();
                    }
                    if (ip.Equals(caller))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
