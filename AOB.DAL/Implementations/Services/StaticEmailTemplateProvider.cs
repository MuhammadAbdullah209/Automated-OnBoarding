using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.Common.Templates;
using AOB.BAL.Interfaces.Common;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Reads templates from the EmailTemplates string constants.
    /// Replace with a database-backed IEmailTemplateProvider when templates move to the DB.
    /// </summary>
    public class StaticEmailTemplateProvider : IEmailTemplateProvider
    {
        public Task<EmailTemplate> GetTemplateAsync(EmailTemplateType templateType)
        {
            var template = templateType switch
            {
                EmailTemplateType.EmailVerification =>
                    new EmailTemplate(EmailTemplates.EmailVerificationSubject, EmailTemplates.EmailVerificationBody),
                EmailTemplateType.PasswordResetOtp =>
                    new EmailTemplate(EmailTemplates.PasswordResetOtpSubject, EmailTemplates.PasswordResetOtpBody),
                EmailTemplateType.InformationRequested =>
                    new EmailTemplate(EmailTemplates.InformationRequestedSubject, EmailTemplates.InformationRequestedBody),
                EmailTemplateType.UsersCsvRequested =>
                    new EmailTemplate(EmailTemplates.UsersCsvRequestedSubject, EmailTemplates.UsersCsvRequestedBody),
                EmailTemplateType.InternalUserCredentials =>
                    new EmailTemplate(EmailTemplates.InternalUserCredentialsSubject, EmailTemplates.InternalUserCredentialsBody),
                EmailTemplateType.ProjectCompleted =>
                    new EmailTemplate(EmailTemplates.ProjectCompletedSubject, EmailTemplates.ProjectCompletedBody),
                _ => throw new ArgumentOutOfRangeException(nameof(templateType), templateType, "Unknown email template.")
            };

            return Task.FromResult(template);
        }
    }
}
