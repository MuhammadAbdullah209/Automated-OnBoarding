namespace AOB.BAL.DTOs.Documents
{
    /// <summary>An uploaded file opened for download. The caller must dispose the stream.</summary>
    public class DocumentFileDto
    {
        public Stream Content { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
