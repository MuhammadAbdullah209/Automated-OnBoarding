using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AOB.WEB.Filters
{
    /// <summary>Shared shell data for signed-in customers (nav badges, top-bar company).</summary>
    public class CustomerShellViewDataFilter(
        IProjectService projectService,
        ICustomerService customerService) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.User.IsInRole(nameof(UserType.Customer)))
            {
                var customerIdClaim = context.HttpContext.User.FindFirst(AppClaimTypes.CustomerId)?.Value;
                if (Guid.TryParse(customerIdClaim, out var customerId))
                {
                    var page = await projectService.GetProjectsPagedAsync(customerId, new ProjectListQuery
                    {
                        PageSize = ProjectListQuery.MaxPageSize
                    });

                    var needsYou = page.Items.Count(p =>
                        ProjectOnboardingPresentation.MatchesListView(p.Status, p.InformationRequestStatus, ProjectListView.NeedsYou, p.AwaitingUsersCsv));

                    if (context.Controller is Controller controller)
                    {
                        controller.ViewData["ProjectsNeedsYouCount"] = needsYou;

                        if (controller.ViewData["UserCompanyName"] is not string existing
                            || string.IsNullOrWhiteSpace(existing))
                        {
                            var profile = await customerService.GetProfileAsync(customerId);
                            if (!string.IsNullOrWhiteSpace(profile.Data?.CompanyName))
                            {
                                controller.ViewData["UserCompanyName"] = profile.Data!.CompanyName;
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}
