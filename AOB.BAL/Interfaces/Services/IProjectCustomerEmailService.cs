namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Customer-facing project emails (templates in <see cref="Common.Templates.EmailTemplates"/>).</summary>
    public interface IProjectCustomerEmailService
    {
        /// <summary>Asks the customer to upload the users CSV for account creation.</summary>
        Task SendUsersCsvRequestedAsync(Guid customerId, Guid projectId, string projectName);

        /// <summary>Notifies the customer that the project is completed.</summary>
        Task SendProjectCompletedAsync(Guid customerId, Guid projectId, string projectName);
    }
}
