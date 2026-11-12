using AOB.BAL.Common.Enums;

namespace AOB.BAL.Interfaces.Common
{
    /// <summary>
    /// Stores uploaded files. Every kind of file (customer documents, project documents, ...)
    /// goes through the same three functions; the FileCategory only decides the folder.
    /// The current implementation uses the local disk; another one (blob storage, S3...) can
    /// replace it without touching the services.
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Saves the content under a generated name in the category's folder, inside the owner's
        /// sub-folder (e.g. the customer or project id), and returns the stored path. Keep that
        /// path to read or delete the file later.
        /// </summary>
        Task<string> SaveAsync(FileCategory category, Guid ownerId, string extension, Stream content);

        /// <summary>Opens a stored file for reading, or returns null if it no longer exists.</summary>
        Task<Stream?> OpenReadAsync(string storedPath);

        /// <summary>Deletes a stored file. Does nothing if it is already gone.</summary>
        Task DeleteAsync(string storedPath);
    }
}
