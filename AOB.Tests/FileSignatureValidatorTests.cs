using AOB.DAL.Common;
using Xunit;

namespace AOB.Tests
{
    public class FileSignatureValidatorTests
    {
        private static MemoryStream Bytes(params byte[] bytes) => new(bytes);

        [Fact]
        public async Task Pdf_signature_is_accepted_and_stream_is_rewound()
        {
            using var stream = Bytes("%PDF-1.7 body"u8.ToArray());

            Assert.True(await FileSignatureValidator.MatchesExtensionAsync(".pdf", stream));
            Assert.Equal(0, stream.Position);
        }

        [Fact]
        public async Task Renamed_executable_is_rejected_for_every_binary_type()
        {
            foreach (var extension in new[] { ".pdf", ".png", ".jpg", ".docx", ".xlsx", ".doc", ".xls" })
            {
                using var stream = Bytes(0x4D, 0x5A, 0x90, 0x00, 0x03); // "MZ" Windows executable
                Assert.False(await FileSignatureValidator.MatchesExtensionAsync(extension, stream));
            }
        }

        [Fact]
        public async Task Text_types_must_not_contain_binary_bytes()
        {
            using var text = Bytes("{\"a\":1}"u8.ToArray());
            using var binary = Bytes(0x7B, 0x00, 0x01);

            Assert.True(await FileSignatureValidator.MatchesExtensionAsync(".json", text));
            Assert.False(await FileSignatureValidator.MatchesExtensionAsync(".json", binary));
        }

        [Fact]
        public async Task Unknown_extension_is_rejected_and_served_as_binary()
        {
            using var stream = Bytes(1, 2, 3);

            Assert.False(await FileSignatureValidator.MatchesExtensionAsync(".exe", stream));
            Assert.Equal("application/octet-stream", FileSignatureValidator.ContentTypeFor(".html"));
        }
    }
}
