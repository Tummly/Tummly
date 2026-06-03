namespace TummlyBackend.DTOs.Auth
{
    public class SetupAccountDto
    {
        public string Token { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }

        public string? GroupName { get; set; }

        public string? BusinessCategory { get; set; }

        public string? NumLocations { get; set; }

        public string? PrimaryPhone { get; set; }

        public string? BusinessLink { get; set; }

        public string? RolloutApproach { get; set; }

        public string? GuestPrompt { get; set; }

        public string? ThankYouMessage { get; set; }

        public List<string>? FeedbackItems { get; set; }

        public string? OfferType { get; set; }

        public string? OfferTitle { get; set; }

        public string? OfferMessage { get; set; }

        public string? OfferExpiry { get; set; }

        public string? RedemptionMethod { get; set; }

        public string? UsageLimit { get; set; }

        public string? GuestPreview { get; set; }

        public List<LocationDto>? Locations { get; set; }
    }

    public class LocationDto
    {
        public string? LocationName { get; set; }

        public string? Address { get; set; }

        public string? Postcode { get; set; }

        public string? LocationPhone { get; set; }

        public string? LocalContact { get; set; }

        public bool IncludeInRollout { get; set; }
    }
}