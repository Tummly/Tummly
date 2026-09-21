using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminPaymentRefundService : IAdminPaymentRefundService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly TimeProvider _clock;
        private readonly IAdminAuditService _audit;
        private readonly ILogger<AdminPaymentRefundService> _logger;

        public AdminPaymentRefundService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            TimeProvider clock,
            IAdminAuditService audit,
            ILogger<AdminPaymentRefundService>? logger = null
        )
        {
            _context = context;
            _merchant = merchant;
            _clock = clock;
            _audit = audit;
            _logger =
                logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AdminPaymentRefundService>.Instance;
        }

        public async Task<AdminPaymentRefundResult> RefundAsync(
            AdminPaymentRefundRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var idempotencyKey = request.IdempotencyKey?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(idempotencyKey))
            {
                return AdminPaymentRefundResult.Fail("idempotency_key_required");
            }

            var orderId = request.OrderId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(orderId))
            {
                return AdminPaymentRefundResult.Fail("order_id_required");
            }

            var existing = await _context.AdminPaymentRefundIntents
                .FirstOrDefaultAsync(
                    row => row.IdempotencyKey == idempotencyKey,
                    cancellationToken
                );
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(existing.RefundOrderId))
                {
                    return AdminPaymentRefundResult.Ok(existing.RefundOrderId);
                }

                return AdminPaymentRefundResult.Fail("refund_in_progress");
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == request.RestaurantId,
                    cancellationToken
                );
            if (billingAccount == null)
            {
                return AdminPaymentRefundResult.Fail("restaurant_not_found");
            }

            var ownsPayment = await _context.CreditLedgerEntries
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == request.RestaurantId
                        && row.EntryType == CreditLedgerEntryTypes.TopupAllocation
                        && row.SourcePaymentRef == orderId,
                    cancellationToken
                );
            if (!ownsPayment)
            {
                var invoiceOwns = await _context.TummlyVatInvoices
                    .AsNoTracking()
                    .AnyAsync(
                        row =>
                            row.RestaurantId == request.RestaurantId
                            && row.RevolutOrderId == orderId,
                        cancellationToken
                    );
                if (!invoiceOwns)
                {
                    return AdminPaymentRefundResult.Fail("payment_not_found");
                }
            }

            var bindableRemaining = await SumBindableForPaymentRefAsync(
                request.RestaurantId,
                orderId,
                cancellationToken
            );

            if (request.AmountMinor is int partialAmount && bindableRemaining > 0)
            {
                var retrieved = await _merchant.GetOrderAsync(
                    orderId,
                    cancellationToken
                );
                var fullAmount = retrieved.AmountMinor;
                if (
                    fullAmount == null
                    || partialAmount < fullAmount.Value
                )
                {
                    return AdminPaymentRefundResult.Fail(
                        "partial_refund_while_bindable"
                    );
                }
            }

            var intent = new AdminPaymentRefundIntent
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                RestaurantId = request.RestaurantId,
                SourcePaymentOrderId = orderId,
                AmountMinor = request.AmountMinor,
                ActorStaffUserId = request.ActorStaffUserId,
                CreatedAtUtc = _clock.GetUtcNow().UtcDateTime,
            };
            _context.AdminPaymentRefundIntents.Add(intent);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var raced = await _context.AdminPaymentRefundIntents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        row => row.IdempotencyKey == idempotencyKey,
                        cancellationToken
                    );
                if (
                    raced != null
                    && !string.IsNullOrWhiteSpace(raced.RefundOrderId)
                )
                {
                    return AdminPaymentRefundResult.Ok(raced.RefundOrderId);
                }

                throw;
            }

            var merchantIdempotencyKey = idempotencyKey;
            var shopOrderId = await _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == request.RestaurantId
                    && row.RevolutOrderId == orderId
                )
                .Select(row => (Guid?)row.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (shopOrderId is Guid sid)
            {
                // Same Merchant key as operator/admin shop cancel — prevents
                // double full refund via Admin payment refund + shop cancel.
                merchantIdempotencyKey = ShopOrderCancelRefund.IdempotencyKey(sid);
            }

            var refunded = await _merchant.RefundOrderAsync(
                orderId,
                request.AmountMinor,
                merchantIdempotencyKey,
                cancellationToken
            );
            if (!refunded.Succeeded || string.IsNullOrWhiteSpace(refunded.Id))
            {
                _logger.LogWarning(
                    "Revolut refund failed for payment order {OrderId} restaurant {RestaurantId}: {ErrorCode}",
                    orderId,
                    request.RestaurantId,
                    refunded.ErrorCode ?? "(none)"
                );
                return AdminPaymentRefundResult.Fail(
                    refunded.ErrorCode ?? "revolut_refund_failed"
                );
            }

            intent.RefundOrderId = refunded.Id.Trim();
            _audit.Append(
                new AdminAuditAppendRequest(
                    AdminAuditActions.PaymentRefund,
                    await ResolveAdminIdentityAsync(
                        request.ActorStaffUserId,
                        cancellationToken
                    ),
                    AdminAuditTargetTypes.PaymentOrder,
                    orderId,
                    ActorAdminUserId: request.ActorStaffUserId,
                    RestaurantId: request.RestaurantId,
                    DetailJson: BuildPaymentRefundDetailJson(
                        intent.RefundOrderId,
                        request.AmountMinor,
                        idempotencyKey
                    )
                )
            );
            await _context.SaveChangesAsync(cancellationToken);
            return AdminPaymentRefundResult.Ok(intent.RefundOrderId);
        }

        private async Task<string> ResolveAdminIdentityAsync(
            int actorStaffUserId,
            CancellationToken cancellationToken
        )
        {
            var email = await _context.Admins
                .AsNoTracking()
                .Where(row => row.Id == actorStaffUserId)
                .Select(row => row.Email)
                .FirstOrDefaultAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(email)
                ? $"admin:{actorStaffUserId}"
                : email;
        }

        private static string BuildPaymentRefundDetailJson(
            string refundOrderId,
            int? amountMinor,
            string idempotencyKey
        )
        {
            return System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    refundOrderId,
                    amountMinor,
                    idempotencyKey,
                }
            );
        }

        private async Task<int> SumBindableForPaymentRefAsync(
            int restaurantId,
            string sourcePaymentRef,
            CancellationToken cancellationToken
        )
        {
            var entries = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync(cancellationToken);
            var grants = CreditLedgerCalculator.TopupGrantsForPaymentRef(
                entries,
                sourcePaymentRef
            );
            if (grants.Count == 0)
            {
                return 0;
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var states = CreditLedgerCalculator.Project(entries, now);
            var grantIds = grants.Select(row => row.Id).ToHashSet();
            return states
                .Where(row => grantIds.Contains(row.Id))
                .Sum(row => Math.Max(row.Bindable, 0));
        }
    }
}
