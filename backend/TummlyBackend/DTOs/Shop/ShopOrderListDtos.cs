namespace TummlyBackend.DTOs.Shop
{
    public sealed class ShopOrderListItemDto
    {
        public Guid Id { get; init; }

        public required string OrderNumber { get; init; }

        public required string OrderDate { get; init; }

        public int LocationId { get; init; }

        public required string LocationName { get; init; }

        public required string MaterialsSummary { get; init; }

        public required IReadOnlyList<string> MaterialTypes { get; init; }

        public required string PlacedBy { get; init; }

        public required string TotalFormatted { get; init; }

        public int TotalGrossPence { get; init; }

        public required string PaymentStatus { get; init; }

        public required string FulfilmentStatus { get; init; }

        public DateTime UpdatedAtUtc { get; init; }
    }

    public sealed class ShopOrderListAggregatesDto
    {
        public int InProgress { get; init; }

        public int Dispatched { get; init; }

        public int DeliveredLast90Days { get; init; }
    }

    public sealed class ShopOrderListResponseDto
    {
        public required IReadOnlyList<ShopOrderListItemDto> Items { get; init; }

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public required ShopOrderListAggregatesDto Aggregates { get; init; }
    }

    public sealed class ShopOrderPaymentSummaryDto
    {
        public DateTime? PaidAtUtc { get; init; }

        public string? RevolutOrderId { get; init; }
    }

    public sealed class ShopOrderProgressDto
    {
        public DateTime? OrderReceivedAtUtc { get; init; }

        public DateTime? ProcessingStartedAtUtc { get; init; }

        public DateTime? DispatchedAtUtc { get; init; }

        public DateTime? DeliveredAtUtc { get; init; }

        public string? TrackingUrl { get; init; }
    }

    public sealed class ShopOrderOperatorDetailDto
    {
        public Guid Id { get; init; }

        public required string OrderNumber { get; init; }

        public required string OrderDate { get; init; }

        public int LocationId { get; init; }

        public required string LocationName { get; init; }

        public required string PlacedBy { get; init; }

        public required string PaymentStatus { get; init; }

        public required string PaymentStatusLabel { get; init; }

        public string? FulfilmentStatus { get; init; }

        public required string FulfilmentStatusLabel { get; init; }

        public required string DeliveryMethod { get; init; }

        public int MaterialsNetPence { get; init; }

        public int VatPence { get; init; }

        public int DeliveryNetPence { get; init; }

        public int GrossPence { get; init; }

        public required string Currency { get; init; }

        public required IReadOnlyList<ShopOrderLineDto> Lines { get; init; }

        public required ShopOrderShipToDto ShipTo { get; init; }

        public required ShopOrderPaymentSummaryDto PaymentSummary { get; init; }

        public required ShopOrderProgressDto Progress { get; init; }

        public DateTime UpdatedAtUtc { get; init; }

        public bool CanCancel { get; init; }

        public string? CancelBlockReason { get; init; }
    }
}
