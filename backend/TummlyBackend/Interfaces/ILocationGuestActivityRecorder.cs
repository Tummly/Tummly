using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Location Guest activity recorder — appends activity events onto the
    /// current DbContext. Caller owns SaveChanges (same unit of work as the
    /// domain write).
    /// </summary>
    public interface ILocationGuestActivityRecorder
    {
        void RecordGuestJoined(LocationGuest locationGuest, DateTime occurredAt);

        void RecordFeedback(
            LocationGuest locationGuest,
            Feedback feedback,
            DateTime occurredAt
        );

        void RecordTagApplied(
            int locationGuestId,
            int guestTagId,
            string tagName,
            DateTime occurredAt
        );

        void RecordTagRemoved(
            int locationGuestId,
            int guestTagId,
            string tagName,
            DateTime occurredAt
        );

        void RecordNoteAdded(
            int locationGuestId,
            string authorDisplayName,
            DateTime occurredAt
        );

        void RecordProfileEdited(
            int locationGuestId,
            IReadOnlyList<string> changedFields,
            DateTime occurredAt
        );

        void RecordClassificationTerminal(
            Feedback feedback,
            DateTime occurredAt
        );
    }
}
