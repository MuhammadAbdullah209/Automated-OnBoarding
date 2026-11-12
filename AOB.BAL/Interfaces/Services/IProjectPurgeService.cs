namespace AOB.BAL.Interfaces.Services
{
    /// <summary>Permanently removes projects whose deletion countdown has run out. Run by a background job.</summary>
    public interface IProjectPurgeService
    {
        /// <summary>
        /// Deletes every project whose scheduled deletion time has passed, together with everything stored for it
        /// (setup answers, gateway fields, provisioning details, uploaded files). Activity log entries are kept.
        /// Returns how many projects were deleted.
        /// </summary>
        Task<int> PurgeDueAsync(CancellationToken cancellationToken = default);
    }
}
