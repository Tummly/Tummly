namespace TummlyBackend.DTOs.Capture
{
    /// <summary>
    /// Typed domain outcomes from the QR lifecycle module (ADR-0025).
    /// Controllers map these to today's HTTP status codes and payloads.
    /// </summary>
    public enum QrLifecycleResultKind
    {
        Ok,
        NotFound,
        Validation,
        Conflict,
        InvalidTransition,
        LocationLocked,
    }

    public sealed class QrLifecycleResult
    {
        public QrLifecycleResultKind Kind { get; init; }

        public string? Message { get; init; }

        public string? Field { get; init; }

        public string? Reason { get; init; }

        public object? Payload { get; init; }

        public static QrLifecycleResult Ok(object payload) => new()
        {
            Kind = QrLifecycleResultKind.Ok,
            Payload = payload,
        };

        public static QrLifecycleResult NotFound(
            string message = "QR code not found."
        ) => new()
        {
            Kind = QrLifecycleResultKind.NotFound,
            Message = message,
        };

        public static QrLifecycleResult Validation(
            string message,
            string? field = null
        ) => new()
        {
            Kind = QrLifecycleResultKind.Validation,
            Message = message,
            Field = field,
        };

        public static QrLifecycleResult Conflict(
            string message,
            string? field = null,
            string? reason = null
        ) => new()
        {
            Kind = QrLifecycleResultKind.Conflict,
            Message = message,
            Field = field,
            Reason = reason,
        };

        public static QrLifecycleResult InvalidTransition(string message) =>
            new()
            {
                Kind = QrLifecycleResultKind.InvalidTransition,
                Message = message,
            };

        public static QrLifecycleResult LocationLocked(
            string message =
                "Per-code Pause and Activate are unavailable while location capture is paused."
        ) => new()
        {
            Kind = QrLifecycleResultKind.LocationLocked,
            Message = message,
        };
    }

    public sealed class CreateDigitalGuestLinkCommand
    {
        public required int UserId { get; init; }

        public required int LocationId { get; init; }

        public string? LinkName { get; init; }

        public string? InternalDescription { get; init; }

        public string? Channel { get; init; }

        public string? Status { get; init; }
    }

    public sealed class UpdateInternalDescriptionCommand
    {
        public required int UserId { get; init; }

        public required int LocationId { get; init; }

        public required int QrCodeId { get; init; }

        public string? InternalDescription { get; init; }
    }

    public sealed class QrCodeLifecycleCommand
    {
        public required int UserId { get; init; }

        public required int LocationId { get; init; }

        public required int QrCodeId { get; init; }
    }

    public sealed class LocationCaptureLifecycleCommand
    {
        public required int UserId { get; init; }

        public required int LocationId { get; init; }
    }
}
