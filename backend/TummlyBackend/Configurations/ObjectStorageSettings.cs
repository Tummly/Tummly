namespace TummlyBackend.Configurations
{
    public class ObjectStorageSettings
    {
        public const string S3Provider = "S3";

        public const string AzureBlobProvider = "AzureBlob";

        /// <summary>
        /// <c>S3</c> (DigitalOcean Spaces / S3-compatible) or <c>AzureBlob</c>.
        /// </summary>
        public string Provider { get; set; } = S3Provider;

        /// <summary>
        /// S3: regional Spaces endpoint. Azure Blob: blob service URI
        /// (e.g. https://account.blob.core.windows.net).
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// S3 bucket name or Azure Blob container name.
        /// </summary>
        public string Bucket { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional full Azure Storage connection string (local / key auth).
        /// When empty, Azure Blob uses DefaultAzureCredential (managed identity).
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        public string Region { get; set; } = "lon1";

        public bool IsAzureBlob =>
            string.Equals(
                Provider,
                AzureBlobProvider,
                StringComparison.OrdinalIgnoreCase
            );

        public bool IsConfigured =>
            IsAzureBlob
                ? !string.IsNullOrWhiteSpace(Endpoint)
                    && !string.IsNullOrWhiteSpace(Bucket)
                : !string.IsNullOrWhiteSpace(Endpoint)
                    && !string.IsNullOrWhiteSpace(Bucket)
                    && !string.IsNullOrWhiteSpace(AccessKey)
                    && !string.IsNullOrWhiteSpace(SecretKey);
    }
}
