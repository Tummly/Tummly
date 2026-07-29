using TummlyBackend.DTOs.SmartGuestLink;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ISmartGuestLinkService
    {
        /// <summary>Crypto-random 32-char token; retries on QR code token uniqueness collision.</summary>
        Task<string> GenerateTokenAsync();

        /// <summary>
        /// Read-only resolve for guest feedback form metadata. Only resolves
        /// Active QR codes — Paused/Archived/unknown tokens return null.
        /// </summary>
        Task<GuestLinkLocationInfo?> ResolveForGuestAsync(string token);

        /// <summary>
        /// Tracked resolve for feedback/STT persistence. Only resolves
        /// Active QR codes — Paused/Archived/unknown tokens return null.
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
