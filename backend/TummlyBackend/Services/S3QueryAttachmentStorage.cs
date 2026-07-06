using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class S3QueryAttachmentStorage : IQueryAttachmentStorage
    {
        private readonly ObjectStorageSettings _settings;
        private readonly Lazy<IAmazonS3> _client;

        public S3QueryAttachmentStorage(IOptions<ObjectStorageSettings> settings)
        {
            _settings = settings.Value;
            _client = new Lazy<IAmazonS3>(CreateClient);
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();

            var request = new PutObjectRequest
            {
                BucketName = _settings.Bucket,
                Key = storageKey,
                InputStream = content,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private,
            };

            await _client.Value.PutObjectAsync(request, cancellationToken);
        }

        public async Task<Stream> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();

            var response = await _client.Value.GetObjectAsync(
                _settings.Bucket,
                storageKey,
                cancellationToken
            );

            return response.ResponseStream;
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

            await _client.Value.DeleteObjectAsync(
                _settings.Bucket,
                storageKey,
                cancellationToken
            );
        }

        private IAmazonS3 CreateClient()
        {
            var config = new AmazonS3Config
            {
                ServiceURL = NormalizeEndpoint(_settings.Endpoint),
                ForcePathStyle = false,
            };

            if (!string.IsNullOrWhiteSpace(_settings.Region))
            {
                config.AuthenticationRegion = _settings.Region;
            }

            return new AmazonS3Client(
                _settings.AccessKey,
                _settings.SecretKey,
                config
            );
        }

        private void EnsureConfigured()
        {
            if (!IsConfigured)
            {
                throw new InvalidOperationException(
                    "Object storage is not configured. Set ObjectStorage__Endpoint, "
                        + "ObjectStorage__Bucket, ObjectStorage__AccessKey, and "
                        + "ObjectStorage__SecretKey."
                );
            }
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            var trimmed = endpoint.Trim().TrimEnd('/');

            if (
                !trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            )
            {
                return $"https://{trimmed}";
            }

            return trimmed;
        }
    }
}
