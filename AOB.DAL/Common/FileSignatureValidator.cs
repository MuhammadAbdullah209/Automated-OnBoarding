namespace AOB.DAL.Common
{
    /// <summary>
    /// Checks that an uploaded file really is what its extension says (by its first bytes), and gives the
    /// content type the server serves it with, so the type sent by the browser is never trusted.
    /// This does not replace malware scanning.
    /// </summary>
    public static class FileSignatureValidator
    {
        private static readonly byte[] Pdf = "%PDF-"u8.ToArray();
        private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04];
        private static readonly byte[] Ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        private const int TextProbeBytes = 8192;

        /// <summary>Content type served for an allowed extension; anything else downloads as a plain binary.</summary>
        public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".json" => "application/json",
            ".yaml" or ".yml" => "application/yaml",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        /// <summary>
        /// True when the stream starts with the signature for the extension (text types: no binary bytes).
        /// The stream is rewound to its start. Extensions without a known signature are refused.
        /// </summary>
        public static async Task<bool> MatchesExtensionAsync(string extension, Stream content)
        {
            if (!content.CanSeek)
            {
                return false;
            }

            var buffer = new byte[TextProbeBytes];
            content.Position = 0;
            var read = await content.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false);
            content.Position = 0;

            var head = buffer.AsSpan(0, read);
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => head.StartsWith(Pdf),
                ".png" => head.StartsWith(Png),
                ".jpg" or ".jpeg" => head.StartsWith(Jpeg),
                ".docx" or ".xlsx" => head.StartsWith(Zip),
                ".doc" or ".xls" => head.StartsWith(Ole),
                ".json" or ".yaml" or ".yml" or ".txt" => read > 0 && !head.Contains((byte)0),
                _ => false
            };
        }
    }
}
