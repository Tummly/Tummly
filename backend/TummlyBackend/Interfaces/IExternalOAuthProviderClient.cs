namespace TummlyBackend.Interfaces
{
    public sealed class ExternalOAuthProfile
    {
        public string Provider { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool EmailVerified { get; set; }

        public string? FullName { get; set; }
    }

    public interface IExternalOAuthProviderClient
    {
        string BuildAuthorizationUrl(string provider, string state);

        Task<ExternalOAuthProfile> ExchangeCodeAsync(
            string provider,
            string code,
            CancellationToken cancellationToken = default
        );
    }
}
