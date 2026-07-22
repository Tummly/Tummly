namespace TummlyBackend.Interfaces
{
    public interface IGuestsListService
    {
        Task<object> GetListAsync(
            int locationId,
            string locationName,
            string smartGroup,
            string? q,
            string sort,
            int page,
            int pageSize
        );
    }
}
