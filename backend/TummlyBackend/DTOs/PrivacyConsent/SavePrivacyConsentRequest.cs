namespace TummlyBackend.DTOs.PrivacyConsent
{
    public sealed class SavePrivacyConsentRequest
    {
        public string? SmsConsentWording { get; set; }

        public string? EmailConsentWording { get; set; }
    }
}
