using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Helpers;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.ViewModels.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// The signed-in customer's profile overview and organisation &amp; contacts form.
    /// </summary>
    [Authorize(Roles = nameof(AOB.BAL.Common.Enums.UserType.Customer))]
    public class CustomerProfileController(
        ICustomerService customerService,
        ICustomerDocumentService customerDocumentService,
        ICustomerStatusService customerStatusService,
        ICustomerRepository customerRepository) : Controller
    {
        private static readonly HashSet<string> EditSections = new(StringComparer.OrdinalIgnoreCase)
        {
            "company", "pc", "tc", "hours", "partner"
        };

        [HttpGet]
        public async Task<IActionResult> Index(string? edit)
        {
            var customerId = GetCustomerId();
            var profile = await LoadProfileAsync();
            if (profile is null)
            {
                return NotFound();
            }

            await customerStatusService.RefreshDerivedStatusAsync(customerId);
            var customer = await customerRepository.GetByIdAsync(customerId);
            var documents = await customerDocumentService.GetChecklistAsync(customerId);

            var model = new CustomerProfileOverviewViewModel
            {
                Profile = profile,
                Status = customer?.Status ?? AOB.BAL.Common.Enums.CustomerStatus.Pending,
                Documents = documents,
                EditSection = edit is not null && EditSections.Contains(edit) ? edit.ToLowerInvariant() : null
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSection(string section, CustomerProfileDto posted)
        {
            section = section?.ToLowerInvariant() ?? "";
            if (!EditSections.Contains(section))
            {
                return BadRequest();
            }

            var profile = await LoadProfileAsync();
            if (profile is null)
            {
                return NotFound();
            }

            ApplySection(profile, section, posted);
            ModelState.Clear();
            ValidateSectionFields(profile, section);
            ValidateDropdowns(profile, section);

            if (!ModelState.IsValid)
            {
                var customer = await customerRepository.GetByIdAsync(GetCustomerId());
                var documents = await customerDocumentService.GetChecklistAsync(GetCustomerId());
                var model = new CustomerProfileOverviewViewModel
                {
                    Profile = profile,
                    Status = customer?.Status ?? AOB.BAL.Common.Enums.CustomerStatus.Pending,
                    Documents = documents,
                    EditSection = section
                };
                return View("Index", model);
            }

            var result = await customerService.SaveProfileAsync(GetCustomerId(), profile);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your details.");
                var customer = await customerRepository.GetByIdAsync(GetCustomerId());
                var documents = await customerDocumentService.GetChecklistAsync(GetCustomerId());
                return View("Index", new CustomerProfileOverviewViewModel
                {
                    Profile = profile,
                    Status = customer?.Status ?? AOB.BAL.Common.Enums.CustomerStatus.Pending,
                    Documents = documents,
                    EditSection = section
                });
            }

            TempData["SuccessTitle"] = "Saved";
            TempData["Success"] = $"{SectionTitle(section)} updated.";
            return RedirectToAction(nameof(Index), null, $"os-{section}");
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var profile = await LoadProfileAsync();
            if (profile is null)
            {
                return NotFound();
            }

            if (CustomerProfileCompletionRules.ShouldForceProfileEdit(profile))
            {
                return RedirectToAction(nameof(Index), new { edit = "company" });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerProfileDto model)
        {
            ValidateDropdowns(model);
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await customerService.SaveProfileAsync(GetCustomerId(), model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your details.");
                return View(model);
            }

            TempData["SuccessTitle"] = "Saved";
            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        private static string SectionTitle(string section) => section switch
        {
            "company" => "Company details",
            "pc" => "Primary project contact",
            "tc" => "Technical contact",
            "hours" => "Operating hours",
            "partner" => "Technology partner",
            _ => "Profile"
        };

        private static void ApplySection(CustomerProfileDto target, string section, CustomerProfileDto posted)
        {
            switch (section)
            {
                case "company":
                    target.CompanyName = posted.CompanyName;
                    target.CompanyRegistrationNumber = posted.CompanyRegistrationNumber;
                    target.RegisteredCountryCode = posted.RegisteredCountryCode;
                    target.IndustrySector = posted.IndustrySector;
                    target.CompanyWebsite = posted.CompanyWebsite;
                    break;
                case "pc":
                    target.PrimaryContactFullName = posted.PrimaryContactFullName;
                    target.PrimaryContactJobTitle = posted.PrimaryContactJobTitle;
                    target.PrimaryContactPhoneNumber = posted.PrimaryContactPhoneNumber;
                    break;
                case "tc":
                    target.TechnicalContactFullName = posted.TechnicalContactFullName;
                    target.TechnicalContactJobTitle = posted.TechnicalContactJobTitle;
                    target.TechnicalContactEmail = posted.TechnicalContactEmail;
                    target.TechnicalContactPhoneNumber = posted.TechnicalContactPhoneNumber;
                    break;
                case "hours":
                    target.OperatingPattern = posted.OperatingPattern;
                    target.PrimaryTimeZone = posted.PrimaryTimeZone;
                    break;
                case "partner":
                    target.HasTechnologyPartner = posted.HasTechnologyPartner;
                    break;
            }
        }

        private void ValidateSectionFields(CustomerProfileDto profile, string section)
        {
            var names = section switch
            {
                "company" => new[]
                {
                    nameof(CustomerProfileDto.CompanyName),
                    nameof(CustomerProfileDto.CompanyRegistrationNumber),
                    nameof(CustomerProfileDto.RegisteredCountryCode),
                    nameof(CustomerProfileDto.IndustrySector),
                    nameof(CustomerProfileDto.CompanyWebsite)
                },
                "pc" => new[]
                {
                    nameof(CustomerProfileDto.PrimaryContactFullName),
                    nameof(CustomerProfileDto.PrimaryContactJobTitle),
                    nameof(CustomerProfileDto.PrimaryContactEmail),
                    nameof(CustomerProfileDto.PrimaryContactPhoneNumber)
                },
                "tc" => new[]
                {
                    nameof(CustomerProfileDto.TechnicalContactFullName),
                    nameof(CustomerProfileDto.TechnicalContactJobTitle),
                    nameof(CustomerProfileDto.TechnicalContactEmail),
                    nameof(CustomerProfileDto.TechnicalContactPhoneNumber)
                },
                "hours" => new[]
                {
                    nameof(CustomerProfileDto.OperatingPattern),
                    nameof(CustomerProfileDto.PrimaryTimeZone)
                },
                "partner" => Array.Empty<string>(),
                _ => Array.Empty<string>()
            };

            foreach (var name in names)
            {
                var prop = typeof(CustomerProfileDto).GetProperty(name);
                if (prop is null) continue;
                var value = prop.GetValue(profile);
                var context = new ValidationContext(profile) { MemberName = name };
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateProperty(value, context, results))
                {
                    foreach (var r in results)
                    {
                        ModelState.AddModelError(name, r.ErrorMessage ?? "Invalid value.");
                    }
                }
            }
        }

        private async Task<CustomerProfileDto?> LoadProfileAsync()
        {
            var result = await customerService.GetProfileAsync(GetCustomerId());
            if (!result.Succeeded || result.Data is null)
            {
                return null;
            }

            result.Data.PrimaryContactEmail ??= User.FindFirstValue(ClaimTypes.Email);
            return result.Data;
        }

        private void ValidateDropdowns(CustomerProfileDto model, string? sectionOnly = null)
        {
            void Country()
            {
                if (!CustomerFormOptions.IsValid(CustomerFormOptions.Countries, model.RegisteredCountryCode?.ToUpperInvariant()))
                {
                    ModelState.AddModelError(nameof(model.RegisteredCountryCode), "Select a valid country.");
                }
            }

            void Sector()
            {
                if (!string.IsNullOrEmpty(model.IndustrySector)
                    && !CustomerFormOptions.IsValid(CustomerFormOptions.IndustrySectors, model.IndustrySector))
                {
                    ModelState.AddModelError(nameof(model.IndustrySector), "Select a valid industry sector.");
                }
            }

            void Pattern()
            {
                if (!string.IsNullOrEmpty(model.OperatingPattern)
                    && !CustomerFormOptions.IsValid(CustomerFormOptions.OperatingPatterns, model.OperatingPattern))
                {
                    ModelState.AddModelError(nameof(model.OperatingPattern), "Select a valid operating pattern.");
                }
            }

            void TimeZone()
            {
                if (!string.IsNullOrEmpty(model.PrimaryTimeZone)
                    && !CustomerFormOptions.IsValid(CustomerFormOptions.TimeZones, model.PrimaryTimeZone))
                {
                    ModelState.AddModelError(nameof(model.PrimaryTimeZone), "Select a valid timezone.");
                }
            }

            if (sectionOnly is null)
            {
                Country();
                Sector();
                Pattern();
                TimeZone();
                return;
            }

            switch (sectionOnly)
            {
                case "company":
                    Country();
                    Sector();
                    break;
                case "hours":
                    Pattern();
                    TimeZone();
                    break;
            }
        }

        private Guid GetCustomerId()
            => Guid.Parse(User.FindFirst(AppClaimTypes.CustomerId)!.Value);
    }
}
