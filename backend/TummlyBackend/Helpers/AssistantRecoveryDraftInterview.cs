using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Models;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantRecoveryDraftState
    {
        public string Target { get; set; } = "recovery";
        public int? FeedbackId { get; set; }
        public string? FeedbackLabel { get; set; }
        public string? Intent { get; set; }
        public string? IntentLabel { get; set; }
        public string? Channel { get; set; }
        public string? Purpose { get; set; }
        public string? PurposeLabel { get; set; }
        public string? Tone { get; set; }
        public string? ToneLabel { get; set; }
        public string? IncludeNotes { get; set; }
        public bool IncludeNotesAsked { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? Category { get; set; }
        public string? CategoryLabel { get; set; }
        public string? Note { get; set; }
        public int? OfferId { get; set; }
        public string? OfferLabel { get; set; }
        public bool UsefulOptionalsSkipped { get; set; }
        public string? Blocker { get; set; }
    }

    public sealed record AssistantRecoveryDraftTurn(
        AssistantRecoveryDraftState State,
        string Title,
        string Body,
        bool IsReady
    );

    public static partial class AssistantRecoveryDraftInterview
    {
        private static readonly (string Id, string Label)[] Intents =
        [
            ("respond-to-guest", "Respond to the guest"),
            ("respond-and-record-internal-action", "Respond and record an internal action"),
            ("record-internal-action-only", "Record an internal action only"),
            ("respond-with-recovery-offer", "Respond with a recovery offer"),
        ];

        private static readonly (string Id, string Label)[] Purposes =
        [
            ("acknowledge_feedback", "Acknowledge the feedback"),
            ("apologise_and_confirm_follow_up", "Apologise and confirm follow-up"),
            ("ask_for_more_information", "Ask for more information"),
            ("confirm_operational_action", "Confirm an operational action"),
            ("create_custom_response", "Create a custom response"),
        ];

        private static readonly (string Id, string Label)[] Tones =
        [
            ("warm_and_apologetic", "Warm and apologetic"),
            ("direct_and_practical", "Direct and practical"),
            ("appreciative", "Appreciative"),
            ("use_restaurant_tone", "Use restaurant tone"),
        ];

        private static readonly (string Id, string Label)[] Categories =
        [
            ("team_briefed", "Team briefed"),
            ("order_or_service_process_reviewed", "Order or service process reviewed"),
            ("delivery_issue_investigated", "Delivery issue investigated"),
            ("product_quality_checked", "Product quality checked"),
            ("cleaning_issue_addressed", "Cleaning issue addressed"),
            ("staff_follow_up_completed", "Staff follow-up completed"),
            ("other_action", "Other action"),
        ];

        public static bool IsRecoveryDraftAsk(string message)
        {
            var lower = message.Trim().ToLowerInvariant();
            var recoveryish = lower.Contains("recovery", StringComparison.Ordinal)
                || lower.Contains("respond to the guest", StringComparison.Ordinal)
                || lower.Contains("respond and record", StringComparison.Ordinal)
                || lower.Contains("internal action", StringComparison.Ordinal)
                || lower.Contains("recovery offer", StringComparison.Ordinal);
            var draftish = lower.Contains("draft", StringComparison.Ordinal)
                || lower.Contains("review", StringComparison.Ordinal)
                || lower.Contains("prepare", StringComparison.Ordinal);
            return recoveryish && draftish;
        }

        public static AssistantRecoveryDraftState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantRecoveryDraftState>(json);
                if (state is null)
                {
                    return null;
                }

                if (!string.Equals(state.Target, "recovery", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return state;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(AssistantRecoveryDraftState state)
            => JsonSerializer.Serialize(state);

        public static AssistantRecoveryDraftTurn Apply(
            AssistantRecoveryDraftState? current,
            string message,
            AssistantFeedbackEvidence feedback,
            AssistantOffersEvidence offers
        )
        {
            var state = current ?? new AssistantRecoveryDraftState();
            var text = message.Trim();
            var lower = text.ToLowerInvariant();

            if (lower.Contains("draft it now", StringComparison.Ordinal)
                || lower.Contains("skip the rest", StringComparison.Ordinal))
            {
                state.UsefulOptionalsSkipped = true;
            }

            ApplyNamedOption(text, Intents, (id, label) =>
            {
                state.Intent = id;
                state.IntentLabel = label;
            });
            ApplyNamedOption(text, Purposes, (id, label) =>
            {
                state.Purpose = id;
                state.PurposeLabel = label;
            });
            ApplyNamedOption(text, Tones, (id, label) =>
            {
                state.Tone = id;
                state.ToneLabel = label;
            });
            ApplyNamedOption(text, Categories, (id, label) =>
            {
                state.Category = id;
                state.CategoryLabel = label;
            });

            if (state.FeedbackId is null)
            {
                ResolveFeedback(state, text, feedback);
            }

            var notesMatch = IncludeNotesRegex().Match(text);
            if (notesMatch.Success)
            {
                state.IncludeNotes = notesMatch.Groups["notes"].Value.Trim();
                state.IncludeNotesAsked = true;
            }
            else if (lower.Contains("no notes", StringComparison.Ordinal)
                || lower.Contains("skip notes", StringComparison.Ordinal)
                || lower.Contains("without notes", StringComparison.Ordinal))
            {
                state.IncludeNotes = "";
                state.IncludeNotesAsked = true;
            }

            var subjectMatch = SubjectRegex().Match(text);
            if (subjectMatch.Success)
            {
                state.Subject = subjectMatch.Groups["subject"].Value.Trim();
            }

            var messageMatch = MessageRegex().Match(text);
            if (messageMatch.Success)
            {
                state.Message = messageMatch.Groups["message"].Value.Trim();
            }

            var noteMatch = NoteRegex().Match(text);
            if (noteMatch.Success)
            {
                state.Note = noteMatch.Groups["note"].Value.Trim();
            }

            if (NeedsOffer(state.Intent))
            {
                ResolveOffer(state, text, offers);
            }

            ApplySilentFills(state, feedback);
            state.Blocker = ResolveBlocker(state, feedback);

            if (state.FeedbackId is null)
            {
                return AskFeedback(state, feedback);
            }

            if (state.Intent is null)
            {
                return new AssistantRecoveryDraftTurn(
                    state,
                    "Recovery draft details",
                    "Which recovery intent should we use? "
                        + string.Join(", ", Intents.Select(item => item.Label))
                        + ".",
                    false
                );
            }

            if (state.Blocker is not null)
            {
                return new AssistantRecoveryDraftTurn(
                    state,
                    "Recovery draft blocked",
                    state.Blocker,
                    false
                );
            }

            if (NeedsGuestFields(state.Intent))
            {
                if (state.Purpose is null && state.Intent != "respond-with-recovery-offer")
                {
                    return new AssistantRecoveryDraftTurn(
                        state,
                        "Recovery draft details",
                        "What is the purpose of this guest response? "
                            + string.Join(", ", Purposes.Select(item => item.Label))
                            + ".",
                        false
                    );
                }

                if (state.Tone is null)
                {
                    return new AssistantRecoveryDraftTurn(
                        state,
                        "Recovery draft details",
                        "Which tone should we use? "
                            + string.Join(", ", Tones.Select(item => item.Label))
                            + ".",
                        false
                    );
                }
            }

            if (NeedsInternalFields(state.Intent))
            {
                if (state.Category is null)
                {
                    return new AssistantRecoveryDraftTurn(
                        state,
                        "Recovery draft details",
                        "Which internal action category applies? "
                            + string.Join(", ", Categories.Select(item => item.Label))
                            + ".",
                        false
                    );
                }

                if (string.IsNullOrWhiteSpace(state.Note))
                {
                    return new AssistantRecoveryDraftTurn(
                        state,
                        "Recovery draft details",
                        "Please add an internal action note (this stays internal).",
                        false
                    );
                }
            }

            if (NeedsOffer(state.Intent) && state.OfferId is null)
            {
                var candidates = offers.Catalog
                    .Where(offer => offer.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    .Take(5)
                    .Select(offer => offer.Title)
                    .ToList();
                var candidateCopy = candidates.Count == 0
                    ? "There are no Active offers to attach."
                    : $"Choose one Active offer: {string.Join(", ", candidates)}.";
                return new AssistantRecoveryDraftTurn(
                    state,
                    "Recovery draft details",
                    $"Which existing offer should this recovery include? {candidateCopy}",
                    false
                );
            }

            if (NeedsGuestFields(state.Intent)
                && !state.UsefulOptionalsSkipped
                && !state.IncludeNotesAsked)
            {
                return new AssistantRecoveryDraftTurn(
                    state,
                    "Recovery draft details",
                    "Any include-notes for the draft (or say “no notes” / “Draft it now”)?",
                    false
                );
            }

            EnsureBarCopy(state);

            return new AssistantRecoveryDraftTurn(
                state,
                "Recovery draft ready",
                BuildSummary(state, offers),
                true
            );
        }

        public static bool IsReady(AssistantRecoveryDraftState state)
        {
            if (state.FeedbackId is null
                || state.Intent is null
                || state.Blocker is not null
                || !AssistantActionCatalog.RecoveryIntents.Contains(state.Intent))
            {
                return false;
            }

            if (NeedsGuestFields(state.Intent))
            {
                if (state.Tone is null
                    || (state.Intent != "respond-with-recovery-offer" && state.Purpose is null)
                    || string.IsNullOrWhiteSpace(state.Message))
                {
                    return false;
                }

                if (state.Channel == "email" && string.IsNullOrWhiteSpace(state.Subject))
                {
                    return false;
                }
            }

            if (NeedsInternalFields(state.Intent)
                && (state.Category is null || string.IsNullOrWhiteSpace(state.Note)))
            {
                return false;
            }

            if (NeedsOffer(state.Intent) && state.OfferId is null)
            {
                return false;
            }

            return true;
        }

        public static AssistantRecoveryDraftPayloadDto ToPayload(
            AssistantRecoveryDraftState state
        )
            => new()
            {
                FeedbackId = state.FeedbackId!.Value,
                Intent = state.Intent!,
                Channel = state.Channel,
                Purpose = state.Intent == "respond-with-recovery-offer"
                    ? "include_a_recovery_offer"
                    : state.Purpose,
                Tone = state.Tone,
                IncludeNotes = state.IncludeNotes ?? "",
                Subject = state.Subject,
                Message = state.Message,
                Category = state.Category,
                Note = state.Note,
                OfferId = state.OfferId,
                UseConfirmedActionForGuestResponse =
                    state.Intent == "respond-and-record-internal-action",
            };

        private static void ApplySilentFills(
            AssistantRecoveryDraftState state,
            AssistantFeedbackEvidence feedback
        )
        {
            if (state.FeedbackId is null)
            {
                return;
            }

            var row = feedback.Rows.FirstOrDefault(item => item.Id == state.FeedbackId.Value);
            if (row is null)
            {
                return;
            }

            if (NeedsGuestFields(state.Intent) && state.Channel is null)
            {
                state.Channel = ChannelFromContact(row.ContactType);
            }

            if (state.Intent == "respond-with-recovery-offer")
            {
                state.Purpose = "include_a_recovery_offer";
                state.PurposeLabel = "Include a recovery offer";
            }
        }

        private static string? ResolveBlocker(
            AssistantRecoveryDraftState state,
            AssistantFeedbackEvidence feedback
        )
        {
            if (state.FeedbackId is null)
            {
                return null;
            }

            var row = feedback.Rows.FirstOrDefault(item => item.Id == state.FeedbackId.Value);
            if (row is null)
            {
                return "That Feedback is not in Analysis scope. Choose another Feedback.";
            }

            if (row.WorkflowStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            {
                return "This Feedback is Resolved. Reopen it before starting recovery.";
            }

            if (NeedsGuestFields(state.Intent)
                && ChannelFromContact(row.ContactType) is null)
            {
                return "This Feedback has no contact method for a guest response.";
            }

            return null;
        }

        private static AssistantRecoveryDraftTurn AskFeedback(
            AssistantRecoveryDraftState state,
            AssistantFeedbackEvidence feedback
        )
        {
            var candidates = feedback.Rows
                .Take(5)
                .Select(FormatFeedbackLabel)
                .ToList();
            var more = feedback.Rows.Count > 5
                ? $" and {feedback.Rows.Count - 5} more"
                : "";
            var candidateCopy = candidates.Count == 0
                ? "There is no in-scope Feedback to recover."
                : $"Candidates: {string.Join("; ", candidates)}{more}.";
            return new AssistantRecoveryDraftTurn(
                state,
                "Recovery draft details",
                $"Which Feedback should we recover (guest name plus date)? {candidateCopy}",
                false
            );
        }

        private static void EnsureBarCopy(AssistantRecoveryDraftState state)
        {
            if (!NeedsGuestFields(state.Intent))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(state.Message))
            {
                state.Message =
                    "Thank you for sharing this feedback. We are looking into it and will follow up.";
            }

            if (state.Channel == "email" && string.IsNullOrWhiteSpace(state.Subject))
            {
                state.Subject = "Following up on your recent visit";
            }
        }

        private static string BuildSummary(
            AssistantRecoveryDraftState state,
            AssistantOffersEvidence offers
        )
        {
            var lines = new List<string>
            {
                $"- **Feedback:** {state.FeedbackLabel}",
                $"- **Intent:** {state.IntentLabel}",
            };
            if (NeedsGuestFields(state.Intent))
            {
                if (state.Channel is not null)
                {
                    lines.Add($"- **Channel:** {state.Channel.ToUpperInvariant()}");
                }

                if (state.PurposeLabel is not null)
                {
                    lines.Add($"- **Purpose:** {state.PurposeLabel}");
                }

                if (state.ToneLabel is not null)
                {
                    lines.Add($"- **Tone:** {state.ToneLabel}");
                }

                if (state.IncludeNotesAsked)
                {
                    lines.Add(
                        $"- **Include notes:** {(string.IsNullOrWhiteSpace(state.IncludeNotes) ? "(none)" : state.IncludeNotes)}"
                    );
                }

                if (state.Channel == "email" && state.Subject is not null)
                {
                    lines.Add($"- **Subject:** {state.Subject}");
                }

                if (state.Message is not null)
                {
                    lines.Add($"- **Message:** {state.Message}");
                }
            }

            if (NeedsInternalFields(state.Intent))
            {
                if (state.CategoryLabel is not null)
                {
                    lines.Add($"- **Internal action:** {state.CategoryLabel}");
                }

                if (state.Note is not null)
                {
                    lines.Add($"- **Note:** {state.Note}");
                }
            }

            if (NeedsOffer(state.Intent))
            {
                var offer = offers.Catalog.FirstOrDefault(item => item.Id == state.OfferId);
                lines.Add($"- **Offer:** {offer?.Title ?? state.OfferLabel}");
            }

            return string.Join("\n", lines);
        }

        private static bool NeedsGuestFields(string? intent)
            => intent is "respond-to-guest"
                or "respond-and-record-internal-action"
                or "respond-with-recovery-offer";

        private static bool NeedsInternalFields(string? intent)
            => intent is "respond-and-record-internal-action"
                or "record-internal-action-only";

        private static bool NeedsOffer(string? intent)
            => intent == "respond-with-recovery-offer";

        private static string? ChannelFromContact(string contactType)
        {
            if (contactType.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                return "email";
            }

            if (contactType.Equals("Phone", StringComparison.OrdinalIgnoreCase))
            {
                return "sms";
            }

            return null;
        }

        private static void ResolveFeedback(
            AssistantRecoveryDraftState state,
            string text,
            AssistantFeedbackEvidence feedback
        )
        {
            var matches = feedback.Rows
                .Where(row =>
                {
                    var label = FormatFeedbackLabel(row);
                    return text.Contains(row.GuestName, StringComparison.OrdinalIgnoreCase)
                        || text.Contains(label, StringComparison.OrdinalIgnoreCase)
                        || label.Contains(text, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            if (matches.Count == 1)
            {
                state.FeedbackId = matches[0].Id;
                state.FeedbackLabel = FormatFeedbackLabel(matches[0]);
            }
        }

        private static void ResolveOffer(
            AssistantRecoveryDraftState state,
            string text,
            AssistantOffersEvidence offers
        )
        {
            var matches = offers.Catalog
                .Where(offer =>
                    offer.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
                    && (text.Contains(offer.Title, StringComparison.OrdinalIgnoreCase)
                        || offer.Title.Contains(text, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (matches.Count == 1)
            {
                state.OfferId = matches[0].Id;
                state.OfferLabel = matches[0].Title;
            }
        }

        private static string FormatFeedbackLabel(AssistantFeedbackEvidenceRow row)
        {
            var date = row.CreatedAt.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            return $"{row.GuestName} ({date})";
        }

        private static void ApplyNamedOption(
            string text,
            IEnumerable<(string Id, string Label)> options,
            Action<string, string> apply
        )
        {
            var matches = options
                .Where(option =>
                    text.Contains(option.Label, StringComparison.OrdinalIgnoreCase)
                    || text.Contains(option.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1)
            {
                apply(matches[0].Id, matches[0].Label);
            }
        }

        [GeneratedRegex(
            "(?:include[- ]notes|notes)\\s*:\\s*(?<notes>[^\\n;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex IncludeNotesRegex();

        [GeneratedRegex(
            "(?:subject)\\s*:\\s*(?<subject>[^\\n;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex SubjectRegex();

        [GeneratedRegex(
            "(?:message|body)\\s*:\\s*(?<message>[^\\n]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex MessageRegex();

        [GeneratedRegex(
            "(?:internal note|note)\\s*:\\s*(?<note>[^\\n;]+)",
            RegexOptions.IgnoreCase
        )]
        private static partial Regex NoteRegex();
    }
}
