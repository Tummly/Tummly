using System.Text.RegularExpressions;
using Minio;
using Minio.DataModel.Args;
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

        private readonly string _endpointHost;
        private readonly string _bucket;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly ILogger<S3QueryAttachmentStorage> _logger;
        private readonly Lazy<IMinioClient> _client;

        public S3QueryAttachmentStorage(
            IOptions<ObjectStorageSettings> settings,
            ILogger<S3QueryAttachmentStorage> logger
        )
        {
            _logger = logger;
            var normalized = NormalizeSettings(settings.Value);
            _endpointHost = ParseEndpointHost(normalized.Endpoint);
            _bucket = normalized.Bucket;
            _accessKey = normalized.AccessKey;
            _secretKey = normalized.SecretKey;
            _client = new Lazy<IMinioClient>(CreateClient);

            if (IsConfigured)
            {
                _logger.LogInformation(
                    "Object storage configured for bucket {Bucket} at {EndpointHost}",
                    _bucket,
                    _endpointHost
                );
            }
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_endpointHost)
            && !string.IsNullOrWhiteSpace(_bucket)
            && !string.IsNullOrWhiteSpace(_accessKey)
            && !string.IsNullOrWhiteSpace(_secretKey);

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
                // Stream directly to Spaces when length is known — avoid a full
                // in-memory copy of each attachment on the request path.
                await _client.Value.PutObjectAsync(
                    new PutObjectArgs()
                        .WithBucket(_bucket)
                        .WithObject(storageKey)
                        .WithStreamData(content)
                        .WithObjectSize(contentLength)
                        .WithContentType(contentType),
                    cancellationToken
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Spaces upload failed for {StorageKey}: {ErrorMessage}",
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

            var memoryStream = new MemoryStream();

            await _client.Value.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(storageKey)
                    .WithCallbackStream(async (stream, ct) =>
                    {
                        await stream.CopyToAsync(memoryStream, ct);
                    }),
                cancellationToken
            );

            memoryStream.Position = 0;
            return memoryStream;
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

            await _client.Value.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(_bucket)
                    .WithObject(storageKey),
                cancellationToken
            );
        }

        private IMinioClient CreateClient()
        {
            return new MinioClient()
                .WithEndpoint(_endpointHost)
                .WithCredentials(_accessKey, _secretKey)
                .WithSSL()
                .Build();
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

        private static string ParseEndpointHost(string endpoint)
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            return endpoint
                .Trim()
                .TrimStart('/')
                .TrimEnd('/');
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
