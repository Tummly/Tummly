namespace TummlyBackend.DTOs.Guests
{
    public sealed class PatchGuestMarketingPreferenceRequest
    {
        public string? Preference { get; init; }

        public string? Note { get; init; }
    }

    public sealed class PatchGuestMarketingPreferenceResult
    {
        public bool Success { get; init; }

        public string Preference { get; init; }
            = string.Empty;

        public bool PreferenceChanged { get; init; }

        public bool NoteCreated { get; init; }

        public string? NoteError { get; init; }
    }

    public enum GuestMarketingPreferenceUpdateStatus
    {
        Updated,
        NotFound,
        ValidationError,
    }

    public sealed class GuestMarketingPreferenceUpdateOutcome
    {
        public GuestMarketingPreferenceUpdateStatus Status { get; init; }

        public string? ErrorMessage { get; init; }

        public PatchGuestMarketingPreferenceResult? Result { get; init; }

        public static GuestMarketingPreferenceUpdateOutcome Updated(
            PatchGuestMarketingPreferenceResult result
        ) => new()
        {
            Status = GuestMarketingPreferenceUpdateStatus.Updated,
            Result = result,
        };

        public static GuestMarketingPreferenceUpdateOutcome NotFound() => new()
        {
            Status = GuestMarketingPreferenceUpdateStatus.NotFound,
            ErrorMessage = "Guest not found.",
        };

        public static GuestMarketingPreferenceUpdateOutcome ValidationError(
            string message
        ) => new()
        {
            Status = GuestMarketingPreferenceUpdateStatus.ValidationError,
            ErrorMessage = message,
        };
    }
}
