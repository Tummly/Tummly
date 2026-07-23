using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Appends Location Guest activity events onto the current DbContext.
    /// Caller owns SaveChanges (same unit of work as the domain write).
    /// </summary>
    public interface ILocationGuestActivityEmitter
    {
        void EmitGuestJoined(LocationGuest locationGuest, DateTime occurredAt);

        void EmitFeedback(
            LocationGuest locationGuest,
            Feedback feedback,
            DateTime occurredAt
        );

        void EmitTagApplied(
            int locationGuestId,
            int guestTagId,
            string tagName,
            DateTime occurredAt
        );

        void EmitTagRemoved(
            int locationGuestId,
            int guestTagId,
            string tagName,
            DateTime occurredAt
        );

        void EmitNoteAdded(
            int locationGuestId,
            string authorDisplayName,
            DateTime occurredAt
        );

        void EmitProfileEdited(
            int locationGuestId,
            IReadOnlyList<string> changedFields,
            DateTime occurredAt
        );

        void EmitClassificationTerminal(
            Feedback feedback,
            DateTime occurredAt
        );
    }
}
