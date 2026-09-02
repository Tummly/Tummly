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

        public int LocationId { get; set; }

        public string LocationNameSnapshot { get; set; } = string.Empty;

        public string FulfilmentStatus { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string? TrackingUrl { get; set; }

        public string? OpsNotes { get; set; }

        public DateTime? PaidAtUtc { get; set; }

        public int GrossPence { get; set; }

        public List<AdminShopOrderLineSummaryDto> Lines { get; set; } = [];
    }

    public sealed class AdminShopOrderLineSummaryDto
    {
        public string CatalogSkuId { get; set; } = string.Empty;

        public string TitleSnapshot { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int UnitNetPence { get; set; }

        public int LineNetPence { get; set; }
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
