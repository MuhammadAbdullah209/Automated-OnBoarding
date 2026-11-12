using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// Loads and saves the four onboarding screens of a project. Multiple-choice answers are stored
    /// as comma-separated enum names (see EnumCsv). Each save also moves the project forward:
    /// Draft becomes InProgress and CurrentStep advances to the next screen.
    /// </summary>
    public class ProjectConfigurationService(
        IProjectRepository projectRepository,
        IPaymentGatewayRepository gatewayRepository,
        IUnitOfWork unitOfWork,
        IActivityLogService activityLog) : IProjectConfigurationService
    {
        private const string Submitted = "This project has been submitted and can no longer be edited.";

        // ---------------------------------------------------------------- Discovery

        public async Task<ServiceResult<ProjectDiscoveryDto>> GetDiscoveryAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<ProjectDiscoveryDto>.Failure("Project not found.");
            }

            return ServiceResult<ProjectDiscoveryDto>.Success(MapDiscovery(project));
        }

        private static ProjectDiscoveryDto MapDiscovery(Project project)
        {
            var d = project.Discovery;
            return new ProjectDiscoveryDto
            {
                Services = EnumCsv.Parse<PciPalService>(project.Services),
                CrmSystem = project.CrmSystem,
                CurrentPaymentMethods = EnumCsv.Parse<CurrentPaymentMethod>(d?.CurrentPaymentMethods),
                PaymentJourneyDescription = d?.PaymentJourneyDescription,
                CrmPaymentExperience = d?.CrmPaymentExperience,
                PortalUrls = d?.PortalUrls
            };
        }

        public async Task<ServiceResult> SaveDiscoveryAsync(Guid customerId, Guid projectId, ProjectDiscoveryDto dto)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }
            if (ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                return ServiceResult.Failure(Submitted);
            }

            if (dto.Services.Count == 0 || dto.CrmSystem is null || dto.CurrentPaymentMethods.Count == 0)
            {
                return ServiceResult.Failure("Please answer all the required questions.");
            }
            if (dto.Services.Any(s => !ProjectOptions.AvailableServices.Contains(s)))
            {
                return ServiceResult.Failure("One of the selected services is not available yet.");
            }
            var isCrm = dto.CrmSystem == CrmSystem.Crm;
            if (dto.CrmPaymentExperience is null)
            {
                return ServiceResult.Failure("Choose how the payment page should be shown.");
            }
            var embedded = dto.CrmPaymentExperience == CrmPaymentExperience.EmbeddedIframe;
            var portalUrls = PortalUrlParser.Parse(dto.PortalUrls);
            if (embedded && (portalUrls.Error is not null || portalUrls.Urls.Count == 0))
            {
                return ServiceResult.Failure(portalUrls.Error ?? "Enter at least one portal URL.");
            }
            if (dto.CurrentPaymentMethods.Contains(CurrentPaymentMethod.AnotherWay)
                && string.IsNullOrWhiteSpace(dto.PaymentJourneyDescription))
            {
                return ServiceResult.Failure("Describe your payment journey, because you selected “Another way”.");
            }

            project.Services = EnumCsv.Join(dto.Services);
            project.CrmSystem = dto.CrmSystem;
            if (!isCrm)
            {
                // A CRM product only makes sense for a CRM.
                project.CrmPlatform = null;
            }

            var discovery = project.Discovery ??= new ProjectDiscovery();
            discovery.CurrentPaymentMethods = EnumCsv.Join(dto.CurrentPaymentMethods);
            discovery.PaymentJourneyDescription = Clean(dto.PaymentJourneyDescription);
            discovery.CrmPaymentExperience = dto.CrmPaymentExperience;
            discovery.PortalUrls = embedded ? string.Join('\n', portalUrls.Urls) : null;
            discovery.UpdatedAt = DateTimeOffset.UtcNow;

            return await AdvanceAndSaveAsync(project, ProjectStep.TechnicalEnvironment);
        }

        // ---------------------------------------------------------------- Technical environment

        public async Task<ServiceResult<ProjectTechnicalEnvironmentDto>> GetTechnicalEnvironmentAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<ProjectTechnicalEnvironmentDto>.Failure("Project not found.");
            }

            return ServiceResult<ProjectTechnicalEnvironmentDto>.Success(MapTechnicalEnvironment(project));
        }

        private static ProjectTechnicalEnvironmentDto MapTechnicalEnvironment(Project project)
        {
            var t = project.TechnicalEnvironment;
            return new ProjectTechnicalEnvironmentDto
            {
                CrmPlatform = project.CrmPlatform,
                PaymentRegions = EnumCsv.Parse<PaymentRegion>(t?.PaymentRegions),
                StagingRegions = EnumCsv.Parse<PciPalRegion>(t?.StagingRegions),
                ProductionRegions = EnumCsv.Parse<PciPalRegion>(t?.ProductionRegions),
                OutboundAccess = t?.OutboundAccess
            };
        }

        public async Task<ServiceResult> SaveTechnicalEnvironmentAsync(Guid customerId, Guid projectId, ProjectTechnicalEnvironmentDto dto)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }
            if (ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                return ServiceResult.Failure(Submitted);
            }
            if (project.Discovery is null)
            {
                return ServiceResult.Failure("Complete Business & Use-Case Discovery first.");
            }

            if (dto.StagingRegions.Count == 0 || dto.ProductionRegions.Count == 0 || dto.OutboundAccess is null)
            {
                return ServiceResult.Failure("Please answer all the required questions.");
            }
            // Staging and production are each offered in only some regions.
            if (dto.StagingRegions.Any(r => !ProjectOptions.StagingRegionLabels.ContainsKey(r))
                || dto.ProductionRegions.Any(r => !ProjectOptions.ProductionRegionLabels.ContainsKey(r)))
            {
                return ServiceResult.Failure("Choose staging and production regions from the lists offered.");
            }
            if (project.CrmSystem == CrmSystem.Crm && dto.CrmPlatform is null)
            {
                return ServiceResult.Failure("Select your business system / CRM.");
            }

            project.CrmPlatform = project.CrmSystem == CrmSystem.Crm ? dto.CrmPlatform : null;

            var env = project.TechnicalEnvironment ??= new ProjectTechnicalEnvironment();
            env.PaymentRegions = EnumCsv.Join(dto.PaymentRegions);
            env.StagingRegions = EnumCsv.Join(dto.StagingRegions);
            env.ProductionRegions = EnumCsv.Join(dto.ProductionRegions);
            env.OutboundAccess = dto.OutboundAccess;
            env.UpdatedAt = DateTimeOffset.UtcNow;

            return await AdvanceAndSaveAsync(project, ProjectStep.PaymentConfiguration);
        }

        // ---------------------------------------------------------------- Payment configuration

        public async Task<ServiceResult<ProjectPaymentConfigurationDto>> GetPaymentConfigurationAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<ProjectPaymentConfigurationDto>.Failure("Project not found.");
            }

            return ServiceResult<ProjectPaymentConfigurationDto>.Success(MapPaymentConfiguration(project));
        }

        private static ProjectPaymentConfigurationDto MapPaymentConfiguration(Project project)
        {
            var c = project.PaymentConfiguration;
            return new ProjectPaymentConfigurationDto
            {
                PaymentGatewayId = c?.PaymentGatewayId,
                SupportedCards = EnumCsv.Parse<CardScheme>(c?.SupportedCards),
                MaxPaymentAttempts = c?.MaxPaymentAttempts,
                TotalMerchantAccounts = c?.TotalMerchantAccounts,
                MerchantIdentification = c?.MerchantIdentification,
                MerchantIdentificationOther = c?.MerchantIdentificationOther,
                CustomerAcquirer = c?.CustomerAcquirer,
                PaymentTypes = EnumCsv.Parse<PaymentType>(c?.PaymentTypes),
                Currencies = EnumCsv.Parse<CurrencyCode>(c?.Currencies),
                CardNumberMasking = c?.CardNumberMasking,
                SmsServiceProvider = c?.SmsServiceProvider,
                OtherSmsProvider = c?.OtherSmsProvider,
                EmailServiceProvider = c?.EmailServiceProvider,
                OtherEmailProvider = c?.OtherEmailProvider,
                HasSandboxEnvironment = c?.HasSandboxEnvironment ?? false
            };
        }

        public async Task<ServiceResult> SavePaymentConfigurationAsync(Guid customerId, Guid projectId, ProjectPaymentConfigurationDto dto)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }
            if (ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                return ServiceResult.Failure(Submitted);
            }
            if (project.TechnicalEnvironment is null)
            {
                return ServiceResult.Failure("Complete the Technical Environment step first.");
            }
            if (dto.SupportedCards.Count == 0)
            {
                return ServiceResult.Failure("Select at least one supported card type.");
            }

            if (dto.MaxPaymentAttempts is int attempts && !ProjectOptions.IsAllowedMaxPaymentAttempts(attempts))
            {
                return ServiceResult.Failure("Select a number of attempts (1–5 or Unlimited).");
            }

            if (dto.TotalMerchantAccounts is int accounts && (accounts < 1 || accounts > 1000))
            {
                return ServiceResult.Failure("Total merchant accounts must be between 1 and 1000.");
            }

            // More than one merchant account: say how PCI Pal should tell them apart.
            var hasSeveralMerchants = dto.TotalMerchantAccounts is > 1;
            if (hasSeveralMerchants && dto.MerchantIdentification is null)
            {
                return ServiceResult.Failure("Choose how PCI Pal should identify between the merchants.");
            }
            if (hasSeveralMerchants && dto.MerchantIdentification == MerchantIdentification.Other
                && string.IsNullOrWhiteSpace(dto.MerchantIdentificationOther))
            {
                return ServiceResult.Failure("Describe how PCI Pal should identify between the merchants.");
            }

            // The gateway must be one of the configured ones: it decides which fields the next step offers.
            if (dto.PaymentGatewayId is not { } gatewayId || await gatewayRepository.GetByIdAsync(gatewayId) is null)
            {
                return ServiceResult.Failure("Select your payment gateway.");
            }

            var config = project.PaymentConfiguration ??= new ProjectPaymentConfiguration();

            // A different gateway has different fields, so the fields chosen for the old one no longer
            // apply. They are removed, which makes the Adaptive Configuration step incomplete until it is
            // saved again (its other answers are kept and pre-filled).
            if (config.PaymentGatewayId != gatewayId)
            {
                foreach (var row in project.GatewayFields.ToList())
                {
                    project.GatewayFields.Remove(row);
                }
            }

            config.PaymentGatewayId = gatewayId;
            config.SupportedCards = EnumCsv.Join(dto.SupportedCards);
            config.MaxPaymentAttempts = dto.MaxPaymentAttempts;
            config.TotalMerchantAccounts = dto.TotalMerchantAccounts;
            // Only kept while there is more than one merchant account; the free text only for "Others".
            config.MerchantIdentification = hasSeveralMerchants ? dto.MerchantIdentification : null;
            config.MerchantIdentificationOther = hasSeveralMerchants && dto.MerchantIdentification == MerchantIdentification.Other
                ? Clean(dto.MerchantIdentificationOther)
                : null;
            config.CustomerAcquirer = Clean(dto.CustomerAcquirer);
            config.PaymentTypes = EnumCsv.Join(dto.PaymentTypes);
            config.Currencies = EnumCsv.Join(dto.Currencies);
            config.CardNumberMasking = dto.CardNumberMasking;
            config.SmsServiceProvider = dto.SmsServiceProvider;
            // The customer's own provider only applies when they chose "Other".
            config.OtherSmsProvider = dto.SmsServiceProvider == SmsServiceProvider.Other ? dto.OtherSmsProvider : null;
            config.EmailServiceProvider = dto.EmailServiceProvider;
            if (dto.EmailServiceProvider == EmailServiceProvider.Other && dto.OtherEmailProvider is null)
            {
                return ServiceResult.Failure("Select an email provider.");
            }

            config.OtherEmailProvider = dto.EmailServiceProvider == EmailServiceProvider.Other ? dto.OtherEmailProvider : null;
            config.HasSandboxEnvironment = dto.HasSandboxEnvironment;
            config.UpdatedAt = DateTimeOffset.UtcNow;

            return await AdvanceAndSaveAsync(project, ProjectStep.GatewayConfiguration);
        }

        // ---------------------------------------------------------------- Gateway configuration

        public async Task<ServiceResult<GatewayFieldOptionsDto>> GetGatewayFieldOptionsAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<GatewayFieldOptionsDto>.Failure("Project not found.");
            }

            if (project.PaymentConfiguration?.PaymentGatewayId is not { } gatewayId
                || await gatewayRepository.GetByIdAsync(gatewayId) is not { } gateway)
            {
                return ServiceResult<GatewayFieldOptionsDto>.Failure("Select your payment gateway on the Payment Configuration step first.");
            }

            var fields = await gatewayRepository.GetFieldsAsync(gatewayId);
            return ServiceResult<GatewayFieldOptionsDto>.Success(new GatewayFieldOptionsDto
            {
                GatewayId = gateway.Id,
                GatewayName = gateway.Name,
                Fields = fields.Select(f => new GatewayFieldOptionDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    FieldType = f.FieldType,
                    IsInput = f.IsInput,
                    Description = f.Description
                }).ToList()
            });
        }

        public async Task<ServiceResult<ProjectGatewayConfigurationDto>> GetGatewayConfigurationAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<ProjectGatewayConfigurationDto>.Failure("Project not found.");
            }

            return ServiceResult<ProjectGatewayConfigurationDto>.Success(MapGatewayConfiguration(project));
        }

        private static ProjectGatewayConfigurationDto MapGatewayConfiguration(Project project)
        {
            var g = project.GatewayConfiguration;
            // The fields the customer ticked: the selectable ones (Conditional / Optional) of the gateway.
            var selected = project.GatewayFields
                .Where(f => f.FieldType is GatewayFieldType.Conditional or GatewayFieldType.Optional
                            && f.PaymentGatewayFieldId.HasValue)
                .ToList();
            return new ProjectGatewayConfigurationDto
            {
                SelectedInputFields = selected.Where(f => f.IsInput).Select(f => f.PaymentGatewayFieldId!.Value).ToList(),
                SelectedOutputFields = selected.Where(f => !f.IsInput).Select(f => f.PaymentGatewayFieldId!.Value).ToList(),
                PostTransactionMethods = EnumCsv.Parse<PostTransactionMethod>(g?.PostTransactionMethods),
                RedirectUrl = g?.RedirectUrl,
                ContactCentrePlatform = g?.ContactCentrePlatform,
                ConnectionType = g?.ConnectionType,
                HandoverNotes = g?.HandoverNotes
            };
        }

        // ---------------------------------------------------------------- All details

        public async Task<ServiceResult<ProjectFullDetailsDto>> GetFullDetailsAsync(Guid customerId, Guid projectId)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult<ProjectFullDetailsDto>.Failure("Project not found.");
            }

            var overview = ProjectMapper.ToOverview(project);

            // Authentication first, then required, conditional and optional, each by name.
            var fields = project.GatewayFields
                .OrderBy(f => f.FieldType).ThenBy(f => f.Name)
                .Select(f => new ProjectGatewayFieldDto
                {
                    Name = f.Name,
                    FieldType = f.FieldType,
                    IsInput = f.IsInput,
                    Description = f.Description
                })
                .ToList();

            return ServiceResult<ProjectFullDetailsDto>.Success(new ProjectFullDetailsDto
            {
                Project = overview,
                CreatedAt = project.CreatedAt,
                LastUpdatedAt = project.UpdatedAt,
                Discovery = overview.HasDiscovery ? MapDiscovery(project) : null,
                TechnicalEnvironment = overview.HasTechnicalEnvironment ? MapTechnicalEnvironment(project) : null,
                PaymentConfiguration = overview.HasPaymentConfiguration ? MapPaymentConfiguration(project) : null,
                PaymentGatewayName = project.PaymentConfiguration?.PaymentGateway?.Name,
                GatewayConfiguration = overview.HasGatewayConfiguration ? MapGatewayConfiguration(project) : null,
                ApiDocumentation = project.Documents
                    .Where(d => d.Type == ProjectDocumentType.ApiDocumentation)
                    .OrderByDescending(d => d.UploadedAt)
                    .Select(d => new ProjectDocumentInfoDto
                    {
                        Id = d.Id,
                        FileName = d.OriginalFileName,
                        FileSizeBytes = d.FileSizeBytes,
                        UploadedAt = d.UploadedAt
                    })
                    .FirstOrDefault(),
                GatewayFields = fields
            });
        }

        public async Task<ServiceResult> SaveRedirectUrlAsync(Guid customerId, Guid projectId, string? redirectUrl)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }
            if (!ProjectStatusRules.CanCustomerProvideMissingItems(project.Status))
            {
                return ServiceResult.Failure("This project is already being provisioned and can no longer be changed.");
            }

            var config = project.GatewayConfiguration;
            if (config is null || !EnumCsv.Parse<PostTransactionMethod>(config.PostTransactionMethods).Contains(PostTransactionMethod.Redirect))
            {
                return ServiceResult.Failure("Redirect is not selected for this project.");
            }
            if (!UrlValidator.IsValidRedirectUrl(redirectUrl))
            {
                return ServiceResult.Failure("Enter a valid redirect URL starting with https://.");
            }

            config.RedirectUrl = Clean(redirectUrl);
            config.UpdatedAt = DateTimeOffset.UtcNow;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(customerId, projectId, "Redirect URL saved", "The redirect URL was added from the project overview.");

            return ServiceResult.Success();
        }

        public async Task<ServiceResult> SaveGatewayConfigurationAsync(Guid customerId, Guid projectId, ProjectGatewayConfigurationDto dto)
        {
            var project = await projectRepository.GetWithConfigurationAsync(customerId, projectId);
            if (project is null)
            {
                return ServiceResult.Failure("Project not found.");
            }
            if (ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                return ServiceResult.Failure(Submitted);
            }
            if (project.PaymentConfiguration is null)
            {
                return ServiceResult.Failure("Complete the Payment Configuration step first.");
            }

            var takesPhonePayments = ProjectMapper.ToOverview(project).TakesPhonePayments;
            if (takesPhonePayments && (dto.ContactCentrePlatform is null || dto.ConnectionType is null))
            {
                return ServiceResult.Failure("Choose your contact centre platform and connection type.");
            }

            // The fields come from the gateway chosen on the previous step.
            if (project.PaymentConfiguration.PaymentGatewayId is not { } gatewayId)
            {
                return ServiceResult.Failure("Select a payment gateway on the Payment Configuration step first.");
            }
            var gatewayFields = await gatewayRepository.GetFieldsAsync(gatewayId);

            // Only selectable fields of that gateway may be chosen, and only in the right direction.
            var selectableInputs = SelectableFieldIds(gatewayFields, isInput: true);
            var selectableOutputs = SelectableFieldIds(gatewayFields, isInput: false);
            if (dto.SelectedInputFields.Any(id => !selectableInputs.Contains(id))
                || dto.SelectedOutputFields.Any(id => !selectableOutputs.Contains(id)))
            {
                return ServiceResult.Failure("One of the selected fields is not valid.");
            }

            // Embedded CRM flows do not use Redirect; drop it if discovery says iframe embed.
            if (project.Discovery?.CrmPaymentExperience == CrmPaymentExperience.EmbeddedIframe)
            {
                dto.PostTransactionMethods = dto.PostTransactionMethods
                    .Where(m => m != PostTransactionMethod.Redirect)
                    .ToList();
            }

            // Redirect needs somewhere to send the customer afterwards.
            var usesRedirect = dto.PostTransactionMethods.Contains(PostTransactionMethod.Redirect);
            if (usesRedirect && !UrlValidator.IsValidRedirectUrl(dto.RedirectUrl))
            {
                return ServiceResult.Failure("Enter a valid redirect URL starting with https://.");
            }

            var config = project.GatewayConfiguration ??= new ProjectGatewayConfiguration();
            config.PostTransactionMethods = EnumCsv.Join(dto.PostTransactionMethods);
            config.RedirectUrl = usesRedirect ? Clean(dto.RedirectUrl) : null;
            config.ContactCentrePlatform = takesPhonePayments ? dto.ContactCentrePlatform : null;
            config.ConnectionType = takesPhonePayments ? dto.ConnectionType : null;
            config.HandoverNotes = takesPhonePayments ? Clean(dto.HandoverNotes) : null;
            config.UpdatedAt = DateTimeOffset.UtcNow;

            SyncGatewayFields(project, gatewayFields, dto);

            return await AdvanceAndSaveAsync(project, ProjectStep.ReadinessCheck);
        }

        /// <summary>
        /// Makes the project's field rows match the gateway's fields: authentication and required fields
        /// always, selectable ones only when ticked. Each row keeps its own copy of the field's name, type,
        /// direction and description, so a submitted project is not changed by later edits to the gateway.
        /// </summary>
        private static void SyncGatewayFields(
            Project project, IReadOnlyList<PaymentGatewayField> gatewayFields, ProjectGatewayConfigurationDto dto)
        {
            var wanted = gatewayFields
                .Where(f => !IsSelectable(f)
                            || (f.IsInput ? dto.SelectedInputFields : dto.SelectedOutputFields).Contains(f.Id))
                .ToList();
            var wantedIds = wanted.Select(f => f.Id).ToHashSet();

            // Drop rows that no longer apply (including rows saved before gateways were configured in the database).
            foreach (var row in project.GatewayFields.ToList())
            {
                if (row.PaymentGatewayFieldId is not { } id || !wantedIds.Contains(id))
                {
                    project.GatewayFields.Remove(row);
                }
            }

            foreach (var field in wanted)
            {
                var row = project.GatewayFields.FirstOrDefault(r => r.PaymentGatewayFieldId == field.Id);
                if (row is null)
                {
                    row = new ProjectGatewayFields { PaymentGatewayFieldId = field.Id };
                    project.GatewayFields.Add(row);
                }

                row.Name = field.Name;
                row.FieldType = field.FieldType;
                row.IsInput = field.IsInput;
                row.Description = field.Description;
            }
        }

        private static bool IsSelectable(PaymentGatewayField field)
            => field.FieldType is GatewayFieldType.Conditional or GatewayFieldType.Optional;

        private static HashSet<Guid> SelectableFieldIds(IReadOnlyList<PaymentGatewayField> fields, bool isInput)
            => fields.Where(f => IsSelectable(f) && f.IsInput == isInput).Select(f => f.Id).ToHashSet();

        // ---------------------------------------------------------------- Shared

        /// <summary>Moves the project on (resume from draft, CurrentStep never goes backwards) and saves.</summary>
        private async Task<ServiceResult> AdvanceAndSaveAsync(Project project, ProjectStep nextStep)
        {
            if (project.Status == ProjectStatus.Draft)
            {
                project.Status = ProjectStatus.InProgress;
            }
            if (project.CurrentStep < nextStep)
            {
                project.CurrentStep = nextStep;
            }
            project.UpdatedAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync();
            await activityLog.LogProjectAsync(project.CustomerId, project.Id, "Step saved", $"{StepSavedLabel(nextStep)} was saved.");
            return ServiceResult.Success();
        }

        /// <summary>The step that was just saved is the one before the step the project moves on to.</summary>
        private static string StepSavedLabel(ProjectStep nextStep)
            => ProjectOptions.ProjectStepLabels[(ProjectStep)((int)nextStep - 1)];

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
