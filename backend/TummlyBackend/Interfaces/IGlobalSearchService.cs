using TummlyBackend.DTOs.Search;

namespace TummlyBackend.Interfaces
{
    public interface IGlobalSearchService
    {
        Task<GlobalSearchResponse> SearchAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken = default
        );
    }
}
