using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    public interface ILocationsActivityService
    {
        Task<object> GetActivityAsync(LocationsActivityQuery query);
    }
}
