using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface IPrintReadyQrMaterialsService
    {
        /// <summary>
        /// Ensure Starter QR materials for one Owned location (Table Tent,
        /// Window Sticker, Offer Card). Idempotent when Ready versions match.
        /// Never mints or mutates QR code tokens.
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

        public int Quantity { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? FileName { get; set; }

        public string? LastError { get; set; }
    }
}
