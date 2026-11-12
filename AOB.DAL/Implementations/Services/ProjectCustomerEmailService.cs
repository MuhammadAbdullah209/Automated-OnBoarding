using AOB.BAL.Common.Enums;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Sends project lifecycle emails to the customer's primary contact (or login email), using
    /// <see cref="BAL.Common.Templates.EmailTemplates"/> via <see cref="IEmailService"/>.
    /// </summary>
    public class ProjectCustomerEmailService(
        OnboardingDbContext context,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ProjectCustomerEmailService> logger) : IProjectCustomerEmailService
    {
        public Task SendUsersCsvRequestedAsync(Guid customerId, Guid projectId, string projectName)
            => SendAsync(customerId, projectId, projectName, EmailTemplateType.UsersCsvRequested, BuildUsersCsvLink);

        public Task SendProjectCompletedAsync(Guid customerId, Guid projectId, string projectName)
            => SendAsync(customerId, projectId, projectName, EmailTemplateType.ProjectCompleted, BuildProjectDetailsLink);

        private async Task SendAsync(
            Guid customerId,
            Guid projectId,
            string projectName,
            EmailTemplateType templateType,
            Func<string, Guid, bool, string> buildLink)
        {
            try
            {
                var customer = await context.Customers.AsNoTracking()
                    .Where(c => c.Id == customerId)
                    .Select(c => new
                    {
                        c.PrimaryContactEmail,
                        c.PrimaryContactFullName,
                        LoginEmail = c.Auth != null ? c.Auth.Email : null,
                        HasLogin = c.UserAuthId != null
                    })
                    .FirstOrDefaultAsync();

                var to = !string.IsNullOrWhiteSpace(customer?.PrimaryContactEmail)
                    ? customer!.PrimaryContactEmail
                    : customer?.LoginEmail;
                if (string.IsNullOrWhiteSpace(to))
                {
                    logger.LogWarning(
                        "No contact email for customer {CustomerId}; {Template} for project {ProjectId} was not sent.",
                        customerId, templateType, projectId);
                    return;
                }

                var baseUrl = (configuration["AppSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
                var link = buildLink(baseUrl, projectId, customer!.HasLogin);
                var contactName = string.IsNullOrWhiteSpace(customer.PrimaryContactFullName) ? "there" : customer.PrimaryContactFullName;

                await emailService.SendTemplatedAsync(to, templateType, new Dictionary<string, string>
                {
                    ["ContactName"] = contactName,
                    ["ProjectName"] = projectName,
                    ["Link"] = link
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not send {Template} email for project {ProjectId}.", templateType, projectId);
            }
        }

        private static string BuildUsersCsvLink(string baseUrl, Guid projectId, bool hasLogin)
            => hasLogin ? $"{baseUrl}/Projects/Details/{projectId}#users-card" : baseUrl;

        private static string BuildProjectDetailsLink(string baseUrl, Guid projectId, bool hasLogin)
            => hasLogin ? $"{baseUrl}/Projects/Details/{projectId}" : baseUrl;
    }
}
