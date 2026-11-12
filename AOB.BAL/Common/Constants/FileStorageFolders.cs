using AOB.BAL.Common.Enums;

namespace AOB.BAL.Common.Constants
{
    /// <summary>Folder name used for each FileCategory inside the file storage root.</summary>
    public static class FileStorageFolders
    {
        public static readonly IReadOnlyDictionary<FileCategory, string> Names = new Dictionary<FileCategory, string>
        {
            [FileCategory.CustomerDocument] = "customer-documents",
            [FileCategory.ProjectDocument] = "project-documents"
        };

        /// <summary>Folder for the category; fails loudly if a new category has no folder yet.</summary>
        public static string For(FileCategory category)
            => Names.TryGetValue(category, out var folder)
                ? folder
                : throw new InvalidOperationException($"No storage folder is defined for file category '{category}'.");
    }
}
