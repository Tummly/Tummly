using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaign fire / send execution (ticket 31): drop-only revalidate, settle
    /// accepted units, cannot-start → Failed + release, no auto-retry clock.
    /// </summary>
    public class CampaignFireService : ICampaignFireService
    {
        public const string ScheduledStatus = "scheduled";
        public const string SendingStatus = "sending";
        public const string SentStatus = "sent";
        public const string PartiallySentStatus = "partially-sent";
        public const string FailedStatus = "failed";

        public const string AcceptedOutcome = "accepted";
        public const string SkippedIneligibleOutcome = "skipped-ineligible";
        public const string RejectedOutcome = "rejected";

        private readonly ApplicationDbContext _context;
        private readonly ICampaignEligibilityService _eligibility;
        private readonly ICampaignBillingReserve _billingReserve;
        private readonly ICampaignOutboundSender _outbound;
        private readonly ICampaignSendStartGate _sendStartGate;
        private readonly Func<DateTime> _utcNow;

        public CampaignFireService(
            ApplicationDbContext context,
            ICampaignEligibilityService eligibility,
            ICampaignBillingReserve billingReserve,
            ICampaignOutboundSender outbound,
            ICampaignSendStartGate sendStartGate,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _eligibility = eligibility;
            _billingReserve = billingReserve;
            _outbound = outbound;
            _sendStartGate = sendStartGate;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<CampaignFireResult> FireAsync(
            int campaignId,
            CancellationToken cancellationToken = default
        )
        {
            var entity = await _context.Campaigns
                .FirstOrDefaultAsync(
                    campaign => campaign.Id == campaignId,
                    cancellationToken
                );

            if (entity == null)
            {
                return new CampaignFireResult.NotFound();
            }

            var status = (entity.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (status is not (ScheduledStatus or SendingStatus or PartiallySentStatus))
            {
                return new CampaignFireResult.NotFireable
                {
                    Message =
                        "Campaign must be scheduled, sending, or partially-sent to fire.",
                };
            }

            var now = _utcNow();
            if (
                status == ScheduledStatus
                && entity.ScheduledAtUtc is DateTime due
                && DateTime.SpecifyKind(due, DateTimeKind.Utc) > now
            )
            {
                return new CampaignFireResult.NotDue();
            }

            var channel = (entity.Channel ?? string.Empty).Trim().ToLowerInvariant();
            if (channel.Length == 0)
            {
                return await FailCannotStartAsync(
                    entity,
                    unusedUnits: entity.ReservedEstimate ?? 0,
                    cancellationToken
                );
            }

            var gate = await _sendStartGate.EvaluateAsync(
                entity.Id,
                entity.RestaurantLocationId,
                cancellationToken
            );
            if (gate is not CampaignSendStartGateResult.Clear)
            {
                return await FailCannotStartAsync(
                    entity,
                    unusedUnits: entity.ReservedEstimate ?? 0,
                    cancellationToken
                );
            }

            var audienceKey = (entity.AudienceKey ?? string.Empty).Trim();
            IReadOnlyList<int> liveEligibleIds;
            try
            {
                liveEligibleIds =
                    await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                        entity.RestaurantLocationId,
                        audienceKey,
                        channel,
                        cancellationToken
                    );
            }
            catch (ArgumentException)
            {
                return await FailCannotStartAsync(
                    entity,
                    unusedUnits: entity.ReservedEstimate ?? 0,
                    cancellationToken
                );
            }

            var liveEligible = liveEligibleIds.ToHashSet();

            var frozenRows = await _context.CampaignFrozenRecipients
                .Where(row => row.CampaignId == entity.Id)
                .OrderBy(row => row.Id)
                .ToListAsync(cancellationToken);

            var alreadyAccepted = await _context.CampaignRecipientDeliveries
                .Where(
                    row =>
                        row.CampaignId == entity.Id
                        && row.Outcome == AcceptedOutcome
                )
                .Select(row => row.LocationGuestId)
                .ToListAsync(cancellationToken);
            var alreadyAcceptedSet = alreadyAccepted.ToHashSet();

            var alreadyHandled = await _context.CampaignRecipientDeliveries
                .Where(row => row.CampaignId == entity.Id)
                .Select(row => row.LocationGuestId)
                .ToListAsync(cancellationToken);
            var handledSet = alreadyHandled.ToHashSet();

            // Drop-only: frozen ∩ live eligible; never silent-add.
            var sendable = frozenRows
                .Select(row => row.LocationGuestId)
                .Where(id => liveEligible.Contains(id))
                .Where(id => !alreadyAcceptedSet.Contains(id))
                .Distinct()
                .ToList();

            var newlyIneligibleFrozen = frozenRows
                .Select(row => row.LocationGuestId)
                .Where(id => !liveEligible.Contains(id))
                .Where(id => !alreadyAcceptedSet.Contains(id))
                .Distinct()
                .ToList();

            if (
                alreadyAcceptedSet.Count == 0
                && sendable.Count == 0
            )
            {
                foreach (var guestId in newlyIneligibleFrozen)
                {
                    await UpsertDeliveryAsync(
                        entity.Id,
                        guestId,
                        channel,
                        SkippedIneligibleOutcome,
                        acceptedAtUtc: null,
                        now,
                        cancellationToken
                    );
                }

                return await FailCannotStartAsync(
                    entity,
                    unusedUnits: entity.ReservedEstimate ?? frozenRows.Count,
                    cancellationToken
                );
            }

            if (status == ScheduledStatus)
            {
                entity.Status = SendingStatus;
                entity.UpdatedAt = now;
            }

            var acceptedThisRun = 0;
            var skippedThisRun = 0;

            foreach (var guestId in newlyIneligibleFrozen)
            {
                await UpsertDeliveryAsync(
                    entity.Id,
                    guestId,
                    channel,
                    SkippedIneligibleOutcome,
                    acceptedAtUtc: null,
                    now,
                    cancellationToken
                );
                skippedThisRun++;
                handledSet.Add(guestId);
            }

            var guestContacts = await _context.LocationGuests
                .AsNoTracking()
                .Include(lg => lg.MasterGuest)
                .Where(lg => sendable.Contains(lg.Id))
                .ToDictionaryAsync(lg => lg.Id, cancellationToken);

            foreach (var guestId in sendable)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (!guestContacts.TryGetValue(guestId, out var locationGuest))
                {
                    await UpsertDeliveryAsync(
                        entity.Id,
                        guestId,
                        channel,
                        SkippedIneligibleOutcome,
                        acceptedAtUtc: null,
                        now,
                        cancellationToken
                    );
                    skippedThisRun++;
                    handledSet.Add(guestId);
                    continue;
                }

                var toAddress =
                    channel == "email"
                        ? locationGuest.MasterGuest?.Email?.Trim()
                        : locationGuest.MasterGuest?.Mobile?.Trim();
                if (string.IsNullOrWhiteSpace(toAddress))
                {
                    await UpsertDeliveryAsync(
                        entity.Id,
                        guestId,
                        channel,
                        SkippedIneligibleOutcome,
                        acceptedAtUtc: null,
                        now,
                        cancellationToken
                    );
                    skippedThisRun++;
                    handledSet.Add(guestId);
                    continue;
                }

                CampaignOutboundSendResult sendResult;
                try
                {
                    sendResult = await _outbound.SendAsync(
                        new CampaignOutboundSendRequest
                        {
                            CampaignId = entity.Id,
                            LocationGuestId = guestId,
                            Channel = channel,
                            ToAddress = toAddress,
                            Subject = entity.MessageSubject,
                            Body = entity.MessageBody ?? string.Empty,
                        },
                        cancellationToken
                    );
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested
                )
                {
                    break;
                }

                if (sendResult is CampaignOutboundSendResult.Accepted)
                {
                    await UpsertDeliveryAsync(
                        entity.Id,
                        guestId,
                        channel,
                        AcceptedOutcome,
                        acceptedAtUtc: now,
                        now,
                        CancellationToken.None
                    );
                    acceptedThisRun++;
                    alreadyAcceptedSet.Add(guestId);
                    handledSet.Add(guestId);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                else
                {
                    await UpsertDeliveryAsync(
                        entity.Id,
                        guestId,
                        channel,
                        RejectedOutcome,
                        acceptedAtUtc: null,
                        now,
                        CancellationToken.None
                    );
                    handledSet.Add(guestId);
                }
            }

            var totalAccepted = alreadyAcceptedSet.Count;

            // Remaining = still eligible frozen guests with no delivery fact yet
            // (mid-send stop). Rejected / skipped are terminal for this run.
            var remainingUnsent = frozenRows
                .Select(row => row.LocationGuestId)
                .Distinct()
                .Count(
                    id =>
                        liveEligible.Contains(id) && !handledSet.Contains(id)
                );

            if (
                acceptedThisRun > 0
                && !string.IsNullOrWhiteSpace(entity.BillingReservationRef)
            )
            {
                var settle = await _billingReserve.SettleAsync(
                    new CampaignBillingSettleRequest
                    {
                        CampaignId = entity.Id,
                        ReservationRef = entity.BillingReservationRef,
                        Channel = channel,
                        AcceptedUnits = acceptedThisRun,
                    },
                    CancellationToken.None
                );

                if (settle is CampaignBillingSettleResult.Failed)
                {
                    // Keep hold + Sending / Partially sent; no auto-retry clock.
                    entity.Status =
                        remainingUnsent >= 1 || totalAccepted < frozenRows.Count
                            ? PartiallySentStatus
                            : SendingStatus;
                    entity.UpdatedAt = now;
                    await _context.SaveChangesAsync(CancellationToken.None);

                    var skippedOnSettleFail = await CountOutcomeAsync(
                        entity.Id,
                        SkippedIneligibleOutcome,
                        CancellationToken.None
                    );

                    return new CampaignFireResult.Ok
                    {
                        Campaign = ToDto(
                            entity,
                            acceptedCount: totalAccepted,
                            skippedIneligibleCount: skippedOnSettleFail,
                            remainingUnsentCount: remainingUnsent
                        ),
                    };
                }
            }

            // Release unused units for eligibility shrink when the send completes
            // (no remaining unsent). Mid-send stop keeps the residual hold.
            if (
                skippedThisRun > 0
                && remainingUnsent == 0
                && !string.IsNullOrWhiteSpace(entity.BillingReservationRef)
            )
            {
                var release = await _billingReserve.ReleaseAsync(
                    new CampaignBillingReleaseRequest
                    {
                        CampaignId = entity.Id,
                        ReservationRef = entity.BillingReservationRef,
                    },
                    CancellationToken.None
                );

                if (release is CampaignBillingReleaseResult.Failed)
                {
                    // Status still advances; Billing can reconcile the residual hold.
                }
            }

            if (totalAccepted == 0)
            {
                return await FailCannotStartAsync(
                    entity,
                    unusedUnits: entity.ReservedEstimate ?? frozenRows.Count,
                    cancellationToken
                );
            }

            if (remainingUnsent >= 1)
            {
                entity.Status = PartiallySentStatus;
            }
            else
            {
                entity.Status = SentStatus;
                entity.BillingReservationRef = null;
            }

            entity.UpdatedAt = now;
            await _context.SaveChangesAsync(CancellationToken.None);

            var skippedTotal = await CountOutcomeAsync(
                entity.Id,
                SkippedIneligibleOutcome,
                CancellationToken.None
            );

            return new CampaignFireResult.Ok
            {
                Campaign = ToDto(
                    entity,
                    acceptedCount: totalAccepted,
                    skippedIneligibleCount: skippedTotal,
                    remainingUnsentCount: remainingUnsent
                ),
            };
        }

        private async Task<CampaignFireResult> FailCannotStartAsync(
            Campaign entity,
            int unusedUnits,
            CancellationToken cancellationToken
        )
        {
            var now = _utcNow();
            var channel = (entity.Channel ?? string.Empty).Trim().ToLowerInvariant();
            var reservationRef = entity.BillingReservationRef;

            if (
                !string.IsNullOrWhiteSpace(reservationRef)
                && unusedUnits > 0
            )
            {
                await _billingReserve.ReleaseAsync(
                    new CampaignBillingReleaseRequest
                    {
                        CampaignId = entity.Id,
                        ReservationRef = reservationRef,
                    },
                    CancellationToken.None
                );
            }

            entity.Status = FailedStatus;
            entity.BillingReservationRef = null;
            entity.UpdatedAt = now;
            await _context.SaveChangesAsync(CancellationToken.None);

            return new CampaignFireResult.CannotStart
            {
                Campaign = ToDto(
                    entity,
                    acceptedCount: 0,
                    skippedIneligibleCount: 0,
                    remainingUnsentCount: 0
                ),
            };
        }

        private async Task UpsertDeliveryAsync(
            int campaignId,
            int locationGuestId,
            string channel,
            string outcome,
            DateTime? acceptedAtUtc,
            DateTime now,
            CancellationToken cancellationToken
        )
        {
            var existing = await _context.CampaignRecipientDeliveries
                .FirstOrDefaultAsync(
                    row =>
                        row.CampaignId == campaignId
                        && row.LocationGuestId == locationGuestId
                        && row.Channel == channel,
                    cancellationToken
                );

            if (existing != null)
            {
                if (existing.Outcome == AcceptedOutcome)
                {
                    return;
                }

                existing.Outcome = outcome;
                existing.AcceptedAtUtc = acceptedAtUtc;
                existing.UpdatedAtUtc = now;
                return;
            }

            _context.CampaignRecipientDeliveries.Add(
                new CampaignRecipientDelivery
                {
                    CampaignId = campaignId,
                    LocationGuestId = locationGuestId,
                    Channel = channel,
                    Outcome = outcome,
                    AcceptedAtUtc = acceptedAtUtc,
                    UpdatedAtUtc = now,
                }
            );
        }

        private async Task<int> CountOutcomeAsync(
            int campaignId,
            string outcome,
            CancellationToken cancellationToken
        )
        {
            return await _context.CampaignRecipientDeliveries.CountAsync(
                row => row.CampaignId == campaignId && row.Outcome == outcome,
                cancellationToken
            );
        }

        private static CampaignFireDto ToDto(
            Campaign entity,
            int acceptedCount,
            int skippedIneligibleCount,
            int remainingUnsentCount
        )
        {
            return new CampaignFireDto
            {
                Id = entity.Id,
                Status = entity.Status,
                AcceptedCount = acceptedCount,
                SkippedIneligibleCount = skippedIneligibleCount,
                RemainingUnsentCount = remainingUnsentCount,
                BillingReservationRef = entity.BillingReservationRef,
                RowVersion = entity.RowVersion,
            };
        }
    }
}
