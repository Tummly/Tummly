using TummlyBackend.DTOs.OwnedLocation;

namespace TummlyBackend.Interfaces
{
    public interface IOwnedLocationService
    {
        Task<OwnedLocationResult> ResolveAsync(int userId, int locationId);
    }
}
