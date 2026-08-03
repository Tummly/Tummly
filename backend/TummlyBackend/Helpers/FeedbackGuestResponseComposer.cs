using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Shared collaborator for the three guest-response send paths (respond to
    /// guest, respond and record an internal action, respond with a recovery
    /// offer). Centralises channel/contact/body/subject validation and
    /// FeedbackGuestResponse construction so the rules only need updating in
    /// one place.
    /// </summary>
    public static class FeedbackGuestResponseComposer
    {
        public const int MaxBodyLength = 5000;
        public const int MaxSubjectLength = 300;

        public readonly record struct ValidatedContent(string? Subject, string Body);

        /// <summary>
        /// Trims and validates body/subject against the channel's rules.
        /// Email requires a non-empty subject; SMS must omit it.
        /// </summary>
        public static ValidatedContent ValidateContent(
            FeedbackGuestResponseChannel channel,
            string? subject,
            string? body
        )
        {
            var trimmedBody = (body ?? string.Empty).Trim();
            if (trimmedBody.Length == 0)
            {
                throw new ArgumentException("Body is required.");
            }

            if (trimmedBody.Length > MaxBodyLength)
            {
                throw new ArgumentException(
                    $"Body must be at most {MaxBodyLength} characters."
                );
            }

            string? trimmedSubject = null;
            if (channel == FeedbackGuestResponseChannel.Email)
            {
                trimmedSubject = (subject ?? string.Empty).Trim();
                if (trimmedSubject.Length == 0)
                {
                    throw new ArgumentException(
                        "Subject is required for email."
                    );
                }

                if (trimmedSubject.Length > MaxSubjectLength)
                {
                    throw new ArgumentException(
                        $"Subject must be at most {MaxSubjectLength} characters."
                    );
                }
            }
            else if (!string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException(
                    "Subject must be omitted for SMS."
                );
            }

            return new ValidatedContent(trimmedSubject, trimmedBody);
        }

        /// <summary>
        /// Ensures the requested channel matches the Feedback's guest contact.
        /// </summary>
        public static void EnsureChannelMatchesContact(
            Feedback feedback,
            FeedbackGuestResponseChannel channel
        )
        {
            var hasContact = !string.IsNullOrWhiteSpace(feedback.GuestContact);
            if (!hasContact || feedback.ContactType == ContactType.Unknown)
            {
                throw new ArgumentException(
                    "No contact method available for this Feedback."
                );
            }

            if (
                channel == FeedbackGuestResponseChannel.Email
                && feedback.ContactType != ContactType.Email
            )
            {
                throw new ArgumentException(
                    "Email channel requires an Email contact."
                );
            }

            if (
                channel == FeedbackGuestResponseChannel.Sms
                && feedback.ContactType != ContactType.Phone
            )
            {
                throw new ArgumentException(
                    "SMS channel requires a Phone contact."
                );
            }
        }

        /// <summary>
        /// Builds a FeedbackGuestResponse entity from already-validated content.
        /// Callers are expected to have called ValidateContent and
        /// EnsureChannelMatchesContact first.
        /// </summary>
        public static FeedbackGuestResponse Build(
            Feedback feedback,
            FeedbackGuestResponseChannel channel,
            FeedbackRecoveryIntent intent,
            ValidatedContent content,
            string? purpose,
            string? tone,
            string? includeNotes,
            int authorUserId,
            string authorDisplayName,
            DateTime createdAt
        )
        {
            var maskedDestination = FeedbackGuestResponseMapping.MaskDestination(
                feedback.ContactType,
                feedback.GuestContact
            );

            return new FeedbackGuestResponse
            {
                FeedbackId = feedback.Id,
                Channel = channel,
                Intent = intent,
                MaskedDestination = maskedDestination,
                Subject = content.Subject,
                Body = content.Body,
                Purpose = string.IsNullOrWhiteSpace(purpose)
                    ? null
                    : purpose.Trim(),
                Tone = string.IsNullOrWhiteSpace(tone) ? null : tone.Trim(),
                IncludeNotes = string.IsNullOrWhiteSpace(includeNotes)
                    ? null
                    : includeNotes.Trim(),
                AuthorUserId = authorUserId,
                AuthorDisplayName = authorDisplayName,
                CreatedAt = createdAt,
            };
        }
    }
}
