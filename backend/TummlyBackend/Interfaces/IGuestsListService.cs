using TummlyBackend.DTOs.Guests;

namespace TummlyBackend.Interfaces
{
    public interface IGuestsListService
    {
        Task<object> GetListAsync(GuestsListQuery query);
    }
}
