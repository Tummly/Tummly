namespace TummlyBackend.Billing.Pricebook
{
    public sealed class PricebookSnapshot
    {
        public required string Id { get; init; }

        public required IReadOnlyDictionary<string, PricebookPlan> Plans { get; init; }

        public required IReadOnlyList<PricebookTopUpPack> TopUpPacks { get; init; }

        public int VatRateBps { get; init; }

        public int? ExtraGroupLocationMonthlyNetPence { get; init; }

        /// <summary>Never populated for Operator reads — loader strips this block.</summary>
        public object? InternalCostAssumptions { get; init; }

        /// <summary>Never populated for Operator reads — loader strips this block.</summary>
        public object? PaymentProvider { get; init; }
    }

    public sealed class PricebookPlan
    {
        public required string Key { get; init; }

        public required string DisplayName { get; init; }

        public int MonthlyNetPence { get; init; }

        public int AnnualNetPence { get; init; }

        public int IncludedLocations { get; init; }

        public int ActiveOffersAccount { get; init; }

        public PricebookChannelCredits? CreditsOneTime { get; init; }

        public PricebookChannelCredits? CreditsMonthly { get; init; }

        public int ActiveQrPlacementsPerLocation { get; init; }
    }

    public sealed class PricebookChannelCredits
    {
        public int Ai { get; init; }

        public int Email { get; init; }

        public int Sms { get; init; }
    }

    public sealed class PricebookTopUpPack
    {
        public required string Channel { get; init; }

        public int Quantity { get; init; }

        public int NetPence { get; init; }

        public bool ApprovalRequired { get; init; }
    }
}
