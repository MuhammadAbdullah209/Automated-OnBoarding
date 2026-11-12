using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Common;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Common
{
    /// <summary>
    /// Applies pending migrations and creates the starter logins from configuration:
    ///  - "Seed:PcipalPartner" (Email, Password): the default PCIPAL partner and its login. A partner
    ///    needs a UserAuth row, which needs a password hash, so both are created here. Without it,
    ///    direct customer sign-up fails.
    ///  - "Seed:InternalAdmin" (Email, Password, FullName): a super admin who sees everything and creates internal users.
    /// A section that is missing is skipped with a warning.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbSeeder));

            // Applying migrations needs schema-change rights. Set "Database:MigrateOnStartup" to false where the app runs
            // as a least-privileged login and run the migrations from the deployment pipeline instead.
            if (configuration.GetValue("Database:MigrateOnStartup", true))
            {
                await context.Database.MigrateAsync();
            }

            await SeedPcipalPartnerAsync(context, configuration, hasher, logger);
            await SeedInternalAdminAsync(context, configuration, hasher, logger);

            // Customer statuses are re-checked against the current onboarding rules.
            var statusService = scope.ServiceProvider.GetRequiredService<AOB.BAL.Interfaces.Services.ICustomerStatusService>();
            var onboardedIds = await context.Customers.Where(c => c.Status != CustomerStatus.Disabled).Select(c => c.Id).ToListAsync();
            foreach (var customerId in onboardedIds)
            {
                await statusService.RefreshDerivedStatusAsync(customerId);
            }
        }

        private static async Task SeedPcipalPartnerAsync(
            OnboardingDbContext context, IConfiguration configuration, IPasswordHasher hasher, ILogger logger)
        {
            var (email, password) = ReadCredentials(configuration, "Seed:PcipalPartner");
            if (email is null || password is null)
            {
                logger.LogWarning("Seed:PcipalPartner is not configured; the PCIPAL partner was not created.");
                return;
            }

            var normalizedEmail = email.NormalizeEmail();
            if (await context.UserAuths.AnyAsync(u => u.UserType == UserType.Partner && u.NormalizedEmail == normalizedEmail))
            {
                return;
            }

            context.UserAuths.Add(new UserAuth
            {
                Email = email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = hasher.Hash(password),
                UserType = UserType.Partner,
                EmailConfirmed = true,
                // The configured password is only a starting point: the first sign-in must replace it.
                MustChangePassword = true,
                Partner = new Partner
                {
                    Id = PartnerConstants.PcipalPartnerId,
                    Name = PartnerConstants.PcipalPartnerName
                }
            });

            await context.SaveChangesAsync();
            logger.LogInformation("Created default PCIPAL partner login for {Email}.", email);
        }

        private static async Task SeedInternalAdminAsync(
            OnboardingDbContext context, IConfiguration configuration, IPasswordHasher hasher, ILogger logger)
        {
            var (email, password) = ReadCredentials(configuration, "Seed:InternalAdmin");
            if (email is null || password is null)
            {
                logger.LogWarning("Seed:InternalAdmin is not configured; no super admin was created.");
                return;
            }

            var normalizedEmail = email.NormalizeEmail();
            if (await context.UserAuths.AnyAsync(u => u.UserType == UserType.SuperAdmin && u.NormalizedEmail == normalizedEmail))
            {
                return;
            }

            context.UserAuths.Add(new UserAuth
            {
                Email = email.Trim(),
                NormalizedEmail = normalizedEmail,
                PasswordHash = hasher.Hash(password),
                UserType = UserType.SuperAdmin,
                EmailConfirmed = true,
                MustChangePassword = true,
                InternalUser = new InternalUser
                {
                    FullName = configuration["Seed:InternalAdmin:FullName"] ?? "Super Admin"
                }
            });

            await context.SaveChangesAsync();
            logger.LogInformation("Created super admin login for {Email}.", email);
        }

        private static (string? Email, string? Password) ReadCredentials(IConfiguration configuration, string sectionPath)
        {
            var section = configuration.GetSection(sectionPath);
            var email = section["Email"];
            var password = section["Password"];
            return string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
                ? (null, null)
                : (email, password);
        }
    }
}
