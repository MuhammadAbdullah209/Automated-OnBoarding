namespace AOB.BAL.Interfaces.Repositories
{
    /// <summary>
    /// Commits the pending changes of all repositories in one transaction.
    /// Services call their repositories directly to add/update/query, then call
    /// SaveChangesAsync once when done.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>Persists all pending changes. Returns the number of affected rows.</summary>
        Task<int> SaveChangesAsync();
    }
}
