using System.Text.Json.Serialization;

namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminShopOrdersListQuery
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 25;

        public string? Q { get; set; }

        public int? RestaurantId { get; set; }

        public IReadOnlyList<string> FulfilmentStatus { get; set; } =
            Array.Empty<string>();

        /// <summary>Only Dispatched or Delivered orders whose carrier evidence is missing.</summary>
        public bool DispatchEvidenceMissing { get; set; }
    }

    public sealed class AdminShopOrderListResponseDto
    {
        public List<AdminShopOrderListItemDto> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public sealed class AdminShopOrderListItemDto
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public int RestaurantId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public int LocationId { get; set; }

        public string LocationNameSnapshot { get; set; } = string.Empty;

        public string FulfilmentStatus { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public bool IsComplimentary { get; set; }

        /// <summary>Revolut payment order UUID used for admin refunds.</summary>
        public string? RevolutOrderId { get; set; }

        public string? TrackingUrl { get; set; }

        public string? OpsNotes { get; set; }

        /// <summary>When the order was placed (UTC).</summary>
        public DateTime CreatedAtUtc { get; set; }

        public DateTime? PaidAtUtc { get; set; }

        /// <summary>When Admin recorded that preparation started (UTC).</summary>
        public DateTime? PreparationStartedAtUtc { get; set; }

        /// <summary>
        /// When set, warehouse production has started and operator cancel is blocked.
        /// </summary>
        public DateTime? ProductionStartedAtUtc { get; set; }

        /// <summary>Dispatch evidence time (UTC); null until dispatched.</summary>
        public DateTime? DispatchedAtUtc { get; set; }

        public string? DispatchCarrierName { get; set; }

        public string? DispatchCarrierReference { get; set; }

        /// <summary>How the dispatch evidence was recorded (for example <c>legacy_backfill</c>).</summary>
        public string? DispatchSource { get; set; }

        /// <summary>Admin who recorded the dispatch evidence; null for legacy dispatches.</summary>
        public int? DispatchedByAdminUserId { get; set; }

        public DateTime? DeliveredAtUtc { get; set; }

        public int GrossPence { get; set; }

        public List<AdminShopOrderLineSummaryDto> Lines { get; set; } = [];

        public List<AdminShopPrintAssetDto> PrintAssets { get; set; } = [];

        /// <summary>
        /// Why the paid order needs Admin attention (for example
        /// <c>qr_capacity_unavailable_after_payment</c>); null when none.
        /// </summary>
        public string? ReviewReason { get; set; }

        /// <summary>
        /// QR slot reservations held in <c>review</c> after 24 h without a
        /// final Revolut answer (spec 4.4.5).
        /// </summary>
        public List<AdminShopQrReservationReviewDto> ReviewQrReservations { get; set; } = [];
    }

    public sealed class AdminShopQrReservationReviewDto
    {
        public string Status { get; set; } = string.Empty;

        public string QrType { get; set; } = string.Empty;

        public string? LastReconcileError { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class AdminShopOrderLineSummaryDto
    {
        public string CatalogSkuId { get; set; } = string.Empty;

        public string TitleSnapshot { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int UnitNetPence { get; set; }

        public int LineNetPence { get; set; }
    }

    public sealed class AdminShopPrintAssetDto
    {
        public string QrType { get; set; } = string.Empty;

        /// <summary>The ordered product title, for example "QR Counter Display".</summary>
        public string ProductTitle { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public string? LastError { get; set; }

        public bool Approved { get; set; }

        public string? PrintRoute { get; set; }
    }

    public sealed class AdminShopOrderFulfilmentPatchDto
    {
        public string? FulfilmentStatus { get; set; }

        private string? _trackingUrl;
        private bool _trackingUrlSet;

        [JsonPropertyName("trackingUrl")]
        public string? TrackingUrl
        {
            get => _trackingUrl;
            set
            {
                _trackingUrl = value;
                _trackingUrlSet = true;
            }
        }

        [JsonIgnore]
        public bool TrackingUrlSet => _trackingUrlSet;

        private string? _opsNotes;
        private bool _opsNotesSet;

        [JsonPropertyName("opsNotes")]
        public string? OpsNotes
        {
            get => _opsNotes;
            set
            {
                _opsNotes = value;
                _opsNotesSet = true;
            }
        }

        [JsonIgnore]
        public bool OpsNotesSet => _opsNotesSet;
    }

    /// <summary>
    /// Dispatch evidence from Admin. Carrier name and reference are required;
    /// the tracking URL is optional supporting information.
    /// </summary>
    public sealed class AdminShopDispatchRequest
    {
        public string? CarrierName { get; init; }

        public string? CarrierReference { get; init; }

        public string? TrackingUrl { get; init; }
    }

    /// <summary>
    /// Backfill of dispatch evidence for a legacy order. All three fields are
    /// required: the actual dispatch time is supplied by Admin.
    /// </summary>
    public sealed class AdminShopDispatchEvidenceRequest
    {
        public string? CarrierName { get; init; }

        public string? CarrierReference { get; init; }

        public DateTimeOffset? DispatchedAtUtc { get; init; }
    }

    /// <summary>Audited return to the last substantiated status.</summary>
    public sealed class AdminShopStatusCorrectionRequest
    {
        public string? Reason { get; init; }
    }

    public sealed class AdminShopForceCancelRequest
    {
        public string Reason { get; init; } = string.Empty;

        public bool SkipRefund { get; init; }
    }

    public sealed class AdminShopOrderFulfilmentResult
    {
        public bool Succeeded { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public AdminShopOrderListItemDto? Order { get; init; }

        public static AdminShopOrderFulfilmentResult Ok(
            AdminShopOrderListItemDto order
        ) => new() { Succeeded = true, Order = order };

        public static AdminShopOrderFulfilmentResult Fail(
            string code,
            string message
        ) =>
            new()
            {
                Succeeded = false,
                ErrorCode = code,
                ErrorMessage = message,
            };
    }

    public sealed class AdminShopOrdersExportResult
    {
        public string FileName { get; init; } = string.Empty;

        public string ContentType { get; init; } = "text/csv";

        public byte[] Content { get; init; } = [];
    }
}
