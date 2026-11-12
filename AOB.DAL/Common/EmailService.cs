using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.Interfaces.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Common
{
    /// <summary>
    /// Sends email through Microsoft Graph (POST /users/{sender}/sendMail) using the app's
    /// client-credentials flow. Settings come from the "MicrosoftGraph" configuration section.
    /// If those settings are incomplete, the email is written to the log instead of being sent
    /// so local development works without credentials.
    /// </summary>
    public class EmailService(
        MicrosoftGraphSettings settings,
        IEmailTemplateProvider templateProvider,
        IConfiguration configuration,
        ILogger<EmailService> logger) : IEmailService
    {
        private const string GraphScope = "https://graph.microsoft.com/.default";

        // Shared across instances so the access token cache below is effective.
        private static readonly HttpClient Http = new();
        private static readonly SemaphoreSlim TokenLock = new(1, 1);
        private static string? _cachedToken;
        private static DateTimeOffset _cachedTokenExpiry = DateTimeOffset.MinValue;

        public async Task SendAsync(string toAddress, string subject, string htmlBody)
        {
            if (!settings.IsConfigured)
            {
                // Verification links and one-time codes are in the body, so it is written to the log only on a developer machine.
                var isDevelopment = string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
                logger.LogWarning(
                    "MicrosoftGraph settings are incomplete, so the email was not sent. To: {To} | Subject: {Subject} | Body: {Body}",
                    toAddress, subject, isDevelopment ? htmlBody : "(withheld outside Development)");
                return;
            }

            var token = await GetAccessTokenAsync();

            var payload = new
            {
                message = new
                {
                    subject,
                    body = new { contentType = "HTML", content = htmlBody },
                    toRecipients = new[] { new { emailAddress = new { address = toAddress } } }
                },
                saveToSentItems = false
            };

            var url = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(settings.SenderEmail)}/sendMail";
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await Http.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Accepted && !response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Microsoft Graph sendMail failed ({(int)response.StatusCode}): {Truncate(error)}");
            }
        }

        public async Task SendTemplatedAsync(string toAddress, EmailTemplateType templateType, IDictionary<string, string> placeholders)
        {
            var template = await templateProvider.GetTemplateAsync(templateType);

            var subject = template.Subject;
            // The shared layout's logo needs an absolute URL, so it is filled here for every template.
            var baseUrl = (configuration["AppSettings:BaseUrl"] ?? string.Empty).TrimEnd('/');
            var body = template.Body.Replace("{{LogoUrl}}", WebUtility.HtmlEncode(baseUrl + "/images/pcipal-logo.png"));
            foreach (var (key, value) in placeholders)
            {
                var token = "{{" + key + "}}";
                subject = subject.Replace(token, value);
                // Values are encoded so user-supplied text can never inject markup into the email.
                body = body.Replace(token, WebUtility.HtmlEncode(value));
            }

            // Credentials and OTPs must never reach the log, so the not-configured fallback in SendAsync is not used for them.
            if (templateType is EmailTemplateType.InternalUserCredentials or EmailTemplateType.PasswordResetOtp && !settings.IsConfigured)
            {
                logger.LogWarning(
                    "MicrosoftGraph settings are incomplete, so the email was not sent. To: {To} | Subject: {Subject} | Body withheld (contains a secret).",
                    toAddress, subject);
                return;
            }

            await SendAsync(toAddress, subject, body);
        }

        private static string Truncate(string text) => text.Length <= 500 ? text : text[..500] + "...";

        /// <summary>Gets an app-only Graph token, reusing the cached one until shortly before it expires.</summary>
        private async Task<string> GetAccessTokenAsync()
        {
            if (_cachedToken is not null && _cachedTokenExpiry > DateTimeOffset.UtcNow.AddMinutes(2))
            {
                return _cachedToken;
            }

            await TokenLock.WaitAsync();
            try
            {
                if (_cachedToken is not null && _cachedTokenExpiry > DateTimeOffset.UtcNow.AddMinutes(2))
                {
                    return _cachedToken;
                }

                var tokenUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(settings.TenantId)}/oauth2/v2.0/token";
                using var response = await Http.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = settings.ClientId,
                    ["client_secret"] = settings.ClientSecret,
                    ["scope"] = GraphScope,
                    ["grant_type"] = "client_credentials"
                }));

                var json = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Could not get a Microsoft Graph token ({(int)response.StatusCode}): {Truncate(json)}");
                }

                using var doc = JsonDocument.Parse(json);
                _cachedToken = doc.RootElement.GetProperty("access_token").GetString()!;
                _cachedTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32());
                return _cachedToken;
            }
            finally
            {
                TokenLock.Release();
            }
        }
    }
}
