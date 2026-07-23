namespace TummlyBackend.DTOs.Guests
{
    public sealed class PatchGuestIdentityRequest
    {
        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }
    }

    public sealed class PatchGuestIdentityResult
    {
        public bool Success { get; init; }

        public IReadOnlyList<string> ChangedFields { get; init; }
            = Array.Empty<string>();
    }

    public enum GuestIdentityUpdateStatus
    {
        Updated,
        NotFound,
        ValidationError,
        IdentityCollision,
    }

    public sealed class GuestIdentityUpdateOutcome
    {
        public GuestIdentityUpdateStatus Status { get; init; }

        public string? ErrorMessage { get; init; }

        public PatchGuestIdentityResult? Result { get; init; }

        public static GuestIdentityUpdateOutcome Updated(
            PatchGuestIdentityResult result
        ) => new()
        {
            Status = GuestIdentityUpdateStatus.Updated,
            Result = result,
        };

        public static GuestIdentityUpdateOutcome NotFound() => new()
        {
            Status = GuestIdentityUpdateStatus.NotFound,
            ErrorMessage = "Guest not found.",
        };

        public static GuestIdentityUpdateOutcome ValidationError(
            string message
        ) => new()
        {
            Status = GuestIdentityUpdateStatus.ValidationError,
            ErrorMessage = message,
        };

        public static GuestIdentityUpdateOutcome IdentityCollision(
            string message
        ) => new()
        {
            Status = GuestIdentityUpdateStatus.IdentityCollision,
            ErrorMessage = message,
        };
    }
}
