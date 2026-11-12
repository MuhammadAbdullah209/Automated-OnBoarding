using AOB.WEB.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.ViewComponents
{
    // One small view component per form on the Auth page. Each simply renders its own
    // Views/Shared/Components/<Name>/Default.cshtml with the model the page passes in.
    // Used in Views/Auth/Index.cshtml, e.g. <vc:login-form model="Model.Login" />.

    /// <summary>Email + password sign-in form.</summary>
    public class LoginFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(LoginViewModel model) => View(model);
    }

    /// <summary>Create-account form (email + password).</summary>
    public class RegisterFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(RegisterViewModel model) => View(model);
    }

    /// <summary>Step 1 of forgot-password: ask for the email to send the OTP to.</summary>
    public class ForgotPasswordFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ForgotPasswordViewModel model) => View(model);
    }

    /// <summary>Ask for the email to re-send the verification link to.</summary>
    public class ResendVerificationFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ForgotPasswordViewModel model) => View(model);
    }

    /// <summary>Check your inbox + resend verification (prototype vCheckEmail).</summary>
    public class CheckEmailFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ForgotPasswordViewModel model) => View(model);
    }

    /// <summary>Step 2 of forgot-password: enter the OTP that was emailed.</summary>
    public class VerifyOtpFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(VerifyOtpViewModel model) => View(model);
    }

    /// <summary>Step 3 of forgot-password: choose a new password (shown only after the OTP is verified).</summary>
    public class ResetPasswordFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ResetPasswordViewModel model) => View(model);
    }

    /// <summary>New password + confirmation after signing in with a temporary password (no session yet).</summary>
    public class ChangeTemporaryPasswordFormViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ResetPasswordViewModel model) => View(model);
    }
}
