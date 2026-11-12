namespace AOB.BAL.Common.Templates
{
    /// <summary>
    /// Email templates kept as strings for now. Placeholders use {{Name}} syntax and are
    /// filled by IEmailService. These will later move to the database behind IEmailTemplateProvider.
    /// Every body shares the same layout: table-based, inline CSS, 600px rounded card, light blue
    /// header with the logo, footer. {{LogoUrl}} is filled by IEmailService for every email.
    /// </summary>
    public static class EmailTemplates
    {
        private const string Navy = "#0b1f3a";
        private const string Accent = "#2457b0";
        private const string Text = "#1f2933";
        private const string Muted = "#3d5a80";
        private const string Border = "#d6e0f0";
        private const string Tint = "#f3f6fb";
        private const string HeaderBlue = "#bfd4f6";
        private const string Mono = "'Courier New',Courier,monospace";

        private const string P = "margin:0 0 16px 0;font-size:15px;line-height:24px;color:" + Text + ";";

        /// <summary>Every subject reads "PCI Pal Onboarding | Purpose".</summary>
        private const string SubjectPrefix = "PCI Pal Onboarding | ";

        // Placeholders: {{ConsumerName}}, {{VerificationLink}}, {{ExpiryDuration}}
        public const string EmailVerificationSubject = SubjectPrefix + "Verify Your Email Address";

        public static readonly string EmailVerificationBody = Layout(
            "Verify your email address",
            Greeting("{{ConsumerName}}") +
            Para("Thank you for registering with PCI Pal.") +
            Para("Please verify your email address by selecting the button below. This helps us confirm that the email address belongs to you.") +
            Button("Verify Email", "{{VerificationLink}}") +
            FallbackLink("{{VerificationLink}}") +
            Para("This link expires in <strong>{{ExpiryDuration}}</strong>.") +
            Note("If you did not create this account, you can ignore this email. Do not share verification links with anyone.") +
            SignOff());

        // Placeholders: {{ConsumerName}}, {{OTP}}, {{OTPExpiryDuration}}, {{SupportContact}}
        public const string PasswordResetOtpSubject = SubjectPrefix + "Identification Code";

        public static readonly string PasswordResetOtpBody = Layout(
            "Identification Code",
            Greeting("{{ConsumerName}}") +
            Para("We received a request to reset the password for your account.") +
            Para("Use the passcode below to continue:") +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 16px 0;\"><tr>" +
            "<td align=\"center\" style=\"background-color:" + Tint + ";border:1px solid " + Border + ";border-radius:8px;padding:18px 16px;" +
            "font-family:" + Mono + ";font-size:30px;font-weight:bold;letter-spacing:8px;color:" + Navy + ";\">{{OTP}}</td>" +
            "</tr></table>" +
            Para("This passcode expires in <strong>{{OTPExpiryDuration}}</strong>. For your security, do not share this passcode with anyone.") +
            Note("If you did not request a password reset, you can ignore this email. If you remain concerned, contact {{SupportContact}}.") +
            SignOff());

        // Placeholders: {{ConsumerName}}, {{ApplicationReference}}, {{InformationRequestSummary}},
        // {{ConsumerPortalLink}}, {{SupportContact}}.
        // {{ResponseDeadline}} is not used yet: there is no deadline setting, and the PDF requirement is to
        // omit the line rather than invent a date. Add it as a separate paragraph once a deadline exists.
        public const string InformationRequestedSubject = SubjectPrefix + "Additional Information Required";

        public static readonly string InformationRequestedBody = Layout(
            "Additional information required",
            Greeting("{{ConsumerName}}") +
            Para("We have reviewed your PCI Pal application and need some additional information before we can continue the review.") +
            Details("Request details",
                DetailRow("Application reference", "{{ApplicationReference}}", monospace: true) +
                DetailRow("Information requested", "{{InformationRequestSummary}}")) +
            Para("Please sign in to the secure consumer portal using the link below to review the request and submit your response.") +
            Button("Open Consumer Portal", "{{ConsumerPortalLink}}") +
            FallbackLink("{{ConsumerPortalLink}}") +
            Note("If you need assistance, contact {{SupportContact}}.") +
            SignOff());

        // Placeholders: {{ContactName}}, {{ProjectName}}, {{Link}}
        public const string UsersCsvRequestedSubject = SubjectPrefix + "Users List Required for {{ProjectName}}";

        public static readonly string UsersCsvRequestedBody = Layout(
            "Users list required",
            Greeting("{{ContactName}}") +
            Para("To create accounts for the project <strong>{{ProjectName}}</strong> we need a list of your users.") +
            Para("Please upload a CSV file with three columns: <strong>name, email, role</strong>. The role is either <strong>admin</strong> or <strong>user</strong>.") +
            Button("Upload the users", "{{Link}}") +
            FallbackLink("{{Link}}") +
            Note("If you have any questions, just reply to this email.") +
            SignOff());

        // Placeholders: {{ContactName}}, {{ProjectName}}, {{Link}}
        public const string ProjectCompletedSubject = SubjectPrefix + "{{ProjectName}} Is Complete";

        public static readonly string ProjectCompletedBody = Layout(
            "Project complete",
            Greeting("{{ContactName}}") +
            Para("Your project <strong>{{ProjectName}}</strong> is now complete.") +
            Button("View the project", "{{Link}}") +
            FallbackLink("{{Link}}") +
            Note("Thank you for working with PCI Pal. If you have any questions, just reply to this email.") +
            SignOff());

        // Placeholders: {{FullName}}, {{Email}}, {{TemporaryPassword}}, {{LoginUrl}}
        public const string InternalUserCredentialsSubject = SubjectPrefix + "Your Temporary Login Credentials";

        public static readonly string InternalUserCredentialsBody = Layout(
            "Your login details",
            Greeting("{{FullName}}") +
            Para("Your email address has been successfully verified. Your account is now ready for login.") +
            Details("Your temporary login credentials",
                DetailRow("Email", "{{Email}}") +
                DetailRow("Temporary password", "{{TemporaryPassword}}", monospace: true) +
                DetailRow("Login URL", "<a href=\"{{LoginUrl}}\" style=\"color:" + Accent + ";text-decoration:underline;word-break:break-all;\">{{LoginUrl}}</a>")) +
            Para("Please log in using these credentials and change your temporary password immediately after your first login.") +
            Note("For security reasons, do not share your temporary password with anyone.") +
            SignOff());

        private static string Greeting(string namePlaceholder) => Para("Hello " + namePlaceholder + ",");

        private static string Para(string html) => "<p style=\"" + P + "\">" + html + "</p>";

        private static string SignOff() =>
            "<p style=\"margin:28px 0 0 0;font-size:15px;line-height:24px;color:" + Text + ";\">Regards,<br>" +
            "<strong style=\"color:" + Navy + ";\">PCI Pal Onboarding</strong></p>";

        private static string Button(string label, string hrefPlaceholder) =>
            "<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:8px 0 20px 0;\"><tr>" +
            "<td style=\"border-radius:8px;background-color:" + Accent + ";\">" +
            "<a href=\"" + hrefPlaceholder + "\" target=\"_blank\" style=\"display:inline-block;padding:14px 30px;font-size:15px;" +
            "font-weight:bold;color:#ffffff;text-decoration:none;border-radius:8px;\">" + label + "</a>" +
            "</td></tr></table>";

        /// <summary>Plain URL under a button, for clients that block or mangle buttons.</summary>
        private static string FallbackLink(string hrefPlaceholder) =>
            "<p style=\"margin:0 0 20px 0;font-size:13px;line-height:20px;color:" + Muted + ";\">" +
            "If the button doesn't work, copy and paste this link into your browser:<br>" +
            "<a href=\"" + hrefPlaceholder + "\" style=\"color:" + Accent + ";text-decoration:underline;word-break:break-all;\">" + hrefPlaceholder + "</a></p>";

        /// <summary>Tinted, bordered box for help and security lines.</summary>
        private static string Note(string html) =>
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"margin:0 0 8px 0;\"><tr>" +
            "<td style=\"background-color:" + Tint + ";border:1px solid " + Border + ";border-radius:8px;padding:14px 16px;" +
            "font-size:14px;line-height:22px;color:" + Text + ";\">" + html + "</td></tr></table>";

        /// <summary>Bordered card with a bold heading row followed by label/value rows.</summary>
        private static string Details(string heading, string rows) =>
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" " +
            "style=\"margin:0 0 20px 0;border:1px solid " + Border + ";border-radius:8px;border-collapse:separate;\">" +
            "<tr><td style=\"padding:14px 16px;font-size:14px;font-weight:bold;color:" + Navy + ";\">" + heading + "</td></tr>" +
            rows +
            "</table>";

        private static string DetailRow(string label, string valueHtml, bool monospace = false) =>
            "<tr><td style=\"padding:12px 16px 14px 16px;border-top:1px solid " + Border + ";\">" +
            "<div style=\"font-size:11px;line-height:16px;font-weight:bold;letter-spacing:1px;text-transform:uppercase;color:" + Muted + ";\">" + label + "</div>" +
            "<div style=\"margin-top:4px;font-size:15px;line-height:22px;color:" + Text + ";" +
            (monospace ? "font-family:" + Mono + ";" : string.Empty) + "\">" + valueHtml + "</div>" +
            "</td></tr>";

        /// <summary>
        /// Shared shell: tinted background, centred 600px rounded white card, light blue header with the
        /// PCI Pal logo (alt text shows if images are blocked), title, content and footer.
        /// </summary>
        private static string Layout(string title, string content) =>
            "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>" + title + "</title></head>" +
            "<body style=\"margin:0;padding:0;background-color:#eef2f8;\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"background-color:#eef2f8;\"><tr>" +
            "<td align=\"center\" style=\"padding:28px 12px;\">" +
            "<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" " +
            "style=\"max-width:600px;background-color:#ffffff;border:1px solid " + Border + ";border-radius:12px;border-collapse:separate;" +
            "font-family:Arial,Helvetica,sans-serif;\">" +
            "<tr><td style=\"background-color:" + HeaderBlue + ";padding:22px 32px;border-radius:12px 12px 0 0;\">" +
            "<img src=\"{{LogoUrl}}\" width=\"140\" alt=\"PCI Pal\" style=\"display:block;width:140px;max-width:140px;height:auto;border:0;" +
            "font-size:22px;font-weight:bold;color:" + Navy + ";\">" +
            "</td></tr>" +
            "<tr><td style=\"padding:32px 32px 28px 32px;\">" +
            "<h1 style=\"margin:0 0 20px 0;font-size:22px;line-height:30px;font-weight:bold;color:" + Navy + ";\">" + title + "</h1>" +
            content +
            "</td></tr>" +
            "<tr><td style=\"padding:16px 32px;border-top:1px solid " + Border + ";background-color:#f8fafd;border-radius:0 0 12px 12px;" +
            "font-size:12px;line-height:18px;color:" + Muted + ";\">" +
            "This is an automated message about your PCI Pal Onboarding account." +
            "</td></tr>" +
            "</table></td></tr></table></body></html>";
    }
}
