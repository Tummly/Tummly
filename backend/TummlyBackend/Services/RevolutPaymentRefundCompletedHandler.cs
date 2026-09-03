using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RevolutPaymentRefundCompletedHandler
        : IRevolutPaymentRefundCompletedHandler
    {
        private readonly ApplicationDbContext _context;
        private readonly ICreditLedger _ledger;
        private readonly ITummlyVatInvoiceService _vatInvoices;
        private readonly TimeProvider _clock;
        private readonly ILogger<RevolutPaymentRefundCompletedHandler> _logger;

        public RevolutPaymentRefundCompletedHandler(
            ApplicationDbContext context,
            ICreditLedger ledger,
            ITummlyVatInvoiceService vatInvoices,
            TimeProvider clock,
            ILogger<RevolutPaymentRefundCompletedHandler>? logger = null
        )
        {
            _context = context;
            _ledger = ledger;
            _vatInvoices = vatInvoices;
            _clock = clock;
            _logger =
                logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RevolutPaymentRefundCompletedHandler>.Instance;
        }

        public async Task HandleAsync(
            RevolutPaymentRefundCompletedRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var refundOrderId = request.RefundOrderId.Trim();
            var sourcePaymentRef =
                request.RelatedOrderId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(sourcePaymentRef))
            {
                _logger.LogWarning(
                    "Revolut refund order {RefundOrderId} missing related_order_id; skip drain/TCN",
                    refundOrderId
                );
                return;
            }

            var restaurantId = await ResolveRestaurantIdAsync(
                sourcePaymentRef,
                cancellationToken
            );
            if (restaurantId == null)
            {
                _logger.LogWarning(
                    "Revolut refund order {RefundOrderId} related payment {SourcePaymentRef} has no restaurant; skip drain/TCN",
                    refundOrderId,
                    sourcePaymentRef
                );
                return;
            }

            // Chargeback family shares this branch (no mint/applier) but
            // drain+TCN for this ticket is payment_refund only (lock 09).
            if (
                !string.Equals(
                    request.OrderType?.Trim(),
                    RevolutOrderTypes.Refund,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return;
            }

            var adminIntent = await _context.AdminPaymentRefundIntents
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RefundOrderId == refundOrderId,
                    cancellationToken
                );

            var drain = await _ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = restaurantId.Value,
                    SourcePaymentRef = sourcePaymentRef,
                    CorrectionSource = CorrectionSources.PaymentRefund,
                },
                cancellationToken
            );
            if (
                !drain.Succeeded
                && drain.Code is not "source_payment_ref_not_found"
            )
            {
                _logger.LogWarning(
                    "DrainUnusedTopup failed for refund {RefundOrderId} payment {SourcePaymentRef}: {Code}",
                    refundOrderId,
                    sourcePaymentRef,
                    drain.Code ?? "(none)"
                );
            }

            int? netPence = request.AmountMinor is int gross && gross > 0
                ? EstimateNetFromGrossPence(gross)
                : null;

            var creditNote = await _vatInvoices.MintCreditNoteForRefundAsync(
                new TummlyVatCreditNoteMintRequest(
                    RefundOrderId: refundOrderId,
                    OriginalPaymentOrderId: sourcePaymentRef,
                    RestaurantId: restaurantId.Value,
                    RefundCompletedUtc: _clock.GetUtcNow().UtcDateTime,
                    NetPenceOverride: netPence
                ),
                cancellationToken
            );

            BillingActivityWriter.TryAppend(
                _context,
                new BillingActivityAppendRequest
                {
                    RestaurantId = restaurantId.Value,
                    Kind = BillingActivityKinds.CreditNoteIssued,
                    OccurredAtUtc = _clock.GetUtcNow().UtcDateTime,
                    CreditNoteNo = creditNote.DocumentNumber,
                }
            );

            // ADR 0046: admin reconcile after manual Revolut refund → paymentStatus=refunded.
            var shopOrder = await _context.ShopOrders
                .FirstOrDefaultAsync(
                    row =>
                        row.RestaurantId == restaurantId.Value
                        && row.RevolutOrderId == sourcePaymentRef
                        && row.PaymentStatus == ShopPaymentStatuses.Paid,
                    cancellationToken
                );
            if (shopOrder != null)
            {
                shopOrder.PaymentStatus = ShopPaymentStatuses.Refunded;
                shopOrder.UpdatedAtUtc = _clock.GetUtcNow().UtcDateTime;
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (adminIntent == null)
            {
                _logger.LogWarning(
                    "Unexpected Business-UI refund completed: restaurant {RestaurantId}, refund order {RefundOrderId}, original payment {SourcePaymentRef}. Drain and TCN applied; Support should review.",
                    restaurantId.Value,
                    refundOrderId,
                    sourcePaymentRef
                );
            }
        }

        private async Task<int?> ResolveRestaurantIdAsync(
            string sourcePaymentRef,
            CancellationToken cancellationToken
        )
        {
            var fromLedger = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row =>
                    row.SourcePaymentRef == sourcePaymentRef
                    && row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                )
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromLedger != null)
            {
                return fromLedger;
            }

            return await _context.TummlyVatInvoices
                .AsNoTracking()
                .Where(row => row.RevolutOrderId == sourcePaymentRef)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Gross → net at 20% exclusive (gross = net + 0.2*net ⇒ net = gross/1.2).
        /// </summary>
        private static int EstimateNetFromGrossPence(int grossPence)
        {
            return (int)Math.Round(
                grossPence / 1.2m,
                MidpointRounding.AwayFromZero
            );
        }
    }
}
