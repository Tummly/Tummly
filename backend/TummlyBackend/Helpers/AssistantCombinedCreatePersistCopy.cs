using TummlyBackend.DTOs.Offers;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Server-owned completing live answer copy for Create Campaign with Offer.
    /// Overwrites Fake/model body. Eligible count is never invented.
    /// </summary>
    public static class AssistantCombinedCreatePersistCopy
    {
        public const string SuccessTitle = "Campaign Draft saved with Offer";
        public const string FailureTitle = "Campaign Draft not saved";

        public static string SuccessBody(
            string locationName,
            string channelLabel,
            string audienceLabel,
            int? eligibleCount,
            string typeLabel,
            string valueLabel,
            string validityLabel,
            string offerTitle,
            string campaignName
        )
        {
            return
                "## Interpretation\n"
                + $"I saved a Campaign Draft with an attached Offer for {locationName}.\n\n"
                + "## Data\n"
                + $"- **Location:** {locationName}\n"
                + $"- **Channel:** {channelLabel}\n"
                + $"- **Audience:** {AudienceLine(channelLabel, audienceLabel, eligibleCount)}\n"
                + $"- **Type:** {typeLabel}\n"
                + $"- **Value:** {valueLabel}\n"
                + $"- **Validity:** {validityLabel}\n"
                + $"- **Offer:** {offerTitle} — **Status:** Active (attached to this Campaign Draft)\n"
                + $"- **Name:** {campaignName}\n"
                + "- **Status:** Draft\n\n"
                + "Nothing was sent or scheduled.";
        }

        public static string PartialFailureBody(
            string failedStep,
            string locationName,
            string typeLabel,
            string valueLabel,
            string validityLabel,
            string offerTitle
        )
        {
            return
                "## Interpretation\n"
                + $"Campaign was not saved. The {failedStep} step failed.\n\n"
                + "## Data\n"
                + $"- **Location:** {locationName}\n"
                + $"- **Type:** {typeLabel}\n"
                + $"- **Value:** {valueLabel}\n"
                + $"- **Validity:** {validityLabel}\n"
                + $"- **Title:** {offerTitle}\n"
                + "- **Status:** Draft (not Active)\n"
                + "- Not attached to a Campaign\n\n"
                + "## Recommendation\n"
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or finish in Campaigns or Offers UI.";
        }

        public static string FullFailureBody(string failedStep)
            => "## Interpretation\n"
                + FullFailureOpener(failedStep)
                + "\n\n## Recommendation\n"
                + AssistantNextTryCopy.Sentence
                + " Retry this send, or create the Campaign in Campaigns or the Offer in Offers.";

        public static string InFlightCampaignRefusalBody(string campaignName)
            => "## Interpretation\n"
                + "I could not save this Campaign with Offer. "
                + $"Attach from chat is not allowed for **{campaignName}** because that Campaign is already scheduled, active, paused, or sent.\n\n"
                + "## Recommendation\n"
                + AssistantNextTryCopy.Sentence
                + " Attach the Offer in Campaigns UI.";

        public static string TypeLabel(CatalogOfferDto offer)
            => AssistantOfferPathTerms.TypeLabel(offer.OfferType);

        public static string ValueLabel(CatalogOfferDto offer)
        {
            var state = new AssistantOfferPathTermsState
            {
                OfferType = offer.OfferType,
                DiscountPercentage = offer.DiscountPercentage,
                DiscountAmount = offer.DiscountAmount,
                FreeItemText = offer.FreeItemText,
                ReplacementItemText = offer.ReplacementItemText,
            };
            return AssistantOfferPathTerms.ValueLabel(state);
        }

        public static string ValidityLabel(CatalogOfferDto offer)
        {
            var state = new AssistantOfferPathTermsState
            {
                Validity = offer.Validity,
                ExpiryDate = offer.ExpiryDate,
            };
            return AssistantOfferPathTerms.ValidityLabel(state);
        }

        private static string FullFailureOpener(string failedStep)
            => "I could not save this Campaign with Offer. "
                + $"The {failedStep} step failed.";

        private static string AudienceLine(
            string channelLabel,
            string audienceLabel,
            int? eligibleCount
        )
        {
            var countQualifier = string.Equals(
                channelLabel,
                "SMS",
                StringComparison.OrdinalIgnoreCase
            )
                ? "SMS-eligible"
                : "Email-eligible";
            return eligibleCount is int count
                ? $"{audienceLabel} ({count} {countQualifier})"
                : $"{audienceLabel} (eligible count unavailable)";
        }
    }
}
