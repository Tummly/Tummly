using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Outcome of resolving the QR placement for a (location, QR type) key.
    /// </summary>
    public abstract record QrPlacementResolution;

    /// <summary>
    /// An Active, not archived code of that type exists. Oldest by CreatedAt wins.
    /// </summary>
    public sealed record Reuse(int QrCodeId) : QrPlacementResolution;

    /// <summary>
    /// No Active code exists. <paramref name="ReactivateQrCodeId"/> is the oldest
    /// Paused, not archived code of that type, or null when a new code is needed.
    /// </summary>
    public sealed record NeedsSlot(int? ReactivateQrCodeId) : QrPlacementResolution;

    public interface IQrPlacementResolver
    {
        Task<QrPlacementResolution> ResolveAsync(
            int locationId,
            QrType qrType,
            CancellationToken ct
        );
    }
}
