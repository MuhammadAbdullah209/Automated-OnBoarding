using AOB.BAL.Common.Constants;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>Lists, creates and updates internal users. Super admin only.</summary>
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public class InternalUsersController(IInternalUserService internalUserService) : Controller
    {
        public async Task<IActionResult> Index()
            => View(await internalUserService.GetAllAsync());

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await internalUserService.GetAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            // Only accounts still pending verification can be edited.
            if (user.EmailConfirmed)
            {
                TempData["Error"] = "This user has already verified their email, so their name and email can no longer be changed.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = id;
            return View(new EditInternalUserViewModel { FullName = user.FullName, Email = user.Email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditInternalUserViewModel model)
        {
            ViewData["UserId"] = id;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await internalUserService.UpdateAsync(id, model.FullName, model.Email);
            if (!result.Succeeded)
            {
                if (result.Error == "Internal user not found.")
                {
                    return NotFound();
                }

                // Verified in the meantime (or a hand-crafted post): nothing to fix on the form.
                if (result.Error?.StartsWith("This user has already verified") == true)
                {
                    TempData["Error"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(nameof(model.Email), result.Error ?? "Could not update the internal user.");
                return View(model);
            }

            if (result.Data)
            {
                TempData["Success"] = $"Internal user \"{model.FullName.Trim()}\" was updated. A new verification link was emailed to {model.Email.Trim()}; earlier links no longer work.";
            }
            else if (await internalUserService.GetAsync(id) is { IsActive: false })
            {
                TempData["Success"] = $"Internal user \"{model.FullName.Trim()}\" was updated. They are inactive, so no verification link was sent; activate them and use \"Resend verification\".";
            }
            else
            {
                TempData["Error"] = $"Internal user \"{model.FullName.Trim()}\" was updated, but the verification email could not be sent. Use \"Resend verification\" to try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Activates or deactivates an internal user, who then cannot sign in.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(Guid id, bool active)
        {
            var result = await internalUserService.SetActiveAsync(id, active);
            if (result.Succeeded)
            {
                TempData["Success"] = active ? "The internal user was activated." : "The internal user was deactivated and can no longer sign in.";
            }
            else
            {
                TempData["Error"] = result.Error ?? "Could not update the internal user.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>Re-sends the verification link to an internal user who has not verified their email yet.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerification(Guid id)
        {
            var result = await internalUserService.ResendVerificationAsync(id);
            if (result.Succeeded)
            {
                TempData["Success"] = "A new verification link was emailed. Earlier links no longer work.";
            }
            else
            {
                TempData["Error"] = result.Error ?? "Could not send the verification email.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Create() => View(new CreateInternalUserViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInternalUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await internalUserService.CreateAsync(new CreateInternalUserDto
            {
                FullName = model.FullName,
                Email = model.Email
            });
            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(model.Email), result.Error ?? "Could not create the internal user.");
                return View(model);
            }

            if (result.Data)
            {
                TempData["Success"] = $"Internal user \"{model.FullName.Trim()}\" was created. A verification link was emailed to {model.Email.Trim()}.";
            }
            else
            {
                TempData["Error"] = $"Internal user \"{model.FullName.Trim()}\" was created, but the verification email could not be sent. Use \"Resend verification\" to try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
