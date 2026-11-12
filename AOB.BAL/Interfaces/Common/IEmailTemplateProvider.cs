using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;

namespace AOB.BAL.Interfaces.Common
{
    /// <summary>
    /// Supplies email templates. The current implementation reads string constants;
    /// a database-backed implementation can replace it later with no caller changes.
    /// </summary>
    public interface IEmailTemplateProvider
    {
        Task<EmailTemplate> GetTemplateAsync(EmailTemplateType templateType);
    }
}
