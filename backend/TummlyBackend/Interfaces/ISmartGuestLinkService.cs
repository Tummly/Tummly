using TummlyBackend.DTOs.SmartGuestLink;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ISmartGuestLinkService
    {
        /// <summary>Crypto-random 32-char token; retries on DB uniqueness collision.</summary>
        Task<string> GenerateTokenAsync();

        /// <summary>Read-only resolve for guest feedback form metadata.</summary>
        Task<GuestLinkLocationInfo?> ResolveForGuestAsync(string token);

        /// <summary>Tracked resolve for feedback persistence.</summary>
        Task<RestaurantLocation?> ResolveLocationForWriteAsync(string token);

        /// <summary>Canonical Smart Guest Link URL: {Frontend:BaseUrl}/scan/{token}</summary>
        string BuildGuestUrl(string linkToken);
    }
}
