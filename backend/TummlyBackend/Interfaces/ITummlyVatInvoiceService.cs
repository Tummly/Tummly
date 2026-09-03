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

        /// <summary>
        /// Idempotent TCN mint for a money loss (Revolut refund order UUID or
        /// dispute id). Reuses the row when
        /// <see cref="TummlyVatCreditNoteMintRequest.RefundOrderId"/> already
        /// exists as <see cref="TummlyVatInvoice.RevolutOrderId"/>.
        /// </summary>
        Task<TummlyVatInvoice> MintCreditNoteForRefundAsync(
            TummlyVatCreditNoteMintRequest request,
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
        DateTime PaymentSuccessUtc,
        int? NetPenceOverride = null,
        string? LineDescriptionOverride = null,
        string? CustomerBillingEmail = null,
        string? SellerBillingEmail = null,
        string? DeliverToSnapshot = null,
        string? PaymentMethodSummary = null,
        IReadOnlyList<TummlyBackend.Helpers.TummlyVatInvoiceLineItemDto>? LineItems =
            null
    );

    public sealed record TummlyVatCreditNoteMintRequest(
        string RefundOrderId,
        string OriginalPaymentOrderId,
        int RestaurantId,
        DateTime RefundCompletedUtc,
        int? NetPenceOverride = null,
        string? LineDescriptionOverride = null,
        string? CustomerBillingEmail = null,
        string? SellerBillingEmail = null,
        string? DeliverToSnapshot = null,
        string? PaymentMethodSummary = null,
        IReadOnlyList<TummlyBackend.Helpers.TummlyVatInvoiceLineItemDto>? LineItems =
            null
    );
}
