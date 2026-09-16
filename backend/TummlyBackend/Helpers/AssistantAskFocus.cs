namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Narrow ask focus for question-first live answers. Filters which evidence
    /// domains belong in the body and drives create-vs-retrieve routing later.
    /// </summary>
    public enum AssistantAskFocusKind
    {
        CampaignsActive = 0,
        CampaignsAny = 1,
        Feedback = 2,
        OffersRedemptions = 3,
        OffersClaims = 4,
        CaptureQr = 5,
        Performance = 6,
        Guests = 7,
        CreateCampaign = 8,
        CreateOffer = 9,
        MixedSummary = 10,
        Unknown = 11,
    }

    /// <summary>
    /// Evidence domains that retrieve can fill. Used with
    /// <see cref="AssistantAskFocus.IncludesDomain"/>.
    /// </summary>
    public enum AssistantEvidenceDomain
    {
        Feedback = 0,
        Offers = 1,
        Campaigns = 2,
        Capture = 3,
        Home = 4,
        Guests = 5,
    }

    /// <summary>
    /// Classifies the operator ask into a focus kind and domain include rules.
    /// </summary>
    public static class AssistantAskFocus
    {
        private static readonly string[] CreateCampaignNeedles =
        [
            "create a campaign",
            "create campaign",
            "can you create a campaign",
            "start a campaign",
            "help me create a campaign",
        ];

        private static readonly string[] CreateOfferNeedles =
        [
            "create an offer",
            "create offer",
            "can you create an offer",
            "start an offer",
            "help me create an offer",
        ];

        private static readonly string[] CaptureQrNeedles =
        [
            "qr scan",
            "qr scans",
            "qr code scan",
            "scanned the qr",
            "scan the qr",
            "any qr",
        ];

        private static readonly string[] OffersRedemptionNeedles =
        [
            "offer redemption",
            "offer redemptions",
            "redeem an offer",
            "redeem offer",
            "redeemed an offer",
            "redeemed offer",
            "anyone redeem",
            "did anyone redeem",
            "redemption",
            "redemptions",
            "redeem",
        ];

        private static readonly string[] OffersClaimNeedles =
        [
            "offer claim",
            "offer claims",
            "claim an offer",
            "claimed an offer",
            "anyone claim",
        ];

        private static readonly string[] CampaignActiveNeedles =
        [
            "active campaign",
            "active campaigns",
            "campaigns live",
            "campaign live",
            "live campaign",
            "live campaigns",
            "campaigns sending",
            "campaign sending",
            "in-flight campaign",
            "in flight campaign",
            "scheduled campaign",
            "scheduled campaigns",
        ];

        private static readonly string[] CampaignAnyNeedles =
        [
            "campaign",
            "campaigns",
        ];

        private static readonly string[] FeedbackNeedles =
        [
            "feedback",
            "complain",
            "needs attention",
        ];

        private static readonly string[] PerformanceNeedles =
        [
            "performance overview",
            "performance",
        ];

        private static readonly string[] GuestsNeedles =
        [
            "location guest",
            "location guests",
            "list guests",
            "show guests",
            "how many guests",
            "guest count",
            "guests joined",
        ];

        private static readonly string[] MixedSummaryNeedles =
        [
            "summarise",
            "summarize",
            "overview",
            "what is going on",
            "how are we doing",
        ];

        public static AssistantAskFocusKind Detect(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (lower.Length == 0)
            {
                return AssistantAskFocusKind.Unknown;
            }

            // Create beats mutate-shaped campaign wording for focus.
            if (ContainsAny(lower, CreateCampaignNeedles))
            {
                return AssistantAskFocusKind.CreateCampaign;
            }

            if (ContainsAny(lower, CreateOfferNeedles))
            {
                return AssistantAskFocusKind.CreateOffer;
            }

            if (ContainsAny(lower, CaptureQrNeedles)
                || (ContainsAny(lower, "qr") && ContainsAny(lower, "scan")))
            {
                return AssistantAskFocusKind.CaptureQr;
            }

            if (ContainsAny(lower, OffersRedemptionNeedles))
            {
                return AssistantAskFocusKind.OffersRedemptions;
            }

            if (ContainsAny(lower, OffersClaimNeedles))
            {
                return AssistantAskFocusKind.OffersClaims;
            }

            if (ContainsAny(lower, CampaignActiveNeedles)
                || (NamesCampaign(lower)
                    && ContainsAny(lower, "active", "live", "sending", "scheduled")))
            {
                return AssistantAskFocusKind.CampaignsActive;
            }

            if (NamesCampaign(lower))
            {
                return AssistantAskFocusKind.CampaignsAny;
            }

            if (ContainsAny(lower, FeedbackNeedles))
            {
                return AssistantAskFocusKind.Feedback;
            }

            if (ContainsAny(lower, GuestsNeedles))
            {
                return AssistantAskFocusKind.Guests;
            }

            if (ContainsAny(lower, PerformanceNeedles))
            {
                return AssistantAskFocusKind.Performance;
            }

            if (ContainsAny(lower, MixedSummaryNeedles))
            {
                return AssistantAskFocusKind.MixedSummary;
            }

            return AssistantAskFocusKind.Unknown;
        }

        public static bool IncludesDomain(
            AssistantAskFocusKind focus,
            AssistantEvidenceDomain domain
        )
            => focus switch
            {
                AssistantAskFocusKind.CampaignsActive
                    or AssistantAskFocusKind.CampaignsAny
                    => domain == AssistantEvidenceDomain.Campaigns,

                AssistantAskFocusKind.Feedback
                    => domain == AssistantEvidenceDomain.Feedback,

                AssistantAskFocusKind.OffersRedemptions
                    or AssistantAskFocusKind.OffersClaims
                    => domain == AssistantEvidenceDomain.Offers,

                AssistantAskFocusKind.CaptureQr
                    => domain == AssistantEvidenceDomain.Capture,

                AssistantAskFocusKind.Performance
                    => domain == AssistantEvidenceDomain.Home,

                AssistantAskFocusKind.Guests
                    => domain == AssistantEvidenceDomain.Guests,

                // Create asks do not pull retrieve domains for the body.
                AssistantAskFocusKind.CreateCampaign
                    or AssistantAskFocusKind.CreateOffer
                    => false,

                AssistantAskFocusKind.MixedSummary
                    or AssistantAskFocusKind.Unknown
                    => true,

                _ => true,
            };

        private static bool NamesCampaign(string lower)
            => ContainsAny(lower, CampaignAnyNeedles);

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (var needle in needles)
            {
                if (haystack.Contains(needle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
