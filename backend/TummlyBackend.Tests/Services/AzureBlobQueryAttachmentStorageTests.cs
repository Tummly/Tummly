using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AzureBlobQueryAttachmentStorageTests
    {
        [Fact]
        public void IsConfigured_is_false_when_endpoint_or_bucket_missing()
        {
            var storage = CreateStorage(
                new ObjectStorageSettings
                {
                    Provider = ObjectStorageSettings.AzureBlobProvider,
                    Endpoint = "",
                    Bucket = "help-centre-attachments",
                }
            );

            Assert.False(storage.IsConfigured);
        }

        [Fact]
        public void IsConfigured_is_true_when_endpoint_and_bucket_set_for_AzureBlob()
        {
            var storage = CreateStorage(
                new ObjectStorageSettings
                {
                    Provider = ObjectStorageSettings.AzureBlobProvider,
                    Endpoint = "https://sttummlyqavfavue.blob.core.windows.net",
                    Bucket = "help-centre-attachments",
                }
            );

            Assert.True(storage.IsConfigured);
        }

        [Fact]
        public async Task UploadAsync_throws_when_not_configured()
        {
            var storage = CreateStorage(
                new ObjectStorageSettings
                {
                    Provider = ObjectStorageSettings.AzureBlobProvider,
                }
            );

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    storage.UploadAsync(
                        "key",
                        new MemoryStream([1, 2, 3]),
                        "application/octet-stream",
                        3
                    )
            );

            Assert.Contains(
                "ObjectStorage__Endpoint",
                ex.Message,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "ObjectStorage__Bucket",
                ex.Message,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task Upload_OpenRead_Delete_round_trip_through_blob_port()
        {
            var port = new FakeBlobPort();
            var storage = CreateStorage(
                new ObjectStorageSettings
                {
                    Provider = ObjectStorageSettings.AzureBlobProvider,
                    Endpoint = "https://stexample.blob.core.windows.net",
                    Bucket = "help-centre-attachments",
                },
                port
            );

            var payload = new byte[] { 10, 20, 30 };
            await storage.UploadAsync(
                "queries/1/a.pdf",
                new MemoryStream(payload),
                "application/pdf",
                payload.Length
            );

            await using var read = await storage.OpenReadAsync("queries/1/a.pdf");
            using var buffer = new MemoryStream();
            await read.CopyToAsync(buffer);

            Assert.Equal(payload, buffer.ToArray());
            Assert.Equal("application/pdf", port.LastContentType);

            await storage.DeleteAsync("queries/1/a.pdf");
            Assert.False(port.Objects.ContainsKey("queries/1/a.pdf"));
        }

        private static AzureBlobQueryAttachmentStorage CreateStorage(
            ObjectStorageSettings settings,
            IBlobObjectClient? port = null
        )
        {
            return new AzureBlobQueryAttachmentStorage(
                Options.Create(settings),
                NullLogger<AzureBlobQueryAttachmentStorage>.Instance,
                port ?? new FakeBlobPort()
            );
        }

        private sealed class FakeBlobPort : IBlobObjectClient
        {
            public Dictionary<string, byte[]> Objects { get; } = new();

            public string? LastContentType { get; private set; }

            public Task UploadAsync(
                string storageKey,
                Stream content,
                string contentType,
                long contentLength,
                CancellationToken cancellationToken = default
            )
            {
                using var copy = new MemoryStream();
                content.CopyTo(copy);
                Objects[storageKey] = copy.ToArray();
                LastContentType = contentType;
                return Task.CompletedTask;
            }

            public Task<Stream> OpenReadAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<Stream>(
                    new MemoryStream(Objects[storageKey])
                );
            }

            public Task DeleteAsync(
                string storageKey,
                CancellationToken cancellationToken = default
            )
            {
                Objects.Remove(storageKey);
                return Task.CompletedTask;
            }
        }
    }
}
