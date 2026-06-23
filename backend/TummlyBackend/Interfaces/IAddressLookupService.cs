using TummlyBackend.DTOs.Address;

namespace TummlyBackend.Interfaces
{
    public interface IAddressLookupService
    {
        Task<IReadOnlyList<AddressSuggestionDto>> SuggestAsync(
            string query,
            CancellationToken cancellationToken = default
        );

        Task<AddressResolveResultDto?> ResolvePostcodeAsync(
            string postcode,
            string? addressHint = null,
            CancellationToken cancellationToken = default
        );

        Task<AddressPremiseDto?> ResolveSuggestionAsync(
            string suggestionId,
            CancellationToken cancellationToken = default
        );
    }
}
