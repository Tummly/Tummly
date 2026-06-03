using System.Collections.Generic;

namespace TummlyBackend.DTOs.Trial
{
    public class CompleteSetupDto
    {
        /*
         =========================================
         SECURITY
         =========================================
        */
        public string? Token { get; set; }

        /*
         =========================================
         ACCOUNT
         =========================================
        */
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        /*
         =========================================
         GROUP / BUSINESS
         =========================================
        */
        public string GroupName { get; set; }
        public string BusinessCategory { get; set; }
        public string? PrimaryPhone { get; set; }
        public string? BusinessLink { get; set; }

        /*
         =========================================
         LOCATIONS (INLINE - NO SEPARATE DTO)
         =========================================
        */
        public List<LocationItem> Locations { get; set; }

        public class LocationItem
        {
            public string LocationName { get; set; }
            public string Address { get; set; }
            public string Postcode { get; set; }
            public string? LocationPhone { get; set; }
            public string? LocalContact { get; set; }
            public bool IncludeInRollout { get; set; }
        }

        /*
         =========================================
         ROLLOUT
         =========================================
        */
        public string? RolloutApproach { get; set; }
        public string? GuestPrompt { get; set; }

        /*
         =========================================
         FEEDBACK (INLINE OBJECT)
         =========================================
        */
        public FeedbackConfig FeedbackItems { get; set; }

        public class FeedbackConfig
        {
            public bool Rating { get; set; }
            public bool IssueTags { get; set; }
            public bool Comment { get; set; }
            public bool FirstName { get; set; }
            public bool Contact { get; set; }
            public bool Consent { get; set; }
        }

        /*
         =========================================
         THANK YOU
         =========================================
        */
        public string? ThankYouMessage { get; set; }

        /*
         =========================================
         OFFER
         =========================================
        */
        public string? OfferType { get; set; }
        public string? OfferTitle { get; set; }
        public string? OfferMessage { get; set; }
        public string? OfferExpiry { get; set; }
        public string? RedemptionMethod { get; set; }
        public string? UsageLimit { get; set; }
        public string? GuestPreview { get; set; }
    }
}