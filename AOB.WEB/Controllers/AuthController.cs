using System.Security.Claims;
using AOB.BAL.Common.Constants;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// Handles registration, email verification, login, logout and the forgot-password flow
    /// using cookie authentication. Every screen is the single Auth/Index view; the action
    /// only decides which form (view component) it shows via AuthPageViewModel.Mode.
    /// Business rules live in IAuthService; this controller only maps HTTP to service calls.
    /// </summary>
    [AllowAnonymous]
    public class AuthController(IAuthService authService, ILoggerFactory loggerFactory) : Controller
    {
        // ---------- Login ----------

        [HttpGet]
        /*public IActionResult Login(string? returnUrl = null)
            => Page(AuthMode.Login, p => p.Login.ReturnUrl = returnUrl);*/

        public IActionResult Login()
           => Page(AuthMode.Login);

        [EnableRateLimiting("auth-login")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.Login, p => p.Login = model);
            }

            var result = await authService.LoginAsync(new LoginDto { Email = model.Email, Password = model.Password });
            if (!result.Succeeded || result.Data is null)
            {
                Audit("LoginFailed", model.Email, result.Error);
                ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
                return Page(AuthMode.Login, p => p.Login = model);
            }

            // A temporary password does not sign the user in: no cookie is issued. The same page switches to the
            // change-password form, which carries the one-time grant from the service in a hidden field.
            if (result.Data.MustChangePassword)
            {
                Audit("TemporaryPasswordAccepted", model.Email);
                return Page(AuthMode.ChangeTemporaryPassword, p =>
                {
                    p.Reset.Email = result.Data.Email;
                    p.Reset.ResetToken = result.Data.PasswordChangeToken ?? string.Empty;
                    p.Info = "You signed in with a temporary password. Choose a new password, then sign in with it.";
                });
            }

            await SignInAsync(result.Data, model.RememberMe);
            Audit("LoginSucceeded", result.Data.Email, $"role={result.Data.UserType}");

            // Url.IsLocalUrl guards against open-redirect attacks.
            return !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }

        // Safe to call while signed out (it just clears the cookie), so it needs no [Authorize].
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Audit("Logout", User.FindFirst(ClaimTypes.Email)?.Value);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You're signed out.";
            return RedirectToAction(nameof(Login));
        }

        // ---------- Registration & email verification ----------

        [HttpGet]
        public IActionResult Register() => Page(AuthMode.Register);

        [EnableRateLimiting("auth-sensitive")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.Register, p => p.Register = model);
            }

            var result = await authService.RegisterDirectCustomerAsync(new RegisterDto
            {
                CompanyName = model.CompanyName,
                Email = model.Email,
                Password = model.Password
            });

            if (!result.Succeeded)
            {
                // A taken email is shown under the Email field, like the live check; other errors at the top.
                var field = result.Error == ValidationMessages.EmailInUse ? nameof(model.Email) : string.Empty;
                ModelState.AddModelError(field, result.Error ?? "Registration failed.");
                return Page(AuthMode.Register, p => p.Register = model);
            }

            return Page(AuthMode.CheckEmail, p => p.Email.Email = model.Email);
        }

        /// <summary>
        /// [Remote] check behind the sign-up form's Email field: true, or the message to show.
        /// Register re-checks on submit, so this is only a convenience.
        /// </summary>
        [EnableRateLimiting("auth-lookup")]
        [HttpGet]
        public async Task<IActionResult> IsEmailAvailable(string? email)
            => Json(await authService.IsEmailAvailableAsync(email ?? string.Empty) ? true : ValidationMessages.EmailInUse);

        /// <summary>Target of the link in the verification email; then sends the user to the login form.</summary>
        [EnableRateLimiting("auth-lookup")]
        [HttpGet]
        public IActionResult VerifyEmail(string? token)
            => string.IsNullOrWhiteSpace(token)
                ? RedirectToAction(nameof(Login))
                : View("ConfirmEmail", token);

        /// <summary>The verification itself. A GET never changes anything, so a mail scanner opening the link cannot consume it.</summary>
        [EnableRateLimiting("auth-lookup")]
        [HttpPost, ValidateAntiForgeryToken, ActionName(nameof(VerifyEmail))]
        public async Task<IActionResult> ConfirmVerifyEmail([FromForm] string? token)
        {
            var result = await authService.VerifyEmailAsync(token ?? string.Empty);
            Audit(result.Succeeded ? "EmailVerified" : "EmailVerificationFailed", null);

            if (result.Succeeded)
            {
                TempData["Success"] = result.Data
                    ? "Your email has been verified. We have emailed you a temporary password; sign in with it and choose a new password."
                    : "Your email has been verified. Please sign in.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult CheckEmail(string? email)
            => Page(string.IsNullOrWhiteSpace(email) ? AuthMode.ResendVerification : AuthMode.CheckEmail,
                p => p.Email.Email = email ?? string.Empty);

        [HttpGet]
        public IActionResult ResendVerification() => RedirectToAction(nameof(CheckEmail));

        [EnableRateLimiting("auth-sensitive")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.ResendVerification, p => p.Email = model);
            }

            await authService.ResendVerificationEmailAsync(model.Email);

            TempData["Success"] = "Verification email has been resent.";
            return Page(AuthMode.CheckEmail, p => p.Email.Email = model.Email);
        }

        // ---------- Forgot password: email -> OTP -> new password ----------

        /// <summary>Step 1: ask for the email.</summary>
        [HttpGet]
        public IActionResult ForgotPassword() => Page(AuthMode.ForgotPassword);

        [EnableRateLimiting("auth-sensitive")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.ForgotPassword, p => p.Email = model);
            }

            await authService.ForgotPasswordAsync(model.Email);
            Audit("PasswordResetRequested", model.Email);

            // Same message whether or not the account exists; move on to the OTP form.
            return Page(AuthMode.VerifyOtp, p => p.Otp.Email = model.Email);
        }

        /// <summary>Step 2: the OTP on its own. Only a correct code unlocks the password form.</summary>
        [EnableRateLimiting("auth-sensitive")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.VerifyOtp, p => p.Otp = model);
            }

            var result = await authService.VerifyPasswordResetOtpAsync(model.Email, model.Otp);
            if (!result.Succeeded || result.Data is null)
            {
                Audit("PasswordResetCodeRejected", model.Email);
                ModelState.AddModelError(string.Empty, result.Error ?? "Invalid code.");
                model.Otp = string.Empty;
                return Page(AuthMode.VerifyOtp, p => p.Otp = model);
            }

            // Code accepted: show the new-password form. The grant travels in a hidden field.
            return Page(AuthMode.ResetPassword, p =>
            {
                p.Reset.Email = model.Email;
                p.Reset.ResetToken = result.Data;
            });
        }

        /// <summary>Step 3: set the new password (requires the grant from step 2).</summary>
        [EnableRateLimiting("auth-sensitive")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.ResetPassword, p => p.Reset = model);
            }

            var result = await authService.ResetPasswordAsync(new ResetPasswordDto
            {
                Email = model.Email,
                ResetToken = model.ResetToken,
                NewPassword = model.NewPassword
            });

            if (!result.Succeeded)
            {
                // The grant is gone or expired, so the user has to start over.
                Audit("PasswordResetFailed", model.Email);
                TempData["Error"] = result.Error ?? "Password reset failed.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            Audit("PasswordResetCompleted", model.Email);

            TempData["Success"] = "Your password has been reset. Please sign in.";
            return RedirectToAction(nameof(Login));
        }

        // ---------- First sign-in with a temporary password ----------

        /// <summary>
        /// Sets the new password for a user who signed in with a temporary one (requires the grant issued by
        /// Login). Still no session: the user goes back to the sign-in form and signs in with the new password.
        /// </summary>
        [EnableRateLimiting("auth-sensitive")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeTemporaryPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Page(AuthMode.ChangeTemporaryPassword, p => p.Reset = model);
            }

            var result = await authService.ChangeTemporaryPasswordAsync(new ResetPasswordDto
            {
                Email = model.Email,
                ResetToken = model.ResetToken,
                NewPassword = model.NewPassword
            });

            if (!result.Succeeded)
            {
                Audit("TemporaryPasswordChangeFailed", model.Email);
                ModelState.AddModelError(string.Empty, result.Error ?? "Your password could not be changed.");
                return Page(AuthMode.ChangeTemporaryPassword, p => p.Reset = model);
            }

            Audit("TemporaryPasswordChanged", model.Email);

            TempData["Success"] = "Your password has been changed. Please sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // ---------- Helpers ----------

        /// <summary>
        /// Security event for the "AOB.Security" log category: what happened, to which account, from which address.
        /// Control characters are stripped from the user-supplied email so entries cannot be forged.
        /// </summary>
        private void Audit(string eventName, string? email, string? detail = null)
        {
            var safeEmail = new string((email ?? string.Empty).Where(c => !char.IsControl(c)).ToArray());
            loggerFactory.CreateLogger("AOB.Security").LogInformation(
                "SecurityEvent {Event} Account={Account} Ip={Ip} Detail={Detail}",
                eventName, safeEmail, HttpContext.Connection.RemoteIpAddress, detail);
        }

        /// <summary>Renders the single Auth page in the given mode.</summary>
        private ViewResult Page(AuthMode mode, Action<AuthPageViewModel>? configure = null)
        {
            var page = new AuthPageViewModel { Mode = mode };
            configure?.Invoke(page);
            return View("Index", page);
        }

        /// <summary>Builds the claims principal and issues the auth cookie.</summary>
        private async Task SignInAsync(AuthenticatedUserDto user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.DisplayName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.UserType.ToString())
            };

            // Extra claims let controllers scope data to the user's partner/customer.
            if (user.PartnerId.HasValue)
            {
                claims.Add(new Claim(AppClaimTypes.PartnerId, user.PartnerId.Value.ToString()));
            }
            if (user.CustomerId.HasValue)
            {
                claims.Add(new Claim(AppClaimTypes.CustomerId, user.CustomerId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = isPersistent });
        }
    }
}
