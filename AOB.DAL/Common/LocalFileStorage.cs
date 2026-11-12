using AOB.BAL.Common.Constants;
using AOB.BAL.Common.Enums;
using AOB.BAL.Interfaces.Common;
using Microsoft.Extensions.Configuration;

namespace AOB.DAL.Common
{
    /// <summary>
    /// Keeps uploaded files on local disk under "FileStorage:RootPath" (default App_Data/Uploads,
    /// relative to the app's working folder). The folder is outside wwwroot, so files can only be
    /// reached through the application's own download action.
    /// </summary>
    public class LocalFileStorage(IConfiguration configuration) : IFileStorage
    {
        private readonly string _root = Path.GetFullPath(
            configuration["FileStorage:RootPath"] ?? Path.Combine("App_Data", "Uploads"),
            Directory.GetCurrentDirectory());

        public async Task<string> SaveAsync(FileCategory category, Guid ownerId, string extension, Stream content)
        {
            // Folder comes from the category and the name is generated here, so nothing from the
            // upload can influence the path. Result looks like: customer-documents/<ownerId>/<guid>.pdf
            var storedPath = $"{FileStorageFolders.For(category)}/{ownerId:N}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var fullPath = ResolvePath(storedPath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await using var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(target);

            return storedPath;
        }

        public Task<Stream?> OpenReadAsync(string storedPath)
        {
            var fullPath = ResolvePath(storedPath);
            Stream? stream = File.Exists(fullPath)
                ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true)
                : null;
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string storedPath)
        {
            var fullPath = ResolvePath(storedPath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }

        /// <summary>Maps a stored path to a disk path and refuses anything that escapes the root folder.</summary>
        private string ResolvePath(string storedPath)
        {
            var fullPath = Path.GetFullPath(storedPath, _root);
            if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid file path.");
            }
            return fullPath;
        }
    }
}
