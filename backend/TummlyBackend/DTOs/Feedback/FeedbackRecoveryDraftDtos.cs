namespace TummlyBackend.DTOs.Feedback
{
    public sealed class PrepareFeedbackRecoveryDraftRequest
    {
        public string Channel { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;

        public string Tone { get; set; } = string.Empty;

        public string? IncludeNotes { get; set; }

        /// <summary>prepare | rewrite</summary>
        public string Mode { get; set; } = "prepare";

        public string? CurrentBody { get; set; }

        public string? CurrentSubject { get; set; }

        /// <summary>
        /// Optional confirmed internal-action category (Respond and record).
        /// </summary>
        public string? ConfirmedInternalActionCategory { get; set; }

        /// <summary>
        /// Optional confirmed internal-action note (Respond and record).
        /// </summary>
        public string? ConfirmedInternalActionNote { get; set; }
    }

    public sealed class PrepareFeedbackRecoveryDraftResultDto
    {
        public bool Success { get; init; }

        public string? Body { get; init; }

        public string? Subject { get; init; }

        public string? Channel { get; init; }

        public bool Retryable { get; init; }

        public string? Message { get; init; }
    }
}
