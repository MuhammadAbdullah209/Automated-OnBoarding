using System.Text;
using AOB.BAL.Common.Enums;
using AOB.BAL.Common.Helpers;
using AOB.BAL.Common.Models;
using AOB.BAL.DTOs.Projects;
using AOB.BAL.Entities;
using AOB.BAL.Interfaces.Services;
using AOB.DAL.OnboardingContext;
using Microsoft.EntityFrameworkCore;

namespace AOB.DAL.Implementations.Services
{
    /// <summary>
    /// The users a customer wants accounts for. They come from a CSV (name, email, role) that the customer uploads,
    /// or that an internal user asks for. Only a project whose payment page is hosted by PCI Pal needs it, and
    /// only while it is in account creation.
    /// </summary>
    public class ProjectUsersService(
        OnboardingDbContext context,
        IActivityLogService activityLog,
        IProjectCustomerEmailService projectCustomerEmail) : IProjectUsersService
    {
        private const long MaxFileBytes = 1024 * 1024;

        public async Task<IReadOnlyList<CustomerUserDto>> GetUsersAsync(Guid customerId, Guid projectId)
            => await context.CustomerUsers.AsNoTracking()
                .Where(u => u.ProjectId == projectId && u.Project.CustomerId == customerId)
                .OrderBy(u => u.Name)
                .Select(u => new CustomerUserDto { Name = u.Name, Email = u.Email, Role = u.Role, CreatedAt = u.CreatedAt })
                .ToListAsync();

        public async Task<ServiceResult<int>> UploadCsvAsync(Guid customerId, Guid projectId, string fileName, Stream content, long length)
        {
            var project = await LoadAsync(customerId, projectId);
            var problem = CheckAccountNeeded(project);
            if (problem is not null)
            {
                return ServiceResult<int>.Failure(problem);
            }

            if (await context.ProjectProvisioningDetails.AnyAsync(d => d.ProjectId == projectId && d.AccountCreatedAt != null))
            {
                return ServiceResult<int>.Failure("The account was already created, so the users can no longer be changed.");
            }

            if (!string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult<int>.Failure("Upload a .csv file.");
            }

            if (length <= 0 || length > MaxFileBytes)
            {
                return ServiceResult<int>.Failure("The file is empty or larger than 1 MB.");
            }

            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var (rows, errors) = UserCsvParser.Parse(await reader.ReadToEndAsync());
            if (errors.Count > 0)
            {
                return ServiceResult<int>.Failure(string.Join("\n", errors));
            }

            // The new list replaces the old one.
            await context.CustomerUsers.Where(u => u.ProjectId == projectId).ExecuteDeleteAsync();
            var now = DateTimeOffset.UtcNow;
            context.CustomerUsers.AddRange(rows.Select(r => new CustomerUser
            {
                ProjectId = projectId,
                Name = r.Name,
                Email = r.Email,
                Role = r.Role,
                CreatedAt = now
            }));
            project!.UsersCsvRequestedAt = null;
            project.UpdatedAt = now;
            await context.SaveChangesAsync();

            var admins = rows.Count(r => r.Role == CustomerUserRole.Admin);
            await activityLog.LogProjectAsync(customerId, projectId, "Users uploaded",
                $"{rows.Count} user(s) were uploaded from a CSV ({admins} admin, {rows.Count - admins} user).");
            return ServiceResult<int>.Success(rows.Count);
        }

        public async Task<ServiceResult> RequestCsvAsync(Guid customerId, Guid projectId)
        {
            var project = await LoadAsync(customerId, projectId);
            var problem = CheckAccountNeeded(project);
            if (problem is not null)
            {
                return ServiceResult.Failure(problem);
            }

            project!.UsersCsvRequestedAt = DateTimeOffset.UtcNow;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();
            await activityLog.LogProjectAsync(customerId, projectId, "Users CSV requested",
                "An internal user asked the customer to upload the users CSV (name, email, role).");
            await projectCustomerEmail.SendUsersCsvRequestedAsync(customerId, projectId, project!.Name);
            return ServiceResult.Success();
        }

        private async Task<Project?> LoadAsync(Guid customerId, Guid projectId)
            => await context.Projects
                .Include(p => p.Discovery)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.CustomerId == customerId);

        private static string? CheckAccountNeeded(Project? project)
        {
            if (project is null)
            {
                return "Project not found.";
            }

            if (project.Status != ProjectStatus.AccountCreation
                || project.Discovery?.CrmPaymentExperience != CrmPaymentExperience.HostedByPciPal)
            {
                return "Users are only needed while a project with a PCI Pal hosted payment page is in account creation.";
            }

            return null;
        }

    }
}
