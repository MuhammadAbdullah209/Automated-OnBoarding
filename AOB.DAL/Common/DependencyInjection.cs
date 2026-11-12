using AOB.BAL.Common.Models;
using AOB.BAL.Interfaces.Common;
using AOB.BAL.Interfaces.Repositories;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.Implementations.Repositories;
using AOB.DAL.Implementations.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AOB.DAL.Common
{
    /// <summary>
    /// Single place where data-access and service registrations live, keeping Program.cs clean.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<OnboardingDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("OnboardingDb")));

            // Repositories and the unit of work share the request-scoped DbContext.
            services.AddScoped<IUserAuthRepository, UserAuthRepository>();
            services.AddScoped<IPartnerRepository, PartnerRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IOtpTokenRepository, OtpTokenRepository>();
            services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectDocumentRepository, ProjectDocumentRepository>();
            services.AddScoped<IPaymentGatewayRepository, PaymentGatewayRepository>();
            services.AddScoped<ICustomerDocumentRepository, CustomerDocumentRepository>();
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            // Email: sent through Microsoft Graph ("MicrosoftGraph" config section); templates from string constants (for now).
            services.AddSingleton(configuration.GetSection(MicrosoftGraphSettings.SectionName).Get<MicrosoftGraphSettings>()
                                  ?? new MicrosoftGraphSettings());
            services.AddSingleton<IEmailTemplateProvider, StaticEmailTemplateProvider>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<ICustomerStatusService, CustomerStatusService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();
            services.AddScoped<IProjectPurgeService, ProjectPurgeService>();
            services.AddScoped<IProjectProvisioningService, ProjectProvisioningService>();
            services.AddScoped<IProjectInformationService, ProjectInformationService>();
            services.AddScoped<IProjectCustomerEmailService, ProjectCustomerEmailService>();
            services.AddScoped<IProjectUsersService, ProjectUsersService>();

            // The domain rules that decide standard vs custom (see AOB.BAL.Domain.BuildRules.BuildRuleSet).
            services.AddSingleton(new AOB.BAL.Domain.BuildRules.BuildClassifier());

            // Provisioning steps run in this order.
            services.AddScoped<IProvisioningStep, PlatformAndTenantStep>();
            services.AddScoped<IProvisioningStep, ServiceCredentialsStep>();
            services.AddScoped<IProvisioningStep, FlowStep>();
            services.AddScoped<IPartnerService, PartnerService>();
            services.AddScoped<IInternalUserService, InternalUserService>();
            services.AddScoped<IDocumentTypeService, DocumentTypeService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IFlowLaunchService, FlowLaunchService>();
            services.AddScoped<IProjectConfigurationService, ProjectConfigurationService>();
            services.AddScoped<IProjectDocumentService, ProjectDocumentService>();
            services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
            services.AddScoped<ICustomerDocumentService, CustomerDocumentService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
