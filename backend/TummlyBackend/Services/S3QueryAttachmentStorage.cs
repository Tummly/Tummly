using System.Text.RegularExpressions;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    public class S3QueryAttachmentStorage : IQueryAttachmentStorage
    {
        private static readonly Regex BucketHostEndpointPattern = new(
            @"^https?://(?<bucket>[^.]+)\.(?<region>[a-z0-9-]+)\.digitaloceanspaces\.com/?$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private readonly string _endpoint;
        private readonly string _bucket;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly ILogger<S3QueryAttachmentStorage> _logger;
        private readonly Lazy<IAmazonS3> _client;

        static S3QueryAttachmentStorage()
        {
            // AWSSDK.S3 3.7.412+ adds checksum headers that DO Spaces rejects.
            AWSConfigsS3.DisableDefaultChecksumValidation = true;
        }

        public S3QueryAttachmentStorage(
            IOptions<ObjectStorageSettings> settings,
            ILogger<S3QueryAttachmentStorage> logger
        )
        {
            _logger = logger;
            var normalized = NormalizeSettings(settings.Value);
            _endpoint = normalized.Endpoint;
            _bucket = normalized.Bucket;
            _accessKey = normalized.AccessKey;
            _secretKey = normalized.SecretKey;
            _client = new Lazy<IAmazonS3>(CreateClient);

            if (IsConfigured)
            {
                _logger.LogInformation(
                    "Object storage configured for bucket {Bucket} at {Endpoint}",
                    _bucket,
                    _endpoint
                );
            }
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_endpoint)
            && !string.IsNullOrWhiteSpace(_bucket)
            && !string.IsNullOrWhiteSpace(_accessKey)
            && !string.IsNullOrWhiteSpace(_secretKey);

        public async Task UploadAsync(
            string storageKey,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default
        )
        {
            EnsureConfigured();

            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = storageKey,
                InputStream = buffer,
                ContentType = contentType,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true,
                UseChunkEncoding = false,
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
                new GetObjectRequest
                {
                    BucketName = _bucket,
                    Key = storageKey,
                },
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
                _bucket,
                storageKey,
                cancellationToken
            );
        }

        private IAmazonS3 CreateClient()
        {
            // Match DigitalOcean's minimal C# example; path-style avoids host/signing mismatches.
            // https://docs.digitalocean.com/products/spaces/how-to/use-aws-sdks/
            var config = new AmazonS3Config
            {
                ServiceURL = _endpoint,
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
            };

            return new AmazonS3Client(
                new BasicAWSCredentials(_accessKey, _secretKey),
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

        internal static ObjectStorageSettings NormalizeSettings(
            ObjectStorageSettings settings
        )
        {
            var endpoint = SanitizeConfigValue(settings.Endpoint).TrimEnd('/');
            var bucket = SanitizeConfigValue(settings.Bucket);
            var accessKey = SanitizeConfigValue(settings.AccessKey);
            var secretKey = SanitizeConfigValue(settings.SecretKey);

            var bucketHostMatch = BucketHostEndpointPattern.Match(endpoint);

            if (bucketHostMatch.Success)
            {
                var hostBucket = bucketHostMatch.Groups["bucket"].Value;
                var region = bucketHostMatch.Groups["region"].Value;

                endpoint = $"https://{region}.digitaloceanspaces.com";

                if (string.IsNullOrWhiteSpace(bucket))
                {
                    bucket = hostBucket;
                }
            }
            else if (
                !endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            )
            {
                endpoint = $"https://{endpoint}";
            }

            return new ObjectStorageSettings
            {
                Endpoint = endpoint,
                Bucket = bucket,
                AccessKey = accessKey,
                SecretKey = secretKey,
                Region = SanitizeConfigValue(settings.Region),
            };
        }

        /// <summary>
        /// Railway users sometimes paste .env-style quoted values; quotes break SigV4.
        /// </summary>
        private static string SanitizeConfigValue(string value)
        {
            var trimmed = value.Trim();

            if (
                trimmed.Length >= 2
                && trimmed.StartsWith('"')
                && trimmed.EndsWith('"')
            )
            {
                return trimmed[1..^1].Trim();
            }

            if (
                trimmed.Length >= 2
                && trimmed.StartsWith('\'')
                && trimmed.EndsWith('\'')
            )
            {
                return trimmed[1..^1].Trim();
            }

            return trimmed;
        }
    }
}
