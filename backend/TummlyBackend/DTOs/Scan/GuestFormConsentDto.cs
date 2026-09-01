namespace TummlyBackend.DTOs.Scan
{
    public sealed class GuestFormConsentDto
    {
        public bool EmailMarketingEnabled { get; init; }

        public bool SmsMarketingEnabled { get; init; }

        public bool FeedbackFollowUpEnabled { get; init; }

        public string? EmailConsentWording { get; init; }

        public string? SmsConsentWording { get; init; }

        public required string FeedbackFollowUpWording { get; init; }

        public bool HasAnyEnabledPermission =>
            EmailMarketingEnabled
            || SmsMarketingEnabled
            || FeedbackFollowUpEnabled;
    }
}
