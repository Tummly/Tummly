using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IPrintReadyQrMaterialsService
    {
        /// <summary>
        /// Ensure Starter QR materials for one Owned location for the current
        /// complimentary-starter placement set. Mints missing Capture QR types
        /// first (idempotent), then ensures print-ready assets. Idempotent when
        /// Ready versions match. Does not rotate existing QR tokens.
        /// </summary>
        Task EnsureStarterMaterialsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Ensures one order-scoped PDF for each distinct physical QR type on
        /// a Paid Shop order. Quantity does not change PDF cardinality.
        /// Existing Active QR codes are read only; this method never mints.
        /// </summary>
        Task EnsureShopOrderMaterialsAsync(
            Guid shopOrderId,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Marks every Starter and Shop asset for a rotated physical QR type
        /// stale, then queues regeneration without waiting for PDF work.
        /// </summary>
        Task<bool> TryInvalidateAfterQrRotationAsync(
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<PrintMaterialsLocationReadinessDto>> ListReadinessAsync(
            int operatorUserId,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<ShopPrintAssetReadinessDto>>
            ListShopOrderReadinessAsync(
                Guid shopOrderId,
                CancellationToken cancellationToken = default
            );

        Task EnsureAllStarterMaterialsForOperatorAsync(
            int operatorUserId,
            CancellationToken cancellationToken = default
        );

        Task<PrintReadyQrDownload?> DownloadAsync(
            int operatorUserId,
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken = default
        );

        Task<PrintReadyQrDownload?> DownloadShopOrderAsync(
            Guid shopOrderId,
            QrType qrType,
            CancellationToken cancellationToken = default
        );

        Task<PrintMaterialsAssetReadinessDto?> RetryAsync(
            int operatorUserId,
            int locationId,
            QrType qrType,
            CancellationToken cancellationToken = default
        );

        Task<ShopPrintAssetReadinessDto?> RetryShopOrderAsync(
            Guid shopOrderId,
            QrType qrType,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );

        Task<ShopPrintAssetPreviewDto?> GetShopOrderPreviewAsync(
            Guid shopOrderId,
            QrType qrType,
            CancellationToken cancellationToken = default
        );

        Task<ShopPrintAssetReadinessDto?> ApproveShopOrderAsync(
            Guid shopOrderId,
            QrType qrType,
            int actorAdminUserId,
            string actorIdentity,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record PrintReadyQrDownload(
        byte[] Content,
        string ContentType,
        string FileName
    );

    public sealed class PrintMaterialsLocationReadinessDto
    {
        public int LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public IReadOnlyList<PrintMaterialsAssetReadinessDto> Assets { get; set; }
            = Array.Empty<PrintMaterialsAssetReadinessDto>();
    }

    public sealed class PrintMaterialsAssetReadinessDto
    {
        public string QrType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public string? LastError { get; set; }
    }

    public sealed class ShopPrintAssetReadinessDto
    {
        public string QrType { get; set; } = string.Empty;

        /// <summary>
        /// The ordered product title (for example <c>QR Counter Display</c>),
        /// so a screen never derives a name from <see cref="QrType"/>.
        /// </summary>
        public string ProductTitle { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public string? LastError { get; set; }

        public bool Approved { get; set; }

        public string? PrintRoute { get; set; }
    }

    public sealed class ShopPrintAssetPreviewDto
    {
        public string ProductTitle { get; set; } = string.Empty;

        /// <summary>Same value as the CSV <c>JobId</c> column for this print job.</summary>
        public string JobId { get; set; } = string.Empty;

        public int LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public string Placement { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string QrDestinationUrl { get; set; } = string.Empty;

        public string? OfferHeadline { get; set; }

        public string PrintRoute { get; set; } = string.Empty;

        public DateTime? ApprovedAtUtc { get; set; }
    }
}
