using System.Text.Json;
using TummlyBackend.DTOs.Assistant;

namespace TummlyBackend.Helpers
{
    public sealed class AssistantRecoveryEligibilitySnapshot
    {
        public string WorkflowStatus { get; set; } = string.Empty;

        public string ContactType { get; set; } = string.Empty;

        public bool HasContact { get; set; }

        public string? MarketingPreference { get; set; }

        public string Channel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stored Feedback recovery work on an Assistant conversation.
    /// Distinct from a recovery Draft interview catalogue.
    /// </summary>
    public sealed class AssistantRecoveryWorkState
    {
        public string Target { get; set; } = AssistantRecoveryWork.Target;

        public int FeedbackId { get; set; }

        public int? LocationId { get; set; }

        public string Intent { get; set; } = string.Empty;

        public string? Channel { get; set; }

        public string? Purpose { get; set; }

        public string? Tone { get; set; }

        public string? IncludeNotes { get; set; }

        public string? Subject { get; set; }

        public string? Message { get; set; }

        public string? Category { get; set; }

        public string? Note { get; set; }

        public int? OfferId { get; set; }

        public bool UseConfirmedActionForGuestResponse { get; set; }

        public AssistantRecoveryEligibilitySnapshot? EligibilitySnapshot { get; set; }
    }

    public static class AssistantRecoveryWork
    {
        public const string Target = "recovery-work";

        public static AssistantRecoveryWorkState? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var state = JsonSerializer.Deserialize<AssistantRecoveryWorkState>(json);
                if (state is null
                    || !string.Equals(
                        state.Target,
                        Target,
                        StringComparison.OrdinalIgnoreCase
                    )
                    || state.FeedbackId <= 0
                    || string.IsNullOrWhiteSpace(state.Intent))
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

        public static string Serialize(AssistantRecoveryWorkState state)
            => JsonSerializer.Serialize(state);

        public static AssistantRecoveryDraftPayloadDto ToPayload(
            AssistantRecoveryWorkState state
        )
            => new()
            {
                FeedbackId = state.FeedbackId,
                LocationId = state.LocationId,
                Intent = state.Intent,
                Channel = state.Channel,
                Purpose = state.Purpose,
                Tone = state.Tone,
                IncludeNotes = state.IncludeNotes ?? "",
                Subject = state.Subject,
                Message = state.Message,
                Category = state.Category,
                Note = state.Note,
                OfferId = state.OfferId,
                UseConfirmedActionForGuestResponse =
                    state.UseConfirmedActionForGuestResponse,
            };
    }
}
