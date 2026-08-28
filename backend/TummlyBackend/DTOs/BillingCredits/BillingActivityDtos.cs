namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class BillingActivityListDto
    {
        public bool Success { get; set; } = true;

        public List<BillingActivityRowDto> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public sealed class BillingActivityRowDto
    {
        public long Id { get; set; }

        public string Kind { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }

        public string? ActorDisplayName { get; set; }

        public string? Channel { get; set; }

        public int? Qty { get; set; }

        public string? CampaignName { get; set; }

        public string? InvoiceNo { get; set; }

        public string? CreditNoteNo { get; set; }

        public string? Plan { get; set; }

        public string? Cadence { get; set; }

        public string? ScheduledDateLabel { get; set; }

        public string? LocationName { get; set; }

        public string? ManualAdjustDirection { get; set; }

        public string? ConsumeSource { get; set; }
    }
}
