using TummlyBackend.Helpers;

namespace TummlyBackend.Tests.Helpers
{
    public class AssistantProductExpertTopicsTests
    {
        [Theory]
        [InlineData("what can you do")]
        [InlineData("What can you help")]
        [InlineData("what are your capabilities")]
        [InlineData("what does the assistant do")]
        [InlineData("What does the AI Assistant do?")]
        public void Detect_CapabilitiesNeedles(string message)
        {
            var topic = Assert.Single(AssistantProductExpertTopics.Detect(message));
            Assert.Equal(AssistantProductExpertTopic.Capabilities, topic);
        }

        [Fact]
        public void Detect_WhatCanYouDraft_IsNotCapabilities()
        {
            Assert.Empty(AssistantProductExpertTopics.Detect("what can you draft"));
        }

        [Theory]
        [InlineData("campaign vs offer")]
        [InlineData("difference between campaign and offer")]
        [InlineData("difference between a campaign and an offer")]
        [InlineData("campaign versus offer")]
        public void Detect_CampaignVsOfferNeedles(string message)
        {
            var topic = Assert.Single(AssistantProductExpertTopics.Detect(message));
            Assert.Equal(AssistantProductExpertTopic.CampaignVsOffer, topic);
        }

        [Theory]
        [InlineData("campaign status")]
        [InlineData("offer status")]
        [InlineData("what is draft")]
        [InlineData("draft vs active")]
        [InlineData("active vs draft")]
        [InlineData("what does active mean")]
        [InlineData("in flight")]
        [InlineData("in-flight")]
        public void Detect_StatusesNeedles(string message)
        {
            var topic = Assert.Single(AssistantProductExpertTopics.Detect(message));
            Assert.Equal(AssistantProductExpertTopic.Statuses, topic);
        }

        [Theory]
        [InlineData("analysis scope")]
        [InlineData("change scope")]
        [InlineData("change analysis scope")]
        [InlineData("reporting period")]
        [InlineData("all owned locations")]
        [InlineData("all locations scope")]
        public void Detect_AnalysisScopeNeedles(string message)
        {
            var topic = Assert.Single(AssistantProductExpertTopics.Detect(message));
            Assert.Equal(AssistantProductExpertTopic.AnalysisScope, topic);
        }

        [Theory]
        [InlineData("does the assistant send")]
        [InlineData("does the ai assistant send")]
        [InlineData("will you send from chat")]
        [InlineData("can you schedule from chat")]
        [InlineData("draft vs send")]
        [InlineData("does it go live")]
        [InlineData("send from chat")]
        [InlineData("schedule from chat")]
        public void Detect_DraftVsSend_WithCapabilityGuard(string message)
        {
            var topic = Assert.Single(AssistantProductExpertTopics.Detect(message));
            Assert.Equal(AssistantProductExpertTopic.DraftVsSend, topic);
        }

        [Theory]
        [InlineData("will you send")]
        [InlineData("can you schedule")]
        public void Detect_DraftVsSend_WithoutCapabilityGuard_IsEmpty(string message)
        {
            Assert.Empty(AssistantProductExpertTopics.Detect(message));
        }

        [Fact]
        public void Detect_MultiTopic_ReturnsConcatenationOrder()
        {
            Assert.Equal(
                [
                    AssistantProductExpertTopic.AnalysisScope,
                    AssistantProductExpertTopic.CampaignVsOffer,
                    AssistantProductExpertTopic.Statuses,
                    AssistantProductExpertTopic.DraftVsSend,
                    AssistantProductExpertTopic.Capabilities,
                ],
                AssistantProductExpertTopics.Detect(
                    "What can you do, campaign vs offer, campaign status, "
                    + "analysis scope, and draft vs send"
                )
            );
        }

        [Fact]
        public void IsMixedRetrieve_RestaurantAskPlusProduct_IsTrue()
        {
            Assert.True(
                AssistantProductExpertTopics.IsMixedRetrieve(
                    "Summarise recent feedback and campaign vs offer"
                )
            );
        }

        [Fact]
        public void IsMixedRetrieve_InFlightAlone_IsPureStatuses()
        {
            Assert.False(AssistantProductExpertTopics.IsMixedRetrieve("in flight"));
            var topic = Assert.Single(AssistantProductExpertTopics.Detect("in flight"));
            Assert.Equal(AssistantProductExpertTopic.Statuses, topic);
        }

        [Fact]
        public void Assemble_Capabilities_MatchesLockedCopy()
        {
            var assembled = AssistantProductExpertTopics.Assemble(
                [AssistantProductExpertTopic.Capabilities]
            );

            Assert.Equal("What the AI Assistant can do", assembled.Title);
            Assert.Equal("Assistant capabilities", assembled.ConversationTitle);
            Assert.Equal(AssistantProductExpertCopy.CapabilitiesBody, assembled.Body);
            Assert.Contains("## Read", assembled.Body, StringComparison.Ordinal);
            Assert.Contains("## Create", assembled.Body, StringComparison.Ordinal);
            Assert.Contains("## Limits", assembled.Body, StringComparison.Ordinal);
            Assert.Contains(
                "Create Campaign with Offer",
                assembled.Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public void Assemble_MultiTopic_UsesProductFactsTitleAndOrder()
        {
            var assembled = AssistantProductExpertTopics.Assemble(
                [
                    AssistantProductExpertTopic.CampaignVsOffer,
                    AssistantProductExpertTopic.AnalysisScope,
                    AssistantProductExpertTopic.Capabilities,
                ]
            );

            Assert.Equal("Product facts", assembled.Title);
            Assert.Equal("Product facts", assembled.ConversationTitle);
            Assert.Equal(
                AssistantProductExpertCopy.AnalysisScopeBody
                    + "\n\n"
                    + AssistantProductExpertCopy.CampaignVsOfferBody
                    + "\n\n"
                    + AssistantProductExpertCopy.CapabilitiesBody,
                assembled.Body
            );
        }

        [Fact]
        public void Copy_CampaignVsOffer_MatchesPrd()
        {
            Assert.Equal("Campaign vs Offer", AssistantProductExpertCopy.CampaignVsOfferTitle);
            Assert.Equal(
                """
                A **Campaign** is Email or SMS outreach to eligible **Location Guests** at an **Owned location**. You schedule and send it from Campaigns.

                An **Offers catalog** offer is a reusable benefit definition on the Offers page. You attach it to a Campaign, **Feedback recovery**, or Guest form thank-you.

                A **Campaign** carries the message and audience. An offer carries the benefit terms. **Campaign offer attach** links them.
                """.Replace("\r\n", "\n"),
                AssistantProductExpertCopy.CampaignVsOfferBody
            );
        }

        [Fact]
        public void Copy_Statuses_MatchesPrd()
        {
            Assert.Equal(
                "Campaign and Offer statuses",
                AssistantProductExpertCopy.StatusesTitle
            );
            Assert.Equal(
                """
                **Campaign** stored statuses include **Draft**, **Scheduled**, **Sending**, **Sent**, **Partially sent**, **Paused**, **Failed**, and **Cancelled**. A **Campaign Draft** is not sent.

                **Offers catalog** stored statuses include **Draft**, **Active**, **Paused**, **Expired**, and **Archived**. **Draft** is not in flight. **Active** means at least one live attach. First live **Campaign offer attach** promotes a stored **Draft** offer to **Active** when applicable.
                """.Replace("\r\n", "\n"),
                AssistantProductExpertCopy.StatusesBody
            );
        }

        [Fact]
        public void Copy_AnalysisScope_MatchesPrd()
        {
            Assert.Equal("Analysis scope", AssistantProductExpertCopy.AnalysisScopeTitle);
            Assert.Equal(
                """
                **Analysis scope** is the **AI Assistant** data window: one **Owned location** or **All owned locations** (multi-location operators only), plus one **Reporting period** (Last 7 days, Last 30 days, This month, or Custom up to 180 days).

                It is separate from the dashboard location switcher and from **Home performance date range**. Use **Change Scope** in the Assistant header to change it. **Compare turn** reads extra named locations for that turn only (up to three when scope is one location).
                """.Replace("\r\n", "\n"),
                AssistantProductExpertCopy.AnalysisScopeBody
            );
        }

        [Fact]
        public void Copy_DraftVsSend_MatchesPrd()
        {
            Assert.Equal("Draft vs send", AssistantProductExpertCopy.DraftVsSendTitle);
            Assert.Equal(
                """
                The **AI Assistant** saves **Campaign Draft**s and **Offers catalog** **Draft**s and prepares **Feedback recovery**. It does not send Email or SMS, schedule a Campaign, issue an offer, activate an offer, or redeem an offer.

                After a create turn, use Review **Action**s or Campaigns, Offers, or Recovery in the dashboard. Send and schedule stay in Campaigns.
                """.Replace("\r\n", "\n"),
                AssistantProductExpertCopy.DraftVsSendBody
            );
        }

        [Fact]
        public void Copy_Capabilities_MatchesPrd()
        {
            Assert.Equal(
                "What the AI Assistant can do",
                AssistantProductExpertCopy.CapabilitiesTitle
            );
            Assert.Equal(
                """
                ## Read
                In your current **Analysis scope** (one **Owned location** or **All owned locations**, plus **Reporting period**), I answer questions about Feedback, offers, Campaigns, Capture, and Performance overview using data in that scope.

                ## Create
                - **Create Campaign Draft** — save a **Campaign Draft**. Nothing is sent or scheduled.
                - **Create Campaign with Offer** — in one send, match or create an **Offers catalog** offer, save or update a **Campaign Draft**, and attach the offer. The Campaign stays **Draft**. Nothing is sent, scheduled, or issued.
                - **Offer path** — save an **Offers catalog** **Draft** only. It is not attached to a Campaign and is not **Active**.
                - **Recovery path** — prepare **Feedback recovery** for a guest. It is not a stored Draft.

                ## Limits
                I save **Draft**s and prepare recovery work. I do not send, schedule, issue, activate, or redeem from chat. For step-by-step how-to, use Help Centre.
                """.Replace("\r\n", "\n"),
                AssistantProductExpertCopy.CapabilitiesBody
            );
        }

        [Theory]
        [InlineData("support")]
        [InlineData("contact support")]
        [InlineData("raise a ticket")]
        public void HelpCentreAsk_IncludesSupportNeedles(string message)
        {
            Assert.True(AssistantAskIntent.IsHelpCentreAsk(message));
        }
    }
}
