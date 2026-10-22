using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class BulkRecoveryBatchService : IBulkRecoveryBatchService
    {
        public const int MaxMembers = AssistantRecoveryWork.MaxBulkFeedbackIds;

        private readonly ApplicationDbContext _context;
        private readonly IFeedbackGuestResponsesService _guestResponses;
        private readonly TimeProvider _clock;

        public BulkRecoveryBatchService(
            ApplicationDbContext context,
            IFeedbackGuestResponsesService guestResponses,
            TimeProvider clock
        )
        {
            _context = context;
            _guestResponses = guestResponses;
            _clock = clock;
        }

        public async Task<BulkRecoveryBatchDto?> CreateDraftAsync(
            int restaurantId,
            int locationId,
            int createdByUserId,
            IReadOnlyList<int> feedbackIds,
            FeedbackGuestResponseChannel channel,
            FeedbackRecoveryIntent intent,
            string? subject,
            string bodyTemplate,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        )
        {
            if (intent != FeedbackRecoveryIntent.RespondToGuest)
            {
                throw new ArgumentException(
                    "Intent must be respond_to_guest."
                );
            }

            if (string.IsNullOrWhiteSpace(bodyTemplate))
            {
                throw new ArgumentException("Body template is required.");
            }

            var distinctIds = feedbackIds
                .Where(id => id > 0)
                .Distinct()
                .Take(MaxMembers)
                .ToList();
            if (distinctIds.Count < 2)
            {
                throw new ArgumentException(
                    "Bulk recovery requires at least two Feedback ids."
                );
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    l => l.Id == locationId && l.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (location is null)
            {
                return null;
            }

            var feedbacks = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.LocationGuest)
                .Where(f =>
                    distinctIds.Contains(f.Id)
                    && f.RestaurantLocationId == locationId
                )
                .ToListAsync(cancellationToken);

            var eligible = new List<Feedback>();
            foreach (var feedback in feedbacks.OrderByDescending(f => f.CreatedAt)
                         .ThenByDescending(f => f.Id))
            {
                var outcome = AssistantRecoveryEligibility.Evaluate(
                    feedback,
                    AssistantRecoveryEligibility.IntentRespondToGuest
                );
                if (outcome is not AssistantRecoveryEligibility.Outcome.Allowed allowed)
                {
                    continue;
                }

                var wireChannel = allowed.Channel ?? "";
                if (!string.Equals(
                        wireChannel,
                        FeedbackGuestResponseMapping.ToWireChannel(channel),
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }

                eligible.Add(feedback);
            }

            if (eligible.Count < 2)
            {
                throw new InvalidOperationException(
                    "Fewer than two Feedback rows are eligible for this channel."
                );
            }

            var template = BulkRecoveryPersonalise.EnsureGuestFirstNameToken(
                bodyTemplate.Trim()
            );
            var now = _clock.GetUtcNow().UtcDateTime;
            var batch = new BulkRecoveryBatch
            {
                RestaurantId = restaurantId,
                RestaurantLocationId = locationId,
                CreatedByUserId = createdByUserId,
                Channel = channel,
                Intent = intent,
                Subject = string.IsNullOrWhiteSpace(subject)
                    ? null
                    : subject.Trim(),
                BodyTemplate = template,
                Purpose = string.IsNullOrWhiteSpace(purpose)
                    ? null
                    : purpose.Trim(),
                Tone = string.IsNullOrWhiteSpace(tone) ? null : tone.Trim(),
                IncludeNotes = string.IsNullOrWhiteSpace(includeNotes)
                    ? null
                    : includeNotes.Trim(),
                Status = BulkRecoveryBatchStatus.Draft,
                CreatedAt = now,
            };

            foreach (var feedback in eligible)
            {
                batch.Members.Add(
                    new BulkRecoveryBatchMember
                    {
                        FeedbackId = feedback.Id,
                        Status = BulkRecoveryBatchMemberStatus.Pending,
                    }
                );
            }

            _context.BulkRecoveryBatches.Add(batch);
            await _context.SaveChangesAsync(cancellationToken);

            return await GetAsync(batch.Id, cancellationToken);
        }

        public async Task<BulkRecoveryBatchDto?> GetAsync(
            int batchId,
            CancellationToken cancellationToken = default
        )
        {
            var batch = await _context.BulkRecoveryBatches
                .AsNoTracking()
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
            if (batch is null)
            {
                return null;
            }

            var feedbackIds = batch.Members.Select(m => m.FeedbackId).ToList();
            var feedbacks = await _context.Feedbacks
                .AsNoTracking()
                .Where(f => feedbackIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            var members = batch.Members
                .OrderBy(m => m.Id)
                .Select(m =>
                {
                    if (!feedbacks.TryGetValue(m.FeedbackId, out var feedback))
                    {
                        return new BulkRecoveryBatchMemberDto
                        {
                            Id = m.Id,
                            FeedbackId = m.FeedbackId,
                            GuestName = string.Empty,
                            MaskedDestination = "••••",
                            Status = BulkRecoveryBatchMapping.ToWireMemberStatus(
                                m.Status
                            ),
                            FeedbackGuestResponseId = m.FeedbackGuestResponseId,
                            FailureReason = m.FailureReason,
                        };
                    }

                    return BulkRecoveryBatchMapping.ToMemberDto(m, feedback);
                })
                .ToList();

            return BulkRecoveryBatchMapping.ToDto(batch, members);
        }

        public async Task<BulkRecoveryBatchDto?> UpdateDraftAsync(
            int batchId,
            string? subject,
            string? bodyTemplate,
            string? purpose,
            string? tone,
            string? includeNotes,
            CancellationToken cancellationToken = default
        )
        {
            var batch = await _context.BulkRecoveryBatches
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
            if (batch is null)
            {
                return null;
            }

            if (batch.Status != BulkRecoveryBatchStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Only draft batches can be updated."
                );
            }

            if (subject is not null)
            {
                batch.Subject = string.IsNullOrWhiteSpace(subject)
                    ? null
                    : subject.Trim();
            }

            if (bodyTemplate is not null)
            {
                if (string.IsNullOrWhiteSpace(bodyTemplate))
                {
                    throw new ArgumentException("Body template is required.");
                }

                batch.BodyTemplate =
                    BulkRecoveryPersonalise.EnsureGuestFirstNameToken(
                        bodyTemplate.Trim()
                    );
            }

            if (purpose is not null)
            {
                batch.Purpose = string.IsNullOrWhiteSpace(purpose)
                    ? null
                    : purpose.Trim();
            }

            if (tone is not null)
            {
                batch.Tone = string.IsNullOrWhiteSpace(tone)
                    ? null
                    : tone.Trim();
            }

            if (includeNotes is not null)
            {
                batch.IncludeNotes = string.IsNullOrWhiteSpace(includeNotes)
                    ? null
                    : includeNotes.Trim();
            }

            await _context.SaveChangesAsync(cancellationToken);
            return await GetAsync(batchId, cancellationToken);
        }

        public async Task<SendBulkRecoveryBatchResultDto?> SendAsync(
            int batchId,
            int authorUserId,
            CancellationToken cancellationToken = default
        )
        {
            var batch = await _context.BulkRecoveryBatches
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == batchId, cancellationToken);
            if (batch is null)
            {
                return null;
            }

            if (batch.Status != BulkRecoveryBatchStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Only draft batches can be sent."
                );
            }

            if (batch.Intent != FeedbackRecoveryIntent.RespondToGuest)
            {
                throw new InvalidOperationException(
                    "Bulk send is available for guest message recoveries."
                );
            }

            var now = _clock.GetUtcNow().UtcDateTime;
            var sent = 0;
            var skipped = 0;
            var failed = 0;
            var total = batch.Members.Count;

            foreach (var member in batch.Members.OrderBy(m => m.Id))
            {
                if (member.Status != BulkRecoveryBatchMemberStatus.Pending)
                {
                    continue;
                }

                Feedback? feedback;
                try
                {
                    feedback = await _context.Feedbacks
                        .Include(f => f.LocationGuest)
                        .FirstOrDefaultAsync(
                            f => f.Id == member.FeedbackId,
                            cancellationToken
                        );
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    MarkMember(
                        member,
                        BulkRecoveryBatchMemberStatus.Failed,
                        "Could not load Feedback.",
                        now
                    );
                    failed += 1;
                    continue;
                }

                if (feedback is null)
                {
                    MarkMember(
                        member,
                        BulkRecoveryBatchMemberStatus.Skipped,
                        "Not eligible.",
                        now
                    );
                    skipped += 1;
                    continue;
                }

                if (feedback.LocationGuest is null
                    && feedback.LocationGuestId is int locationGuestId)
                {
                    feedback.LocationGuest = await _context.LocationGuests
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            guest => guest.Id == locationGuestId,
                            cancellationToken
                        );
                }

                if (AssistantRecoveryEligibility.Evaluate(
                        feedback,
                        AssistantRecoveryEligibility.IntentRespondToGuest
                    ) is not AssistantRecoveryEligibility.Outcome.Allowed)
                {
                    MarkMember(
                        member,
                        BulkRecoveryBatchMemberStatus.Skipped,
                        "Not eligible.",
                        now
                    );
                    skipped += 1;
                    continue;
                }

                var body = BulkRecoveryPersonalise.SubstituteGuestFirstName(
                    batch.BodyTemplate,
                    feedback.GuestName
                );
                var subject = batch.Subject is null
                    ? null
                    : BulkRecoveryPersonalise.SubstituteGuestFirstName(
                        batch.Subject,
                        feedback.GuestName
                    );

                try
                {
                    var result = await _guestResponses.SendAsync(
                        member.FeedbackId,
                        authorUserId,
                        batch.Channel,
                        batch.Intent,
                        subject,
                        body,
                        batch.Purpose,
                        batch.Tone,
                        batch.IncludeNotes,
                        idempotencyKey: $"bulk-recovery-{batch.Id}-{member.FeedbackId}",
                        cancellationToken: cancellationToken
                    );
                    if (result?.GuestResponse is null)
                    {
                        MarkMember(
                            member,
                            BulkRecoveryBatchMemberStatus.Skipped,
                            "Send returned no response.",
                            now
                        );
                        skipped += 1;
                        continue;
                    }

                    member.Status = BulkRecoveryBatchMemberStatus.Sent;
                    member.FeedbackGuestResponseId = result.GuestResponse.Id;
                    member.FailureReason = null;
                    member.ProcessedAt = now;
                    sent += 1;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    MarkMember(
                        member,
                        BulkRecoveryBatchMemberStatus.Failed,
                        TruncateReason(ex.Message),
                        now
                    );
                    failed += 1;
                }
            }

            batch.CompletedAt = now;
            if (sent == 0)
            {
                batch.Status = BulkRecoveryBatchStatus.Failed;
            }
            else if (sent == total)
            {
                batch.Status = BulkRecoveryBatchStatus.Completed;
            }
            else
            {
                batch.Status = BulkRecoveryBatchStatus.Partial;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new SendBulkRecoveryBatchResultDto
            {
                BatchId = batch.Id,
                Status = BulkRecoveryBatchMapping.ToWireStatus(batch.Status),
                Sent = sent,
                Skipped = skipped,
                Failed = failed,
                Total = total,
            };
        }

        private static void MarkMember(
            BulkRecoveryBatchMember member,
            BulkRecoveryBatchMemberStatus status,
            string? reason,
            DateTime processedAt
        )
        {
            member.Status = status;
            member.FailureReason = reason;
            member.ProcessedAt = processedAt;
        }

        private static string TruncateReason(string message)
        {
            var trimmed = message.Trim();
            return trimmed.Length <= 500 ? trimmed : trimmed[..500];
        }
    }
}
