using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class FeedbackCloseOutsService : IFeedbackCloseOutsService
    {
        public const int MaxListLimit = 100;

        private readonly ApplicationDbContext _context;

        public FeedbackCloseOutsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FeedbackCloseOutResultDto?> CloseOutAsync(
            int feedbackId,
            int authorUserId,
            FeedbackCloseOutIntent intent,
            FeedbackCloseOutReason reason,
            string? noteBody,
            CancellationToken cancellationToken = default
        )
        {
            ValidateNoteBody(reason, noteBody);

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

            FeedbackInternalNote? note = null;
            if (reason == FeedbackCloseOutReason.Other)
            {
                note = new FeedbackInternalNote
                {
                    FeedbackId = feedbackId,
                    Body = noteBody!.Trim(),
                    AuthorUserId = authorUserId,
                    AuthorDisplayName = authorDisplayName,
                    CreatedAt = createdAt,
                };
            }

            var closeOut = new FeedbackCloseOut
            {
                FeedbackId = feedbackId,
                Intent = intent,
                Reason = reason,
                WorkflowStatusChange = statusChange,
                InternalNote = note,
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };

            _context.FeedbackCloseOuts.Add(closeOut);
            await _context.SaveChangesAsync(cancellationToken);

            FeedbackInternalNoteItemDto? noteDto = null;
            if (note != null)
            {
                noteDto = new FeedbackInternalNoteItemDto
                {
                    Id = note.Id,
                    Body = note.Body,
                    AuthorDisplayName = note.AuthorDisplayName,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                };
            }

            return new FeedbackCloseOutResultDto
            {
                WorkflowStatus =
                    FeedbackWorkflowStatusMapping.ToWire(
                        feedback.WorkflowStatus
                    ),
                NeedsAttention =
                    FeedbackWorkflowStatusMapping.NeedsAttention(feedback),
                CloseOut = ToItemDto(closeOut, fromStatus),
                Note = noteDto,
            };
        }

        public async Task<IReadOnlyList<FeedbackCloseOutItemDto>> ListForFeedbackAsync(
            int feedbackId,
            CancellationToken cancellationToken = default
        )
        {
            var rows = await _context.FeedbackCloseOuts
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

        private static void ValidateNoteBody(
            FeedbackCloseOutReason reason,
            string? noteBody
        )
        {
            var hasBody = !string.IsNullOrWhiteSpace(noteBody);

            if (reason == FeedbackCloseOutReason.Other)
            {
                if (!hasBody)
                {
                    throw new ArgumentException(
                        "Note body is required when reason is other."
                    );
                }

                var trimmed = noteBody!.Trim();
                if (trimmed.Length > FeedbackInternalNotesService.MaxBodyLength)
                {
                    throw new ArgumentException(
                        $"Note body must be at most {FeedbackInternalNotesService.MaxBodyLength} characters."
                    );
                }

                return;
            }

            if (hasBody)
            {
                throw new ArgumentException(
                    "Note body must be omitted unless reason is other."
                );
            }
        }

        private static FeedbackCloseOutItemDto ToItemDto(
            FeedbackCloseOut closeOut,
            FeedbackWorkflowStatus fromStatus
        )
        {
            return new FeedbackCloseOutItemDto
            {
                Id = closeOut.Id,
                Intent = FeedbackCloseOutMapping.ToWireIntent(closeOut.Intent),
                Reason = FeedbackCloseOutMapping.ToWireReason(closeOut.Reason),
                WorkflowStatusChangeId = closeOut.WorkflowStatusChangeId,
                InternalNoteId = closeOut.InternalNoteId,
                AuthorDisplayName = closeOut.AuthorDisplayName,
                CreatedAt = closeOut.CreatedAt,
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
