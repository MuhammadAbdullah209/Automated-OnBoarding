using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.DTOs.Customers;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.ViewModels.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// Lets partner users create and list their customers, view customer profiles, and read
    /// customer project details. A partner is always taken from the signed-in user's claim; internal staff can open any customer read-only.
    /// </summary>
    [Authorize(Roles = nameof(UserType.Partner) + "," + AppRoles.InternalAccess)]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly ICustomerDocumentService _customerDocumentService;
        private readonly IProjectDocumentService _projectDocumentService;
        private readonly IPartnerService _partnerService;

        public CustomersController(
            ICustomerService customerService,
            ICustomerDocumentService customerDocumentService,
            IProjectDocumentService projectDocumentService,
            IPartnerService partnerService)
        {
            _customerService = customerService;
            _customerDocumentService = customerDocumentService;
            _projectDocumentService = projectDocumentService;
            _partnerService = partnerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, bool? hasLogin, bool? isDirect, Guid? partnerId, int page = 1)
        {
            if (User.IsInRole(nameof(UserType.Partner)))
            {
                var result = await _customerService.GetPartnerCustomersPagedAsync(GetPartnerId(), new CustomerListQuery
                {
                    Search = q,
                    HasLogin = hasLogin,
                    Page = page,
                    PageSize = CustomerListQuery.DefaultPageSize
                });

                return View(new CustomersIndexViewModel
                {
                    Result = result,
                    Search = q,
                    HasLogin = hasLogin,
                    IsAdminList = false,
                    TabCounts = new Dictionary<string, int>
                    {
                        ["all"] = await PartnerCountAsync(null),
                        ["login"] = await PartnerCountAsync(true),
                        ["nologin"] = await PartnerCountAsync(false)
                    }
                });
            }

            if (User.IsInRole(nameof(UserType.SuperAdmin)) || User.IsInRole(nameof(UserType.InternalUser)))
            {
                var result = await _customerService.GetAllCustomersPagedAsync(new CustomerListQuery
                {
                    Search = q,
                    HasLogin = hasLogin,
                    IsDirect = isDirect,
                    PartnerId = partnerId,
                    Page = page,
                    PageSize = CustomerListQuery.DefaultPageSize
                });

                var partners = await _partnerService.GetAllAsync();

                return View(new CustomersIndexViewModel
                {
                    Result = result,
                    Search = q,
                    HasLogin = hasLogin,
                    IsDirect = isDirect,
                    PartnerId = partnerId,
                    IsAdminList = true,
                    PartnerFilters = partners,
                    TabCounts = new Dictionary<string, int>
                    {
                        ["all"] = await AdminCountAsync(null),
                        ["direct"] = await AdminCountAsync(true),
                        ["partner"] = await AdminCountAsync(false)
                    }
                });
            }

            return Forbid();
        }

        // Tab counts on the customer list: totals per tab, independent of the search and other filters.
        private async Task<int> PartnerCountAsync(bool? hasLogin)
            => (await _customerService.GetPartnerCustomersPagedAsync(GetPartnerId(), new CustomerListQuery
            {
                HasLogin = hasLogin,
                PageSize = 1
            })).TotalCount;

        private async Task<int> AdminCountAsync(bool? isDirect)
            => (await _customerService.GetAllCustomersPagedAsync(new CustomerListQuery
            {
                IsDirect = isDirect,
                PageSize = 1
            })).TotalCount;

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            if (await ResolvePartnerIdAsync(id) is not { } partnerId)
            {
                return NotFound();
            }

            var result = await _customerService.GetPartnerCustomerDetailsAsync(partnerId, id);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            return View(CustomerProfileOverviewViewModel.FromPartnerDetail(result.Data));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDisabled(Guid id, bool disabled)
        {
            if (await ResolvePartnerIdAsync(id) is null)
            {
                return NotFound();
            }

            var result = await _customerService.SetCustomerDisabledAsync(id, disabled);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? (disabled ? "Customer has been disabled." : "Customer has been re-enabled.")
                : result.Error;

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>Legacy URL — same project detail case view as <see cref="ProjectsController.Details"/>.</summary>
        [HttpGet]
        public IActionResult Project(Guid id, Guid projectId)
            => RedirectToAction(nameof(ProjectsController.Details), "Projects", new { customerId = id, id = projectId });

        /// <summary>Legacy URL — same as <see cref="ProjectsController.Summary"/>.</summary>
        [HttpGet]
        public IActionResult ProjectSummary(Guid id, Guid projectId)
            => RedirectToAction(nameof(ProjectsController.Summary), "Projects", new { customerId = id, id = projectId });

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(Guid id, Guid documentTypeId)
        {
            if (await ResolvePartnerIdAsync(id) is null)
            {
                return NotFound();
            }

            var result = await _customerDocumentService.DownloadAsync(id, documentTypeId);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadProjectDocument(Guid id, Guid projectId, ProjectDocumentType type)
        {
            if (await ResolvePartnerIdAsync(id) is null)
            {
                return NotFound();
            }

            var result = await _projectDocumentService.DownloadAsync(id, projectId, type);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadProjectApiDocumentation(Guid id, Guid projectId)
        {
            if (await ResolvePartnerIdAsync(id) is null)
            {
                return NotFound();
            }

            var result = await _projectDocumentService.DownloadAsync(id, projectId, ProjectDocumentType.ApiDocumentation);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpGet]
        [Authorize(Roles = nameof(UserType.Partner))]
        public IActionResult Create() => View(new CreateCustomerViewModel());

        [HttpPost]
        [Authorize(Roles = nameof(UserType.Partner))]
        public async Task<IActionResult> Create(CreateCustomerViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _customerService.CreatePartnerCustomerAsync(GetPartnerId(), model.CompanyName);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not create customer.");
                return View(model);
            }

            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        private Guid GetPartnerId()
            => Guid.Parse(User.FindFirst(AppClaimTypes.PartnerId)!.Value);

        /// <summary>
        /// The partner whose customer this is, or null when the signed-in user may not see it.
        /// A partner only sees its own customers; internal staff see any customer.
        /// </summary>
        private async Task<Guid?> ResolvePartnerIdAsync(Guid customerId)
        {
            if (User.IsInRole(nameof(UserType.Partner)))
            {
                var partnerId = GetPartnerId();
                return await _customerService.PartnerOwnsCustomerAsync(partnerId, customerId) ? partnerId : null;
            }

            return await _customerService.GetOwningPartnerIdAsync(customerId);
        }
    }
}

