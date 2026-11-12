using AOB.BAL.Common.Enums;

namespace AOB.BAL.Interfaces.Common
{
    /// <summary>
    /// The single place the application sends email from. Swap the implementation
    /// (SMTP, SendGrid, Graph...) without touching callers.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>Sends an HTML email.</summary>
        Task SendAsync(string toAddress, string subject, string htmlBody);

        /// <summary>
        /// Loads the template, replaces {{Placeholder}} tokens (values are HTML-encoded) and sends it.
        /// </summary>
        Task SendTemplatedAsync(string toAddress, EmailTemplateType templateType, IDictionary<string, string> placeholders);
    }
}
