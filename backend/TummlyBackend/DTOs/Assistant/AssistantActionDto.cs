namespace TummlyBackend.DTOs.Assistant
{
    public class AssistantActionDto
    {
        public string Type { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string? Tab { get; set; }

        public string? Sentiment { get; set; }

        public string? DetectedTag { get; set; }

        public int? Count { get; set; }

        public int? OfferId { get; set; }

        public int? GuestId { get; set; }

        public string? SmartGroup { get; set; }

        public bool? MarketingEligible { get; set; }

        public int? FeedbackId { get; set; }

        public string? Intent { get; set; }

        public int? CampaignId { get; set; }
    }
}
