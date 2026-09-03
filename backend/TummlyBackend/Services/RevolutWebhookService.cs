using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class RevolutWebhookService : IRevolutWebhookService
    {
        private static readonly HashSet<string> TerminalOrderStates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "failed",
                "cancelled",
            };

        private static readonly HashSet<string> OrderEvents =
            new(StringComparer.Ordinal)
            {
                "ORDER_COMPLETED",
                "ORDER_FAILED",
                "ORDER_CANCELLED",
                "ORDER_AUTHORISED",
                "ORDER_PAYMENT_DECLINED",
                "ORDER_PAYMENT_FAILED",
            };

        private static readonly HashSet<string> SubscriptionEvents =
            new(StringComparer.Ordinal)
            {
                "SUBSCRIPTION_INITIATED",
                "SUBSCRIPTION_CANCELLED",
                "SUBSCRIPTION_OVERDUE",
                "SUBSCRIPTION_FINISHED",
            };

        private static readonly HashSet<string> DisputeEvents =
            new(StringComparer.Ordinal)
            {
                "DISPUTE_ACTION_REQUIRED",
                "DISPUTE_UNDER_REVIEW",
                "DISPUTE_WON",
                "DISPUTE_LOST",
            };

        private readonly ApplicationDbContext _context;
        private readonly IRevolutMerchantClient _merchant;
        private readonly IRevolutOrderCompletedApplier _applier;
        private readonly IBillingAccountLifecycle _lifecycle;
        private readonly IRevolutDunningPayAdapter _dunningPay;
        private readonly IRevolutPaymentRefundCompletedHandler _paymentRefundHandler;
        private readonly ICreditLedger _ledger;
        private readonly ITummlyVatInvoiceService _vatInvoices;
        private readonly TimeProvider _clock;
        private readonly RevolutSettings _settings;
        private readonly ILogger<RevolutWebhookService> _logger;

        public RevolutWebhookService(
            ApplicationDbContext context,
            IRevolutMerchantClient merchant,
            IRevolutOrderCompletedApplier applier,
            IBillingAccountLifecycle lifecycle,
            TimeProvider clock,
            IOptions<RevolutSettings> settings,
            ILogger<RevolutWebhookService>? logger = null,
            IRevolutDunningPayAdapter? dunningPay = null,
            IRevolutPaymentRefundCompletedHandler? paymentRefundHandler = null,
            ICreditLedger? ledger = null,
            ITummlyVatInvoiceService? vatInvoices = null
        )
        {
            _context = context;
            _merchant = merchant;
            _applier = applier;
            _lifecycle = lifecycle;
            _dunningPay = dunningPay ?? NoOpRevolutDunningPayAdapter.Instance;
            _paymentRefundHandler =
                paymentRefundHandler ?? NoOpPaymentRefundCompletedHandler.Instance;
            _ledger = ledger ?? NoOpWebhookCreditLedger.Instance;
            _vatInvoices = vatInvoices ?? NoOpWebhookVatInvoiceService.Instance;
            _clock = clock;
            _settings = settings.Value;
            _logger =
                logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RevolutWebhookService>.Instance;
        }

        public async Task<RevolutWebhookHandleResult> HandleAsync(
            string rawBody,
            string? signatureHeader,
            string? requestTimestamp,
            CancellationToken cancellationToken = default
        )
        {
            if (
                !RevolutWebhookSignature.Verify(
                    _settings.WebhookSigningSecret,
                    requestTimestamp ?? string.Empty,
                    rawBody ?? string.Empty,
                    signatureHeader ?? string.Empty
                )
            )
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.BadSignature
                );
            }

            if (
                string.IsNullOrWhiteSpace(rawBody)
                || !TryParseEnvelope(rawBody, out var envelope)
            )
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }

            if (
                string.Equals(
                    envelope.Event,
                    "ORDER_COMPLETED",
                    StringComparison.Ordinal
                )
            )
            {
                return await HandleOrderCompletedAsync(
                    envelope,
                    rawBody,
                    cancellationToken
                );
            }

            if (
                string.Equals(
                    envelope.Event,
                    "SUBSCRIPTION_OVERDUE",
                    StringComparison.Ordinal
                )
            )
            {
                return await HandleSubscriptionOverdueAsync(
                    envelope,
                    cancellationToken
                );
            }

            if (DisputeEvents.Contains(envelope.Event))
            {
                return await HandleDisputeAsync(envelope, cancellationToken);
            }

            if (!TryResolveObjectId(envelope, out var objectId))
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }

            if (
                OrderEvents.Contains(envelope.Event)
                && !string.Equals(
                    envelope.Event,
                    "ORDER_COMPLETED",
                    StringComparison.Ordinal
                )
            )
            {
                return await HandleShopOrderTerminalEventAsync(
                    envelope.Event,
                    objectId,
                    cancellationToken
                );
            }

            return await ClaimRecordOnlyAsync(
                envelope.Event,
                objectId,
                RevolutWebhookClaimDispositions.Recorded,
                cancellationToken
            );
        }

        private async Task<RevolutWebhookHandleResult> HandleShopOrderTerminalEventAsync(
            string eventName,
            string orderId,
            CancellationToken cancellationToken
        )
        {
            var shopIntentExists = await _context.RevolutOrderIntents
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.OrderId == orderId
                        && row.Purpose
                            == RevolutOrderIntentPurposes.ShopMaterialsOrder,
                    cancellationToken
                );
            if (!shopIntentExists)
            {
                return await ClaimRecordOnlyAsync(
                    eventName,
                    orderId,
                    RevolutWebhookClaimDispositions.Recorded,
                    cancellationToken
                );
            }

            var existing = await FindClaimAsync(
                eventName,
                orderId,
                cancellationToken
            );
            if (existing != null)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Replay
                );
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var again = await FindClaimAsync(
                    eventName,
                    orderId,
                    cancellationToken
                );
                if (again != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                _context.RevolutWebhookEventClaims.Add(
                    new RevolutWebhookEventClaim
                    {
                        Id = Guid.NewGuid(),
                        Event = eventName,
                        ObjectId = orderId,
                        Disposition = RevolutWebhookClaimDispositions.Applied,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await _context.SaveChangesAsync(cancellationToken);

                await ShopMaterialsOrderPaymentFailure.TryMarkFailedAsync(
                    _context,
                    orderId,
                    cancellationToken
                );

                await transaction.CommitAsync(cancellationToken);
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<RevolutWebhookHandleResult> HandleDisputeAsync(
            WebhookEnvelope envelope,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(envelope.DisputeId))
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }

            var disputeId = envelope.DisputeId.Trim();
            var eventName = envelope.Event;

            if (
                string.Equals(
                    eventName,
                    "DISPUTE_UNDER_REVIEW",
                    StringComparison.Ordinal
                )
            )
            {
                return await ClaimRecordOnlyAsync(
                    eventName,
                    disputeId,
                    RevolutWebhookClaimDispositions.Recorded,
                    cancellationToken
                );
            }

            var existing = await FindClaimAsync(
                eventName,
                disputeId,
                cancellationToken
            );
            if (existing != null)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Replay
                );
            }

            var retrieved = await _merchant.GetDisputeAsync(
                disputeId,
                cancellationToken
            );
            if (retrieved is null || !retrieved.Succeeded)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.RetryLater
                );
            }

            var paymentOrderId = retrieved.PaymentOrderId?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(paymentOrderId))
            {
                return await ClaimRecordOnlyAsync(
                    eventName,
                    disputeId,
                    RevolutWebhookClaimDispositions.Recorded,
                    cancellationToken
                );
            }

            var restaurantId = await ResolveRestaurantIdForPaymentOrderAsync(
                paymentOrderId,
                cancellationToken
            );
            if (restaurantId == null)
            {
                return await ClaimRecordOnlyAsync(
                    eventName,
                    disputeId,
                    RevolutWebhookClaimDispositions.Recorded,
                    cancellationToken
                );
            }

            return await ClaimAndApplyDisputeAsync(
                eventName,
                disputeId,
                paymentOrderId,
                restaurantId.Value,
                cancellationToken
            );
        }

        private async Task<RevolutWebhookHandleResult> ClaimAndApplyDisputeAsync(
            string eventName,
            string disputeId,
            string paymentOrderId,
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var claimId = Guid.NewGuid();
            try
            {
                var again = await FindClaimAsync(
                    eventName,
                    disputeId,
                    cancellationToken
                );
                if (again != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                _context.RevolutWebhookEventClaims.Add(
                    new RevolutWebhookEventClaim
                    {
                        Id = claimId,
                        Event = eventName,
                        ObjectId = disputeId,
                        Disposition = RevolutWebhookClaimDispositions.Applied,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                await AbortClaimTransactionAsync(claimId, cancellationToken);
                var conflict = await FindClaimAsync(
                    eventName,
                    disputeId,
                    cancellationToken
                );
                if (conflict != null)
                {
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                throw;
            }
            catch
            {
                await AbortClaimTransactionAsync(claimId, cancellationToken);
                throw;
            }

            try
            {
                if (
                    string.Equals(
                        eventName,
                        "DISPUTE_ACTION_REQUIRED",
                        StringComparison.Ordinal
                    )
                )
                {
                    await ApplyDisputeActionRequiredAsync(
                        restaurantId,
                        paymentOrderId,
                        cancellationToken
                    );
                }
                else if (
                    string.Equals(
                        eventName,
                        "DISPUTE_WON",
                        StringComparison.Ordinal
                    )
                )
                {
                    await ApplyDisputeWonAsync(
                        restaurantId,
                        paymentOrderId,
                        cancellationToken
                    );
                }
                else if (
                    string.Equals(
                        eventName,
                        "DISPUTE_LOST",
                        StringComparison.Ordinal
                    )
                )
                {
                    await ApplyDisputeLostAsync(
                        restaurantId,
                        paymentOrderId,
                        disputeId,
                        cancellationToken
                    );
                }
            }
            catch
            {
                await AbortClaimTransactionAsync(claimId, cancellationToken);
                throw;
            }

            return new RevolutWebhookHandleResult(
                RevolutWebhookHandleStatus.Accepted
            );
        }

        private async Task ApplyDisputeActionRequiredAsync(
            int restaurantId,
            string paymentOrderId,
            CancellationToken cancellationToken
        )
        {
            await _lifecycle.SetChargebackRestrictionAsync(
                restaurantId,
                restricted: true,
                cancellationToken
            );

            var hasTopup = await HasTopupAllocationAsync(
                restaurantId,
                paymentOrderId,
                cancellationToken
            );
            if (!hasTopup)
            {
                return;
            }

            await _ledger.DrainUnusedTopupAsync(
                new CreditLedgerDrainTopupRequest
                {
                    RestaurantId = restaurantId,
                    SourcePaymentRef = paymentOrderId,
                    CorrectionSource = CorrectionSources.Dispute,
                },
                cancellationToken
            );
        }

        private async Task ApplyDisputeWonAsync(
            int restaurantId,
            string paymentOrderId,
            CancellationToken cancellationToken
        )
        {
            var hadDisputeDrain = await HasDisputeDrainAsync(
                restaurantId,
                paymentOrderId,
                cancellationToken
            );
            if (hadDisputeDrain)
            {
                await _ledger.RestoreUnusedTopupAsync(
                    new CreditLedgerRestoreTopupRequest
                    {
                        RestaurantId = restaurantId,
                        SourcePaymentRef = paymentOrderId,
                    },
                    cancellationToken
                );
            }

            await _lifecycle.SetChargebackRestrictionAsync(
                restaurantId,
                restricted: false,
                cancellationToken
            );
        }

        private async Task ApplyDisputeLostAsync(
            int restaurantId,
            string paymentOrderId,
            string disputeId,
            CancellationToken cancellationToken
        )
        {
            await _vatInvoices.MintCreditNoteForRefundAsync(
                new TummlyVatCreditNoteMintRequest(
                    RefundOrderId: disputeId,
                    OriginalPaymentOrderId: paymentOrderId,
                    RestaurantId: restaurantId,
                    RefundCompletedUtc: _clock.GetUtcNow().UtcDateTime,
                    LineDescriptionOverride: "Credit note — dispute"
                ),
                cancellationToken
            );

            var hadDisputeDrain = await HasDisputeDrainAsync(
                restaurantId,
                paymentOrderId,
                cancellationToken
            );
            if (!hadDisputeDrain)
            {
                return;
            }

            // Use current ledger totals for the payment ref (same held/consumed
            // as the ACTION_REQUIRED drain report). Do not drain again.
            var entries = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync(cancellationToken);
            var channels = CreditLedgerCalculator.SummarizePaymentRef(
                entries,
                paymentOrderId,
                _clock.GetUtcNow().UtcDateTime
            );
            if (
                channels.Count > 0
                && channels.All(row => row.Consumed == 0 && row.Held == 0)
            )
            {
                await _lifecycle.SetChargebackRestrictionAsync(
                    restaurantId,
                    restricted: false,
                    cancellationToken
                );
            }
        }

        private Task<bool> HasTopupAllocationAsync(
            int restaurantId,
            string paymentOrderId,
            CancellationToken cancellationToken
        )
        {
            return _context.CreditLedgerEntries
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.SourcePaymentRef == paymentOrderId
                        && row.EntryType
                            == CreditLedgerEntryTypes.TopupAllocation,
                    cancellationToken
                );
        }

        private Task<bool> HasDisputeDrainAsync(
            int restaurantId,
            string paymentOrderId,
            CancellationToken cancellationToken
        )
        {
            return _context.CreditLedgerEntries
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.SourcePaymentRef == paymentOrderId
                        && row.EntryType == CreditLedgerEntryTypes.Refund
                        && row.CorrectionSource == CorrectionSources.Dispute,
                    cancellationToken
                );
        }

        private async Task<int?> ResolveRestaurantIdForPaymentOrderAsync(
            string paymentOrderId,
            CancellationToken cancellationToken
        )
        {
            var fromLedger = await _context.CreditLedgerEntries
                .AsNoTracking()
                .Where(row => row.SourcePaymentRef == paymentOrderId)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromLedger != null)
            {
                return fromLedger;
            }

            var fromInvoice = await _context.TummlyVatInvoices
                .AsNoTracking()
                .Where(row => row.RevolutOrderId == paymentOrderId)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromInvoice != null)
            {
                return fromInvoice;
            }

            var fromIntent = await _context.RevolutOrderIntents
                .AsNoTracking()
                .Where(row => row.OrderId == paymentOrderId)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (fromIntent != null)
            {
                return fromIntent;
            }

            return await _context.RevolutPendingPaySessions
                .AsNoTracking()
                .Where(row => row.SetupOrderId == paymentOrderId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<RevolutWebhookHandleResult> HandleOrderCompletedAsync(
            WebhookEnvelope envelope,
            string rawBody,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(envelope.OrderId))
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }

            var orderId = envelope.OrderId.Trim();

            var existing = await _context.RevolutWebhookEventClaims
                .FirstOrDefaultAsync(
                    row =>
                        row.Event == "ORDER_COMPLETED"
                        && row.ObjectId == orderId,
                    cancellationToken
                );
            if (existing != null)
            {
                // Earlier retrieve used api/1.0/orders without subscription_data,
                // so we claimed skipped_unknown. Drop that claim and re-apply.
                if (
                    string.Equals(
                        existing.Disposition,
                        RevolutWebhookClaimDispositions.SkippedUnknownBillingReason,
                        StringComparison.Ordinal
                    )
                )
                {
                    _context.RevolutWebhookEventClaims.Remove(existing);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }
            }

            var retrieved = await _merchant.GetOrderAsync(
                orderId,
                cancellationToken
            );
            if (retrieved is null || !retrieved.Succeeded)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.RetryLater
                );
            }

            var state = retrieved.State?.Trim() ?? string.Empty;
            if (IsTerminalNonCompleted(state))
            {
                return await ClaimRecordOnlyAsync(
                    "ORDER_COMPLETED",
                    orderId,
                    RevolutWebhookClaimDispositions.SkippedTerminal,
                    cancellationToken
                );
            }

            if (
                !string.Equals(
                    state,
                    "completed",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.RetryLater
                );
            }

            return await ClaimAndApplyCompletedAsync(
                orderId,
                state,
                retrieved,
                rawBody,
                cancellationToken
            );
        }

        private async Task<RevolutWebhookHandleResult> ClaimAndApplyCompletedAsync(
            string orderId,
            string state,
            RevolutOrderRetrieveResult retrieved,
            string rawBody,
            CancellationToken cancellationToken
        )
        {
            var intent = await _context.RevolutOrderIntents
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.OrderId == orderId,
                    cancellationToken
                );
            var isOneTimeIntent =
                intent != null
                && (
                    string.Equals(
                        intent.Purpose,
                        RevolutOrderIntentPurposes.PlanUpgradeProration,
                        StringComparison.Ordinal
                    )
                    || string.Equals(
                        intent.Purpose,
                        RevolutOrderIntentPurposes.ExtraLocation,
                        StringComparison.Ordinal
                    )
                    || string.Equals(
                        intent.Purpose,
                        RevolutOrderIntentPurposes.Topup,
                        StringComparison.Ordinal
                    )
                    || string.Equals(
                        intent.Purpose,
                        RevolutOrderIntentPurposes.ShopMaterialsOrder,
                        StringComparison.Ordinal
                    )
                );

            var reason = retrieved.BillingReason?.Trim() ?? string.Empty;
            var isRefundFamily = RevolutOrderTypes.IsRefundFamily(
                retrieved.OrderType
            );
            var isMintable =
                !isRefundFamily
                && RevolutOrderCompletedApplier.IsMintableBillingReason(reason);
            var disposition = isRefundFamily
                ? RevolutWebhookClaimDispositions.Applied
                : isOneTimeIntent || isMintable
                    ? RevolutWebhookClaimDispositions.Applied
                    : RevolutOrderCompletedApplier.IsFinalSettlement(reason)
                        ? RevolutWebhookClaimDispositions.Recorded
                        : RevolutWebhookClaimDispositions.SkippedUnknownBillingReason;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            var claimId = Guid.NewGuid();
            try
            {
                var again = await FindClaimAsync(
                    "ORDER_COMPLETED",
                    orderId,
                    cancellationToken
                );
                if (again != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                // Unique claim must land before side effects (lock 04).
                _context.RevolutWebhookEventClaims.Add(
                    new RevolutWebhookEventClaim
                    {
                        Id = claimId,
                        Event = "ORDER_COMPLETED",
                        ObjectId = orderId,
                        Disposition = disposition,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await _context.SaveChangesAsync(cancellationToken);

                if (
                    disposition
                    == RevolutWebhookClaimDispositions.SkippedUnknownBillingReason
                )
                {
                    _logger.LogWarning(
                        "Revolut ORDER_COMPLETED skipped unknown billing_reason {BillingReason} for order {OrderId}",
                        string.IsNullOrEmpty(reason) ? "(missing)" : reason,
                        orderId
                    );
                }
                else if (isRefundFamily)
                {
                    await _paymentRefundHandler.HandleAsync(
                        new RevolutPaymentRefundCompletedRequest(
                            RefundOrderId: orderId,
                            RelatedOrderId: retrieved.RelatedOrderId,
                            OrderType: retrieved.OrderType,
                            AmountMinor: retrieved.AmountMinor,
                            RawOrderBody: retrieved.RawBody ?? string.Empty
                        ),
                        cancellationToken
                    );
                }
                else if (isOneTimeIntent || isMintable)
                {
                    await _applier.ApplyAsync(
                        new RevolutOrderCompletedApplyRequest(
                            OrderId: orderId,
                            OrderState: state,
                            BillingReason: retrieved.BillingReason,
                            SubscriptionId: retrieved.SubscriptionId
                                ?? intent?.RevolutSubscriptionId,
                            RawWebhookBody: rawBody,
                            RawOrderBody: retrieved.RawBody ?? string.Empty,
                            PaymentMethodSummary: retrieved.PaymentMethodSummary
                        ),
                        cancellationToken
                    );
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                if (!isRefundFamily && (isMintable || isOneTimeIntent))
                {
                    await TryPatchMerchantInvoiceReferenceAsync(
                        orderId,
                        cancellationToken
                    );
                }

                if (!isRefundFamily)
                {
                    await TryRecoverOrPayDunningAfterCompletedAsync(
                        orderId,
                        retrieved.SubscriptionId,
                        isMintable,
                        cancellationToken
                    );
                }

                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }
            catch (DbUpdateException)
            {
                await AbortClaimTransactionAsync(claimId, cancellationToken);

                var conflict = await FindClaimAsync(
                    "ORDER_COMPLETED",
                    orderId,
                    cancellationToken
                );
                if (conflict != null)
                {
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                throw;
            }
            catch
            {
                await AbortClaimTransactionAsync(claimId, cancellationToken);
                throw;
            }
        }

        private sealed class NoOpPaymentRefundCompletedHandler
            : IRevolutPaymentRefundCompletedHandler
        {
            public static readonly NoOpPaymentRefundCompletedHandler Instance =
                new();

            public Task HandleAsync(
                RevolutPaymentRefundCompletedRequest request,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }

        private async Task TryPatchMerchantInvoiceReferenceAsync(
            string orderId,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var invoice = await _context.TummlyVatInvoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        row => row.RevolutOrderId == orderId,
                        cancellationToken
                    );
                if (invoice == null)
                {
                    return;
                }

                var result = await _merchant.UpdateOrderMerchantReferenceAsync(
                    orderId,
                    invoice.DocumentNumber,
                    cancellationToken
                );
                if (!result.Succeeded)
                {
                    _logger.LogWarning(
                        "Revolut merchant reference PATCH failed for order {OrderId} invoice {DocumentNumber}: {ErrorCode}",
                        orderId,
                        invoice.DocumentNumber,
                        result.ErrorCode ?? "(none)"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Revolut merchant reference PATCH threw for order {OrderId}",
                    orderId
                );
            }
        }

        private async Task<RevolutWebhookHandleResult> HandleSubscriptionOverdueAsync(
            WebhookEnvelope envelope,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(envelope.SubscriptionId))
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }

            var subscriptionId = envelope.SubscriptionId.Trim();

            var retrieved = await _merchant.GetSubscriptionAsync(
                subscriptionId,
                cancellationToken
            );
            if (retrieved is null || !retrieved.Succeeded)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.RetryLater
                );
            }

            if (
                !string.Equals(
                    retrieved.State?.Trim(),
                    "overdue",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return await ClaimRecordOnlyAsync(
                    "SUBSCRIPTION_OVERDUE",
                    subscriptionId,
                    RevolutWebhookClaimDispositions.Recorded,
                    cancellationToken
                );
            }

            string? outstandingOrderId = null;
            if (!string.IsNullOrWhiteSpace(retrieved.CurrentCycleId))
            {
                var cycle = await _merchant.GetSubscriptionCycleAsync(
                    subscriptionId,
                    retrieved.CurrentCycleId.Trim(),
                    cancellationToken
                );
                if (cycle.Succeeded && !string.IsNullOrWhiteSpace(cycle.OrderId))
                {
                    outstandingOrderId = cycle.OrderId.Trim();
                }
            }

            // Start before claim Replay so a later overdue after Recover can
            // open a new episode (claim stays keyed on subscription_id).
            var restaurantId = await ResolveRestaurantIdBySubscriptionAsync(
                subscriptionId,
                cancellationToken
            );
            if (restaurantId != null)
            {
                await _lifecycle.StartDunningEpisodeAsync(
                    restaurantId.Value,
                    _clock.GetUtcNow().UtcDateTime,
                    outstandingOrderId,
                    cancellationToken
                );
            }

            var existing = await FindClaimAsync(
                "SUBSCRIPTION_OVERDUE",
                subscriptionId,
                cancellationToken
            );
            if (existing != null)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Replay
                );
            }

            return await ClaimRecordOnlyAsync(
                "SUBSCRIPTION_OVERDUE",
                subscriptionId,
                RevolutWebhookClaimDispositions.Recorded,
                cancellationToken
            );
        }

        /// <summary>
        /// Cycle / restoration completes recover. Other completes while an
        /// episode is open (e.g. Update payment method) trigger Pay only.
        /// </summary>
        private async Task TryRecoverOrPayDunningAfterCompletedAsync(
            string orderId,
            string? subscriptionId,
            bool isMintableBillingReason,
            CancellationToken cancellationToken
        )
        {
            var restaurantId = await ResolveRestaurantIdForCompletedOrderAsync(
                orderId,
                subscriptionId,
                cancellationToken
            );
            if (restaurantId == null)
            {
                return;
            }

            var account = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId.Value,
                    cancellationToken
                );
            if (account?.DunningEpisodeStartedAt == null)
            {
                return;
            }

            var isOutstandingCycleOrder = string.Equals(
                account.DunningOutstandingOrderId?.Trim(),
                orderId,
                StringComparison.Ordinal
            );
            if (isOutstandingCycleOrder || isMintableBillingReason)
            {
                await _lifecycle.RecoverDunningAsync(
                    restaurantId.Value,
                    _clock.GetUtcNow().UtcDateTime,
                    cancellationToken
                );
                return;
            }

            await _dunningPay.TryPayOutstandingAsync(
                restaurantId.Value,
                cancellationToken
            );
        }

        private async Task<int?> ResolveRestaurantIdForCompletedOrderAsync(
            string orderId,
            string? subscriptionId,
            CancellationToken cancellationToken
        )
        {
            var byOutstanding = await _context.BillingAccounts
                .AsNoTracking()
                .Where(row => row.DunningOutstandingOrderId == orderId)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (byOutstanding != null)
            {
                return byOutstanding;
            }

            var bySetup = await _context.RevolutPendingPaySessions
                .AsNoTracking()
                .Where(row => row.SetupOrderId == orderId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (bySetup != null)
            {
                return bySetup;
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                return null;
            }

            return await ResolveRestaurantIdBySubscriptionAsync(
                subscriptionId.Trim(),
                cancellationToken
            );
        }

        private Task<int?> ResolveRestaurantIdBySubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken
        )
        {
            return _context.RevolutPendingPaySessions
                .AsNoTracking()
                .Where(row => row.RevolutSubscriptionId == subscriptionId)
                .OrderByDescending(row => row.CreatedAtUtc)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Rolls back the claim+apply transaction. Also deletes this attempt's
        /// claim by id when the provider ignores transactions (InMemory tests).
        /// </summary>
        private async Task AbortClaimTransactionAsync(
            Guid claimId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.CurrentTransaction != null)
            {
                await _context.Database.RollbackTransactionAsync(
                    cancellationToken
                );
            }

            _context.ChangeTracker.Clear();

            var leftover = await _context.RevolutWebhookEventClaims
                .FirstOrDefaultAsync(
                    row => row.Id == claimId,
                    cancellationToken
                );
            if (leftover != null)
            {
                _context.RevolutWebhookEventClaims.Remove(leftover);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<RevolutWebhookHandleResult> ClaimRecordOnlyAsync(
            string eventName,
            string objectId,
            string disposition,
            CancellationToken cancellationToken
        )
        {
            var existing = await FindClaimAsync(
                eventName,
                objectId,
                cancellationToken
            );
            if (existing != null)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Replay
                );
            }

            try
            {
                _context.RevolutWebhookEventClaims.Add(
                    new RevolutWebhookEventClaim
                    {
                        Id = Guid.NewGuid(),
                        Event = eventName,
                        ObjectId = objectId,
                        Disposition = disposition,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await _context.SaveChangesAsync(cancellationToken);
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var conflict = await FindClaimAsync(
                    eventName,
                    objectId,
                    cancellationToken
                );
                if (conflict != null)
                {
                    return new RevolutWebhookHandleResult(
                        RevolutWebhookHandleStatus.Replay
                    );
                }

                throw;
            }
        }

        private Task<RevolutWebhookEventClaim?> FindClaimAsync(
            string eventName,
            string objectId,
            CancellationToken cancellationToken
        )
        {
            return _context.RevolutWebhookEventClaims
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.Event == eventName && row.ObjectId == objectId,
                    cancellationToken
                );
        }

        private static bool IsTerminalNonCompleted(string state)
        {
            return TerminalOrderStates.Contains(state);
        }

        private static bool TryParseEnvelope(
            string rawBody,
            out WebhookEnvelope envelope
        )
        {
            envelope = default!;
            try
            {
                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;
                if (
                    !root.TryGetProperty("event", out var eventElement)
                    || eventElement.ValueKind != JsonValueKind.String
                )
                {
                    return false;
                }

                var eventName = eventElement.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(eventName))
                {
                    return false;
                }

                envelope = new WebhookEnvelope(
                    Event: eventName.Trim(),
                    OrderId: ReadString(root, "order_id"),
                    SubscriptionId: ReadString(root, "subscription_id"),
                    DisputeId: ReadString(root, "dispute_id")
                );
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryResolveObjectId(
            WebhookEnvelope envelope,
            out string objectId
        )
        {
            objectId = string.Empty;
            if (OrderEvents.Contains(envelope.Event))
            {
                if (string.IsNullOrWhiteSpace(envelope.OrderId))
                {
                    return false;
                }

                objectId = envelope.OrderId.Trim();
                return true;
            }

            if (SubscriptionEvents.Contains(envelope.Event))
            {
                if (string.IsNullOrWhiteSpace(envelope.SubscriptionId))
                {
                    return false;
                }

                objectId = envelope.SubscriptionId.Trim();
                return true;
            }

            if (
                DisputeEvents.Contains(envelope.Event)
                || envelope.Event.StartsWith(
                    "DISPUTE_",
                    StringComparison.Ordinal
                )
            )
            {
                if (string.IsNullOrWhiteSpace(envelope.DisputeId))
                {
                    return false;
                }

                objectId = envelope.DisputeId.Trim();
                return true;
            }

            // Unknown subscribed-shaped events: prefer any known object id.
            if (!string.IsNullOrWhiteSpace(envelope.OrderId))
            {
                objectId = envelope.OrderId.Trim();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(envelope.SubscriptionId))
            {
                objectId = envelope.SubscriptionId.Trim();
                return true;
            }

            if (!string.IsNullOrWhiteSpace(envelope.DisputeId))
            {
                objectId = envelope.DisputeId.Trim();
                return true;
            }

            return false;
        }

        private static string? ReadString(JsonElement root, string name)
        {
            if (
                !root.TryGetProperty(name, out var element)
                || element.ValueKind != JsonValueKind.String
            )
            {
                return null;
            }

            return element.GetString();
        }

        private sealed record WebhookEnvelope(
            string Event,
            string? OrderId,
            string? SubscriptionId,
            string? DisputeId
        );
    }

    /// <summary>
    /// Fallback when unit tests construct the webhook service without a ledger.
    /// </summary>
    file sealed class NoOpWebhookCreditLedger : ICreditLedger
    {
        public static readonly NoOpWebhookCreditLedger Instance = new();

        public Task<CreditLedgerWriteResult> ConsumeOnSuccessAsync(
            CreditLedgerConsumeRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerWriteResult> ReserveAsync(
            CreditLedgerReserveRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerWriteResult> SettleAsync(
            CreditLedgerSettleRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerWriteResult> ReleaseAsync(
            CreditLedgerReleaseRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerWriteResult> StaffManualAdjustAsync(
            StaffManualAdjustRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerWriteResult> StaffReverseAsync(
            StaffReverseRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerMintTopupResult> MintTopupAllocationAsync(
            CreditLedgerMintTopupRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerMintTopupResult.Fail("not_implemented"));

        public Task<CreditLedgerDrainTopupResult> DrainUnusedTopupAsync(
            CreditLedgerDrainTopupRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerDrainTopupResult.Ok([]));

        public Task<CreditLedgerRestoreTopupResult> RestoreUnusedTopupAsync(
            CreditLedgerRestoreTopupRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerRestoreTopupResult.Ok([]));

        public Task<CreditLedgerWriteResult> ReleaseHeldAsync(
            CreditLedgerReleaseHeldRequest request,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Fail("not_implemented"));

        public Task<CreditLedgerWriteResult> MintPilotAtActivationAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(CreditLedgerWriteResult.Ok([]));
    }

    file sealed class NoOpWebhookVatInvoiceService : ITummlyVatInvoiceService
    {
        public static readonly NoOpWebhookVatInvoiceService Instance = new();

        public Task<TummlyVatInvoice> MintForCompletedOrderAsync(
            TummlyVatInvoiceMintRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new TummlyVatInvoice
                {
                    Id = Guid.NewGuid(),
                    RevolutOrderId = request.RevolutOrderId,
                    RestaurantId = request.RestaurantId,
                }
            );

        public Task<TummlyVatInvoice> MintCreditNoteForRefundAsync(
            TummlyVatCreditNoteMintRequest request,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new TummlyVatInvoice
                {
                    Id = Guid.NewGuid(),
                    DocumentPrefix = TummlyDocumentSequence.PrefixTcn,
                    DocumentNumber = "TCN-noop",
                    RevolutOrderId = request.RefundOrderId,
                    RelatedRevolutOrderId = request.OriginalPaymentOrderId,
                    RestaurantId = request.RestaurantId,
                    PaymentStatus = TummlyVatInvoice.PaymentStatusPaid,
                }
            );

        public Task<TummlyVatInvoice?> FindByRevolutOrderIdAsync(
            string revolutOrderId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<TummlyVatInvoice?>(null);

        public Task<IReadOnlyList<InvoiceRowDto>> ListInvoiceRowsForRestaurantAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<IReadOnlyList<InvoiceRowDto>>([]);

        public Task<(byte[] Content, string FileName)?> RenderPdfAsync(
            int restaurantId,
            string documentNumber,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<(byte[] Content, string FileName)?>(null);
    }
}
