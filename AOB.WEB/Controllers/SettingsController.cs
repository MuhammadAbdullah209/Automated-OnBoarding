using System.Security.Claims;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.ViewModels.Settings;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// Account settings for any signed-in user (covered by the global [Authorize] filter).
    /// Separate from AuthController because that one is [AllowAnonymous] as a whole.
    /// </summary>
    public class SettingsController(IAuthService authService) : Controller
    {
        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(ChangePassword));

        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(new ChangePasswordViewModel());
            }

            // Whose password changes is decided only by the auth cookie, never by posted data.
            var result = await authService.ChangePasswordAsync(new ChangePasswordDto
            {
                UserAuthId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword
            });

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Your password could not be changed.");
                return View(new ChangePasswordViewModel());
            }

            // Post/Redirect/Get: the success page is a fresh, empty form.
            TempData["Success"] = "Your password has been changed.";
            return RedirectToAction(nameof(ChangePassword));
        }
    }
}
