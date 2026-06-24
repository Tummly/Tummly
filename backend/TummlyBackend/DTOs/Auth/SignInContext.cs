namespace TummlyBackend.DTOs.Auth
{
    public sealed class SignInContext
    {
        public DateTime SignedInAtUtc { get; init; } = DateTime.UtcNow;

        public string? IpAddress { get; init; }

        public string? UserAgent { get; init; }
    }
}
