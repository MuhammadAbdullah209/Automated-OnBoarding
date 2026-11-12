namespace AOB.WEB.Services
{
    /// <summary>
    /// The "HealthCheck" section of configuration. The health page has no sign-in: it answers only to the
    /// addresses listed here (exact IPs or CIDR ranges such as 10.0.0.0/24). Edits to appsettings apply without a restart.
    /// </summary>
    public class HealthCheckSettings
    {
        public const string SectionName = "HealthCheck";

        public bool Enabled { get; set; } = true;

        /// <summary>Callers allowed to open the page. Empty means nobody.</summary>
        public List<string> AllowedIps { get; set; } = [];
    }
}
