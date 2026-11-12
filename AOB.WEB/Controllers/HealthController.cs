using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Models;
using AOB.DAL.OnboardingContext;
using AOB.WEB.Services;
using AOB.WEB.ViewModels.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// Operational health page. No sign-in: access is limited to the addresses in HealthCheck:AllowedIps.
    /// Add ?format=json for a machine-readable answer (HTTP 503 when unhealthy).
    /// </summary>
    [AllowAnonymous]
    [HealthIpFilter]
    [Route("health")]
    public class HealthController(
        OnboardingDbContext db,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        MicrosoftGraphSettings graph) : Controller
    {
        private bool _detailed;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? format, CancellationToken cancellationToken)
        {
            // Anyone on the allow-listed addresses gets the status. Host, OS, database, path and error details are for a
            // signed-in super admin only, so an allow-list mistake (e.g. a proxy address) does not leak them.
            _detailed = User.IsInRole(AppRoles.SuperAdmin);
            const string Hidden = "(super admin only)";

            var process = Process.GetCurrentProcess();
            var model = new HealthViewModel
            {
                CheckedAtUtc = DateTime.UtcNow,
                Environment = _detailed ? environment.EnvironmentName : Hidden,
                AppVersion = _detailed ? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown" : Hidden,
                Runtime = _detailed ? RuntimeInformation.FrameworkDescription : Hidden,
                MachineName = _detailed ? System.Environment.MachineName : Hidden,
                OperatingSystem = _detailed ? RuntimeInformation.OSDescription : Hidden,
                BaseUrl = _detailed ? configuration["AppSettings:BaseUrl"] ?? string.Empty : Hidden,
                Uptime = DateTime.Now - process.StartTime,
                MemoryMb = process.WorkingSet64 / (1024 * 1024),
                CallerIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                ConnectionString = _detailed ? MaskConnectionString(configuration.GetConnectionString("OnboardingDb")) : Hidden,
                DatabaseProvider = _detailed ? db.Database.ProviderName ?? "unknown" : Hidden,
                DatabaseType = _detailed ? DescribeProvider(db.Database.ProviderName) : Hidden
            };

            await CheckDatabaseAsync(model, cancellationToken);
            CheckFileStorage(model);
            CheckEmail(model);
            CheckAppSettings(model);

            model.Overall = model.Checks.Any(c => c.State == HealthState.Unhealthy) ? HealthState.Unhealthy
                : model.Checks.Any(c => c.State == HealthState.Degraded) ? HealthState.Degraded
                : HealthState.Healthy;

            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = model.Overall == HealthState.Unhealthy ? 503 : 200;
                return Json(model, new System.Text.Json.JsonSerializerOptions
                {
                    Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                });
            }

            return View(model);
        }

        private async Task CheckDatabaseAsync(HealthViewModel model, CancellationToken cancellationToken)
        {
            try
            {
                var timer = Stopwatch.StartNew();
                var canConnect = await db.Database.CanConnectAsync(cancellationToken);
                timer.Stop();

                if (!canConnect)
                {
                    model.Checks.Add(new("Database connection", HealthState.Unhealthy, "Could not connect to the database."));
                    return;
                }

                model.Checks.Add(new("Database connection", HealthState.Healthy, $"Connected in {timer.ElapsedMilliseconds} ms."));

                var connection = db.Database.GetDbConnection();
                model.DatabaseName = _detailed ? connection.Database : string.Empty;
                await db.Database.OpenConnectionAsync(cancellationToken);
                try
                {
                    model.DatabaseServerVersion = _detailed ? connection.ServerVersion : string.Empty;
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }

                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                model.Checks.Add(pending.Count == 0
                    ? new("Database migrations", HealthState.Healthy, "All migrations are applied.")
                    : new("Database migrations", HealthState.Degraded, _detailed ? $"{pending.Count} pending: {string.Join(", ", pending)}" : $"{pending.Count} pending."));
            }
            catch (Exception ex)
            {
                model.Checks.Add(new("Database connection", HealthState.Unhealthy, _detailed ? ex.Message : "The database check failed."));
            }
        }

        private void CheckFileStorage(HealthViewModel model)
        {
            var root = configuration["FileStorage:RootPath"] ?? Path.Combine("App_Data", "Uploads");
            var path = Path.IsPathRooted(root) ? root : Path.Combine(environment.ContentRootPath, root);
            try
            {
                Directory.CreateDirectory(path);
                var probe = Path.Combine(path, $".health-{Guid.NewGuid():N}");
                System.IO.File.WriteAllText(probe, "ok");
                System.IO.File.Delete(probe);
                model.Checks.Add(new("File storage", HealthState.Healthy, _detailed ? $"{path} is writable." : "Writable."));
            }
            catch (Exception ex)
            {
                model.Checks.Add(new("File storage", HealthState.Unhealthy, _detailed ? $"{path}: {ex.Message}" : "Not writable."));
            }
        }

        private void CheckEmail(HealthViewModel model)
        {
            model.Checks.Add(graph.IsConfigured
                ? new("Email (Microsoft Graph)", HealthState.Healthy, _detailed ? $"Configured, sending as {graph.SenderEmail}." : "Configured.")
                : new("Email (Microsoft Graph)", HealthState.Degraded, "Not fully configured; emails cannot be sent."));
        }

        private void CheckAppSettings(HealthViewModel model)
        {
            model.Checks.Add(string.IsNullOrWhiteSpace(configuration["AppSettings:BaseUrl"])
                ? new("Application base URL", HealthState.Degraded, "AppSettings:BaseUrl is not set; links in emails will be wrong.")
                : new("Application base URL", HealthState.Healthy, _detailed ? model.BaseUrl : "Configured."));
        }

        private static string DescribeProvider(string? provider) => provider switch
        {
            null => "Unknown",
            _ when provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) => "Microsoft SQL Server",
            _ when provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) => "PostgreSQL",
            _ when provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) => "SQLite",
            _ when provider.Contains("MySql", StringComparison.OrdinalIgnoreCase) => "MySQL",
            _ => provider
        };

        /// <summary>The connection string as configured, with the password replaced.</summary>
        private static string MaskConnectionString(string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "(not configured)";
            }

            try
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
                foreach (var key in new[] { "Password", "Pwd" })
                {
                    if (builder.ContainsKey(key))
                    {
                        builder[key] = "********";
                    }
                }
                return builder.ConnectionString;
            }
            catch (ArgumentException)
            {
                return "(connection string could not be parsed)";
            }
        }
    }
}
