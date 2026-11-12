namespace AOB.WEB.ViewModels.Health
{
    public enum HealthState { Healthy, Degraded, Unhealthy }

    public record HealthCheckItem(string Name, HealthState State, string Detail);

    /// <summary>Everything the health page shows.</summary>
    public class HealthViewModel
    {
        public HealthState Overall { get; set; }
        public DateTime CheckedAtUtc { get; set; }
        public List<HealthCheckItem> Checks { get; set; } = [];

        // Database
        public string DatabaseType { get; set; } = string.Empty;
        public string DatabaseProvider { get; set; } = string.Empty;
        public string DatabaseServerVersion { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        /// <summary>The configured connection string with any password masked.</summary>
        public string ConnectionString { get; set; } = string.Empty;

        // Application
        public string Environment { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public string Runtime { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public TimeSpan Uptime { get; set; }
        public long MemoryMb { get; set; }
        public string CallerIp { get; set; } = string.Empty;
    }
}
