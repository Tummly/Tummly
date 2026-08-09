using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
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
        private readonly Func<DateTime> _utcNow;

        public CampaignLifecycleService(
            ApplicationDbContext context,
            ICampaignEligibilityService eligibility,
            ICampaignBillingReserve billingReserve,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _eligibility = eligibility;
            _billingReserve = billingReserve;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
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
            if (loaded is CampaignLifecycleResult early)
            {
                return early;
            }

            var entity = ((Loaded)loaded).Entity;
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
            if (loaded is CampaignLifecycleResult early)
            {
                return early;
            }

            var entity = ((Loaded)loaded).Entity;
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
            if (loaded is CampaignLifecycleResult early)
            {
                return early;
            }

            var entity = ((Loaded)loaded).Entity;
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
            if (loaded is CampaignLifecycleResult early)
            {
                return early;
            }

            var entity = ((Loaded)loaded).Entity;
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
            if (loaded is CampaignLifecycleResult early)
            {
                return early;
            }

            var entity = ((Loaded)loaded).Entity;
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
            if (loaded is CampaignLifecycleResult early)
            {
                return early;
            }

            var source = ((Loaded)loaded).Entity;
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
                Name = source.Name,
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
                    Units = remaining.Count,
                },
                cancellationToken
            );

            if (reserveResult is CampaignBillingReserveResult.Failed failed)
            {
                _context.ChangeTracker.Clear();
                return new CampaignLifecycleResult.ReserveFailed
                {
                    Message = failed.Message,
                };
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
            entity.ReservedEstimate = remaining.Count;
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

        private async Task<object> LoadForMutationAsync(
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
                return new CampaignLifecycleResult.NotFound();
            }

            if (
                rowVersion.Length == 0
                || !entity.RowVersion.AsSpan().SequenceEqual(rowVersion)
            )
            {
                if (!(rowVersion.Length == 0 && entity.RowVersion.Length == 0))
                {
                    return new CampaignLifecycleResult.Conflict();
                }
            }

            return new Loaded(entity);
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
                return null;
            }

            var release = await _billingReserve.ReleaseAsync(
                new CampaignBillingReleaseRequest
                {
                    CampaignId = entity.Id,
                    ReservationRef = entity.BillingReservationRef,
                },
                cancellationToken
            );

            if (release is CampaignBillingReleaseResult.Failed failed)
            {
                return new CampaignLifecycleResult.ReleaseFailed
                {
                    Message = failed.Message,
                };
            }

            entity.BillingReservationRef = null;
            entity.ReservedEstimate = null;
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

        private sealed record Loaded(Campaign Entity);
    }
}
