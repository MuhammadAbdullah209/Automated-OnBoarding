using AOB.BAL.Common.Constants;
using AOB.BAL.DTOs.Auth;
using AOB.BAL.DTOs.Partners;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.ViewModels.Admin;
using AOB.WEB.ViewModels.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>All partners and their customers, for internal users. Only internal users can create a partner; super admins view only.</summary>
    [Authorize(Roles = AppRoles.InternalAccess)]
    public class PartnersController(IPartnerService partnerService) : Controller
    {
        /// <summary>All partners, with All / Active / Inactive tabs and a name or email search (filtered in memory: the list is short).</summary>
        public async Task<IActionResult> Index(string? q, string? view)
        {
            var all = await partnerService.GetAllAsync();
            var search = q?.Trim();
            var listView = view?.ToLowerInvariant() is "active" or "inactive" ? view.ToLowerInvariant() : "all";

            ViewData["PartnerSearch"] = search;
            ViewData["PartnerView"] = listView;
            ViewData["PartnerCounts"] = new Dictionary<string, int>
            {
                ["all"] = all.Count,
                ["active"] = all.Count(p => p.IsActive),
                ["inactive"] = all.Count(p => !p.IsActive)
            };

            IEnumerable<PartnerSummaryDto> rows = all;
            if (listView != "all")
            {
                rows = rows.Where(p => p.IsActive == (listView == "active"));
            }
            if (!string.IsNullOrEmpty(search))
            {
                rows = rows.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || p.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            return View(rows.ToList());
        }

        /// <summary>
        /// One partner's details. Its customers are on the Customers page filtered to the partner
        /// (direct customers belong to PCIPAL).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var partner = await partnerService.GetAsync(id);
            return partner is null ? NotFound() : View(partner);
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> Edit(Guid id)
        {
            var partner = await partnerService.GetAsync(id);
            if (partner is null)
            {
                return NotFound();
            }

            ViewData["PartnerId"] = id;
            return View(new EditPartnerViewModel { Name = partner.Name, Email = partner.Email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> Edit(Guid id, EditPartnerViewModel model)
        {
            ViewData["PartnerId"] = id;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await partnerService.UpdateAsync(id, new UpdatePartnerDto { Name = model.Name, Email = model.Email });
            if (!result.Succeeded)
            {
                if (result.Error == "Partner not found.")
                {
                    return NotFound();
                }

                ModelState.AddModelError(nameof(model.Email), result.Error ?? "Could not update the partner.");
                return View(model);
            }

            TempData["Success"] = $"Partner \"{model.Name.Trim()}\" was updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Activates or deactivates a partner (and its login), then returns to the page it came from.</summary>
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> SetActive(Guid id, bool active, string? returnTo)
        {
            var result = await partnerService.SetActiveAsync(id, active);
            if (result.Succeeded)
            {
                TempData["Success"] = active ? "The partner was activated." : "The partner was deactivated and can no longer sign in.";
            }
            else
            {
                TempData["Error"] = result.Error ?? "Could not update the partner.";
            }

            return returnTo == nameof(Details)
                ? RedirectToAction(nameof(Details), new { id })
                : RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.InternalUser)]
        public IActionResult Create() => View(new CreateAccountViewModel());

        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await partnerService.CreateAsync(new CreateAccountDto
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password
            });
            if (!result.Succeeded)
            {
                ModelState.AddModelError(nameof(model.Email), result.Error ?? "Could not create the partner.");
                return View(model);
            }

            TempData["Success"] = $"Partner \"{model.Name}\" was created.";
            return RedirectToAction(nameof(Index));
        }
    }
}
