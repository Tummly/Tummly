using TummlyBackend.DTOs.SmartGuestLink;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ISmartGuestLinkService
    {
        /// <summary>Crypto-random 32-char token; retries on QR code token uniqueness collision.</summary>
        Task<string> GenerateTokenAsync();

        /// <summary>
        /// Read-only resolve for guest feedback form metadata. Calls Tick, then
        /// returns Dormant branded payload, Pause/invalid as NotFound, or Live.
        /// Only Active QR codes resolve.
        /// </summary>
        Task<GuestQrResolveResult> ResolveForGuestAsync(string token);

        /// <summary>
        /// Tracked resolve for feedback/STT persistence. Calls Tick, then denies
        /// Dormant and Pause (null). Soft lock and live allow write.
        /// Only Active QR codes resolve.
        /// </summary>
        Task<QrLinkWriteResolution?> ResolveLocationForWriteAsync(string token);

        /// <summary>Canonical QR link URL: {Frontend:BaseUrl}/scan/{token}</summary>
        string BuildGuestUrl(string token);

        /// <summary>
        /// The Active Smart Guest QR link token for a location, or null if
        /// none is Active. Used by operator-facing surfaces (Home, Locations).
        /// </summary>
        Task<string?> GetActiveSmartGuestTokenAsync(int restaurantLocationId);
    }
}
