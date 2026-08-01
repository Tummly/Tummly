using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Derives Feedback details activity from submission time, notes (including
    /// soft-deletes), classification correction facts, and workflow-status
    /// change facts (not a separate event store).
    /// Body edits and same-to-same status no-ops do not produce history rows.
    /// </summary>
    public static class FeedbackActivityHistory
    {
        public static IReadOnlyList<FeedbackActivityEventDto> Derive(
            DateTime feedbackCreatedAt,
            IReadOnlyList<FeedbackInternalNoteActivityFactDto> noteFacts,
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null,
            IReadOnlyList<FeedbackWorkflowStatusChangeItemDto>? workflowChangesNewestFirst = null
        )
        {
            var corrections = correctionsNewestFirst
                ?? Array.Empty<FeedbackClassificationCorrectionItemDto>();
            var workflowChanges = workflowChangesNewestFirst
                ?? Array.Empty<FeedbackWorkflowStatusChangeItemDto>();

            var events = new List<FeedbackActivityEventDto>(
                1
                    + (noteFacts.Count * 2)
                    + corrections.Count
                    + workflowChanges.Count
            )
            {
                new FeedbackActivityEventDto
                {
                    Kind = "feedback_received",
                    At = feedbackCreatedAt,
                },
            };

            var noteEvents = new List<FeedbackActivityEventDto>(noteFacts.Count * 2);
            foreach (var note in noteFacts)
            {
                noteEvents.Add(
                    new FeedbackActivityEventDto
                    {
                        Kind = "note_added",
                        At = note.CreatedAt,
                        ActorDisplayName = note.AuthorDisplayName,
                    }
                );

                if (note.DeletedAt is { } deletedAt)
                {
                    noteEvents.Add(
                        new FeedbackActivityEventDto
                        {
                            Kind = "note_deleted",
                            At = deletedAt,
                            ActorDisplayName = note.DeletedByDisplayName,
                        }
                    );
                }
            }

            var correctionEvents = corrections
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            var workflowEvents = workflowChanges
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(ToActivityEvent);

            events.AddRange(
                noteEvents
                    .Concat(correctionEvents)
                    .Concat(workflowEvents)
                    .OrderBy(e => e.At)
                    .ThenBy(e => e.Kind)
            );

            return events;
        }

        /// <summary>
        /// Backward-compatible overload for callers that only have visible notes
        /// (no soft-delete facts). Prefer the activity-facts overload.
        /// </summary>
        public static IReadOnlyList<FeedbackActivityEventDto> Derive(
            DateTime feedbackCreatedAt,
            IReadOnlyList<FeedbackInternalNoteItemDto> notesNewestFirst,
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null,
            IReadOnlyList<FeedbackWorkflowStatusChangeItemDto>? workflowChangesNewestFirst = null
        )
        {
            var facts = notesNewestFirst
                .Select(n => new FeedbackInternalNoteActivityFactDto
                {
                    Id = n.Id,
                    AuthorDisplayName = n.AuthorDisplayName,
                    CreatedAt = n.CreatedAt,
                })
                .ToList();

            return Derive(
                feedbackCreatedAt,
                facts,
                correctionsNewestFirst,
                workflowChangesNewestFirst
            );
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackClassificationCorrectionItemDto correction
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "classification_corrected",
                At = correction.CreatedAt,
                ActorDisplayName = correction.AuthorDisplayName,
                FromSentiment = correction.FromSentiment,
                ToSentiment = correction.ToSentiment,
            };
        }

        public static FeedbackActivityEventDto ToActivityEvent(
            FeedbackWorkflowStatusChangeItemDto change
        )
        {
            return new FeedbackActivityEventDto
            {
                Kind = "workflow_status_changed",
                At = change.CreatedAt,
                ActorDisplayName = change.AuthorDisplayName,
                FromWorkflowStatus = change.FromWorkflowStatus,
                ToWorkflowStatus = change.ToWorkflowStatus,
            };
        }
    }
}
