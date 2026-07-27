using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Derives Feedback details activity from submission time, notes (including
    /// soft-deletes), and classification correction facts (not a separate store).
    /// Body edits do not produce history rows.
    /// </summary>
    public static class FeedbackActivityHistory
    {
        public static IReadOnlyList<FeedbackActivityEventDto> Derive(
            DateTime feedbackCreatedAt,
            IReadOnlyList<FeedbackInternalNoteActivityFactDto> noteFacts,
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null
        )
        {
            var corrections = correctionsNewestFirst
                ?? Array.Empty<FeedbackClassificationCorrectionItemDto>();

            var events = new List<FeedbackActivityEventDto>(
                1 + (noteFacts.Count * 2) + corrections.Count
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

            events.AddRange(
                noteEvents
                    .Concat(correctionEvents)
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
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null
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

            return Derive(feedbackCreatedAt, facts, correctionsNewestFirst);
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
    }
}
