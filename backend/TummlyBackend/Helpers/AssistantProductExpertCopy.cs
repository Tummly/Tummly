namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Locked product-expert Retrieve copy. Server-owned. Fake and Azure
    /// do not paraphrase these strings.
    /// </summary>
    public static class AssistantProductExpertCopy
    {
        public const string CapabilitiesTitle = "What the AI Assistant can do";

        public const string CapabilitiesConversationTitle = "Assistant capabilities";

        public const string CapabilitiesBody =
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
            """;

        public const string CampaignVsOfferTitle = "Campaign vs Offer";

        public const string CampaignVsOfferBody =
            """
            A **Campaign** is Email or SMS outreach to eligible **Location Guests** at an **Owned location**. You schedule and send it from Campaigns.

            An **Offers catalog** offer is a reusable benefit definition on the Offers page. You attach it to a Campaign, **Feedback recovery**, or Guest form thank-you.

            A **Campaign** carries the message and audience. An offer carries the benefit terms. **Campaign offer attach** links them.
            """;

        public const string StatusesTitle = "Campaign and Offer statuses";

        public const string StatusesBody =
            """
            **Campaign** stored statuses include **Draft**, **Scheduled**, **Sending**, **Sent**, **Partially sent**, **Paused**, **Failed**, and **Cancelled**. A **Campaign Draft** is not sent.

            **Offers catalog** stored statuses include **Draft**, **Active**, **Paused**, **Expired**, and **Archived**. **Draft** is not in flight. **Active** means at least one live attach. First live **Campaign offer attach** promotes a stored **Draft** offer to **Active** when applicable.
            """;

        public const string AnalysisScopeTitle = "Analysis scope";

        public const string AnalysisScopeBody =
            """
            **Analysis scope** is the **AI Assistant** data window: one **Owned location** or **All owned locations** (multi-location operators only), plus one **Reporting period** (Last 7 days, Last 30 days, This month, or Custom up to 180 days).

            It is separate from the dashboard location switcher and from **Home performance date range**. Use **Change Scope** in the Assistant header to change it. **Compare turn** reads extra named locations for that turn only (up to three when scope is one location).
            """;

        public const string DraftVsSendTitle = "Draft vs send";

        public const string DraftVsSendBody =
            """
            The **AI Assistant** saves **Campaign Draft**s and **Offers catalog** **Draft**s and prepares **Feedback recovery**. It does not send Email or SMS, schedule a Campaign, issue an offer, activate an offer, or redeem an offer.

            After a create turn, use Review **Action**s or Campaigns, Offers, or Recovery in the dashboard. Send and schedule stay in Campaigns.
            """;

        public const string MultiTopicTitle = "Product facts";
    }
}
