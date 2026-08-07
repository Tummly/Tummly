using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Server-seeded Campaign template catalogue — six templates, product-global (ticket 21 / §12.2).
    /// </summary>
    public static class CampaignTemplateSeed
    {
        public const int CatalogueVersion = 1;

        public static IReadOnlyList<CampaignTemplateDetailDto> All { get; } =
            new[]
            {
                Template(
                    id: "thank-recent-guests",
                    title: "Thank recent guests",
                    description:
                        "Welcome recently captured guests with a simple, appreciative message.",
                    goalLabel: "Thank recent guests",
                    audienceLabel: "New guests",
                    channelLabel: "Email",
                    offerLabel: "Optional",
                    goalId: "thank-recent-guests",
                    audienceKey: "new-guests",
                    channel: "email",
                    offerStance: "optional"
                ),
                Template(
                    id: "quiet-time-boost",
                    title: "Quiet-time boost",
                    description:
                        "Invite eligible guests back during a day or time you choose.",
                    goalLabel: "Boost a quieter time",
                    audienceLabel: "All eligible guests or saved group",
                    channelLabel: "Email or SMS",
                    offerLabel: "Recommended",
                    goalId: "boost-quieter-time",
                    audienceKey: "all-eligible-or-saved-group",
                    channel: "email-or-sms",
                    offerStance: "recommended"
                ),
                Template(
                    id: "we-miss-you",
                    title: "We miss you",
                    description:
                        "Reconnect with guests who have not recently engaged through Guest Loop.",
                    goalLabel: "Re-engage inactive guests",
                    audienceLabel: "No recent Tummly activity",
                    channelLabel: "Email",
                    offerLabel: "Optional",
                    goalId: "re-engage-inactive",
                    audienceKey: "no-recent-tummly-activity",
                    channel: "email",
                    offerStance: "optional"
                ),
                Template(
                    id: "new-item-announcement",
                    title: "New item announcement",
                    description:
                        "Tell eligible guests about a new menu item or restaurant update.",
                    goalLabel: "Promote something new",
                    audienceLabel: "All eligible guests or saved group",
                    channelLabel: "Email",
                    offerLabel: "Optional",
                    goalId: "promote-something-new",
                    audienceKey: "all-eligible-or-saved-group",
                    channel: "email",
                    offerStance: "optional"
                ),
                Template(
                    id: "bring-a-friend",
                    title: "Bring-a-friend",
                    description:
                        "Invite eligible guests to return with another person using a controlled offer.",
                    goalLabel: "Boost a quieter time or Promote something new",
                    audienceLabel: "Eligible returning guests",
                    channelLabel: "Email",
                    offerLabel: "Optional",
                    goalId: "boost-quieter-time",
                    audienceKey: "eligible-returning-guests",
                    channel: "email",
                    offerStance: "optional"
                ),
                Template(
                    id: "recovery-follow-up",
                    title: "Recovery follow-up",
                    description:
                        "Follow up after a private feedback case has been handled.",
                    goalLabel: "Follow up after completed recovery",
                    audienceLabel: "Completed recovery follow-up",
                    channelLabel: "Based on available permission",
                    offerLabel: "Optional controlled recovery offer",
                    goalId: "follow-up-completed-recovery",
                    audienceKey: "completed-recovery-follow-up",
                    channel: "based-on-permission",
                    offerStance: "optional-controlled-recovery"
                ),
            };

        private static CampaignTemplateDetailDto Template(
            string id,
            string title,
            string description,
            string goalLabel,
            string audienceLabel,
            string channelLabel,
            string offerLabel,
            string goalId,
            string audienceKey,
            string channel,
            string offerStance
        )
        {
            return new CampaignTemplateDetailDto
            {
                Id = id,
                Version = CatalogueVersion,
                Title = title,
                Description = description,
                GoalLabel = goalLabel,
                AudienceLabel = audienceLabel,
                ChannelLabel = channelLabel,
                OfferLabel = offerLabel,
                SuggestsGoal = true,
                SuggestsAudience = true,
                SuggestsChannel = true,
                SuggestsOffer = true,
                Suggestions = new CampaignTemplateSuggestionDefaultsDto
                {
                    GoalId = goalId,
                    AudienceKey = audienceKey,
                    Channel = channel,
                    OfferStance = offerStance,
                },
            };
        }
    }
}
