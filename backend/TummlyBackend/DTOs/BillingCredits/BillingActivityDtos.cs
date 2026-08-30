namespace TummlyBackend.DTOs.BillingCredits
{
    public sealed class BillingActivityListDto
    {
        public bool Success { get; set; } = true;

        public List<BillingActivityItemDto> Items { get; set; } = [];

        public int TotalCount { get; set; }
    }

    public sealed class BillingActivityItemDto
    {
        public string Kind { get; set; } = string.Empty;

        public DateTime OccurredAtUtc { get; set; }

        public string Sentence { get; set; } = string.Empty;
    }
}
