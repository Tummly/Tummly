using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
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
        private readonly ICampaignProductAnalytics _analytics;
        private readonly IOfferIssueService _offerIssues;
        private readonly Func<DateTime> _utcNow;

        public CampaignFireService(
            ApplicationDbContext context,
            ICampaignEligibilityService eligibility,
            ICampaignBillingReserve billingReserve,
            ICampaignOutboundSender outbound,
            ICampaignSendStartGate sendStartGate,
            ICampaignProductAnalytics? analytics = null,
            IOfferIssueService? offerIssues = null,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _eligibility = eligibility;
            _billingReserve = billingReserve;
            _outbound = outbound;
            _sendStartGate = sendStartGate;
            _analytics = analytics ?? NoOpCampaignProductAnalytics.Instance;
            _offerIssues = offerIssues ?? new OfferIssueService(context);
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
                        acceptedUnits: null,
                        now,
                        cancellationToken
                    );
                }

                return await FailCannotStartAsync(
                    entity,
                    cancellationToken
                );
            }

            _analytics.TrackSendStart(entity.Id);

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
                    acceptedUnits: null,
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
                        acceptedUnits: null,
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
                        acceptedUnits: null,
                        now,
                        cancellationToken
                    );
                    skippedThisRun++;
                    handledSet.Add(guestId);
                    continue;
                }

                CampaignOutboundSendResult sendResult;
                GuestResponseEmailOfferBlock? offerBlock = null;
                string? preallocatedClaimCode = null;
                try
                {
                    if (entity.OfferId is int offerIdForSend
                        && string.Equals(channel, "email", StringComparison.Ordinal))
                    {
                        (offerBlock, preallocatedClaimCode) =
                            await TryBuildCampaignOfferEmailBlockAsync(
                                offerIdForSend,
                                now,
                                cancellationToken
                            );
                    }

                    sendResult = await _outbound.SendAsync(
                        new CampaignOutboundSendRequest
                        {
                            CampaignId = entity.Id,
                            LocationGuestId = guestId,
                            Channel = channel,
                            ToAddress = toAddress,
                            Subject = entity.MessageSubject,
                            Body = entity.MessageBody ?? string.Empty,
                            Offer = offerBlock,
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
                    var acceptedUnits = CampaignBillingClose.AcceptedUnitsForDelivery(
                        channel,
                        entity.MessageBody
                    );
                    await UpsertDeliveryAsync(
                        entity.Id,
                        guestId,
                        channel,
                        AcceptedOutcome,
                        acceptedAtUtc: now,
                        acceptedUnits,
                        now,
                        CancellationToken.None
                    );

                    if (entity.OfferId is int catalogOfferId)
                    {
                        await _offerIssues.IssueOnCampaignAcceptedAsync(
                            entity.Id,
                            guestId,
                            catalogOfferId,
                            channel,
                            now,
                            CancellationToken.None,
                            preallocatedClaimCode
                        );
                    }

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
                        acceptedUnits: null,
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

            if (totalAccepted == 0)
            {
                return await FailCannotStartAsync(
                    entity,
                    cancellationToken
                );
            }

            await _context.SaveChangesAsync(CancellationToken.None);

            var settleResult = await CampaignBillingClose.SettleUnbilledAsync(
                entity,
                _billingReserve,
                _context,
                channel,
                CancellationToken.None
            );
            if (!settleResult.Succeeded)
            {
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

                if (entity.Status == PartiallySentStatus)
                {
                    _analytics.TrackSendTerminal(
                        entity.Id,
                        PartiallySentStatus
                    );
                }

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

            if (remainingUnsent >= 1)
            {
                entity.Status = PartiallySentStatus;
                entity.UpdatedAt = now;
                await _context.SaveChangesAsync(CancellationToken.None);
                _analytics.TrackSendTerminal(entity.Id, entity.Status);

                var skippedPartial = await CountOutcomeAsync(
                    entity.Id,
                    SkippedIneligibleOutcome,
                    CancellationToken.None
                );

                return new CampaignFireResult.Ok
                {
                    Campaign = ToDto(
                        entity,
                        acceptedCount: totalAccepted,
                        skippedIneligibleCount: skippedPartial,
                        remainingUnsentCount: remainingUnsent
                    ),
                };
            }

            var close = await CampaignBillingClose.CloseHoldAsync(
                entity,
                _billingReserve,
                _context,
                channel,
                settleUnbilled: false,
                CancellationToken.None
            );
            if (close.SettleFailed || close.ReleaseFailed)
            {
                entity.Status = SendingStatus;
                entity.UpdatedAt = now;
                await _context.SaveChangesAsync(CancellationToken.None);

                var skippedOnCloseFail = await CountOutcomeAsync(
                    entity.Id,
                    SkippedIneligibleOutcome,
                    CancellationToken.None
                );

                return new CampaignFireResult.Ok
                {
                    Campaign = ToDto(
                        entity,
                        acceptedCount: totalAccepted,
                        skippedIneligibleCount: skippedOnCloseFail,
                        remainingUnsentCount: remainingUnsent
                    ),
                };
            }

            entity.Status = SentStatus;
            entity.UpdatedAt = now;
            await _context.SaveChangesAsync(CancellationToken.None);

            _analytics.TrackSendTerminal(entity.Id, entity.Status);

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
            CancellationToken cancellationToken
        )
        {
            var now = _utcNow();
            var channel = (entity.Channel ?? string.Empty).Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(entity.BillingReservationRef))
            {
                await CampaignBillingClose.CloseHoldAsync(
                    entity,
                    _billingReserve,
                    _context,
                    channel,
                    settleUnbilled: false,
                    CancellationToken.None
                );
            }

            entity.Status = FailedStatus;
            entity.UpdatedAt = now;
            await _context.SaveChangesAsync(CancellationToken.None);

            _analytics.TrackSendTerminal(entity.Id, FailedStatus);

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
            int? acceptedUnits,
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
                existing.AcceptedUnits = acceptedUnits;
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
                    AcceptedUnits = acceptedUnits,
                    UpdatedAtUtc = now,
                }
            );
        }

        /// <summary>
        /// Pre-mint Offer Claim code for the guest email (QR + code). Issue row
        /// is still written only after provider Accepted, using this same code.
        /// </summary>
        private async Task<(
            GuestResponseEmailOfferBlock? Offer,
            string? ClaimCode
        )> TryBuildCampaignOfferEmailBlockAsync(
            int catalogOfferId,
            DateTime atUtc,
            CancellationToken cancellationToken
        )
        {
            var catalog = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == catalogOfferId, cancellationToken);

            if (catalog == null
                || !string.Equals(
                    catalog.Status,
                    OfferIssueService.ActiveStatus,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return (null, null);
            }

            var title = catalog.Title?.Trim() ?? string.Empty;
            if (title.Length == 0)
            {
                return (null, null);
            }

            var claimCode = await AllocateUniqueClaimCodeAsync(cancellationToken);
            var expiryAt = CatalogOfferMapping.ComputeExpiryAt(
                catalog.Validity,
                atUtc,
                catalog.CustomExpiryDate
            );

            return (
                new GuestResponseEmailOfferBlock(
                    Title: title,
                    Description: catalog.Description?.Trim() ?? string.Empty,
                    RedemptionCode: claimCode,
                    ExpiryLabel: FeedbackRecoveryOfferMapping.FormatOfferExpiryLabel(
                        expiryAt
                    )
                ),
                claimCode
            );
        }

        private async Task<string> AllocateUniqueClaimCodeAsync(
            CancellationToken cancellationToken
        )
        {
            for (var attempt = 1; ; attempt++)
            {
                var claimCode = FeedbackRecoveryOfferMapping.GenerateRedemptionCode();
                var exists = await _context.OfferIssues
                    .AsNoTracking()
                    .AnyAsync(o => o.ClaimCode == claimCode, cancellationToken);
                if (!exists)
                {
                    return claimCode;
                }

                if (attempt >= OfferIssueService.MaxCodeAttempts)
                {
                    throw new OfferIssueCodeAllocationException();
                }
            }
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
