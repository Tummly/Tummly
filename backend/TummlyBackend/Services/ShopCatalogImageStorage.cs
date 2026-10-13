using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public sealed class ShopCatalogObjectStorageSettings : ObjectStorageSettings
    {
    }

    /// <summary>
    /// In-memory shop catalog images for Testing / local without blob config.
    /// </summary>
    public sealed class InMemoryShopCatalogImageStorage : IShopCatalogImageStorage
    {
        private readonly Dictionary<string, (byte[] Bytes, string ContentType)> _objects =
            new(StringComparer.Ordinal);

        public bool IsConfigured => true;

        public Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        )
        {
            using var memory = new MemoryStream();
            content.CopyTo(memory);
            _objects[storageKey] = (memory.ToArray(), contentType);
            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            if (!_objects.TryGetValue(storageKey, out var entry))
            {
                throw new FileNotFoundException(
                    $"Shop catalog image '{storageKey}' was not found."
                );
            }

            return Task.FromResult<Stream>(new MemoryStream(entry.Bytes));
        }

        public Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            _objects.Remove(storageKey);
            return Task.CompletedTask;
        }
    }

    public sealed class AzureShopCatalogImageStorage : IShopCatalogImageStorage
    {
        private readonly ShopCatalogObjectStorageSettings _settings;
        private readonly Lazy<BlobContainerClient> _container;

        public AzureShopCatalogImageStorage(
            IOptions<ShopCatalogObjectStorageSettings> settings
        )
        {
            _settings = settings.Value;
            // Defer client creation so list/tag APIs work when blob is unset.
            _container = new Lazy<BlobContainerClient>(
                () => CreateContainerClient(_settings)
            );
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();
            var blob = _container.Value.GetBlobClient(storageKey);
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
            EnsureConfigured();
            var blob = _container.Value.GetBlobClient(storageKey);
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
            EnsureConfigured();
            var blob = _container.Value.GetBlobClient(storageKey);
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Shop catalog object storage is not configured."
                );
            }
        }

        private static BlobContainerClient CreateContainerClient(
            ShopCatalogObjectStorageSettings settings
        )
        {
            if (!settings.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Shop catalog object storage is not configured."
                );
            }

            var containerName = settings.Bucket.Trim();
            if (!string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                return new BlobContainerClient(
                    settings.ConnectionString.Trim(),
                    containerName
                );
            }

            var trimmed = settings.Endpoint.Trim().TrimEnd('/');
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

            var serviceClient = new BlobServiceClient(
                new Uri(trimmed),
                new DefaultAzureCredential()
            );
            return serviceClient.GetBlobContainerClient(containerName);
        }
    }

    public sealed class S3ShopCatalogImageStorage : IShopCatalogImageStorage
    {
        private readonly ShopCatalogObjectStorageSettings _settings;

        public S3ShopCatalogImageStorage(
            IOptions<ShopCatalogObjectStorageSettings> settings
        )
        {
            _settings = settings.Value;
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();
            var client = CreateClient();
            var putArgs = new PutObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(storageKey)
                .WithStreamData(content)
                .WithObjectSize(contentLength)
                .WithContentType(contentType);
            await client.PutObjectAsync(putArgs, cancellationToken);
        }

        public async Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();
            var client = CreateClient();
            var memory = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(storageKey)
                .WithCallbackStream(stream => stream.CopyTo(memory));
            await client.GetObjectAsync(getArgs, cancellationToken);
            memory.Position = 0;
            return memory;
        }

        public async Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();
            var client = CreateClient();
            var removeArgs = new RemoveObjectArgs()
                .WithBucket(_settings.Bucket)
                .WithObject(storageKey);
            await client.RemoveObjectAsync(removeArgs, cancellationToken);
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Shop catalog object storage is not configured."
                );
            }
        }

        private IMinioClient CreateClient()
        {
            var endpoint = _settings.Endpoint
                .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
                .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');
            return new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(_settings.AccessKey, _settings.SecretKey)
                .WithSSL(
                    _settings.Endpoint.StartsWith(
                        "https",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Build();
        }
    }
}
