namespace TummlyBackend.Helpers
{
    /// <summary>
    /// Bind Recovery path intent, purpose, and tone from the ask.
    /// Defaults: Respond to the guest, apologise_and_confirm_follow_up,
    /// warm_and_apologetic, empty include-notes.
    /// </summary>
    public static class AssistantRecoveryIntent
    {
        public const string DefaultPurpose = "apologise_and_confirm_follow_up";
        public const string DefaultTone = "warm_and_apologetic";
        public const string OfferPurpose = "include_a_recovery_offer";

        private static readonly (string Id, string[] Needles)[] Purposes =
        [
            ("acknowledge_feedback", ["acknowledge"]),
            (
                "apologise_and_confirm_follow_up",
                ["say sorry", "apologize", "apologise", "sorry"]
            ),
            ("ask_for_more_information", ["more information", "ask for more"]),
            ("confirm_operational_action", ["confirm an operational", "operational action"]),
            ("create_custom_response", ["custom response"]),
        ];

        private static readonly (string Id, string[] Needles)[] Tones =
        [
            ("warm_and_apologetic", ["warm and apologetic", "warm"]),
            ("direct_and_practical", ["direct and practical", "direct"]),
            ("appreciative", ["appreciative"]),
            ("use_restaurant_tone", ["restaurant tone"]),
        ];

        private static readonly (string Id, string[] Needles)[] Categories =
        [
            ("team_briefed", ["team briefed"]),
            (
                "order_or_service_process_reviewed",
                ["order or service process reviewed", "service process reviewed"]
            ),
            ("delivery_issue_investigated", ["delivery issue investigated"]),
            ("product_quality_checked", ["product quality checked"]),
            ("cleaning_issue_addressed", ["cleaning issue addressed"]),
            ("staff_follow_up_completed", ["staff follow-up completed"]),
            ("other_action", ["other action"]),
        ];

        public static string Bind(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            if (LooksLikeInternalOnly(lower))
            {
                return AssistantRecoveryEligibility.IntentInternalOnly;
            }

            if (LooksLikeRespondAndRecord(lower))
            {
                return AssistantRecoveryEligibility.IntentRespondAndRecord;
            }

            if (LooksLikeRecoveryOffer(lower))
            {
                return AssistantRecoveryEligibility.IntentRecoveryOffer;
            }

            return AssistantRecoveryEligibility.IntentRespondToGuest;
        }

        public static string BindPurpose(string userMessage, string intent)
        {
            if (intent == AssistantRecoveryEligibility.IntentRecoveryOffer)
            {
                return OfferPurpose;
            }

            var lower = userMessage.Trim().ToLowerInvariant();
            foreach (var (id, needles) in Purposes)
            {
                if (ContainsAny(lower, needles))
                {
                    return id;
                }
            }

            return DefaultPurpose;
        }

        public static string BindTone(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            foreach (var (id, needles) in Tones)
            {
                if (ContainsAny(lower, needles))
                {
                    return id;
                }
            }

            return DefaultTone;
        }

        public static bool LooksLikeInternalOnly(string lower)
            => ContainsAny(
                lower,
                "internal only",
                "record an internal action only",
                "do not contact",
                "don't contact",
                "no guest reply",
                "just record"
            );

        public static bool LooksLikeRecoveryOffer(string lower)
            => ContainsAny(
                lower,
                "recovery offer",
                "send an offer",
                "give them an offer",
                "offer as recovery",
                "compensate them",
                "voucher"
            );

        public static bool LooksLikeRespondAndRecord(string lower)
            => ContainsAny(
                lower,
                "respond and record",
                "reply and record",
                "respond and log",
                "reply and log"
            );

        public static (string? Category, string? Note) BindInternalFields(string userMessage)
        {
            var lower = userMessage.Trim().ToLowerInvariant();
            string? category = null;
            foreach (var (id, needles) in Categories)
            {
                if (ContainsAny(lower, needles))
                {
                    category = id;
                    break;
                }
            }

            var note = BindNote(userMessage);
            return (category, note);
        }

        private static string? BindNote(string userMessage)
        {
            var marker = "note:";
            var index = userMessage.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return null;
            }

            var note = userMessage[(index + marker.Length)..].Trim();
            return string.IsNullOrWhiteSpace(note) ? null : note;
        }

        private static bool ContainsAny(string lower, params string[] needles)
            => needles.Any(needle => lower.Contains(needle, StringComparison.Ordinal));
    }
}
