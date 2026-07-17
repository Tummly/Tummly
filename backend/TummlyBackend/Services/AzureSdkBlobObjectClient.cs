using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Azure SDK-backed <see cref="IBlobObjectClient"/> for Help Centre attachments.
    /// </summary>
    public sealed class AzureSdkBlobObjectClient : IBlobObjectClient
    {
        private readonly BlobContainerClient _container;

        public AzureSdkBlobObjectClient(IOptions<ObjectStorageSettings> settings)
        {
            _container = CreateContainerClient(settings.Value);
        }

        public AzureSdkBlobObjectClient(BlobContainerClient container)
        {
            _container = container;
        }

        public async Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        )
        {
            var blob = _container.GetBlobClient(storageKey);
            await blob.UploadAsync(content, overwrite: true, cancellationToken);
            await blob.SetHttpHeadersAsync(
                new BlobHttpHeaders { ContentType = contentType },
                cancellationToken: cancellationToken
            );
        }

        public async Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            var blob = _container.GetBlobClient(storageKey);
            var memoryStream = new MemoryStream();
            await blob.DownloadToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            var blob = _container.GetBlobClient(storageKey);
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        internal static BlobContainerClient CreateContainerClient(
            ObjectStorageSettings settings
        )
        {
            var containerName = settings.Bucket.Trim();

            if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                return new BlobContainerClient(
                    settings.ConnectionString.Trim(),
                    containerName
                );
            }

            var serviceUri = NormalizeServiceUri(settings.Endpoint);
            var serviceClient = new BlobServiceClient(
                serviceUri,
                new DefaultAzureCredential()
            );
            return serviceClient.GetBlobContainerClient(containerName);
        }

        private static Uri NormalizeServiceUri(string endpoint)
        {
            var trimmed = endpoint.Trim().TrimEnd('/');
            if (
                !trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith(
                    "https://",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                trimmed = $"https://{trimmed}";
            }

            return new Uri(trimmed);
        }
    }
}
