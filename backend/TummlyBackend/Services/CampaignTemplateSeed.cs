using TummlyBackend.DTOs.Campaigns;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Server-seeded Campaign template catalogue — six templates, product-global (ticket 21 / §12.2).
    /// By-id includes static Campaign template Preview seed (S6 / ticket 23).
    /// </summary>
    public static class CampaignTemplateSeed
    {
        public const int CatalogueVersion = 1;

        private const string FooterDisclaimer =
            "You'll be able to review the audience, offer, message, cost and final recipient count before anything is sent.";

        private const string OfferBlockDescription =
            "Show this code to the team on your next visit. This offer is from [Restaurant Name] and is subject to the terms below.";

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
                    offerStance: "optional",
                    preview: Preview(
                        summaryGoal: "Thank guests who were recently captured.",
                        bestFor: "New captures in the last 7–14 days",
                        suggestedAudience: "New guests",
                        suggestedChannel: "Email",
                        offer: "Optional",
                        suggestedChannels: new[] { "email" },
                        messages: new[]
                        {
                            Message(
                                channel: "email",
                                estimatedUsageLabel: "12 email messages",
                                subject: "Thanks for visiting",
                                body:
                                    "Hi Sarah,\n\nThanks for visiting Burger House.\nWe'd love to see you again this week.",
                                offerBlock: null
                            ),
                        },
                        offerLogic: null,
                        emailCount: 12,
                        smsCount: 0,
                        totalUniqueGuests: 12,
                        suggestedTiming: "Send within 48 hours of capture."
                    )
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
                    offerStance: "recommended",
                    preview: Preview(
                        summaryGoal:
                            "Bring eligible guests back during a quiet day or time",
                        bestFor:
                            "Slower weekdays, lunch gaps, low-footfall periods",
                        suggestedAudience:
                            "All eligible guests or selected saved group",
                        suggestedChannel: "Email or SMS",
                        offer: "Recommended",
                        suggestedChannels: new[] { "email", "sms" },
                        messages: new[]
                        {
                            Message(
                                channel: "email",
                                estimatedUsageLabel: "16 email messages",
                                subject: null,
                                body:
                                    "Hi Sarah,\n\nThanks for visiting Burger House.\nWe'd love to see you again this week. Here's a small thank-you from us.",
                                offerBlock: OfferBlock(
                                    title: "15% off your next order",
                                    redemptionCode: "BURGERCO-4829",
                                    expiryLabel: "Expires: 31 July 2026"
                                )
                            ),
                            Message(
                                channel: "sms",
                                estimatedUsageLabel: "4 SMS messages",
                                subject: null,
                                body:
                                    "Hi Sarah — quiet Tuesday lunch at Burger House? Here's 15% off: BURGERCO-4829. Ends 31 Jul.",
                                offerBlock: OfferBlock(
                                    title: "15% off your next order",
                                    redemptionCode: "BURGERCO-4829",
                                    expiryLabel: "Expires: 31 July 2026"
                                )
                            ),
                        },
                        offerLogic: RecommendedOfferLogic(),
                        emailCount: 16,
                        smsCount: 4,
                        totalUniqueGuests: 20,
                        suggestedTiming: "Send Monday 10am for Tuesday lunch."
                    )
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
                    offerStance: "optional",
                    preview: Preview(
                        summaryGoal:
                            "Reconnect with guests who have gone quiet.",
                        bestFor: "Guests with no recent Tummly activity",
                        suggestedAudience: "No recent Tummly activity",
                        suggestedChannel: "Email",
                        offer: "Optional",
                        suggestedChannels: new[] { "email" },
                        messages: new[]
                        {
                            Message(
                                channel: "email",
                                estimatedUsageLabel: "28 email messages",
                                subject: "We miss you",
                                body:
                                    "Hi Sarah,\n\nIt's been a while since we saw you at Burger House.\nWe'd love to welcome you back soon.",
                                offerBlock: null
                            ),
                        },
                        offerLogic: null,
                        emailCount: 28,
                        smsCount: 0,
                        totalUniqueGuests: 28,
                        suggestedTiming: "Send mid-week morning."
                    )
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
                    offerStance: "optional",
                    preview: Preview(
                        summaryGoal:
                            "Tell eligible guests about something new on the menu.",
                        bestFor: "New dishes, seasonal updates, limited runs",
                        suggestedAudience:
                            "All eligible guests or selected saved group",
                        suggestedChannel: "Email",
                        offer: "Optional",
                        suggestedChannels: new[] { "email" },
                        messages: new[]
                        {
                            Message(
                                channel: "email",
                                estimatedUsageLabel: "40 email messages",
                                subject: "Something new for you",
                                body:
                                    "Hi Sarah,\n\nWe've added something new at Burger House.\nCome in this week and try it.",
                                offerBlock: null
                            ),
                        },
                        offerLogic: null,
                        emailCount: 40,
                        smsCount: 0,
                        totalUniqueGuests: 40,
                        suggestedTiming: "Send the morning of the launch day."
                    )
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
                    offerStance: "optional",
                    preview: Preview(
                        summaryGoal:
                            "Invite returning guests to bring someone with them.",
                        bestFor: "Quiet seats and weekday tables",
                        suggestedAudience: "Eligible returning guests",
                        suggestedChannel: "Email",
                        offer: "Optional",
                        suggestedChannels: new[] { "email" },
                        messages: new[]
                        {
                            Message(
                                channel: "email",
                                estimatedUsageLabel: "18 email messages",
                                subject: "Bring a friend",
                                body:
                                    "Hi Sarah,\n\nNext time you visit Burger House, bring a friend.\nWe'd love to see you both.",
                                offerBlock: null
                            ),
                        },
                        offerLogic: null,
                        emailCount: 18,
                        smsCount: 0,
                        totalUniqueGuests: 18,
                        suggestedTiming: "Send Thursday for a weekend visit."
                    )
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
                    offerStance: "optional-controlled-recovery",
                    preview: Preview(
                        summaryGoal:
                            "Follow up after a completed recovery case.",
                        bestFor: "Closed private feedback recoveries",
                        suggestedAudience: "Completed recovery follow-up",
                        suggestedChannel: "Based on available permission",
                        offer: "Optional controlled recovery offer",
                        suggestedChannels: new[] { "email", "sms" },
                        messages: new[]
                        {
                            Message(
                                channel: "email",
                                estimatedUsageLabel: "6 email messages",
                                subject: "Thank you for your feedback",
                                body:
                                    "Hi Sarah,\n\nThank you again for sharing your feedback.\nWe'd love to welcome you back — here's a small thank-you.",
                                offerBlock: OfferBlock(
                                    title: "10% off your next visit",
                                    redemptionCode: "RECOVER-1042",
                                    expiryLabel: "Expires: 14 days after send"
                                )
                            ),
                            Message(
                                channel: "sms",
                                estimatedUsageLabel: "3 SMS messages",
                                subject: null,
                                body:
                                    "Hi Sarah — thanks again for your feedback. 10% off your next visit: RECOVER-1042.",
                                offerBlock: OfferBlock(
                                    title: "10% off your next visit",
                                    redemptionCode: "RECOVER-1042",
                                    expiryLabel: "Expires: 14 days after send"
                                )
                            ),
                        },
                        offerLogic: new[]
                        {
                            LogicRow("Offer type:", "10% off"),
                            LogicRow("Code type:", "Unique guest code"),
                            LogicRow("Expiry:", "14 days after send"),
                            LogicRow("Usage:", "Single-use"),
                            LogicRow(
                                "Redemption:",
                                "Staff verifies in Tummly redeem screen"
                            ),
                        },
                        emailCount: 6,
                        smsCount: 3,
                        totalUniqueGuests: 6,
                        suggestedTiming: "Send 2–3 days after recovery close."
                    )
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
            string offerStance,
            CampaignTemplatePreviewDto preview
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
                Preview = preview,
            };
        }

        private static CampaignTemplatePreviewDto Preview(
            string summaryGoal,
            string bestFor,
            string suggestedAudience,
            string suggestedChannel,
            string offer,
            IReadOnlyList<string> suggestedChannels,
            IReadOnlyList<CampaignTemplatePreviewMessageDto> messages,
            IReadOnlyList<CampaignTemplatePreviewOfferLogicRowDto>? offerLogic,
            int emailCount,
            int smsCount,
            int totalUniqueGuests,
            string suggestedTiming
        )
        {
            return new CampaignTemplatePreviewDto
            {
                Summary = new CampaignTemplatePreviewSummaryDto
                {
                    Goal = summaryGoal,
                    BestFor = bestFor,
                    SuggestedAudience = suggestedAudience,
                    SuggestedChannel = suggestedChannel,
                    Offer = offer,
                },
                SuggestedChannels = suggestedChannels,
                Messages = messages,
                OfferLogic = offerLogic,
                Eligibility = new CampaignTemplatePreviewEligibilityDto
                {
                    EmailCount = emailCount,
                    SmsCount = smsCount,
                    TotalUniqueGuests = totalUniqueGuests,
                },
                SuggestedTiming = suggestedTiming,
                FooterDisclaimer = FooterDisclaimer,
            };
        }

        private static CampaignTemplatePreviewMessageDto Message(
            string channel,
            string estimatedUsageLabel,
            string? subject,
            string body,
            CampaignTemplatePreviewOfferBlockDto? offerBlock
        )
        {
            return new CampaignTemplatePreviewMessageDto
            {
                Channel = channel,
                EstimatedUsageLabel = estimatedUsageLabel,
                Subject = subject,
                Body = body,
                OfferBlock = offerBlock,
            };
        }

        private static CampaignTemplatePreviewOfferBlockDto OfferBlock(
            string title,
            string redemptionCode,
            string expiryLabel
        )
        {
            return new CampaignTemplatePreviewOfferBlockDto
            {
                Title = title,
                Description = OfferBlockDescription,
                RedemptionCode = redemptionCode,
                ExpiryLabel = expiryLabel,
            };
        }

        private static CampaignTemplatePreviewOfferLogicRowDto LogicRow(
            string label,
            string value
        )
        {
            return new CampaignTemplatePreviewOfferLogicRowDto
            {
                Label = label,
                Value = value,
            };
        }

        private static IReadOnlyList<CampaignTemplatePreviewOfferLogicRowDto>
            RecommendedOfferLogic()
        {
            return new[]
            {
                LogicRow("Offer type:", "10% off"),
                LogicRow("Code type:", "Unique guest code"),
                LogicRow("Expiry:", "Ends Tuesday 5pm"),
                LogicRow("Usage:", "Single-use"),
                LogicRow(
                    "Redemption:",
                    "Staff verifies in Tummly redeem screen"
                ),
            };
        }
    }
}
