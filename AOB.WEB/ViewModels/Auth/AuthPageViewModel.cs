namespace AOB.WEB.ViewModels.Auth
{
    /// <summary>Which form the single Auth page is currently showing.</summary>
    public enum AuthMode
    {
        Login,
        Register,
        ForgotPassword,
        ResendVerification,
        /// <summary>After registration or resend — “Check your inbox” (prototype vCheckEmail).</summary>
        CheckEmail,
        VerifyOtp,
        ResetPassword,

        /// <summary>Shown after a login with a temporary password; uses the Reset model.</summary>
        ChangeTemporaryPassword
    }

    /// <summary>
    /// Model of the single Auth page. Each form is a view component fed by one of these
    /// properties; Mode decides which one is rendered.
    /// </summary>
    public class AuthPageViewModel
    {
        public AuthMode Mode { get; set; } = AuthMode.Login;

        /// <summary>Optional informational line shown above the form (e.g. "code sent").</summary>
        public string? Info { get; set; }

        public LoginViewModel Login { get; set; } = new();

        public RegisterViewModel Register { get; set; } = new();

        /// <summary>Used by both the forgot-password and resend-verification forms.</summary>
        public ForgotPasswordViewModel Email { get; set; } = new();

        public VerifyOtpViewModel Otp { get; set; } = new();

        /// <summary>Used by both the reset-password and change-temporary-password forms.</summary>
        public ResetPasswordViewModel Reset { get; set; } = new();

        public string Title => Mode switch
        {
            AuthMode.Login => "Sign in",
            AuthMode.Register => "Create your onboarding account",
            AuthMode.ForgotPassword => "Reset your password",
            AuthMode.ResendVerification => "Send verification again",
            AuthMode.CheckEmail => "Check your inbox",
            AuthMode.VerifyOtp => "Enter the 6-digit code",
            AuthMode.ResetPassword => "Choose a new password",
            AuthMode.ChangeTemporaryPassword => "Choose a new password",
            _ => "Account"
        };
    }
}
