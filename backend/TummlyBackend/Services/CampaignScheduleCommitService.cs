using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Atomic Campaign schedule / send commit — freeze + Billing Reserve + status
    /// (ticket 26). No fake Campaigns reservation when Billing Reserve is not live.
    /// </summary>
    public class CampaignScheduleCommitService : ICampaignScheduleCommitService
    {
        public const string DraftStatus = "draft";
        public const string ScheduledStatus = "scheduled";
        public const string SendingStatus = "sending";

        public const string SendNowMode = "send-now";
        public const string ScheduleLaterMode = "schedule-later";

        private readonly ApplicationDbContext _context;
        private readonly ICampaignEligibilityService _eligibility;
        private readonly ICampaignBillingReserve _billingReserve;
        private readonly ICampaignFireWork _fireWork;
        private readonly Func<DateTime> _utcNow;

        public CampaignScheduleCommitService(
            ApplicationDbContext context,
            ICampaignEligibilityService eligibility,
            ICampaignBillingReserve billingReserve,
            ICampaignFireWork fireWork,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _eligibility = eligibility;
            _billingReserve = billingReserve;
            _fireWork = fireWork;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<CampaignScheduleCommitResult> CommitAsync(
            int campaignId,
            CommitCampaignScheduleRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (!_billingReserve.IsLive)
            {
                return new CampaignScheduleCommitResult.BillingReserveUnavailable();
            }

            var mode = (request.ScheduleMode ?? string.Empty).Trim();
            if (mode is not (SendNowMode or ScheduleLaterMode))
            {
                return new CampaignScheduleCommitResult.InvalidSchedule
                {
                    Message = "scheduleMode must be send-now or schedule-later.",
                };
            }

            var timeZone = (request.ScheduleTimeZone ?? string.Empty).Trim();
            if (timeZone.Length == 0)
            {
                return new CampaignScheduleCommitResult.InvalidSchedule
                {
                    Message = "scheduleTimeZone is required.",
                };
            }

            var now = _utcNow();
            DateTime? scheduledAtUtc = null;
            if (mode == ScheduleLaterMode)
            {
                if (!request.ScheduledAtUtc.HasValue)
                {
                    return new CampaignScheduleCommitResult.InvalidSchedule
                    {
                        Message =
                            "scheduledAtUtc is required when scheduleMode is schedule-later.",
                    };
                }

                scheduledAtUtc = DateTime.SpecifyKind(
                    request.ScheduledAtUtc.Value,
                    DateTimeKind.Utc
                );
                if (scheduledAtUtc.Value <= now)
                {
                    return new CampaignScheduleCommitResult.InvalidSchedule
                    {
                        Message =
                            "scheduledAtUtc must be strictly after the commit instant.",
                    };
                }
            }

            var entity = await _context.Campaigns
                .FirstOrDefaultAsync(
                    campaign => campaign.Id == campaignId,
                    cancellationToken
                );

            if (entity == null)
            {
                return new CampaignScheduleCommitResult.NotFound();
            }

            if (!string.Equals(entity.Status, DraftStatus, StringComparison.Ordinal))
            {
                return new CampaignScheduleCommitResult.NotDraft();
            }

            if (
                request.RowVersion.Length == 0
                || !entity.RowVersion.AsSpan().SequenceEqual(request.RowVersion)
            )
            {
                // In-memory tests may have empty rowversions on both sides.
                if (
                    !(
                        request.RowVersion.Length == 0
                        && entity.RowVersion.Length == 0
                    )
                )
                {
                    return new CampaignScheduleCommitResult.Conflict();
                }
            }

            var reviewReadyError = ValidateReviewReady(entity);
            if (reviewReadyError != null)
            {
                return new CampaignScheduleCommitResult.NotReviewReady
                {
                    Message = reviewReadyError,
                };
            }

            var channel = entity.Channel!.Trim().ToLowerInvariant();
            var audienceKey = entity.AudienceKey!.Trim();

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
                return new CampaignScheduleCommitResult.NotReviewReady
                {
                    Message = ex.Message,
                };
            }

            if (eligibleIds.Count == 0)
            {
                return new CampaignScheduleCommitResult.ZeroEligible();
            }

            await using var transaction =
                _context.Database.IsRelational()
                    ? await _context.Database.BeginTransactionAsync(
                        cancellationToken
                    )
                    : null;

            try
            {
                var existingFreeze = _context.CampaignFrozenRecipients.Where(
                    row => row.CampaignId == entity.Id
                );
                _context.CampaignFrozenRecipients.RemoveRange(existingFreeze);

                foreach (var locationGuestId in eligibleIds)
                {
                    _context.CampaignFrozenRecipients.Add(
                        new CampaignFrozenRecipient
                        {
                            CampaignId = entity.Id,
                            LocationGuestId = locationGuestId,
                            FrozenAtUtc = now,
                        }
                    );
                }

                entity.ScheduleMode = mode;
                entity.ScheduledAtUtc = scheduledAtUtc;
                entity.ScheduleTimeZone = timeZone;
                entity.ReservedEstimate = eligibleIds.Count;
                entity.Status =
                    mode == SendNowMode ? SendingStatus : ScheduledStatus;
                entity.UpdatedAt = now;

                // Reserve after freeze staging so a Reserve failure rolls back
                // the Draft transition (no fake hold without a live ledger release).
                var reserveResult = await _billingReserve.ReserveAsync(
                    new CampaignBillingReserveRequest
                    {
                        CampaignId = entity.Id,
                        LocationId = entity.RestaurantLocationId,
                        Channel = channel,
                        Units = eligibleIds.Count,
                    },
                    cancellationToken
                );

                if (reserveResult is CampaignBillingReserveResult.Failed failed)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    else
                    {
                        _context.ChangeTracker.Clear();
                    }

                    return new CampaignScheduleCommitResult.ReserveFailed
                    {
                        Message = failed.Message,
                    };
                }

                if (reserveResult is not CampaignBillingReserveResult.Ok reserved)
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    else
                    {
                        _context.ChangeTracker.Clear();
                    }

                    return new CampaignScheduleCommitResult.ReserveFailed
                    {
                        Message = "Billing Reserve returned an unexpected result.",
                    };
                }

                entity.BillingReservationRef = reserved.ReservationRef;

                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                return new CampaignScheduleCommitResult.Conflict();
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }

            if (entity.Status == SendingStatus)
            {
                try
                {
                    await _fireWork.NotifyAsync(entity.Id, cancellationToken);
                }
                catch
                {
                    // Wake is best-effort — commit already succeeded.
                }
            }

            return new CampaignScheduleCommitResult.Ok
            {
                Campaign = ToDto(entity, eligibleIds.Count),
            };
        }

        private static string? ValidateReviewReady(Campaign entity)
        {
            if (string.IsNullOrWhiteSpace(entity.GoalId))
            {
                return "goalId is required before commit.";
            }

            if (string.IsNullOrWhiteSpace(entity.AudienceKey))
            {
                return "audienceKey is required before commit.";
            }

            if (string.IsNullOrWhiteSpace(entity.Channel))
            {
                return "channel is required before commit.";
            }

            if (string.IsNullOrWhiteSpace(entity.OfferStance))
            {
                return "offerStance is required before commit.";
            }

            if (string.IsNullOrWhiteSpace(entity.MessageBody))
            {
                return "messageBody is required before commit.";
            }

            var channel = entity.Channel.Trim().ToLowerInvariant();
            if (
                channel == "email"
                && string.IsNullOrWhiteSpace(entity.MessageSubject)
            )
            {
                return "messageSubject is required for email commit.";
            }

            return null;
        }

        private static CampaignScheduleCommitDto ToDto(
            Campaign entity,
            int frozenCount
        )
        {
            return new CampaignScheduleCommitDto
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
    }
}
