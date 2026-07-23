using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class LocationGuestActivityEmitter : ILocationGuestActivityEmitter
    {
        private readonly ApplicationDbContext _context;

        public LocationGuestActivityEmitter(ApplicationDbContext context)
        {
            _context = context;
        }

        public void EmitGuestJoined(
            LocationGuest locationGuest,
            DateTime occurredAt
        )
        {
            Append(
                kind: LocationGuestActivityKinds.GuestJoined,
                occurredAt: occurredAt,
                locationGuest: locationGuest,
                locationGuestId: locationGuest.Id == 0
                    ? null
                    : locationGuest.Id
            );
        }

        public void EmitFeedback(
            LocationGuest locationGuest,
            Feedback feedback,
            DateTime occurredAt
        )
        {
            Append(
                kind: LocationGuestActivityKinds.Feedback,
                occurredAt: occurredAt,
                locationGuest: locationGuest,
                locationGuestId: locationGuest.Id == 0
                    ? null
                    : locationGuest.Id,
                feedback: feedback,
                feedbackId: feedback.Id == 0 ? null : feedback.Id
            );
        }

        public void EmitTagApplied(
            int locationGuestId,
            int guestTagId,
            string tagName,
            DateTime occurredAt
        )
        {
            Append(
                kind: LocationGuestActivityKinds.TagApplied,
                occurredAt: occurredAt,
                locationGuestId: locationGuestId,
                payload: new LocationGuestActivityPayload
                {
                    GuestTagId = guestTagId,
                    TagName = tagName,
                }
            );
        }

        public void EmitTagRemoved(
            int locationGuestId,
            int guestTagId,
            string tagName,
            DateTime occurredAt
        )
        {
            Append(
                kind: LocationGuestActivityKinds.TagRemoved,
                occurredAt: occurredAt,
                locationGuestId: locationGuestId,
                payload: new LocationGuestActivityPayload
                {
                    GuestTagId = guestTagId,
                    TagName = tagName,
                }
            );
        }

        public void EmitNoteAdded(
            int locationGuestId,
            string authorDisplayName,
            DateTime occurredAt
        )
        {
            Append(
                kind: LocationGuestActivityKinds.NoteAdded,
                occurredAt: occurredAt,
                locationGuestId: locationGuestId,
                payload: new LocationGuestActivityPayload
                {
                    AuthorDisplayName = authorDisplayName,
                }
            );
        }

        public void EmitProfileEdited(
            int locationGuestId,
            IReadOnlyList<string> changedFields,
            DateTime occurredAt
        )
        {
            Append(
                kind: LocationGuestActivityKinds.ProfileEdited,
                occurredAt: occurredAt,
                locationGuestId: locationGuestId,
                payload: new LocationGuestActivityPayload
                {
                    ChangedFields = changedFields,
                }
            );
        }

        public void EmitClassificationTerminal(
            Feedback feedback,
            DateTime occurredAt
        )
        {
            if (
                feedback.ClassificationStatus
                    != ClassificationStatus.Succeeded
                && feedback.ClassificationStatus
                    != ClassificationStatus.Failed
            )
            {
                return;
            }

            var kind =
                feedback.ClassificationStatus
                    == ClassificationStatus.Succeeded
                    ? LocationGuestActivityKinds.ClassificationSucceeded
                    : LocationGuestActivityKinds.ClassificationFailed;

            LocationGuestActivityPayload? payload = null;
            if (
                feedback.ClassificationStatus
                    == ClassificationStatus.Succeeded
                && feedback.Sentiment is { } sentiment
            )
            {
                payload = new LocationGuestActivityPayload
                {
                    Sentiment = FeedbackClassificationMapping.ToWireSentiment(
                        sentiment
                    ),
                };
            }

            Append(
                kind: kind,
                occurredAt: occurredAt,
                locationGuestId: feedback.LocationGuestId,
                feedback: feedback,
                feedbackId: feedback.Id,
                payload: payload
            );
        }

        private void Append(
            string kind,
            DateTime occurredAt,
            int? locationGuestId = null,
            LocationGuest? locationGuest = null,
            int? feedbackId = null,
            Feedback? feedback = null,
            LocationGuestActivityPayload? payload = null
        )
        {
            var row = new LocationGuestActivityEvent
            {
                Kind = kind,
                OccurredAt = GuestsDateWindows.EnsureUtc(occurredAt),
                CreatedAt = DateTime.UtcNow,
                PayloadJson = LocationGuestActivityPayload.Serialize(payload),
            };

            if (locationGuest != null)
            {
                row.LocationGuest = locationGuest;
            }
            else
            {
                row.LocationGuestId = locationGuestId;
            }

            if (feedback != null)
            {
                row.Feedback = feedback;
            }
            else
            {
                row.FeedbackId = feedbackId;
            }

            _context.LocationGuestActivityEvents.Add(row);
        }
    }
}
