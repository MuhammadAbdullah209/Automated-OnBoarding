namespace AOB.BAL.Common.Enums
{
    /// <summary>
    /// The kind of file being stored. Each category has its own folder (see
    /// FileStorageFolders), but all categories use the same IFileStorage upload and retrieval
    /// functions. To add a new kind of file, add a value here and a folder name there.
    /// </summary>
    public enum FileCategory
    {
        CustomerDocument = 1,
        ProjectDocument = 2
    }
}
