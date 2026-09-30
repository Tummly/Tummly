using TummlyBackend.DTOs.Companies;

namespace TummlyBackend.Interfaces
{
    public interface ICompaniesHouseSearchService
    {
        Task<IReadOnlyList<CompanySuggestionDto>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default
        );
    }
}
