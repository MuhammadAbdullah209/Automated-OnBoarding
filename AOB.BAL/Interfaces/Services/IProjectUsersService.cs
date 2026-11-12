using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>
    /// The users a customer wants accounts for, on a custom build whose payment page is hosted by PCI Pal.
    /// The customer uploads a CSV (name, email, role); an internal user can ask for it. An internal user then
    /// marks the account created and the project completed (see <see cref="IProjectService"/>).
    /// </summary>
    public interface IProjectUsersService
    {
        /// <summary>The project's users, by name.</summary>
        Task<IReadOnlyList<CustomerUserDto>> GetUsersAsync(Guid customerId, Guid projectId);

        /// <summary>
        /// Reads the CSV and replaces the project's users with its rows. Nothing is saved unless the whole file is
        /// valid; the errors name the lines that are not. Returns the number of users saved.
        /// </summary>
        Task<ServiceResult<int>> UploadCsvAsync(Guid customerId, Guid projectId, string fileName, Stream content, long length);

        /// <summary>Asks the customer for the users CSV: the customer is shown an Action Required notice and emailed.</summary>
        Task<ServiceResult> RequestCsvAsync(Guid customerId, Guid projectId);
    }
}
