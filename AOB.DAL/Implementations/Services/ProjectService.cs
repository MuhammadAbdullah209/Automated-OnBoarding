using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.Domain.BuildRules;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>A customer's onboarding projects.</summary>
    public class ProjectService(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IActivityLogService activityLog,
        IConfiguration configuration,
        BuildClassifier buildClassifier,
        IProjectProvisioningService provisioning,
        IProjectCustomerEmailService projectCustomerEmail) : IProjectService
    {
        public Task<PagedResult<ProjectSummaryDto>> GetProjectsPagedAsync(Guid customerId, ProjectListQuery query)
            => GetPagedAsync(query, q => projectRepository.ListAsync(customerId, q));

        public Task<PagedResult<ProjectSummaryDto>> GetAllProjectsPagedAsync(ProjectListQuery query)
            => GetPagedAsync(query, projectRepository.ListAllAsync);

        private static async Task<PagedResult<ProjectSummaryDto>> GetPagedAsync(
            ProjectListQuery query,
            Func<ProjectListQuery, Task<(IReadOnlyList<Project> Items, int TotalCount)>> list)
        {
            var normalized = NormalizeListQuery(query);
            var (projects, totalCount) = await list(normalized);

            var totalPages = normalized.PageSize > 0
                ? (int)Math.Ceiling(totalCount / (double)normalized.PageSize)
                : 0;
            var page = normalized.Page;
            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
                normalized.Page = page;
                (projects, totalCount) = await list(normalized);
            }

            return new PagedResult<ProjectSummaryDto>
            {
                Items = projects.Select(ToSummary).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = normalized.PageSize
            };
        }

        private static ProjectListQuery NormalizeListQuery(ProjectListQuery query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? ProjectListQuery.DefaultPageSize : query.PageSize;
            if (pageSize > ProjectListQuery.MaxPageSize)
            {
                pageSize = ProjectListQuery.MaxPageSize;
            }

            var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

            // A reversed range is treated as the user meaning the other way round.
            var (from, to) = (query.CreatedFrom, query.CreatedTo);
            if (from is { } f && to is { } t && f > t)
            {
                (from, to) = (to, from);
            }

            return new ProjectListQuery
            {
                Search = search,
                Status = query.Status,
                Step = query.Step,
                CustomerId = query.CustomerId,
                PartnerId = query.PartnerId,
                CreatedFrom = from,
                CreatedTo = to,
                View = query.View,
                Page = page,
                PageSize = pageSize
            };
        }

        private static ProjectSummaryDto ToSummary(Project p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Services = EnumCsv.Parse<PciPalService>(p.Services),
            Status = p.Status,
            CurrentStep = p.CurrentStep,
            InformationRequestStatus = p.InformationRequestStatus,
            AwaitingUsersCsv = p.Status == ProjectStatus.AccountCreation && p.UsersCsvRequestedAt is not null,
            RequiresAccount = p.Discovery?.CrmPaymentExperience == CrmPaymentExperience.HostedByPciPal,
            CreatedAt = p.CreatedAt,
            DeletionScheduledFor = p.DeletionScheduledFor,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer?.CompanyName
        };

        public async Task<ServiceResult<ProjectOverviewDto>> GetOverviewAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            return project is null
                ? ServiceResult<ProjectOverviewDto>.Failure("Project not found.")
                : ServiceResult<ProjectOverviewDto>.Success(ProjectMapper.ToOverview(project));
        }

        public async Task<ServiceResult<ProjectSubmitResultDto>> SubmitAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<ProjectSubmitResultDto>.Failure("Project not found.");
            }

            if (ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                return ServiceResult<ProjectSubmitResultDto>.Failure("This project has already been submitted.");
            }

            if (!ProjectStatusRules.CanCustomerSubmit(project.Status))
            {
                return ServiceResult<ProjectSubmitResultDto>.Failure("This project cannot be submitted in its current state.");
            }

            if (!ProjectMapper.ToOverview(project).IsReadyToSubmit)
            {
                return ServiceResult<ProjectSubmitResultDto>.Failure("Complete all four steps before submitting.");
            }

            // The domain rules decide: a standard build goes straight to provisioning, anything else waits for approval.
            var assessment = buildClassifier.Assess(BuildAssessmentMapper.ToInput(project));
            project.BuildType = assessment.Type;
            project.BuildReasons = assessment.IsStandard ? null : string.Join("\n", assessment.Reasons);
            project.Status = assessment.IsStandard ? ProjectStatus.Provisioning : ProjectStatus.Approval;
            project.CurrentStep = ProjectStep.ReadinessCheck;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            // The activity log is shown to the customer, so it says where the project went, not why.
            await activityLog.LogProjectAsync(customerId, projectId, "Project submitted",
                assessment.IsStandard
                    ? "The project was submitted and moved to Provisioning."
                    : "The project was submitted and is pending approval.");

            // A standard build starts provisioning by itself: no approval, no button. It runs the provisioning
            // steps, creates the account when PCI Pal hosts the payment page, and ends in Completed. If a step
            // fails the project stays in Provisioning and an internal user can run it again.
            if (assessment.IsStandard)
            {
                await provisioning.RunAsync(projectId);
            }

            var result = new ProjectSubmitResultDto
            {
                BuildType = assessment.Type,
                Reasons = assessment.Reasons.ToList(),
                Status = project.Status
            };

            return ServiceResult<ProjectSubmitResultDto>.Success(result);
        }

        public async Task<ServiceResult> SaveAsDraftAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            if (ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                return ServiceResult.Failure("This project has already been submitted.");
            }

            if (!ProjectStatusRules.CanCustomerSaveAsDraft(project.Status))
            {
                return ServiceResult.Failure("This project cannot be saved as draft.");
            }

            if (!ProjectMapper.ToOverview(project).IsReadyToSubmit)
            {
                return ServiceResult.Failure("Complete all four steps before saving as draft.");
            }

            project.Status = ProjectStatus.Draft;
            project.CurrentStep = ProjectStep.ReadinessCheck;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(customerId, projectId, "Saved as draft", "All four setup steps were complete and the project was saved as a draft.");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> AdvanceInternalWorkflowAsync(Guid projectId)
        {
            var project = await projectRepository.GetByIdAsync(projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            // A project whose provisioning stopped part-way is run again; finished steps are skipped.
            if (project.Status == ProjectStatus.Provisioning)
            {
                return await provisioning.RunAsync(projectId);
            }

            // A project cannot be approved while information it asked for is still outstanding or unreviewed.
            if (project.Status == ProjectStatus.Approval && await projectRepository.HasOpenInformationRequestAsync(projectId))
            {
                return ServiceResult.Failure("Approve every item of the information request first.");
            }

            var next = ProjectStatusRules.NextInternalStatus(project.Status);
            if (next is null)
            {
                return ServiceResult.Failure("This project is not ready for the next internal step.");
            }

            project.Status = next.Value;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(project.CustomerId, project.Id, "Status changed", $"Project moved to {ProjectOptions.ProjectStatusLabels[next.Value]} by the internal workflow.");

            // An approved custom build provisions itself, the same way a standard one does.
            return next == ProjectStatus.Provisioning
                ? await provisioning.RunAsync(projectId)
                : ServiceResult.Success();
        }

        public async Task<ServiceResult> MarkAccountCreatedAsync(Guid projectId)
        {
            var project = await LoadCustomBuildAsync(projectId);
            if (project is null || project.Status != ProjectStatus.AccountCreation)
            {
                return ServiceResult.Failure("The account can only be marked created while the project is in account creation.");
            }

            var detail = await projectRepository.GetProvisioningDetailForUpdateAsync(projectId);
            if (!IsProvisioned(detail))
            {
                return ServiceResult.Failure("Run provisioning first.");
            }

            var users = await projectRepository.CountCustomerUsersAsync(projectId);
            if (users == 0)
            {
                return ServiceResult.Failure("Upload the users CSV first, or ask the customer for it.");
            }

            // The project stays in Account creation until an internal user marks it completed.
            detail!.AccountCreatedAt ??= DateTimeOffset.UtcNow;
            detail.UpdatedAt = DateTimeOffset.UtcNow;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(project.CustomerId, projectId, "Account created",
                $"The account was created for {users} user(s).");
            return ServiceResult.Success();
        }

        public async Task<ServiceResult> MarkCompletedAsync(Guid projectId)
        {
            var project = await LoadCustomBuildAsync(projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            var needsAccount = project.Discovery?.CrmPaymentExperience == CrmPaymentExperience.HostedByPciPal;
            var detailNow = await projectRepository.GetProvisioningDetailAsync(projectId);
            var ready = (project.Status == ProjectStatus.AccountCreation && detailNow?.AccountCreatedAt is not null)
                || (project.Status == ProjectStatus.Provisioning && !needsAccount
                    && IsProvisioned(detailNow));
            if (!ready)
            {
                return ServiceResult.Failure(needsAccount
                    ? "Mark the account created first."
                    : "Run provisioning first.");
            }

            project.Status = ProjectStatus.Completed;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(project.CustomerId, projectId, "Project completed",
                "An internal user marked the project completed.");
            await projectCustomerEmail.SendProjectCompletedAsync(project.CustomerId, projectId, project.Name);
            return ServiceResult.Success();
        }

        // The account and completion are marked by hand whenever an account is needed, and for custom builds.
        private async Task<Project?> LoadCustomBuildAsync(Guid projectId)
        {
            var project = await projectRepository.GetByIdAsync(projectId);
            if (project is null)
            {
                return null;
            }

            var withConfig = await projectRepository.GetWithConfigurationAsync(project.CustomerId, projectId);
            return withConfig;
        }

        private static bool IsProvisioned(ProjectProvisioningDetail? detail)
            => detail is { TenantId: not null, ClientId: not null, FlowId: not null };

        public async Task<ServiceResult<ProvisioningInfoDto>> GetProvisioningInfoAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetForCustomerAsync(customerId, projectId);
            if (project is null || project.Status is not (ProjectStatus.Provisioning or ProjectStatus.AccountCreation or ProjectStatus.Completed))
            {
                return ServiceResult<ProvisioningInfoDto>.Failure("Project not found.");
            }

            var detail = await projectRepository.GetProvisioningDetailAsync(projectId);
            return ServiceResult<ProvisioningInfoDto>.Success(new ProvisioningInfoDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                Status = project.Status,
                StepsComplete = IsProvisioned(detail),
                AccountCreated = detail?.AccountCreatedAt is not null,
                TenantId = detail?.TenantId,
                TenantName = detail?.TenantName,
                Platform = detail?.Platform
            });
        }

        public async Task<ServiceResult<SetupDocumentDto>> GetSetupDocumentAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            var detail = project is null ? null : await projectRepository.GetProvisioningDetailAsync(projectId);
            var hosted = project?.Discovery?.CrmPaymentExperience == CrmPaymentExperience.HostedByPciPal;
            var packReady = project is not null && !hosted && project.Status == ProjectStatus.Completed
                && IsProvisioned(detail) && !string.IsNullOrWhiteSpace(detail!.Region);
            if (!packReady)
            {
                return ServiceResult<SetupDocumentDto>.Failure(hosted
                    ? "The Setup Document Pack is not used for hosted payment page builds."
                    : "The setup document is available once the project is completed and provisioned.");
            }

            // Same URLs the flow launch uses (see FlowLaunchService), written with the <tenantID> and <sessionid>
            // placeholders as in the Word template; the tenant id itself is listed under Tenant Details.
            var host = $"https://{detail.Region}.{detail.Platform}.cloud";
            return ServiceResult<SetupDocumentDto>.Success(new SetupDocumentDto
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                Environment = detail.Platform.Contains("staging", StringComparison.OrdinalIgnoreCase) ? "Staging" : "Production",
                Region = RegionLabel(detail.Region),
                AuthenticationEndpoint = $"https://{detail.Platform}.cloud/api/v1/token",
                SessionEndpoint = $"{host}/api/v1/session/<tenantID>/launch",
                IframeUrl = $"{host}/session/<tenantID>/view/<sessionid>/framed",
                TenantName = detail.TenantName,
                TenantId = detail.TenantId,
                Username = detail.Username,
                ClientId = detail.ClientId,
                ClientSecretExpiry = (detail.UpdatedAt ?? detail.CreatedAt).AddYears(3),
                FlowId = detail.FlowId,
                InputFields = project.GatewayFields
                    .Where(f => f.IsInput && f.FieldType != GatewayFieldType.Authentication)
                    .OrderBy(f => f.FieldType).ThenBy(f => f.Name)
                    .Select(f => new SetupDocumentFieldDto
                    {
                        Caption = f.Name,
                        VariableName = VariableName(f.Name),
                        Notes = f.Description
                    })
                    .ToList()
            });
        }

        /// <summary>"euwest1" → "EUWest1": the two-letter area in capitals, the direction capitalised.</summary>
        private static string RegionLabel(string region)
        {
            var r = region.Trim();
            return r.Length > 2
                ? r[..2].ToUpperInvariant() + char.ToUpperInvariant(r[2]) + r[3..]
                : r.ToUpperInvariant();
        }

        /// <summary>A field name as an API variable: "Cardholder Name" → "cardholderName", "Reference" → "reference".</summary>
        private static string VariableName(string name)
        {
            var words = name.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return string.Empty;
            }

            if (words.Length == 1)
            {
                return char.ToLowerInvariant(words[0][0]) + words[0][1..];
            }

            return string.Concat(words.Select((w, i) => i == 0
                ? w.ToLowerInvariant()
                : char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
        }

        public int GetDeletionGracePeriodMinutes()
            => Math.Max(1, configuration.GetValue("ProjectDeletion:GracePeriodMinutes", 5));

        public async Task<ServiceResult<DateTimeOffset>> ScheduleDeletionAsync(Guid customerId, Guid projectId, string reason)
        {
            var project = await projectRepository.GetForCustomerAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<DateTimeOffset>.Failure("Project not found.");
            }

            // Once provisioning is complete the project can no longer be deleted.
            var provisioned = project.Status is ProjectStatus.AccountCreation or ProjectStatus.Completed
                || (project.Status == ProjectStatus.Provisioning && IsProvisioned(await projectRepository.GetProvisioningDetailAsync(projectId)));
            if (provisioned && project.DeletionScheduledFor is null)
            {
                return ServiceResult<DateTimeOffset>.Failure("This project can no longer be deleted because it has been provisioned.");
            }

            // A project waiting for PCI Pal approval cannot be deleted by the customer.
            if (project.Status == ProjectStatus.Approval && project.DeletionScheduledFor is null)
            {
                return ServiceResult<DateTimeOffset>.Failure("This project can't be deleted while it is pending PCI Pal approval.");
            }

            // Asking again while the countdown runs does not extend it or replace the reason.
            if (project.DeletionScheduledFor is { } already)
            {
                return ServiceResult<DateTimeOffset>.Success(already);
            }

            var trimmed = reason?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
            {
                return ServiceResult<DateTimeOffset>.Failure("Enter a reason for deleting this project.");
            }

            if (trimmed.Length > 1000)
            {
                return ServiceResult<DateTimeOffset>.Failure("The reason must be 1000 characters or fewer.");
            }

            var minutes = GetDeletionGracePeriodMinutes();
            var deleteAt = DateTimeOffset.UtcNow.AddMinutes(minutes);
            project.DeletionScheduledFor = deleteAt;
            project.DeletionReason = trimmed;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            await activityLog.LogProjectAsync(customerId, projectId, "Project deletion scheduled", trimmed);
            return ServiceResult<DateTimeOffset>.Success(deleteAt);
        }

        public async Task<ServiceResult> RestoreAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetForCustomerAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }

            if (project.DeletionScheduledFor is null)
            {
                return ServiceResult.Success();
            }

            if (project.DeletionScheduledFor <= DateTimeOffset.UtcNow)
            {
                return ServiceResult.Failure("The time to restore this project has ended.");
            }

            project.DeletionScheduledFor = null;
            project.DeletionReason = null;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();

            await activityLog.LogProjectAsync(customerId, projectId, "Project restored", "The scheduled deletion was cancelled.");
            return ServiceResult.Success();
        }

        public async Task<bool> IsPendingDeletionAsync(Guid customerId, Guid projectId)
            => (await projectRepository.GetForCustomerAsync(customerId, projectId))?.DeletionScheduledFor is not null;

        public async Task<ServiceResult<Guid>> CreateAsync(Guid customerId, CreateProjectDto dto)
        {
            var name = dto.Name.Trim();
            if (name.Length == 0)
            {
                return ServiceResult<Guid>.Failure("Enter a project name.");
            }

            var services = dto.Services.Distinct().ToList();
            if (services.Count == 0)
            {
                return ServiceResult<Guid>.Failure("Select at least one service.");
            }

            // "Coming soon" services cannot be chosen yet, even if the request says otherwise.
            if (services.Any(s => !ProjectOptions.AvailableServices.Contains(s)))
            {
                return ServiceResult<Guid>.Failure("One of the selected services is not available yet.");
            }

            if (await projectRepository.NameExistsAsync(customerId, name))
            {
                return ServiceResult<Guid>.Failure("You already have a project with this name.");
            }

            var project = new Project
            {
                CustomerId = customerId,
                Name = name,
                Services = EnumCsv.Join(services),
                Status = ProjectStatus.InProgress,
                CurrentStep = ProjectStep.Discovery
            };

            await projectRepository.AddAsync(project);
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(customerId, project.Id, "Project created", $"Project \"{name}\" was created for {string.Join(", ", services.Select(s => ProjectOptions.ServiceLabels[s]))}.");

            return ServiceResult<Guid>.Success(project.Id);
        }
    }
}
