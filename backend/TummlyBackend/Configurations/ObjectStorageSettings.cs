namespace TummlyBackend.Configurations
{
    public class ObjectStorageSettings
    {
        public string Endpoint { get; set; } = string.Empty;

        public string Bucket { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public string Region { get; set; } = "lon1";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Endpoint)
            && !string.IsNullOrWhiteSpace(Bucket)
            && !string.IsNullOrWhiteSpace(AccessKey)
            && !string.IsNullOrWhiteSpace(SecretKey);
    }
}
