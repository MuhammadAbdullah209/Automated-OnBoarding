using System.Net;
using System.Text.Json;

namespace AOB.WEB.Services
{
    /// <summary>The "IpGeolocation" section of configuration.</summary>
    public class IpGeolocationSettings
    {
        public const string SectionName = "IpGeolocation";

        /// <summary>Off: no address leaves the server and locations stay empty.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Lookup URL; {ip} is replaced by the address. Must answer the ipwho.is JSON shape.</summary>
        public string UrlTemplate { get; set; } = "https://ipwho.is/{ip}";
    }

    public record GeoLocation(string? Country, string? CountryCode, string? Region, string? City, string? Isp);

    public interface IIpGeolocator
    {
        /// <summary>Where an address is, or null when it cannot be told (lookup off, failed, unknown address).</summary>
        Task<GeoLocation?> LookupAsync(string ip, CancellationToken cancellationToken = default);
    }

    /// <summary>Looks an address up with an external service (ipwho.is by default, over HTTPS).</summary>
    public class HttpIpGeolocator(HttpClient http, Microsoft.Extensions.Options.IOptionsMonitor<IpGeolocationSettings> options,
        ILogger<HttpIpGeolocator> logger) : IIpGeolocator
    {
        public async Task<GeoLocation?> LookupAsync(string ip, CancellationToken cancellationToken = default)
        {
            if (!IPAddress.TryParse(ip, out var address))
            {
                return null;
            }

            // Private and loopback addresses have no location, and are never sent to a third party.
            if (IPAddress.IsLoopback(address) || IsPrivate(address))
            {
                return new GeoLocation("Local network", null, null, null, null);
            }

            var settings = options.CurrentValue;
            if (!settings.Enabled)
            {
                return null;
            }

            try
            {
                var url = settings.UrlTemplate.Replace("{ip}", Uri.EscapeDataString(ip));
                using var response = await http.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                var root = doc.RootElement;
                if (root.TryGetProperty("success", out var ok) && ok.ValueKind == JsonValueKind.False)
                {
                    return null;
                }

                string? Text(JsonElement e, string name) =>
                    e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

                string? isp = null;
                if (root.TryGetProperty("connection", out var connection) && connection.ValueKind == JsonValueKind.Object)
                {
                    isp = Text(connection, "isp") ?? Text(connection, "org");
                }

                return new GeoLocation(Text(root, "country"), Text(root, "country_code"), Text(root, "region"), Text(root, "city"), isp);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                logger.LogWarning("IP location lookup failed for {Ip}: {Message}", ip, ex.Message);
                return null;
            }
        }

        private static bool IsPrivate(IPAddress ip)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
            {
                return true;
            }

            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return false;
            }

            var b = ip.GetAddressBytes();
            return b[0] == 10
                || (b[0] == 172 && b[1] is >= 16 and <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254);
        }
    }
}
