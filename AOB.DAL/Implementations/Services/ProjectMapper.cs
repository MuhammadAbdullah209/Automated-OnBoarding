using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>Builds the project overview from a project loaded with its screens.</summary>
    internal static class ProjectMapper
    {
        public static ProjectOverviewDto ToOverview(Project project)
        {
            var dto = Map(project);

            // Later steps depend on the Discovery answers. If those were changed after the step was saved, the step
            // is open again until it is saved again (as when the payment gateway changes). A submitted project is
            // read-only, so it keeps what it was submitted with.
            if (!ProjectStatusRules.IsPastCustomerSubmit(project.Status))
            {
                var postTransaction = EnumCsv.Parse<PostTransactionMethod>(project.GatewayConfiguration?.PostTransactionMethods);
                var gateway = project.GatewayConfiguration;

                // Projects saved before the payment page question was asked for everyone answer it before submitting.
                dto.HasDiscovery = dto.HasDiscovery && dto.PaymentExperience is not null;

                // A CRM needs its platform (asked on Technical environment).
                dto.HasTechnicalEnvironment = dto.HasTechnicalEnvironment
                    && (project.CrmSystem != CrmSystem.Crm || project.CrmPlatform is not null);

                // Phone payments need the contact centre answers; an embedded (iFrame) payment form has no Redirect.
                dto.HasGatewayConfiguration = dto.HasGatewayConfiguration
                    && (!dto.TakesPhonePayments || (gateway?.ContactCentrePlatform is not null && gateway.ConnectionType is not null))
                    && (dto.PaymentExperience != CrmPaymentExperience.EmbeddedIframe || !postTransaction.Contains(PostTransactionMethod.Redirect));
            }

            return dto;
        }

        private static ProjectOverviewDto Map(Project project) => new()
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt,
            Services = EnumCsv.Parse<PciPalService>(project.Services),
            Status = project.Status,
            CurrentStep = project.CurrentStep,
            DeletionScheduledFor = project.DeletionScheduledFor,
            BuildType = project.BuildType,
            UsersCsvRequestedAt = project.UsersCsvRequestedAt,
            InformationRequestStatus = project.InformationRequestStatus,
            PaymentExperience = project.Discovery?.CrmPaymentExperience,
            BuildReasons = string.IsNullOrWhiteSpace(project.BuildReasons)
                ? Array.Empty<string>()
                : project.BuildReasons.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            DeletionReason = project.DeletionReason,
            CrmSystem = project.CrmSystem,
            CrmPlatform = project.CrmPlatform,
            TakesPhonePayments = EnumCsv.Parse<CurrentPaymentMethod>(project.Discovery?.CurrentPaymentMethods)
                .Contains(CurrentPaymentMethod.PhoneCallWithAgent),
            UsesWebCallback = EnumCsv.Parse<PostTransactionMethod>(project.GatewayConfiguration?.PostTransactionMethods)
                .Contains(PostTransactionMethod.WebCallback),
            HasApiDocumentation = project.Documents.Any(d => d.Type == ProjectDocumentType.ApiDocumentation),
            HasDiscovery = project.Discovery is not null,
            HasTechnicalEnvironment = project.TechnicalEnvironment is not null,
            HasPaymentConfiguration = project.PaymentConfiguration is not null,
            // Needs its fields too: they are removed when the payment gateway changes, which reopens this step.
            HasGatewayConfiguration = project.GatewayConfiguration is not null && project.GatewayFields.Count > 0
        };
    }
}
