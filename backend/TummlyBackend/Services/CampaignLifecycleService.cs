using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Campaign list lifecycle transitions (ticket 30). No approval workflow.
    /// Resume / Retry revalidate freeze drop-only; never silent-add.
    /// </summary>
    public class CampaignLifecycleService : ICampaignLifecycleService
    {
        public const string DraftStatus = "draft";
        public const int NameMaxLength = 200;
        public const string DuplicateNameSuffix = " - Draft";
        public const string ScheduledStatus = "scheduled";
        public const string SendingStatus = "sending";
        public const string SentStatus = "sent";
        public const string PartiallySentStatus = "partially-sent";
        public const string PausedStatus = "paused";
        public const string FailedStatus = "failed";
        public const string CancelledStatus = "cancelled";

        public const string SendNowMode = "send-now";
        public const string ScheduleLaterMode = "schedule-later";

        private readonly ApplicationDbContext _context;
        private readonly ICampaignEligibilityService _eligibility;
        private readonly ICampaignBillingReserve _billingReserve;
        private readonly ICreditBalanceSnapshot _creditBalance;
        private readonly Func<DateTime> _utcNow;

        public CampaignLifecycleService(
            ApplicationDbContext context,
            ICampaignEligibilityService eligibility,
            ICampaignBillingReserve billingReserve,
            ICreditBalanceSnapshot creditBalance,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _eligibility = eligibility;
            _billingReserve = billingReserve;
            _creditBalance = creditBalance;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public static string BuildDuplicateName(string originalName)
        {
            var source = originalName ?? string.Empty;
            if (source.Length + DuplicateNameSuffix.Length <= NameMaxLength)
            {
                return source + DuplicateNameSuffix;
            }

            var keep = Math.Max(0, NameMaxLength - DuplicateNameSuffix.Length);
            return source[..keep] + DuplicateNameSuffix;
        }

        public async Task<CampaignLifecycleResult> UnscheduleAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var entity = found.Entity;
            if (
                !string.Equals(
                    entity.Status,
                    ScheduledStatus,
                    StringComparison.Ordinal
                )
            )
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "Only Scheduled campaigns can be unscheduled.",
                };
            }

            var releaseError = await ReleaseReservationIfHeldAsync(
                entity,
                cancellationToken
            );
            if (releaseError != null)
            {
                return releaseError;
            }

            ClearFreeze(entity.Id);
            ClearScheduleHold(entity);
            entity.Status = DraftStatus;
            entity.UpdatedAt = _utcNow();

            return await SaveLifecycleAsync(entity, cancellationToken);
        }

        public async Task<CampaignLifecycleResult> PauseAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var entity = found.Entity;
            var fromScheduled = string.Equals(
                entity.Status,
                ScheduledStatus,
                StringComparison.Ordinal
            );
            var fromSending = string.Equals(
                entity.Status,
                SendingStatus,
                StringComparison.Ordinal
            );
            if (!fromScheduled && !fromSending)
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message =
                        "Pause is only allowed from Scheduled or Sending.",
                };
            }

            var releaseError = await ReleaseReservationIfHeldAsync(
                entity,
                cancellationToken
            );
            if (releaseError != null)
            {
                return releaseError;
            }

            // Keep freeze for Resume; clear hold refs after release.
            entity.BillingReservationRef = null;
            entity.ReservedEstimate = null;
            entity.Status = PausedStatus;
            entity.UpdatedAt = _utcNow();

            return await SaveLifecycleAsync(entity, cancellationToken);
        }

        public async Task<CampaignLifecycleResult> CancelAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var entity = found.Entity;
            var fromScheduled = string.Equals(
                entity.Status,
                ScheduledStatus,
                StringComparison.Ordinal
            );
            var fromSending = string.Equals(
                entity.Status,
                SendingStatus,
                StringComparison.Ordinal
            );
            var fromPaused = string.Equals(
                entity.Status,
                PausedStatus,
                StringComparison.Ordinal
            );
            if (!fromScheduled && !fromSending && !fromPaused)
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message =
                        "Cancel is only allowed from Scheduled, Sending, or Paused.",
                };
            }

            var releaseError = await ReleaseReservationIfHeldAsync(
                entity,
                cancellationToken
            );
            if (releaseError != null)
            {
                return releaseError;
            }

            var hasAccepted = await _context.CampaignFrozenRecipients.AnyAsync(
                row =>
                    row.CampaignId == entity.Id && row.AcceptedAtUtc != null,
                cancellationToken
            );

            // Cancel remaining mid-send with ≥1 accept → Partially sent.
            var nextStatus =
                fromSending && hasAccepted
                    ? PartiallySentStatus
                    : CancelledStatus;

            if (nextStatus == CancelledStatus)
            {
                ClearFreeze(entity.Id);
                ClearScheduleHold(entity);
            }
            else
            {
                entity.BillingReservationRef = null;
                entity.ReservedEstimate = null;
            }

            entity.Status = nextStatus;
            entity.UpdatedAt = _utcNow();

            return await SaveLifecycleAsync(entity, cancellationToken);
        }

        public async Task<CampaignLifecycleResult> ResumeAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var entity = found.Entity;
            if (
                !string.Equals(
                    entity.Status,
                    PausedStatus,
                    StringComparison.Ordinal
                )
            )
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "Resume is only allowed from Paused.",
                };
            }

            return await RevalidateReReserveAsync(
                entity,
                resumeTarget: true,
                cancellationToken
            );
        }

        public async Task<CampaignLifecycleResult> RetryRemainingAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var entity = found.Entity;
            if (
                !string.Equals(
                    entity.Status,
                    PartiallySentStatus,
                    StringComparison.Ordinal
                )
            )
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message =
                        "Retry remaining is only allowed from Partially sent.",
                };
            }

            if (!string.IsNullOrWhiteSpace(entity.BillingReservationRef))
            {
                return await RetryWithOpenRefAsync(entity, cancellationToken);
            }

            return await RevalidateReReserveAsync(
                entity,
                resumeTarget: false,
                cancellationToken
            );
        }

        public async Task<CampaignLifecycleResult> DuplicateAsDraftAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var source = found.Entity;
            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == source.RestaurantLocationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId == 0)
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "Campaign location was not found.",
                };
            }

            var billingAccount = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (billingAccount != null)
            {
                var paidDeny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
                    OperatorBillingLockEvaluator.FromBillingAccount(billingAccount)
                );
                if (paidDeny != null)
                {
                    return new CampaignLifecycleResult.OperatorBillingLocked
                    {
                        Code = paidDeny,
                    };
                }
            }

            if (
                !string.Equals(
                    source.Status,
                    FailedStatus,
                    StringComparison.Ordinal
                )
            )
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message =
                        "Duplicate / retry as new Draft is only allowed from Failed.",
                };
            }

            var now = _utcNow();
            var draft = new Campaign
            {
                RestaurantLocationId = source.RestaurantLocationId,
                Status = DraftStatus,
                Name = BuildDuplicateName(source.Name),
                GoalId = source.GoalId,
                TemplateId = source.TemplateId,
                TemplateVersion = source.TemplateVersion,
                AudienceKey = source.AudienceKey,
                Channel = source.Channel,
                OfferStance = source.OfferStance,
                OfferId = source.OfferId,
                MessageSubject = source.MessageSubject,
                MessageBody = source.MessageBody,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _context.Campaigns.Add(draft);
            await _context.SaveChangesAsync(cancellationToken);

            return new CampaignLifecycleResult.Duplicated
            {
                Campaign = new CampaignDuplicateDto
                {
                    Id = draft.Id,
                    LocationId = draft.RestaurantLocationId,
                    Status = draft.Status,
                    Name = draft.Name,
                    GoalId = draft.GoalId,
                    AudienceKey = draft.AudienceKey,
                    Channel = draft.Channel,
                    OfferStance = draft.OfferStance,
                    OfferId = draft.OfferId,
                    MessageSubject = draft.MessageSubject,
                    MessageBody = draft.MessageBody,
                    TemplateId = draft.TemplateId,
                    TemplateVersion = draft.TemplateVersion,
                    RowVersion = draft.RowVersion,
                    CreatedAt = draft.CreatedAt,
                    UpdatedAt = draft.UpdatedAt,
                },
            };
        }

        public async Task<CampaignLifecycleResult> DeleteDraftAsync(
            int campaignId,
            CampaignLifecycleActionRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var loaded = await LoadForMutationAsync(
                campaignId,
                request.RowVersion,
                cancellationToken
            );
            if (loaded is not LoadResult.Found found)
            {
                return ToLifecycleResult(loaded);
            }

            var entity = found.Entity;
            if (
                !string.Equals(
                    entity.Status,
                    DraftStatus,
                    StringComparison.Ordinal
                )
            )
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "Only draft campaigns can be deleted.",
                };
            }

            // Drafts must not hold Billing reservation or freeze rows; clear
            // defensively so InMemory matches SQL cascade behaviour.
            ClearFreeze(entity.Id);
            _context.Campaigns.Remove(entity);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new CampaignLifecycleResult.Conflict();
            }

            return new CampaignLifecycleResult.Deleted();
        }

        private async Task<CampaignLifecycleResult> RevalidateReReserveAsync(
            Campaign entity,
            bool resumeTarget,
            CancellationToken cancellationToken
        )
        {
            if (!_billingReserve.IsLive)
            {
                return new CampaignLifecycleResult.BillingReserveUnavailable();
            }

            if (string.IsNullOrWhiteSpace(entity.Channel))
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "channel is required before resume or retry.",
                };
            }

            if (string.IsNullOrWhiteSpace(entity.AudienceKey))
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "audienceKey is required before resume or retry.",
                };
            }

            var channel = entity.Channel.Trim().ToLowerInvariant();
            var audienceKey = entity.AudienceKey.Trim();
            var now = _utcNow();

            var frozenIds = await _context.CampaignFrozenRecipients
                .Where(row =>
                    row.CampaignId == entity.Id && row.AcceptedAtUtc == null
                )
                .Select(row => row.LocationGuestId)
                .ToListAsync(cancellationToken);

            if (frozenIds.Count == 0)
            {
                return new CampaignLifecycleResult.ZeroEligible();
            }

            IReadOnlyList<int> eligibleIds;
            try
            {
                eligibleIds =
                    await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                        entity.RestaurantLocationId,
                        audienceKey,
                        channel,
                        cancellationToken
                    );
            }
            catch (ArgumentException ex)
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = ex.Message,
                };
            }

            var eligibleSet = eligibleIds.ToHashSet();
            // Drop-only: keep frozen ∩ currently eligible; never silent-add.
            var remaining = frozenIds
                .Where(id => eligibleSet.Contains(id))
                .Distinct()
                .ToList();

            if (remaining.Count == 0)
            {
                return new CampaignLifecycleResult.ZeroEligible();
            }

            var estimatedUnits = CampaignCreditEstimate.EstimateUnits(
                channel,
                entity.MessageBody,
                remaining.Count
            );

            var restaurantId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == entity.RestaurantLocationId)
                .Select(row => row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
            if (restaurantId == 0)
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "Campaign location was not found.",
                };
            }

            var lockDeny = await EvaluateOperatorBillingLockAsync(
                restaurantId,
                cancellationToken
            );
            if (lockDeny != null)
            {
                return new CampaignLifecycleResult.OperatorBillingLocked
                {
                    Code = lockDeny,
                };
            }

            var snapshot = await _creditBalance.GetAccountAsync(
                restaurantId,
                cancellationToken
            );
            var gateRefusal = CampaignCreditGate.EvaluateNewReserve(
                snapshot,
                channel,
                estimatedUnits
            );
            if (gateRefusal != null)
            {
                return gateRefusal.Code switch
                {
                    "channel_hard_stopped" =>
                        new CampaignLifecycleResult.ChannelHardStopped
                        {
                            Channel = gateRefusal.Channel,
                            Remaining = gateRefusal.Remaining,
                            Requested = gateRefusal.Requested,
                        },
                    _ => new CampaignLifecycleResult.InsufficientCredits
                    {
                        Channel = gateRefusal.Channel,
                        Remaining = gateRefusal.Remaining,
                        Requested = gateRefusal.Requested,
                    },
                };
            }

            var dropIds = frozenIds
                .Where(id => !eligibleSet.Contains(id))
                .ToList();
            if (dropIds.Count > 0)
            {
                var dropRows = _context.CampaignFrozenRecipients.Where(row =>
                    row.CampaignId == entity.Id
                    && dropIds.Contains(row.LocationGuestId)
                    && row.AcceptedAtUtc == null
                );
                _context.CampaignFrozenRecipients.RemoveRange(dropRows);
            }

            var reserveResult = await _billingReserve.ReserveAsync(
                new CampaignBillingReserveRequest
                {
                    CampaignId = entity.Id,
                    LocationId = entity.RestaurantLocationId,
                    Channel = channel,
                    Units = estimatedUnits,
                },
                cancellationToken
            );

            if (reserveResult is CampaignBillingReserveResult.Failed failed)
            {
                _context.ChangeTracker.Clear();
                return MapReserveFailure(
                    failed.Message,
                    channel,
                    snapshot,
                    estimatedUnits
                );
            }

            if (reserveResult is not CampaignBillingReserveResult.Ok reserved)
            {
                _context.ChangeTracker.Clear();
                return new CampaignLifecycleResult.ReserveFailed
                {
                    Message = "Billing Reserve returned an unexpected result.",
                };
            }

            entity.BillingReservationRef = reserved.ReservationRef;
            entity.ReservedEstimate = estimatedUnits;
            entity.SettledUnits = 0;
            entity.UpdatedAt = now;

            if (resumeTarget)
            {
                var keepScheduled =
                    string.Equals(
                        entity.ScheduleMode,
                        ScheduleLaterMode,
                        StringComparison.Ordinal
                    )
                    && entity.ScheduledAtUtc.HasValue
                    && entity.ScheduledAtUtc.Value > now;
                entity.Status = keepScheduled
                    ? ScheduledStatus
                    : SendingStatus;
            }
            else
            {
                entity.Status = SendingStatus;
            }

            return await SaveLifecycleAsync(entity, cancellationToken);
        }

        private async Task<CampaignLifecycleResult> RetryWithOpenRefAsync(
            Campaign entity,
            CancellationToken cancellationToken
        )
        {
            if (string.IsNullOrWhiteSpace(entity.Channel))
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "channel is required before retry.",
                };
            }

            if (string.IsNullOrWhiteSpace(entity.AudienceKey))
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = "audienceKey is required before retry.",
                };
            }

            var channel = entity.Channel.Trim().ToLowerInvariant();
            var audienceKey = entity.AudienceKey.Trim();
            var now = _utcNow();

            var frozenIds = await _context.CampaignFrozenRecipients
                .Where(row =>
                    row.CampaignId == entity.Id && row.AcceptedAtUtc == null
                )
                .Select(row => row.LocationGuestId)
                .ToListAsync(cancellationToken);

            if (frozenIds.Count == 0)
            {
                return new CampaignLifecycleResult.ZeroEligible();
            }

            IReadOnlyList<int> eligibleIds;
            try
            {
                eligibleIds =
                    await _eligibility.ListChannelEligibleLocationGuestIdsAsync(
                        entity.RestaurantLocationId,
                        audienceKey,
                        channel,
                        cancellationToken
                    );
            }
            catch (ArgumentException ex)
            {
                return new CampaignLifecycleResult.InvalidStatus
                {
                    Message = ex.Message,
                };
            }

            var eligibleSet = eligibleIds.ToHashSet();
            var remaining = frozenIds
                .Where(id => eligibleSet.Contains(id))
                .Distinct()
                .ToList();
            if (remaining.Count == 0)
            {
                return new CampaignLifecycleResult.ZeroEligible();
            }

            var dropIds = frozenIds
                .Where(id => !eligibleSet.Contains(id))
                .ToList();
            if (dropIds.Count > 0)
            {
                var dropRows = _context.CampaignFrozenRecipients.Where(row =>
                    row.CampaignId == entity.Id
                    && dropIds.Contains(row.LocationGuestId)
                    && row.AcceptedAtUtc == null
                );
                _context.CampaignFrozenRecipients.RemoveRange(dropRows);
            }

            entity.Status = SendingStatus;
            entity.UpdatedAt = now;

            return await SaveLifecycleAsync(entity, cancellationToken);
        }

        private async Task<LoadResult> LoadForMutationAsync(
            int campaignId,
            byte[] rowVersion,
            CancellationToken cancellationToken
        )
        {
            var entity = await _context.Campaigns.FirstOrDefaultAsync(
                campaign => campaign.Id == campaignId,
                cancellationToken
            );
            if (entity == null)
            {
                return new LoadResult.NotFound();
            }

            if (
                rowVersion.Length == 0
                || !entity.RowVersion.AsSpan().SequenceEqual(rowVersion)
            )
            {
                if (!(rowVersion.Length == 0 && entity.RowVersion.Length == 0))
                {
                    return new LoadResult.Conflict();
                }
            }

            return new LoadResult.Found(entity);
        }

        private async Task<CampaignLifecycleResult?> ReleaseReservationIfHeldAsync(
            Campaign entity,
            CancellationToken cancellationToken
        )
        {
            if (
                string.IsNullOrWhiteSpace(entity.BillingReservationRef)
                || !_billingReserve.IsLive
            )
            {
                entity.BillingReservationRef = null;
                entity.ReservedEstimate = null;
                entity.SettledUnits = 0;
                return null;
            }

            var channel = (entity.Channel ?? string.Empty).Trim().ToLowerInvariant();
            var close = await CampaignBillingClose.CloseHoldAsync(
                entity,
                _billingReserve,
                _context,
                channel,
                settleUnbilled: true,
                cancellationToken
            );
            if (close.SettleFailed)
            {
                return new CampaignLifecycleResult.ReleaseFailed
                {
                    Message = close.Message ?? "Settle failed.",
                };
            }

            if (close.ReleaseFailed)
            {
                return new CampaignLifecycleResult.ReleaseFailed
                {
                    Message = close.Message ?? "Release failed.",
                };
            }

            return null;
        }

        private void ClearFreeze(int campaignId)
        {
            var rows = _context.CampaignFrozenRecipients.Where(row =>
                row.CampaignId == campaignId
            );
            _context.CampaignFrozenRecipients.RemoveRange(rows);
        }

        private static void ClearScheduleHold(Campaign entity)
        {
            entity.ScheduleMode = null;
            entity.ScheduledAtUtc = null;
            entity.ScheduleTimeZone = null;
            entity.BillingReservationRef = null;
            entity.ReservedEstimate = null;
            entity.SettledUnits = 0;
        }

        private static CampaignLifecycleResult MapReserveFailure(
            string code,
            string channel,
            CreditBalanceAccountSnapshot? snapshot,
            int requestedUnits
        )
        {
            if (code == "channel_hard_stopped")
            {
                var remaining = snapshot?.Channels
                    .FirstOrDefault(row => row.Channel == channel)
                    ?.Remaining ?? 0;
                return new CampaignLifecycleResult.ChannelHardStopped
                {
                    Channel = channel,
                    Remaining = remaining,
                    Requested = requestedUnits,
                };
            }

            if (code == "insufficient_credits")
            {
                var remaining = snapshot?.Channels
                    .FirstOrDefault(row => row.Channel == channel)
                    ?.Remaining ?? 0;
                return new CampaignLifecycleResult.InsufficientCredits
                {
                    Channel = channel,
                    Remaining = remaining,
                    Requested = requestedUnits,
                };
            }

            return new CampaignLifecycleResult.ReserveFailed
            {
                Message = code,
            };
        }

        private async Task<CampaignLifecycleResult> SaveLifecycleAsync(
            Campaign entity,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new CampaignLifecycleResult.Conflict();
            }

            var frozenCount = await _context.CampaignFrozenRecipients.CountAsync(
                row => row.CampaignId == entity.Id,
                cancellationToken
            );

            return new CampaignLifecycleResult.Ok
            {
                Campaign = ToDto(entity, frozenCount),
            };
        }

        private async Task<string?> EvaluateOperatorBillingLockAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var account = await _context.BillingAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (account == null)
            {
                return null;
            }

            return OperatorBillingLockEvaluator.EvaluateSendOrReserveDeny(
                OperatorBillingLockEvaluator.FromBillingAccount(account),
                _utcNow()
            );
        }

        private static CampaignLifecycleDto ToDto(Campaign entity, int frozenCount)
        {
            return new CampaignLifecycleDto
            {
                Id = entity.Id,
                LocationId = entity.RestaurantLocationId,
                Status = entity.Status,
                Name = entity.Name,
                ScheduleMode = entity.ScheduleMode,
                ScheduledAtUtc = entity.ScheduledAtUtc,
                ScheduleTimeZone = entity.ScheduleTimeZone,
                BillingReservationRef = entity.BillingReservationRef,
                ReservedEstimate = entity.ReservedEstimate,
                FrozenRecipientCount = frozenCount,
                RowVersion = entity.RowVersion,
                UpdatedAt = entity.UpdatedAt,
            };
        }

        private static CampaignLifecycleResult ToLifecycleResult(LoadResult loaded)
        {
            return loaded switch
            {
                LoadResult.NotFound => new CampaignLifecycleResult.NotFound(),
                LoadResult.Conflict => new CampaignLifecycleResult.Conflict(),
                _ => throw new InvalidOperationException(
                    "Unexpected load result."
                ),
            };
        }

        private abstract class LoadResult
        {
            private LoadResult()
            {
            }

            public sealed class Found : LoadResult
            {
                public Found(Campaign entity)
                {
                    Entity = entity;
                }

                public Campaign Entity { get; }
            }

            public sealed class NotFound : LoadResult
            {
            }

            public sealed class Conflict : LoadResult
            {
            }
        }
    }
}
