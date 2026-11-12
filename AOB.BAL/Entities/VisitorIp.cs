using System.ComponentModel.DataAnnotations;

namespace AOB.BAL.Entities
{
    /// <summary>
    /// One IP address that has used the site (or that a super admin blocked by hand): where it is, how often it
    /// came, and whether it is blocked. One row per address; a blocked address gets a 403 on every request.
    /// </summary>
    public class VisitorIp
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>IPv4 or IPv6 text, normalised (an IPv4-mapped IPv6 address is stored as IPv4). Unique.</summary>
        [Required, MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        // Location, from the geolocation lookup (null until looked up, or when the lookup failed).
        [MaxLength(100)] public string? Country { get; set; }
        [MaxLength(10)] public string? CountryCode { get; set; }
        [MaxLength(100)] public string? Region { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(200)] public string? Isp { get; set; }

        /// <summary>When a location lookup was last tried (failed lookups are retried after a day).</summary>
        public DateTimeOffset? GeoLookedUpAt { get; set; }

        public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Requests seen from this address (page requests, not images or scripts).</summary>
        public long HitCount { get; set; }

        [MaxLength(300)] public string? LastPath { get; set; }

        /// <summary>Name of the signed-in user on the last request, if any.</summary>
        [MaxLength(200)] public string? LastUserName { get; set; }

        public bool IsBlocked { get; set; }
        [MaxLength(300)] public string? BlockedReason { get; set; }
        public DateTimeOffset? BlockedAt { get; set; }
        [MaxLength(200)] public string? BlockedByName { get; set; }
    }
}
