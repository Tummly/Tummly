using TummlyBackend.DTOs.Feedback;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Derives Feedback details activity from submission time, notes, and
    /// classification correction facts (not a separate activity store).
    /// </summary>
    public static class FeedbackActivityHistory
    {
        public static IReadOnlyList<FeedbackActivityEventDto> Derive(
            DateTime feedbackCreatedAt,
            IReadOnlyList<FeedbackInternalNoteItemDto> notesNewestFirst,
            IReadOnlyList<FeedbackClassificationCorrectionItemDto>? correctionsNewestFirst = null
        )
        {
            var corrections = correctionsNewestFirst
                ?? Array.Empty<FeedbackClassificationCorrectionItemDto>();

            var events = new List<FeedbackActivityEventDto>(
                1 + notesNewestFirst.Count + corrections.Count
            )
            {
                new FeedbackActivityEventDto
                {
                    Kind = "feedback_received",
                    At = feedbackCreatedAt,
                },
            };

            var noteEvents = notesNewestFirst
                .OrderBy(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Select(note => new FeedbackActivityEventDto
                {
                    Kind = "note_added",
                    At = note.CreatedAt,
                    ActorDisplayName = note.AuthorDisplayName,
                });

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
