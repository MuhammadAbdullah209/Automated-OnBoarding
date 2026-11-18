using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.Helpers;
using AOB.WEB.ViewModels.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AOB.WEB.Controllers
{
    /// <summary>
    /// The signed-in customer's onboarding projects and their four setup screens (Discovery,
    /// Technical Environment, Payment Configuration, Adaptive Configuration) plus the Readiness
    /// Check. The customer always comes from the sign-in claim, so a customer only ever reaches
    /// their own projects. Each screen needs the one before it to be saved first.
    /// Partners and internal staff work on a customer's projects through Customers/{customerId}/Projects;
    /// internal staff also get an all-customers list and a create form with a customer picker.
    /// </summary>
    [Authorize(Roles = nameof(UserType.Customer) + "," + nameof(UserType.Partner) + "," + AppRoles.InternalAccess)]
    public class ProjectsController(
        IProjectService projectService,
        IProjectConfigurationService configService,
        IProjectDocumentService documentService,
        IPaymentGatewayService gatewayService,
        ICustomerService customerService,
        IFlowLaunchService flowLaunchService,
        IActivityLogService activityLog,
        IProjectInformationService informationService,
        IProjectUsersService usersService) : Controller
    {
        // ---------- List, create, project page ----------

        [HttpGet]
        public async Task<IActionResult> Index(string? q, ProjectStatus? status, ProjectStep? step, Guid? customer,
            DateOnly? from, DateOnly? to, ProjectListView? view, int page = 1)
        {
            var listView = view ?? ProjectListView.All;
            var query = new ProjectListQuery
            {
                Search = q,
                Status = status,
                Step = step,
                CreatedFrom = from,
                CreatedTo = to,
                View = listView,
                Page = page,
                PageSize = ProjectListQuery.DefaultPageSize
            };

            var model = new ProjectsIndexViewModel
            {
                Search = q,
                Status = status,
                Step = step,
                CreatedFrom = from,
                CreatedTo = to,
                View = listView
            };

            if (IsOverview)
            {
                // Internal staff: every customer's projects. Partners: their own customers' projects.
                // Either can narrow it to one customer.
                query.CustomerId = customer;
                query.PartnerId = OverviewPartnerId;
                model.Result = await projectService.GetAllProjectsPagedAsync(query);
                model.IsAdminList = true;
                model.CustomerId = customer;
                model.CustomerFilters = await customerService.GetCustomerOptionsAsync(OverviewPartnerId);
                // Tab counts, as on the customer list, for the same customer / partner scope.
                if (model.Result.TotalCount <= ProjectListQuery.MaxPageSize)
                {
                    var allRows = await projectService.GetAllProjectsPagedAsync(new ProjectListQuery
                    {
                        CustomerId = customer,
                        PartnerId = OverviewPartnerId,
                        PageSize = ProjectListQuery.MaxPageSize
                    });
                    if (allRows.TotalCount <= ProjectListQuery.MaxPageSize)
                    {
                        model.ViewCounts = Enum.GetValues<ProjectListView>()
                            .ToDictionary(
                                v => v,
                                v => allRows.Items.Count(p => ProjectOnboardingPresentation.MatchesListView(
                                    p.Status, p.InformationRequestStatus, v, p.AwaitingUsersCsv)));
                    }
                }
            }
            else
            {
                model.Result = await projectService.GetProjectsPagedAsync(GetCustomerId(), query);
                if (model.Result.TotalCount <= ProjectListQuery.MaxPageSize)
                {
                    var allRows = await projectService.GetProjectsPagedAsync(GetCustomerId(), new ProjectListQuery
                    {
                        PageSize = ProjectListQuery.MaxPageSize
                    });
                    model.ViewCounts = Enum.GetValues<ProjectListView>()
                        .ToDictionary(
                            v => v,
                            v => allRows.Items.Count(p => ProjectOnboardingPresentation.MatchesListView(
                                p.Status, p.InformationRequestStatus, v, p.AwaitingUsersCsv)));
                }
            }

            return View(model);
        }

        /// <summary>
        /// Internal staff and partners reach this without a customer in the URL and pick one on the form;
        /// <paramref name="customer"/> preselects it (e.g. from a customer's page).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(Guid? customer, string? returnUrl)
            => await CreateViewAsync(new CreateProjectDto(), customer, returnUrl);

        [HttpPost]
        public async Task<IActionResult> Create(CreateProjectDto model, Guid? customer, string? returnUrl)
        {
            var staffPicksCustomer = IsOverview;
            if (staffPicksCustomer && (customer is not Guid picked || !await CanPickCustomerAsync(picked)))
            {
                ModelState.AddModelError(nameof(customer), "Select a customer.");
            }

            if (!ModelState.IsValid)
            {
                return await CreateViewAsync(model, customer, returnUrl);
            }

            var customerId = staffPicksCustomer ? customer!.Value : GetCustomerId();
            var result = await projectService.CreateAsync(customerId, model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the project.");
                return await CreateViewAsync(model, customer, returnUrl);
            }

            // Straight on to the first screen.
            TempData["Success"] = $"Project created successfully";
            return staffPicksCustomer
                ? RedirectToAction(nameof(Discovery), new { customerId, id = result.Data })
                : RedirectToAction(nameof(Discovery), new { id = result.Data });
        }

        private async Task<IActionResult> CreateViewAsync(CreateProjectDto model, Guid? customer, string? returnUrl)
        {
            if (IsOverview)
            {
                ViewData["CustomerOptions"] = await customerService.GetCustomerOptionsAsync(OverviewPartnerId);
                ViewData["SelectedCustomerId"] = customer;
            }

            var cancelUrl = ResolveCreateCancelUrl(returnUrl, customer);
            ViewData["CreateCancelUrl"] = cancelUrl;
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                ViewData["CreateReturnUrl"] = returnUrl;
            }

            return View("Create", model);
        }

        private string ResolveCreateCancelUrl(string? returnUrl, Guid? customer)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return customer is Guid cid
                ? Url.Action(nameof(Index), new { customer = cid })!
                : Url.Action(nameof(Index))!;
        }

        /// <summary>Staff may pick any existing customer; a partner only one of their own.</summary>
        private async Task<bool> CanPickCustomerAsync(Guid customerId)
            => OverviewPartnerId is Guid partnerId
                ? await customerService.PartnerOwnsCustomerAsync(partnerId, customerId)
                : await customerService.GetOwningPartnerIdAsync(customerId) is not null;

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var customerId = GetCustomerId();
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.DeletionScheduledFor is not null)
            {
                return LeaveScheduledProject(id);
            }

            // Live standard projects use the Status hub (Provisioning), not the pre-submit overview.
            if (project.Status == ProjectStatus.Completed && project.BuildType != BuildType.Custom)
            {
                var liveProv = await projectService.GetProvisioningInfoAsync(customerId, id);
                if (liveProv.Data?.StepsComplete == true)
                {
                    return RedirectToAction(nameof(Provisioning), ProjectRoute(id));
                }
            }

            var full = await configService.GetFullDetailsAsync(customerId, id);
            if (full.Data is null) return NotFound();

            var profile = await customerService.GetProfileAsync(customerId);
            ViewData["ProjectDocuments"] = await documentService.ListForProjectAsync(customerId, id);
            ViewData["ProjectDocumentsProjectId"] = id;
            ViewData["ProjectDocumentsInline"] = true;
            ViewData["ProjectActivity"] = await activityLog.GetForProjectAsync(customerId, id);
            ViewData["CustomerUsers"] = await usersService.GetUsersAsync(customerId, id);
            var provisioning = await projectService.GetProvisioningInfoAsync(customerId, id);
            ViewData["ProvisioningStepsComplete"] = provisioning.Data?.StepsComplete == true;
            ViewData["AccountCreated"] = provisioning.Data?.AccountCreated == true;
            if (full.Data.Project.BuildType == BuildType.Custom)
            {
                ViewData["InformationRequests"] = await informationService.GetRequestsAsync(customerId, id);
                if (User.IsInRole(AppRoles.InternalUser))
                {
                    ViewData["InformationOptions"] = await informationService.GetOptionsAsync(customerId, id);
                }
            }
            else
            {
                ViewData["InformationRequests"] = Array.Empty<InformationRequestDto>();
                ViewData["InformationOptions"] = Array.Empty<InformationRequestOptionDto>();
            }

            return View(new AOB.WEB.ViewModels.Projects.ProjectDetailsViewModel
            {
                Details = full.Data,
                Profile = profile.Succeeded ? profile.Data : null
            });
        }

        /// <summary>Everything the customer entered for the project, in one read-only page.</summary>
        [HttpGet]
        public async Task<IActionResult> Summary(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();

            var result = await configService.GetFullDetailsAsync(GetCustomerId(), id);
            return result.Data is null ? NotFound() : View(result.Data);
        }

        /// <summary>The project's full activity history (the Status page shows only the latest few).</summary>
        [HttpGet]
        public async Task<IActionResult> Activity(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();

            ViewData["ProjectActivity"] = await activityLog.GetForProjectAsync(GetCustomerId(), id);
            return View(project);
        }

        // ---------- Step 1: Business & Use-Case Discovery ----------

        [HttpGet]
        public async Task<IActionResult> Discovery(Guid id)
        {
            if (await LoadProjectAsync(id) is null) return NotFound();

            var result = await configService.GetDiscoveryAsync(GetCustomerId(), id);
            return result.Data is null ? NotFound() : View(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Discovery(Guid id, ProjectDiscoveryDto model)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.IsSubmitted) return SubmittedRedirect(id);

            // Embedded: the portal URLs are needed, one per line.
            if (model.CrmPaymentExperience == CrmPaymentExperience.EmbeddedIframe)
            {
                var portal = PortalUrlParser.Parse(model.PortalUrls);
                if (portal.Error is not null)
                {
                    ModelState.AddModelError(nameof(model.PortalUrls), portal.Error);
                }
                else if (portal.Urls.Count == 0)
                {
                    ModelState.AddModelError(nameof(model.PortalUrls), ValidationMessages.Required);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await configService.SaveDiscoveryAsync(GetCustomerId(), id, model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save.");
                return View(model);
            }

            return RedirectAfterStepSave(id, nameof(TechnicalEnvironment));
        }

        // ---------- Step 2: Technical Environment ----------

        [HttpGet]
        public async Task<IActionResult> TechnicalEnvironment(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (!IsStaff && !project.HasDiscovery) return NeedsStep(id, nameof(Discovery), "Business & Use-Case Discovery");

            var result = await configService.GetTechnicalEnvironmentAsync(GetCustomerId(), id);
            return result.Data is null ? NotFound() : View(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> TechnicalEnvironment(Guid id, ProjectTechnicalEnvironmentDto model)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.IsSubmitted) return SubmittedRedirect(id);
            if (!project.HasDiscovery) return NeedsStep(id, nameof(Discovery), "Business & Use-Case Discovery");

            // The CRM question only exists for CRM projects; check it here so the message shows by the field.
            if (project.CrmSystem == AOB.BAL.Common.Enums.CrmSystem.Crm && model.CrmPlatform is null)
            {
                ModelState.AddModelError(nameof(model.CrmPlatform), ValidationMessages.Required);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await configService.SaveTechnicalEnvironmentAsync(GetCustomerId(), id, model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save.");
                return View(model);
            }

            return RedirectAfterStepSave(id, nameof(PaymentConfiguration));
        }

        // ---------- Step 3: Payment Configuration ----------

        [HttpGet]
        public async Task<IActionResult> PaymentConfiguration(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (!IsStaff && !project.HasTechnicalEnvironment) return NeedsStep(id, nameof(TechnicalEnvironment), "Technical Environment");

            var result = await configService.GetPaymentConfigurationAsync(GetCustomerId(), id);
            return result.Data is null ? NotFound() : await PaymentViewAsync(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> PaymentConfiguration(Guid id, ProjectPaymentConfigurationDto model)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.IsSubmitted) return SubmittedRedirect(id);
            if (!project.HasTechnicalEnvironment) return NeedsStep(id, nameof(TechnicalEnvironment), "Technical Environment");

            // With more than one merchant account the customer says how PCI Pal tells them apart.
            if (model.TotalMerchantAccounts is > 1)
            {
                if (model.MerchantIdentification is null)
                {
                    ModelState.AddModelError(nameof(model.MerchantIdentification), ValidationMessages.Required);
                }
                else if (model.MerchantIdentification == MerchantIdentification.Other
                         && string.IsNullOrWhiteSpace(model.MerchantIdentificationOther))
                {
                    ModelState.AddModelError(nameof(model.MerchantIdentificationOther), ValidationMessages.Required);
                }
            }

            if (!ModelState.IsValid)
            {
                return await PaymentViewAsync(model);
            }

            var result = await configService.SavePaymentConfigurationAsync(GetCustomerId(), id, model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save.");
                return await PaymentViewAsync(model);
            }

            return RedirectAfterStepSave(id, nameof(GatewayConfiguration));
        }

        // ---------- Step 4: Adaptive Configuration (gateway) ----------

        [HttpGet]
        public async Task<IActionResult> GatewayConfiguration(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (!IsStaff && !project.HasPaymentConfiguration) return NeedsStep(id, nameof(PaymentConfiguration), "Payment Configuration");

            var result = await configService.GetGatewayConfigurationAsync(GetCustomerId(), id);
            return result.Data is null ? NotFound() : await GatewayViewAsync(id, result.Data);
        }

        // The upload field sits in the same form as the rest of the screen, so the request is multipart.
        [HttpPost]
        [RequestSizeLimit(50_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
        public async Task<IActionResult> GatewayConfiguration(Guid id, ProjectGatewayConfigurationDto model, IFormFile? apiDocumentation)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.IsSubmitted) return SubmittedRedirect(id);
            if (!project.HasPaymentConfiguration) return NeedsStep(id, nameof(PaymentConfiguration), "Payment Configuration");

            // Web Callback: accept API documentation. It is stored straight away, so the file is kept
            // even if another field on the page still needs fixing.
            if (model.PostTransactionMethods.Contains(PostTransactionMethod.WebCallback) && apiDocumentation is { Length: > 0 })
            {
                await using var content = apiDocumentation.OpenReadStream();
                var upload = await documentService.UploadAsync(GetCustomerId(), id, ProjectDocumentType.ApiDocumentation,
                    apiDocumentation.FileName, apiDocumentation.ContentType, apiDocumentation.Length, content);
                if (!upload.Succeeded)
                {
                    ModelState.AddModelError("ApiDocumentation", upload.Error ?? "Could not upload the file.");
                }
            }

            // Redirect: the customer must say where to send people afterwards.
            if (model.PostTransactionMethods.Contains(PostTransactionMethod.Redirect)
                && !UrlValidator.IsValidRedirectUrl(model.RedirectUrl))
            {
                ModelState.AddModelError(nameof(model.RedirectUrl),
                    string.IsNullOrWhiteSpace(model.RedirectUrl)
                        ? ValidationMessages.Required
                        : "Enter a valid redirect URL starting with https://.");
            }

            // Contact centre questions are only asked when the customer takes phone payments with an agent.
            if (project.TakesPhonePayments)
            {
                if (model.ContactCentrePlatform is null)
                    ModelState.AddModelError(nameof(model.ContactCentrePlatform), ValidationMessages.Required);
                if (model.ConnectionType is null)
                    ModelState.AddModelError(nameof(model.ConnectionType), ValidationMessages.Required);
            }

            if (!ModelState.IsValid)
            {
                return await GatewayViewAsync(id, model);
            }

            var result = await configService.SaveGatewayConfigurationAsync(GetCustomerId(), id, model);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not save.");
                return await GatewayViewAsync(id, model);
            }

            return RedirectAfterStepSave(id, nameof(Readiness));
        }

        /// <summary>The customer's uploaded API documentation, for "Download" on the Summary page.</summary>
        [HttpGet]
        public async Task<IActionResult> DownloadProjectDocument(Guid id, ProjectDocumentType type)
        {
            var result = await documentService.DownloadAsync(GetCustomerId(), id, type);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadApiDocumentation(Guid id)
        {
            var result = await documentService.DownloadAsync(GetCustomerId(), id, ProjectDocumentType.ApiDocumentation);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            // Served as a download and never sniffed, so an uploaded file cannot run as a web page.
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        // ---------- Readiness check and submit ----------

        [HttpGet]
        public async Task<IActionResult> Readiness(Guid id)
        {
            var project = await LoadProjectAsync(id);
            return project is null ? NotFound() : View(project);
        }

        [HttpPost]
        public async Task<IActionResult> Submit(Guid id)
        {
            var result = await projectService.SubmitAsync(GetCustomerId(), id);

            if (result.Succeeded)
            {
                var status = result.Data!.Status;

                // A project waiting for approval does not continue on its own, and no confirmation email is sent on submit.
                TempData["SuccessTitle"] = "Submitted";
                TempData["Success"] = status == ProjectStatus.Approval
                    ? "Your answers are now read-only. PCI Pal will review your project before setup starts."
                    : "Your answers are now read-only and setup will continue automatically.";

                // A standard build provisions itself on submit: go straight to the provisioning page, which
                // then continues to the overview. A project that is not provisioned (custom, or provisioning
                // stopped) goes to the overview instead.
                if (status == ProjectStatus.AccountCreation)
                {
                    return RedirectToAction(nameof(Details), null, ProjectRoute(id), "users-card");
                }

                if (status == ProjectStatus.Completed)
                {
                    return RedirectToAction(nameof(Provisioning), ProvisioningWalkthroughRoute(id));
                }

                if (status == ProjectStatus.Provisioning)
                {
                    TempData["Error"] = "Provisioning did not finish. An internal user can run it again.";
                    return RedirectToAction(nameof(Details), ProjectRoute(id));
                }

                // Not a standard build: it waits for approval, so show the overview with its Pending approval status.
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Readiness), new { id });
        }

        // ---------- Provisioning ----------

        /// <summary>The provisioning timeline (a visual walk-through of the provisioning steps). Provisioning itself starts by itself on submit.</summary>
        [HttpGet]
        public async Task<IActionResult> Provisioning(Guid id, bool fromSubmit = false)
        {
            var customerId = GetCustomerId();
            var info = await projectService.GetProvisioningInfoAsync(customerId, id);
            if (!info.Succeeded || info.Data is null)
            {
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            var overview = await LoadProjectAsync(id);
            var profile = await customerService.GetProfileAsync(customerId);
            var companyName = profile.Data?.CompanyName;
            var servicesLabel = overview?.Services.Count > 0
                ? string.Join(", ", overview!.Services.Select(s => ProjectOptions.ServiceLabels[s]))
                : "—";
            var routeLabel = overview?.BuildType switch
            {
                BuildType.Standard => "Standard: automatic",
                BuildType.Custom => "Custom: specialist review",
                _ => "—"
            };
            var isComplete = info.Data.Status is ProjectStatus.Completed or ProjectStatus.AccountCreation || info.Data.StepsComplete;
            var requiresAccount = overview?.RequiresAccount ?? false;
            var accountCreated = info.Data.AccountCreated;
            var showLiveStaging = isComplete
                && !(info.Data.Status == ProjectStatus.AccountCreation && requiresAccount && !accountCreated);
            ViewData["ProvisioningStepsComplete"] = info.Data.StepsComplete;
            ViewData["AccountCreated"] = accountCreated;
            ViewData["ProjectHubMode"] = true;
            ViewData["ProjectHubLive"] = showLiveStaging;
            // The "Submitted" confirmation is for the customer, once, straight after they submit.
            ViewData["ShowProvSubmittedBanner"] = fromSubmit && !IsStaff && overview?.IsSubmitted == true;
            var liveStandard = showLiveStaging && overview?.BuildType != BuildType.Custom;
            ViewData["ShowLaunchFlow"] = liveStandard && (info.Data.Status == ProjectStatus.Completed || accountCreated);
            ViewData["ShowLaunchFlowPending"] = fromSubmit && liveStandard && !accountCreated;
            ViewData["StaffLiveStandardStatus"] = ViewData["ReadOnlyStaff"] is true && liveStandard;
            ViewData["ShowStaffGenerateSdp"] = ViewData["ReadOnlyStaff"] is true && liveStandard
                && !requiresAccount && info.Data.Status == ProjectStatus.Completed;
            ViewData["ProjectActivity"] = await activityLog.GetForProjectAsync(customerId, id);
            Response.Headers.CacheControl = "no-store";
            return View(new ProvisioningViewModel
            {
                ProjectId = info.Data.ProjectId,
                ProjectName = info.Data.ProjectName,
                CustomerName = string.IsNullOrWhiteSpace(companyName) ? "Customer" : companyName,
                TenantId = string.IsNullOrWhiteSpace(info.Data.TenantId) ? "—" : info.Data.TenantId,
                Environment = info.Data.Platform?.Contains("staging", StringComparison.OrdinalIgnoreCase) == true ? "Staging" : "Production",
                ShortReference = ProjectOnboardingPresentation.ShortReference(info.Data.ProjectId),
                ServicesLabel = servicesLabel,
                RouteLabel = routeLabel,
                // The customer's primary contact from their organisation profile (not whoever is signed in).
                PrimaryContact = profile.Data?.PrimaryContactFullName is { Length: > 0 } contactName
                    ? contactName
                    : profile.Data?.PrimaryContactEmail is { Length: > 0 } contactEmail ? contactEmail : "—",
                SubmittedAt = overview?.CreatedAt ?? DateTimeOffset.UtcNow,
                Status = info.Data.Status,
                BuildType = overview?.BuildType,
                CrmPlatform = overview?.CrmPlatform,
                ReadOnly = ViewData["ReadOnlyStaff"] is true,
                ContinueToOverview = fromSubmit,
                IsComplete = isComplete,
                RequiresAccount = requiresAccount,
                AccountCreated = accountCreated
            });
        }

        // ---------- Setup Documentation Pack ----------

        /// <summary>
        /// The Setup Documentation Pack (SDP) of a completed, provisioned project: the endpoint URLs, tenant and
        /// flow details third parties need, with the customer's details. Printed or saved as PDF from the browser.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SetupDocument(Guid id)
        {
            var customerId = GetCustomerId();
            var document = await projectService.GetSetupDocumentAsync(customerId, id);
            if (!document.Succeeded || document.Data is null)
            {
                TempData["Error"] = document.Error;
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            var profile = await customerService.GetProfileAsync(customerId);
            Response.Headers.CacheControl = "no-store";
            return View(new SetupDocumentViewModel
            {
                Document = document.Data,
                Customer = profile.Succeeded ? profile.Data : null,
                BackUrl = Url.Action(nameof(Details), ProjectRoute(id))!
            });
        }

        // ---------- Flow launch ----------

        /// <summary>
        /// Gets a PCI Pal token and launches the project's flow (both with its ProjectProvisioningDetails),
        /// then shows the launched session in an iframe.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> FlowLaunch(Guid id)
        {
            if ((await LoadProjectAsync(id))?.BuildType == BuildType.Custom)
            {
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            var result = await flowLaunchService.LaunchAsync(GetCustomerId(), id);
            if (!result.Succeeded || result.Data is null)
            {
                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Details), new { id });
            }

            // The page carries live PCI Pal tokens, so it must never be cached.
            Response.Headers.CacheControl = "no-store";
            return View(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAsDraft(Guid id)
        {
            var result = await projectService.SaveAsDraftAsync(GetCustomerId(), id);

            if (result.Succeeded)
            {
                TempData["SuccessTitle"] = "Draft saved";
                TempData["Success"] = "You can submit when every section is complete.";
                return RedirectToAction(nameof(Readiness), new { id });
            }

            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Readiness), new { id });
        }

        /// <summary>
        /// Posts from the project page to the confirmation screen. The project is not scheduled until
        /// the reason is confirmed on that screen.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();

            return project.DeletionScheduledFor is null
                ? RedirectToAction(nameof(ConfirmDelete), ProjectRoute(id))
                : RedirectToAction(nameof(PendingDeletion), ProjectRoute(id));
        }

        /// <summary>Asks for a confirmation and a reason before the deletion countdown starts.</summary>
        [HttpGet]
        public async Task<IActionResult> ConfirmDelete(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.DeletionScheduledFor is not null)
            {
                return LeaveScheduledProject(id);
            }

            Response.Headers.CacheControl = "no-store";
            return View(new DeleteProjectViewModel
            {
                Id = project.Id,
                Name = project.Name,
                GracePeriodMinutes = projectService.GetDeletionGracePeriodMinutes()
            });
        }

        /// <summary>Saves the reason and starts the countdown, then shows the timer.</summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmDelete(Guid id, DeleteProjectViewModel model)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.DeletionScheduledFor is not null)
            {
                return RedirectToAction(nameof(PendingDeletion), ProjectRoute(id));
            }

            model.Id = project.Id;
            model.Name = project.Name;
            model.GracePeriodMinutes = projectService.GetDeletionGracePeriodMinutes();
            model.Reason = (model.Reason ?? string.Empty).Trim();
            if (model.Reason.Length == 0)
            {
                ModelState.AddModelError(nameof(DeleteProjectViewModel.Reason), "Enter a reason for deleting this project.");
            }

            if (!model.Confirmed)
            {
                ModelState.AddModelError(nameof(DeleteProjectViewModel.Confirmed), "Confirm that this project should be deleted.");
            }

            if (!ModelState.IsValid)
            {
                Response.Headers.CacheControl = "no-store";
                return View(model);
            }

            var result = await projectService.ScheduleDeletionAsync(GetCustomerId(), id, model.Reason);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not delete the project.");
                Response.Headers.CacheControl = "no-store";
                return View(model);
            }

            TempData["Success"] = "The project will be deleted shortly. You can restore it until the countdown ends.";
            return RedirectToAction(nameof(PendingDeletion), ProjectRoute(id));
        }

        /// <summary>Countdown for a project that is waiting to be deleted.</summary>
        [HttpGet]
        public async Task<IActionResult> PendingDeletion(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.DeletionScheduledFor is not { } deleteAt)
            {
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            Response.Headers.CacheControl = "no-store";
            return View(new PendingDeletionViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Reason = project.DeletionReason,
                DeleteAt = deleteAt,
                CanRestore = ViewData["ReadOnlyStaff"] is not true && deleteAt > DateTimeOffset.UtcNow,
                ListUrl = ProjectListUrl()
            });
        }

        /// <summary>Cancels a pending deletion while the countdown is still running.</summary>
        [HttpPost]
        public async Task<IActionResult> Restore(Guid id)
        {
            var result = await projectService.RestoreAsync(GetCustomerId(), id);
            if (!result.Succeeded)
            {
                if (string.Equals(result.Error, "Project not found.", StringComparison.Ordinal))
                {
                    return NotFound();
                }

                TempData["Error"] = result.Error;
                return RedirectToAction(nameof(PendingDeletion), ProjectRoute(id));
            }

            TempData["Success"] = "The project was restored.";
            return RedirectToAction(nameof(Details), ProjectRoute(id));
        }

        /// <summary>Moves a project along the internal workflow (Pending approval to Provisioning). Internal users only; the super admin is read-only.</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> AdvanceWorkflow(Guid id)
        {
            var wasApproval = (await LoadProjectAsync(id))?.Status == ProjectStatus.Approval;
            var result = await projectService.AdvanceInternalWorkflowAsync(id);
            var ajax = IsAjax;

            if (result.Succeeded)
            {
                TempData["Success"] = "Project status updated.";

                // Approving a project starts its provisioning. Show the provisioning steps (as the customer sees them
                // after submitting), which then carry on to the overview, instead of jumping to the finished project.
                var project = await LoadProjectAsync(id);
                if (project?.Status is ProjectStatus.Completed or ProjectStatus.AccountCreation
                    || (project?.Status == ProjectStatus.Provisioning && project.BuildType == BuildType.Custom && wasApproval)) // a custom build with no account
                {
                    return ajax
                        ? Json(new { success = true, redirect = Url.Action(nameof(Provisioning), ProvisioningWalkthroughRoute(id)) })
                        : RedirectToAction(nameof(Provisioning), ProvisioningWalkthroughRoute(id));
                }
            }
            else
            {
                if (ajax)
                {
                    return Json(new { success = false, error = result.Error });
                }

                TempData["Error"] = result.Error;
            }

            return ajax
                ? Json(new { success = result.Succeeded, redirect = Url.Action(nameof(Details), ProjectRoute(id)) })
                : RedirectToAction(nameof(Details), ProjectRoute(id));
        }

        // ---------- Custom builds: users, account and completion (internal users finish these by hand) ----------

        /// <summary>An internal user marks the account created (needs the users CSV).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> MarkAccountCreated(Guid id)
            => FinishAction(id, await projectService.MarkAccountCreatedAsync(id), "The account was marked as created.");

        /// <summary>An internal user marks the project completed.</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> MarkCompleted(Guid id)
            => FinishAction(id, await projectService.MarkCompletedAsync(id), "The project was marked as completed.");

        /// <summary>An internal user asks the customer for the users CSV (name, email, role).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> RequestUsersCsv(Guid id)
            => FinishAction(id, await usersService.RequestCsvAsync(GetCustomerId(), id), "The customer was asked for the users CSV.");

        /// <summary>The customer (or their partner) uploads the users CSV: name, email, role (admin or user).</summary>
        [HttpPost]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> UploadUsersCsv(Guid id, IFormFile? usersCsv)
        {
            if (usersCsv is not { Length: > 0 })
            {
                TempData["Error"] = "Choose a CSV file to upload.";
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            await using var content = usersCsv.OpenReadStream();
            var upload = await usersService.UploadCsvAsync(GetCustomerId(), id, usersCsv.FileName, content, usersCsv.Length);
            if (upload.Succeeded)
            {
                TempData["SuccessTitle"] = "Users uploaded";
                TempData["Success"] = upload.Data == 1
                    ? "1 person was added from your CSV."
                    : $"{upload.Data} people were added from your CSV.";
            }
            else
            {
                TempData["Error"] = upload.Error ?? "Could not read the file.";
            }

            return RedirectToAction(nameof(Details), ProjectRoute(id));
        }

        // Answers JSON to the page's script, or a redirect with a message otherwise.
        private IActionResult FinishAction(Guid id, ServiceResult result, string successMessage)
        {
            if (result.Succeeded)
            {
                TempData["Success"] = successMessage;
            }

            if (IsAjax)
            {
                return Json(new { success = result.Succeeded, error = result.Error, redirect = Url.Action(nameof(Details), ProjectRoute(id)) });
            }

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Details), ProjectRoute(id));
        }

        // ---------- Filling in missing integration items from the overview ----------

        /// <summary>Uploads the API documentation (needed for Web Callback) straight from the project overview.</summary>
        [HttpPost]
        [RequestSizeLimit(50_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
        public async Task<IActionResult> UploadApiDocumentation(Guid id, IFormFile? apiDocumentation)
        {
            if (apiDocumentation is not { Length: > 0 })
            {
                TempData["Error"] = "Choose a file to upload.";
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            await using var content = apiDocumentation.OpenReadStream();
            var upload = await documentService.UploadAsync(GetCustomerId(), id, ProjectDocumentType.ApiDocumentation,
                apiDocumentation.FileName, apiDocumentation.ContentType, apiDocumentation.Length, content);

            if (upload.Succeeded)
            {
                TempData["Success"] = "The API documentation was uploaded.";
            }
            else
            {
                TempData["Error"] = upload.Error ?? "Could not upload the file.";
            }

            return RedirectToAction(nameof(Details), ProjectRoute(id));
        }

        /// <summary>Saves the redirect URL straight from the project overview.</summary>
        [HttpPost]
        public async Task<IActionResult> SaveRedirectUrl(Guid id, string? redirectUrl)
        {
            var result = await configService.SaveRedirectUrlAsync(GetCustomerId(), id, redirectUrl);
            if (result.Succeeded)
            {
                TempData["Success"] = "The redirect URL was saved.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Details), ProjectRoute(id));
        }

        // ---------- Document requests (custom builds waiting for approval) ----------

        /// <summary>
        /// An internal user asks the customer for documents. Every ticked document becomes its own request, with the
        /// note typed next to it (notes[CODE]).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> RequestInformation(Guid id, string[]? codes, Dictionary<string, string?>? notes)
        {
            if ((await LoadProjectAsync(id))?.BuildType != BuildType.Custom)
            {
                TempData["Error"] = "Document requests only apply to custom builds.";
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            var drafts = (codes ?? Array.Empty<string>())
                .Select(code => new InformationRequestDraft(code, notes is not null && notes.TryGetValue(code, out var note) ? note : null))
                .ToList();
            var result = await informationService.RequestAsync(GetCustomerId(), id, drafts);
            if (result.Succeeded)
            {
                TempData["Success"] = drafts.Count == 1
                    ? "The document request was sent to the customer."
                    : "The document requests were sent to the customer.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Details), null, ProjectRoute(id), "information-requests");
        }

        /// <summary>An internal user approves, rejects (with a reason) or resolves the customer's response to one document.</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.InternalUser)]
        public async Task<IActionResult> ReviewInformation(Guid id, Guid itemId, InformationReviewDecision decision, string? note)
        {
            var result = await informationService.ReviewItemAsync(GetCustomerId(), id, itemId, decision, note);
            if (result.Succeeded)
            {
                TempData["Success"] = decision switch
                {
                    InformationReviewDecision.Approve => "The document was approved.",
                    InformationReviewDecision.Reject => "The response was rejected and the customer was asked to resubmit.",
                    _ => "The request was resolved."
                };
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToAction(nameof(Details), null, ProjectRoute(id), "information-requests");
        }

        /// <summary>The customer's document requests: each one answered on its own, with its status and history.</summary>
        [HttpGet]
        public async Task<IActionResult> ProvideInformation(Guid id)
        {
            var project = await LoadProjectAsync(id);
            if (project is null) return NotFound();
            if (project.BuildType != BuildType.Custom)
            {
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            var requests = await informationService.GetRequestsAsync(GetCustomerId(), id);
            if (requests.Count == 0)
            {
                return RedirectToAction(nameof(Details), ProjectRoute(id));
            }

            ViewData["InformationRequests"] = requests;
            return View();
        }

        /// <summary>The customer responds to one requested document: a file, a note, or both.</summary>
        [HttpPost]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        public async Task<IActionResult> ProvideInformation(Guid id, Guid itemId, IFormFile? file, string? note)
        {
            InformationUploadDto? upload = file is { Length: > 0 }
                ? new InformationUploadDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    Content = file.OpenReadStream()
                }
                : null;
            try
            {
                var result = await informationService.SubmitAsync(GetCustomerId(), id, itemId, upload, note);
                if (result.Succeeded)
                {
                    TempData["Success"] = "Thank you. Your response was sent for review.";
                }
                else
                {
                    TempData["Error"] = result.Error;
                }
            }
            finally
            {
                if (upload is not null)
                {
                    await upload.Content.DisposeAsync();
                }
            }

            return RedirectToAction(nameof(ProvideInformation), null, ProjectRoute(id), $"request-{itemId:N}");
        }

        /// <summary>Downloads a file the customer uploaded for an information request (the owner and internal staff).</summary>
        [HttpGet]
        public async Task<IActionResult> DownloadInformationDocument(Guid id, Guid documentId)
        {
            var result = await informationService.DownloadAsync(GetCustomerId(), id, documentId);
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            // Served as a download and never sniffed, so an uploaded file cannot run as a web page.
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
        }

        // ---------- Helpers ----------

        /// <summary>
        /// Loads the customer's project and shares it with the views (step bar, conditional questions)
        /// through ViewData. Returns null when it does not exist or belongs to someone else.
        /// </summary>
        private async Task<ProjectOverviewDto?> LoadProjectAsync(Guid id)
        {
            var result = await projectService.GetOverviewAsync(GetCustomerId(), id);
            if (result.Data is not null)
            {
                ViewData["Project"] = result.Data;
            }
            return result.Data;
        }

        /// <summary>After a setup step save, continue to the next step or return to the project list.</summary>
        private IActionResult RedirectAfterStepSave(Guid id, string nextAction)
        {
            if (Request.Form.ContainsKey("saveExit"))
            {
                return RouteData.Values["customerId"] is not null
                    ? RedirectToAction(nameof(Index), new { customerId = RouteData.Values["customerId"] })
                    : RedirectToAction(nameof(Index));
            }

            return RouteData.Values["customerId"] is not null
                ? RedirectToAction(nextAction, new { customerId = RouteData.Values["customerId"], id })
                : RedirectToAction(nextAction, new { id });
        }

        /// <summary>Shows the gateway screen along with the already-uploaded API documentation, if any.</summary>
        private async Task<IActionResult> GatewayViewAsync(Guid id, ProjectGatewayConfigurationDto model)
        {
            // The fields on this screen are those of the gateway chosen on the Payment Configuration step.
            var options = await configService.GetGatewayFieldOptionsAsync(GetCustomerId(), id);
            if (options.Data is null)
            {
                TempData["Error"] = options.Error;
                return RedirectToAction(nameof(PaymentConfiguration), new { id });
            }

            ViewData["GatewayFields"] = options.Data;
            ViewData["ApiDocument"] = await documentService.GetAsync(GetCustomerId(), id, ProjectDocumentType.ApiDocumentation);
            return View("GatewayConfiguration", model);
        }

        /// <summary>Shows the payment screen along with the configured gateways to choose from.</summary>
        private async Task<IActionResult> PaymentViewAsync(ProjectPaymentConfigurationDto model)
        {
            ViewData["Gateways"] = await gatewayService.GetGatewaysAsync();
            return View("PaymentConfiguration", model);
        }

        private IActionResult NeedsStep(Guid id, string action, string stepName)
        {
            TempData["Error"] = $"Please complete \"{stepName}\" first.";
            return RedirectToAction(action, new { id });
        }

        private IActionResult SubmittedRedirect(Guid id)
        {
            TempData["Error"] = "This project has been submitted and can no longer be edited.";
            return RedirectToAction(nameof(Details), new { id });
        }

        private const string ActingCustomerKey = "aob:acting_customer_id";

        /// <summary>
        /// A customer works on their own projects (id from the sign-in claim). A partner works on behalf of
        /// one of their customers, who has no login: the customer id comes from the URL and must belong to
        /// the partner, otherwise the page is not found. Everything downstream is scoped by this id.
        /// </summary>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Internal staff (super admin and internal users) can look at projects but never create or change them.
            if (IsStaff)
            {
                var staffAction = context.ActionDescriptor.RouteValues["action"];
                var viewOnly = staffAction is nameof(Index) or nameof(Details) or nameof(Summary) or nameof(Activity)
                    or nameof(Discovery) or nameof(TechnicalEnvironment) or nameof(PaymentConfiguration)
                    or nameof(GatewayConfiguration) or nameof(Readiness)
                    or nameof(DownloadApiDocumentation) or nameof(DownloadProjectDocument) or nameof(PendingDeletion)
                    or nameof(Provisioning) or nameof(DownloadInformationDocument) or nameof(SetupDocument);
                // Approving a project, and asking for or reviewing the information it needs, are the changes internal
                // users (not the super admin) may make.
                var mayApprove = staffAction is nameof(AdvanceWorkflow) or nameof(RequestInformation) or nameof(ReviewInformation)
                    or nameof(MarkAccountCreated) or nameof(MarkCompleted) or nameof(RequestUsersCsv)
                    && User.IsInRole(AppRoles.InternalUser);
                if (!viewOnly && !mayApprove)
                {
                    context.Result = Forbid();
                    return;
                }

                ViewData["ReadOnlyStaff"] = true;

                if (HttpContext.Request.Method == "GET"
                    && staffAction is nameof(Details) or nameof(Summary) or nameof(Activity) or nameof(Discovery)
                        or nameof(TechnicalEnvironment) or nameof(PaymentConfiguration)
                        or nameof(GatewayConfiguration) or nameof(Readiness) or nameof(Provisioning)
                        or nameof(ProvideInformation))
                {
                    ViewData["ProjectHubMode"] = true;
                }
            }

            if (User.IsInRole(nameof(UserType.Partner)))
            {
                // Without a customer in the URL a partner only gets the list of their customers' projects
                // and the create form (which asks for one of their customers).
                var partnerId = Guid.Parse(User.FindFirst(AppClaimTypes.PartnerId)!.Value);
                if (Guid.TryParse(context.RouteData.Values["customerId"]?.ToString(), out var customerId))
                {
                    if (!await customerService.PartnerOwnsCustomerAsync(partnerId, customerId))
                    {
                        context.Result = NotFound();
                        return;
                    }

                    HttpContext.Items[ActingCustomerKey] = customerId;
                    // Lets the views show "Back to customer" and partner wording.
                    ViewData["ActingCustomerId"] = customerId;
                }
                else if (context.ActionDescriptor.RouteValues["action"] is not (nameof(Index) or nameof(Create)))
                {
                    context.Result = NotFound();
                    return;
                }
            }
            else if (IsStaff)
            {
                // Internal staff may act for any existing customer. Without one in the URL they only get
                // the all-projects list and the create form (which asks for the customer).
                if (Guid.TryParse(context.RouteData.Values["customerId"]?.ToString(), out var customerId))
                {
                    if (await customerService.GetOwningPartnerIdAsync(customerId) is null)
                    {
                        context.Result = NotFound();
                        return;
                    }

                    HttpContext.Items[ActingCustomerKey] = customerId;
                    ViewData["ActingCustomerId"] = customerId;
                }
                else if (context.ActionDescriptor.RouteValues["action"] is not (nameof(Index) or nameof(Create)))
                {
                    context.Result = NotFound();
                    return;
                }
            }

            // While a project waits to be deleted it can be looked at or restored, but not changed.
            if (!IsStaff
                && context.ActionArguments.TryGetValue("id", out var idValue) && idValue is Guid pendingProjectId
                && context.ActionDescriptor.RouteValues["action"] is { } actionName
                && actionName is not (nameof(Index) or nameof(Create) or nameof(Details) or nameof(Summary) or nameof(Activity)
                    or nameof(DownloadApiDocumentation) or nameof(DownloadProjectDocument) or nameof(Delete)
                    or nameof(Restore) or nameof(PendingDeletion) or nameof(ConfirmDelete))
                && await projectService.IsPendingDeletionAsync(GetCustomerId(), pendingProjectId))
            {
                TempData["Error"] = "This project is scheduled for deletion. Restore it to make changes.";
                context.Result = RedirectToAction(nameof(PendingDeletion), new { id = pendingProjectId });
                return;
            }

            await next();
        }

        private bool IsStaff
            => User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.InternalUser);

        private bool IsPartner => User.IsInRole(nameof(UserType.Partner));

        /// <summary>Internal staff or a partner on the all-projects pages (no customer chosen in the URL).</summary>
        private bool IsOverview
            => (IsStaff || IsPartner) && !HttpContext.Items.ContainsKey(ActingCustomerKey);

        /// <summary>On a partner's overview, the partner whose customers it is limited to; null for staff.</summary>
        private Guid? OverviewPartnerId
            => IsPartner ? Guid.Parse(User.FindFirst(AppClaimTypes.PartnerId)!.Value) : null;

        private Guid GetCustomerId()
            => HttpContext.Items[ActingCustomerKey] is Guid acting
                ? acting
                : Guid.Parse(User.FindFirst(AppClaimTypes.CustomerId)!.Value);

        /// <summary>True for a request sent by the page's script (fetch), which wants JSON instead of a redirect.</summary>
        private bool IsAjax => string.Equals(Request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        /// <summary>Route values for one project, including the customer when a partner or staff member is acting.</summary>
        private object ProjectRoute(Guid id)
            => HttpContext.Items[ActingCustomerKey] is Guid customerId
                ? new { customerId, id }
                : new { id };

        /// <summary>Route values that open the provisioning page as a walk-through that continues to the overview.</summary>
        private object ProvisioningWalkthroughRoute(Guid id)
            => HttpContext.Items[ActingCustomerKey] is Guid customerId
                ? new { customerId, id, fromSubmit = true }
                : new { id, fromSubmit = true };

        /// <summary>The project list this person should return to (their own, or the customer they are acting for).</summary>
        private string ProjectListUrl()
            => IsStaff
                ? Url.RouteUrl("default", new { controller = "Projects", action = nameof(Index) })!
                : HttpContext.Items[ActingCustomerKey] is Guid customerId
                ? Url.Action(nameof(Index), new { customerId })!
                : Url.Action(nameof(Index))!;

        /// <summary>
        /// A project that is already waiting to be deleted. Back returns to the list; any other visit
        /// opens the countdown. The choice is made in the browser so the Back button is not a project page.
        /// </summary>
        private IActionResult LeaveScheduledProject(Guid id)
        {
            Response.Headers.CacheControl = "no-store";
            return View("LeaveProject", new ProjectLeaveViewModel
            {
                ListUrl = ProjectListUrl(),
                ContinueUrl = Url.Action(nameof(PendingDeletion), ProjectRoute(id))!
            });
        }
    }
}
