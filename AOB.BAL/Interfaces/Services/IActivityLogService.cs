using AOB.BAL.DTOs.Activity;

namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Records and reads the recent-activity history of customers and their projects.</summary>
    public interface IActivityLogService
    {
        /// <summary>Records a customer-level event. Never throws: logging must not break the action it describes.</summary>
        Task LogCustomerAsync(Guid customerId, string action, string? reason = null);

        /// <summary>Records a project event (it also shows in the customer's feed).</summary>
        Task LogProjectAsync(Guid customerId, Guid projectId, string action, string? reason = null);

        /// <summary>Newest first: everything for the customer, customer and project events together.</summary>
        Task<IReadOnlyList<ActivityLogDto>> GetForCustomerAsync(Guid customerId, int take = 20);

        /// <summary>Newest first: events of one project. When <paramref name="take"/> is null, returns every event for that project.</summary>
        Task<IReadOnlyList<ActivityLogDto>> GetForProjectAsync(Guid customerId, Guid projectId, int? take = null);
    }
}
