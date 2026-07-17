using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class AzureBlobQueryAttachmentStorage : IQueryAttachmentStorage
    {
        private readonly ObjectStorageSettings _settings;
        private readonly IBlobObjectClient _blob;
        private readonly ILogger<AzureBlobQueryAttachmentStorage> _logger;

        public AzureBlobQueryAttachmentStorage(
            IOptions<ObjectStorageSettings> settings,
            ILogger<AzureBlobQueryAttachmentStorage> logger,
            IBlobObjectClient blob
        )
        {
            _settings = settings.Value;
            _logger = logger;
            _blob = blob;

            if (IsConfigured)
            {
                _logger.LogInformation(
                    "Azure Blob object storage configured for container {Bucket} at {Endpoint}",
                    _settings.Bucket,
                    _settings.Endpoint
                );
            }
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.Endpoint)
            && !string.IsNullOrWhiteSpace(_settings.Bucket);

        public async Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();

            if (contentLength < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contentLength),
                    contentLength,
                    "Content length must be non-negative."
                );
            }

            try
            {
                await _blob.UploadAsync(
                    storageKey,
                    content,
                    contentType,
                    contentLength,
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Blob upload failed for {StorageKey}: {ErrorMessage}",
                    storageKey,
                    ex.Message
                );
                throw;
            }
        }

        public async Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();
            return await _blob.OpenReadAsync(storageKey, cancellationToken);
        }

        public async Task DeleteAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            if (!IsConfigured)
            {
                return;
            }

            await _blob.DeleteAsync(storageKey, cancellationToken);
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Object storage is not configured. Set ObjectStorage__Endpoint "
                        + "and ObjectStorage__Bucket for Azure Blob "
                        + $"(Provider={ObjectStorageSettings.AzureBlobProvider})."
                );
            }
        }
    }
}
