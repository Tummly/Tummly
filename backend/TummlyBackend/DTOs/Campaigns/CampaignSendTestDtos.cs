namespace TummlyBackend.DTOs.Campaigns
{
    public sealed class CampaignSendTestRequest
    {
        public int LocationId { get; set; }

        public string ToEmail { get; set; }
            = string.Empty;

        public string? Subject { get; set; }

        public string Body { get; set; }
            = string.Empty;

        /// <summary>
        /// Optional offer chrome for Campaign send test. Never creates a live
        /// offer; sample code is applied server-side.
        /// </summary>
        public CampaignSendTestOfferDto? Offer { get; set; }
    }

    public sealed class CampaignSendTestOfferDto
    {
        public string Title { get; set; }
            = string.Empty;

        public string Description { get; set; }
            = string.Empty;

        public string ExpiryLabel { get; set; }
            = string.Empty;
    }
}
