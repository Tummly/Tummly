using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
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
            ILogger<RevolutWebhookService>? logger = null
        )
        {
            _context = context;
            _merchant = merchant;
            _applier = applier;
            _lifecycle = lifecycle;
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

            if (!TryResolveObjectId(envelope, out var objectId))
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Accepted
                );
            }

            return await ClaimRecordOnlyAsync(
                envelope.Event,
                objectId,
                RevolutWebhookClaimDispositions.Recorded,
                cancellationToken
            );
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

            var existing = await FindClaimAsync(
                "ORDER_COMPLETED",
                orderId,
                cancellationToken
            );
            if (existing != null)
            {
                return new RevolutWebhookHandleResult(
                    RevolutWebhookHandleStatus.Replay
                );
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
                );

            var reason = retrieved.BillingReason?.Trim() ?? string.Empty;
            var isMintable =
                RevolutOrderCompletedApplier.IsMintableBillingReason(reason);
            var disposition = isOneTimeIntent || isMintable
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
                            RawOrderBody: retrieved.RawBody ?? string.Empty
                        ),
                        cancellationToken
                    );
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                if (isMintable || isOneTimeIntent)
                {
                    await TryPatchMerchantInvoiceReferenceAsync(
                        orderId,
                        cancellationToken
                    );
                }

                await TryRecoverDunningAfterCompletedAsync(
                    orderId,
                    retrieved.SubscriptionId,
                    cancellationToken
                );

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

            return await ClaimRecordOnlyAsync(
                "SUBSCRIPTION_OVERDUE",
                subscriptionId,
                RevolutWebhookClaimDispositions.Recorded,
                cancellationToken
            );
        }

        private async Task TryRecoverDunningAfterCompletedAsync(
            string orderId,
            string? subscriptionId,
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

            var episodeOpen = await _context.BillingAccounts
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.RestaurantId == restaurantId.Value
                        && row.DunningEpisodeStartedAt != null,
                    cancellationToken
                );
            if (!episodeOpen)
            {
                return;
            }

            await _lifecycle.RecoverDunningAsync(
                restaurantId.Value,
                _clock.GetUtcNow().UtcDateTime,
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
}
