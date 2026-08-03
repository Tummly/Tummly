using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackRecoveryCompletionsService
        : IFeedbackRecoveryCompletionsService
    {
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;

        public FeedbackRecoveryCompletionsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompleteFeedbackRecoveryResultDto?> CompleteAsync(
            int feedbackId,
            int authorUserId,
            FeedbackRecoveryIntent intent,
            CancellationToken cancellationToken = default
        )
        {
            var author = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == authorUserId, cancellationToken);

            if (author == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

            if (feedback == null)
            {
                return null;
            }

            if (feedback.WorkflowStatus == FeedbackWorkflowStatus.Resolved)
            {
                throw new FeedbackAlreadyResolvedException();
            }

            var hasGuestResponse = await _context.FeedbackGuestResponses
                .AsNoTracking()
                .AnyAsync(
                    r => r.FeedbackId == feedbackId,
                    cancellationToken
                );

            var hasInternalAction = await _context.FeedbackInternalActions
                .AsNoTracking()
                .AnyAsync(
                    a => a.FeedbackId == feedbackId,
                    cancellationToken
                );

            if (intent == FeedbackRecoveryIntent.RespondToGuest)
            {
                if (!hasGuestResponse)
                {
                    throw new ArgumentException(
                        "Recovery completion requires a guest response for this Feedback."
                    );
                }
            }
            else if (intent == FeedbackRecoveryIntent.RecordInternalActionOnly)
            {
                if (!hasInternalAction)
                {
                    throw new ArgumentException(
                        "Recovery completion requires an internal action for this Feedback."
                    );
                }
            }
            else if (
                intent == FeedbackRecoveryIntent.RespondAndRecordInternalAction
            )
            {
                if (!hasGuestResponse || !hasInternalAction)
                {
                    throw new ArgumentException(
                        "Recovery completion requires a guest response and an internal action for this Feedback."
                    );
                }
            }
            else if (intent == FeedbackRecoveryIntent.RespondWithRecoveryOffer)
            {
                var hasRecoveryOffer = await _context.FeedbackRecoveryOffers
                    .AsNoTracking()
                    .AnyAsync(
                        o => o.FeedbackId == feedbackId,
                        cancellationToken
                    );
                if (!hasGuestResponse || !hasRecoveryOffer)
                {
                    throw new ArgumentException(
                        "Recovery completion requires a guest response and recovery offer for this Feedback."
                    );
                }
            }
            else
            {
                throw new ArgumentException("Unsupported recovery intent.");
            }

            var fromStatus = feedback.WorkflowStatus;
            var createdAt = DateTime.UtcNow;
            var authorDisplayName = author.FullName;

            feedback.WorkflowStatus = FeedbackWorkflowStatus.Resolved;

            var statusChange = new FeedbackWorkflowStatusChange
            {
                FeedbackId = feedbackId,
                FromStatus = fromStatus,
                ToStatus = FeedbackWorkflowStatus.Resolved,
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };

            var completion = new FeedbackRecoveryCompletion
            {
                FeedbackId = feedbackId,
                Intent = intent,
                WorkflowStatusChange = statusChange,
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };

            _context.FeedbackRecoveryCompletions.Add(completion);
            await _context.SaveChangesAsync(cancellationToken);

            return new CompleteFeedbackRecoveryResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(feedback.WorkflowStatus),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                Completion = ToItemDto(completion, fromStatus),
            };
        }

        public async Task<IReadOnlyList<FeedbackRecoveryCompletionItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackRecoveryCompletions
                .AsNoTracking()
                .Include(c => c.WorkflowStatusChange)
                .Where(c => c.FeedbackId == feedbackId)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Take(MaxListLimit)
                .ToListAsync(cancellationToken);

            return rows
                .Select(c => ToItemDto(
                    c,
                    c.WorkflowStatusChange?.FromStatus
                        ?? FeedbackWorkflowStatus.New
                ))
                .ToList();
        }

        private static FeedbackRecoveryCompletionItemDto ToItemDto(
            FeedbackRecoveryCompletion completion,
            FeedbackWorkflowStatus fromStatus
        )
        {
            return new FeedbackRecoveryCompletionItemDto
            {
                Id = completion.Id,
                Intent = FeedbackInternalActionMapping.ToWireIntent(
                    completion.Intent
                ),
                WorkflowStatusChangeId = completion.WorkflowStatusChangeId,
                AuthorDisplayName = completion.AuthorDisplayName,
                CreatedAt = completion.CreatedAt,
                FromWorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(fromStatus),
                ToWorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(
                        FeedbackWorkflowStatus.Resolved
                    ),
            };
        }
    }
}
