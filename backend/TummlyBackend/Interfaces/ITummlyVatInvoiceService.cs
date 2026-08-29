using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ITummlyVatInvoiceService
    {
        /// <summary>
        /// Idempotent TM mint for a completed Revolut order. Reuses the row
        /// when <see cref="TummlyVatInvoiceMintRequest.RevolutOrderId"/>
        /// already exists.
        /// </summary>
        Task<TummlyVatInvoice> MintForCompletedOrderAsync(
            TummlyVatInvoiceMintRequest request,
            CancellationToken cancellationToken = default
        );

        Task<TummlyVatInvoice?> FindByRevolutOrderIdAsync(
            string revolutOrderId,
            CancellationToken cancellationToken = default
        );

        Task<IReadOnlyList<InvoiceRowDto>> ListInvoiceRowsForRestaurantAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        );

        Task<(byte[] Content, string FileName)?> RenderPdfAsync(
            int restaurantId,
            string documentNumber,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record TummlyVatInvoiceMintRequest(
        string RevolutOrderId,
        string? RevolutSubscriptionId,
        int RestaurantId,
        string Plan,
        string BillingCycle,
        DateTime PaymentSuccessUtc
    );
}
