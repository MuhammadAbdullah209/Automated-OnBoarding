using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Models;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Provisions a project on its own, from start to finish:
    ///  1. runs the provisioning steps one after another (the project is in Provisioning);
    ///  2. if the payment page is hosted by PCI Pal, creates the account (the project is in Account creation);
    ///  3. moves the project to Completed. An embedded (iFrame) project has no account, so it goes straight to Completed.
    /// A custom build stops after step 1: an internal user marks the account created and the project completed.
    /// Progress is stored after every step, so a failed run carries on from where it stopped.
    /// </summary>
    public class ProjectProvisioningService(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IActivityLogService activityLog,
        IEnumerable<IProvisioningStep> steps,
        IProjectCustomerEmailService projectCustomerEmail,
        ILogger<ProjectProvisioningService> logger) : IProjectProvisioningService
    {
        public async Task<ServiceResult> RunAsync(Guid projectId)
        {
            var summary = await projectRepository.GetByIdAsync(projectId);
            if (summary is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            if (summary.Status != ProjectStatus.Provisioning)
            {
                return ServiceResult.Failure("Only a project in Provisioning can be provisioned.");
            }

            // Loaded with its answers: the payment experience decides whether an account is created.
            var project = await projectRepository.GetWithConfigurationAsync(summary.CustomerId, projectId) ?? summary;

            var detail = await projectRepository.GetProvisioningDetailForUpdateAsync(projectId);
            if (detail is null)
            {
                detail = new ProjectProvisioningDetail { ProjectId = projectId };
                await projectRepository.AddProvisioningDetailAsync(detail);
            }

            await activityLog.LogProjectAsync(project.CustomerId, projectId, "Provisioning started",
                $"Running {steps.Count()} provisioning steps.");

            foreach (var step in steps)
            {
                try
                {
                    if (step.IsDone(detail))
                    {
                        continue;
                    }

                    await step.ExecuteAsync(detail);
                    detail.UpdatedAt = DateTimeOffset.UtcNow;
                    await unitOfWork.SaveChangesAsync();
                    await activityLog.LogProjectAsync(project.CustomerId, projectId, "Provisioning step completed", step.Name);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Provisioning step {Step} failed for project {ProjectId}.", step.Name, projectId);
                    await activityLog.LogProjectAsync(project.CustomerId, projectId, "Provisioning step failed",
                        $"{step.Name} could not be completed. The project stays in Provisioning and can be run again.");
                    return ServiceResult.Failure($"Provisioning stopped at \"{step.Name}\".");
                }
            }

            await activityLog.LogProjectAsync(project.CustomerId, projectId, "Provisioning completed", "All provisioning steps finished.");

            // An account is only created when PCI Pal hosts the payment page.
            var needsAccount = project.Discovery?.CrmPaymentExperience == CrmPaymentExperience.HostedByPciPal;

            // With an account to create (standard or custom), the project moves to Account creation and stops: the
            // customer's users CSV is collected, then an internal user marks the account created and the project completed.
            if (needsAccount)
            {
                if (project.Status == ProjectStatus.Provisioning)
                {
                    project.Status = ProjectStatus.AccountCreation;
                    project.UpdatedAt = DateTimeOffset.UtcNow;
                    await unitOfWork.SaveChangesAsync();
                    await activityLog.LogProjectAsync(project.CustomerId, projectId, "Account creation started",
                        "The payment page is hosted by PCI Pal. The users CSV is needed to create the account.");
                    project.UsersCsvRequestedAt ??= DateTimeOffset.UtcNow;
                    await unitOfWork.SaveChangesAsync();
                    await projectCustomerEmail.SendUsersCsvRequestedAsync(project.CustomerId, projectId, project.Name);
                }

                return ServiceResult.Success();
            }

            // A custom build with no account also waits for an internal user to mark it completed.
            if (project.BuildType == BuildType.Custom)
            {
                return ServiceResult.Success();
            }

            project.Status = ProjectStatus.Completed;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(project.CustomerId, projectId, "Project completed",
                "Provisioning is done. No account is created because the payment page is embedded in the customer's CRM.");
            await projectCustomerEmail.SendProjectCompletedAsync(project.CustomerId, projectId, project.Name);

            return ServiceResult.Success();
        }
    }
}
