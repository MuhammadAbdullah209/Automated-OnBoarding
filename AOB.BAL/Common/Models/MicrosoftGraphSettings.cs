namespace AOB.BAL.Common.Models
{
    /// <summary>
    /// Microsoft Graph (Entra ID app registration) settings used to send email, bound from the
    /// "MicrosoftGraph" section of configuration. The app registration needs the
    /// Mail.Send application permission with admin consent.
    /// </summary>
    public class MicrosoftGraphSettings
    {
        public const string SectionName = "MicrosoftGraph";

        public string TenantId { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;

        /// <summary>Keep this out of source control (user-secrets / environment / key vault).</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Mailbox the emails are sent from.</summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>True when every value needed to call Graph is present.</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret)
            && !string.IsNullOrWhiteSpace(SenderEmail);
    }
}
