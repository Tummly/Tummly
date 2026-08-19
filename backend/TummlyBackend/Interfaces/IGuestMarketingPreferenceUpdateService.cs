using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestMarketingPreferenceUpdateService
    {
        Task<GuestMarketingPreferenceUpdateOutcome> UpdateAsync(
            int locationGuestId,
            int locationId,
            int actorUserId,
            PatchGuestMarketingPreferenceRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
