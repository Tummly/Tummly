namespace TummlyBackend.DTOs.Auth
{
    public sealed class ExternalAuthExchangeDto
    {
        public string Token { get; set; } = "";

        public bool RememberDevice { get; set; } = true;

        public string? DeviceToken { get; set; }
    }

    public sealed class ExternalAuthAcceptTermsDto
    {
        public string Token { get; set; } = "";

        public bool TermsAccepted { get; set; }
    }
}
