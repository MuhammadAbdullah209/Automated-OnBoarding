using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Domain.BuildRules;
using AOB.BAL.Entities;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>Copies what the build rules need out of a fully loaded project.</summary>
    public static class BuildAssessmentMapper
    {
        public static BuildAssessmentInput ToInput(Project project)
        {
            var payment = project.PaymentConfiguration;

            return new BuildAssessmentInput
            {
                Gateway = payment?.PaymentGateway is { } gateway
                    ? new BuildAssessmentInput.GatewayChoice(gateway.Name, gateway.IsStandard)
                    : null,
                // A row that no longer matches a gateway field cannot be checked, so it counts as non-standard.
                Fields = project.GatewayFields
                    .Select(f => new BuildAssessmentInput.FieldChoice(
                        f.Name,
                        f.PaymentGatewayField?.IsStandard ?? false,
                        f.PaymentGatewayField is not null))
                    .ToList(),
                PostTransactionMethods = EnumCsv.Parse<PostTransactionMethod>(project.GatewayConfiguration?.PostTransactionMethods),
                TotalMerchantAccounts = payment?.TotalMerchantAccounts,
                PaymentTypes = EnumCsv.Parse<PaymentType>(payment?.PaymentTypes),
                SmsProvider = payment?.SmsServiceProvider,
                EmailProvider = payment?.EmailServiceProvider
            };
        }
    }
}
