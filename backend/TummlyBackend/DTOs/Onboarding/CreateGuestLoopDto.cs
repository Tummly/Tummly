namespace TummlyBackend.DTOs.Onboarding
{
    public class CreateGuestLoopDto
    {
        /*
         =========================================
         QR & AUTOMATION
         =========================================
        */

        public bool SendPhysicalQrMaterials { get; set; }
            = false;

        public bool AutoSendReviewRequests { get; set; }
            = true;

        /*
         =========================================
         FRONTEND STEP 3
         =========================================
        */

        public string? Touchpoints { get; set; }

        public string? FeedbackTags { get; set; }

        public string? ThankYouMessage { get; set; }

        public string? OfferHeadline { get; set; }

        public string? OfferDetails { get; set; }

        public string? OfferExpiry { get; set; }

        public string? OfferRedemption { get; set; }

        public string? OfferUsageLimit { get; set; }
    }
}