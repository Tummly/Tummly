using System.Net;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class LegalDocumentEndpointTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public LegalDocumentEndpointTests(
            TummlyWebApplicationFactory factory
        )
        {
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("privacy", "Tummly_Privacy_Policy.docx")]
        [InlineData("terms", "Tummly_Terms_and_Conditions.docx")]
        [InlineData("cookie-policy", "Tummly_Cookie_Policy.docx")]
        public async Task DownloadLegalDocument_ReturnsDocxFile(
            string documentKey,
            string fileName
        )
        {
            var response = await _client.GetAsync(
                $"/api/legal/documents/{documentKey}"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                LegalDocuments.ContentType,
                response.Content.Headers.ContentType?.MediaType
            );
            Assert.Equal(
                fileName,
                response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            );

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public async Task DownloadLegalDocument_UnknownKey_ReturnsNotFound()
        {
            var response = await _client.GetAsync(
                "/api/legal/documents/unknown"
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
