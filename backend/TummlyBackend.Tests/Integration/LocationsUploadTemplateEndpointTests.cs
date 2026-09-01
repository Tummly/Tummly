using System.Net;
using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class LocationsUploadTemplateEndpointTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public LocationsUploadTemplateEndpointTests(
            TummlyWebApplicationFactory factory
        )
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DownloadLocationsUploadTemplate_ReturnsCsvFile()
        {
            var response = await _client.GetAsync(
                "/api/auth/locations-upload-template"
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                LocationUploadTemplate.ContentType,
                response.Content.Headers.ContentType?.MediaType
            );
            Assert.Equal(
                LocationUploadTemplate.FileName,
                response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            );

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 0);

            var content = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.StartsWith("Location name,Address,City,Postcode,", content);
        }
    }
}
