using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TummlyBackend.DTOs.Trial
{
    public class CompleteSetupDto
    {
        public string Token { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;
        public string BusinessCategory { get; set; } = string.Empty;

        public string? PrimaryPhone { get; set; }
        public string? BusinessLink { get; set; }

        public List<LocationItem> Locations { get; set; } = new();

        public string? RolloutApproach { get; set; }
        public string? GuestPrompt { get; set; }

        public string? ThankYouMessage { get; set; }

        public string? OfferType { get; set; }
        public string? OfferTitle { get; set; }
        public string? OfferMessage { get; set; }
        public string? OfferExpiry { get; set; }
        public string? RedemptionMethod { get; set; }
        public string? UsageLimit { get; set; }

        public class LocationItem
        {
            public string LocationName { get; set; } = "";
            public string Address { get; set; } = "";
            public string? Postcode { get; set; }
            public string? LocationPhone { get; set; }
            public string? LocalContact { get; set; }
            public bool IncludeInRollout { get; set; }
        }

        public class FeedbackConfig
        {
            public bool Rating { get; set; }
            public bool IssueTags { get; set; }
            public bool Comment { get; set; }
            public bool FirstName { get; set; }
            public bool Contact { get; set; }
            public bool Consent { get; set; }
        }
    }
}