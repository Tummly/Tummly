namespace TummlyBackend.DTOs.PrivacyConsent
{
    public sealed class PatchPrivacyConsentTogglesRequest
    {
        public bool? EmailMarketingPermissionEnabled { get; set; }

        public bool? SmsMarketingPermissionEnabled { get; set; }

        public bool? FeedbackFollowUpPermissionEnabled { get; set; }
    }
}
