using TummlyBackend.DTOs.Locations;

namespace TummlyBackend.Interfaces
{
    public interface ILocationsListService
    {
        Task<object> GetListAsync(LocationsListQuery query);
    }
}
