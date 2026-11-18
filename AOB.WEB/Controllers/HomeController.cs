using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Interfaces.Services;
using AOB.WEB.Models;
using AOB.WEB.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AOB.WEB.Controllers
{
    public class HomeController(
        IDashboardService dashboardService,
        ICustomerService customerService,
        IProjectService projectService,
        ICustomerDocumentService customerDocumentService) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            DashboardViewModel model;

            if (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.InternalUser))
            {
                model = new DashboardViewModel
                {
                    Audience = DashboardAudience.Internal,
                    Data = await dashboardService.GetInternalDashboardAsync(cancellationToken)
                };
            }
            else if (User.IsInRole(nameof(UserType.Partner)))
            {
                var partnerId = Guid.Parse(User.FindFirst(AppClaimTypes.PartnerId)!.Value);
                model = new DashboardViewModel
                {
                    Audience = DashboardAudience.Partner,
                    Data = await dashboardService.GetPartnerDashboardAsync(partnerId, cancellationToken)
                };
            }
            else if (User.IsInRole(nameof(UserType.Customer)))
            {
                var customerId = Guid.Parse(User.FindFirst(AppClaimTypes.CustomerId)!.Value);

                var profileIncomplete = false;
                var profile = await customerService.GetProfileAsync(customerId);
                if (profile.Data is { } p
                    && (string.IsNullOrWhiteSpace(p.CompanyName) || string.IsNullOrWhiteSpace(p.RegisteredCountryCode)))
                {
                    profileIncomplete = true;
                }

                var profileDto = profile.Data;
                var documents = await customerDocumentService.GetChecklistAsync(customerId);
                var projectPage = await projectService.GetProjectsPagedAsync(customerId, new ProjectListQuery
                {
                    PageSize = ProjectListQuery.MaxPageSize
                });
                var queueProjectPage = await projectService.GetProjectsPagedAsync(customerId, new ProjectListQuery
                {
                    PageSize = CustomerHomeQueue.QueueProjectFetchSize
                });
                var sections = CustomerReadinessPresentation.ProfileSections(profileDto);
                var (ready, total) = CustomerReadinessPresentation.Score(profileDto, documents);
                var queue = CustomerHomeQueue.Build(queueProjectPage.Items);

                model = new DashboardViewModel
                {
                    Audience = DashboardAudience.Customer,
                    Data = await dashboardService.GetCustomerDashboardAsync(customerId, cancellationToken),
                    ProfileIncomplete = profileIncomplete,
                    CompanyName = profileDto?.CompanyName,
                    CustomerProfile = profileDto,
                    Documents = documents,
                    CustomerProjects = projectPage.Items,
                    Queue = queue,
                    ReadinessSections = sections,
                    ReadinessReady = ready,
                    ReadinessTotal = total
                };
                if (!string.IsNullOrWhiteSpace(profileDto?.CompanyName))
                {
                    ViewData["UserCompanyName"] = profileDto!.CompanyName;
                }
            }
            else
            {
                model = new DashboardViewModel
                {
                    Audience = DashboardAudience.Internal,
                    Data = await dashboardService.GetInternalDashboardAsync(cancellationToken)
                };
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        /// <summary>
        /// The page for an error status that has no body of its own (404, 429, ...). Program.cs re-executes the
        /// request here with the original status code, so the URL and the status stay the same.
        /// </summary>
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult HttpStatus(int code)
        {
            Response.StatusCode = code;
            return View(code);
        }
    }
}
