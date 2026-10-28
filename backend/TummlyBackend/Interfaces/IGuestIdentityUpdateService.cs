using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestIdentityUpdateService
    {
        Task<GuestIdentityUpdateOutcome> UpdateAsync(
            int userId,
            int locationGuestId,
            int locationId,
            PatchGuestIdentityRequest request,
            CancellationToken cancellationToken = default
        );
    }
}
