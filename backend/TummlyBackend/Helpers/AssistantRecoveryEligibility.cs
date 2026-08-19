using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Deterministic follow-up eligibility for Recovery path.
    /// Guest-message recovery needs a reachable Email or SMS. Location Guest
    /// marketing preference does not grant or deny that response. It still
    /// gates Respond with a recovery offer.
    /// </summary>
    public static class AssistantRecoveryEligibility
    {
        public const string IntentRespondToGuest = "respond-to-guest";
        public const string IntentRespondAndRecord = "respond-and-record-internal-action";
        public const string IntentInternalOnly = "record-internal-action-only";
        public const string IntentRecoveryOffer = "respond-with-recovery-offer";

        public abstract record Outcome
        {
            public sealed record Allowed(
                string Channel,
                AssistantRecoveryEligibilitySnapshot Snapshot
            ) : Outcome;

            public sealed record Blocked(string Kind, string Body) : Outcome;
        }

        public static Outcome Evaluate(
            Feedback? feedback,
            string intent
        )
        {
            if (feedback is null)
            {
                return new Outcome.Blocked(
                    "unavailable",
                    AssistantRecoveryPersistCopy.UnavailableBody()
                );
            }

            if (feedback.WorkflowStatus == FeedbackWorkflowStatus.Resolved)
            {
                return new Outcome.Blocked(
                    "resolved",
                    AssistantRecoveryPersistCopy.ResolvedBody()
                );
            }

            var contactType = feedback.ContactType;
            var hasContact =
                !string.IsNullOrWhiteSpace(feedback.GuestContact)
                && contactType is ContactType.Email or ContactType.Phone;
            var channel = ChannelFromContact(contactType);
            var marketing = feedback.LocationGuest?.MarketingPreference;
            var snapshot = new AssistantRecoveryEligibilitySnapshot
            {
                WorkflowStatus = WorkflowWire(feedback.WorkflowStatus),
                ContactType = ContactWire(contactType),
                HasContact = hasContact,
                MarketingPreference = marketing?.ToWireString(),
                Channel = channel ?? "",
            };

            if (intent == IntentInternalOnly)
            {
                return new Outcome.Allowed("", snapshot);
            }

            if (!hasContact || channel is null)
            {
                return new Outcome.Blocked(
                    "no-contact",
                    AssistantRecoveryPersistCopy.NoContactBody()
                );
            }

            if (intent == IntentRecoveryOffer
                && marketing != LocationGuestMarketingPreference.Allowed)
            {
                return new Outcome.Blocked(
                    "offer-refused",
                    AssistantRecoveryPersistCopy.OfferRefusedBody()
                );
            }

            return new Outcome.Allowed(channel, snapshot);
        }

        public static string? ChannelFromContact(ContactType contactType)
            => contactType switch
            {
                ContactType.Email => "email",
                ContactType.Phone => "sms",
                _ => null,
            };

        private static string WorkflowWire(FeedbackWorkflowStatus status)
            => status switch
            {
                FeedbackWorkflowStatus.InProgress => "in_progress",
                FeedbackWorkflowStatus.Resolved => "resolved",
                _ => "new",
            };

        private static string ContactWire(ContactType contactType)
            => contactType switch
            {
                ContactType.Email => "Email",
                ContactType.Phone => "Phone",
                _ => "Unknown",
            };
    }
}
