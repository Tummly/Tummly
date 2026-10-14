using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public static class AssistantDraftPreviewFactory
    {
        public const string StatusDraftNotSent = "Draft · Not sent";

        public static AssistantDraftPreview Campaign(
            int campaignId,
            string name,
            string? audienceLabel,
            string? channel,
            string? subject,
            string? messageBody,
            string? timingLabel = null
        )
            => new(
                Kind: "campaign",
                StatusLabel: StatusDraftNotSent,
                CampaignId: campaignId,
                Name: name,
                AudienceLabel: audienceLabel,
                Channel: channel,
                Subject: subject,
                MessageBody: messageBody,
                TimingLabel: timingLabel
            );

        public static AssistantDraftPreview Offer(
            int offerId,
            string title,
            string? conditionsSummary,
            string? validityLabel
        )
            => new(
                Kind: "offer",
                StatusLabel: StatusDraftNotSent,
                OfferId: offerId,
                Title: title,
                ConditionsSummary: conditionsSummary,
                ValidityLabel: validityLabel
            );

        public static AssistantDraftPreview Recovery(
            int feedbackId,
            string? channel,
            string? subject,
            string? messageBody,
            string? intent,
            string? purpose
        )
            => new(
                Kind: "recovery",
                StatusLabel: StatusDraftNotSent,
                FeedbackId: feedbackId,
                Channel: channel,
                Subject: subject,
                MessageBody: messageBody,
                Intent: intent,
                Purpose: purpose
            );
    }
}
