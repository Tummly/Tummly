using TummlyBackend.DTOs.Capture;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Guest form thank-you catalog attach per Owned location (ticket 07).
    /// </summary>
    public interface ICaptureThankYouOfferService
    {
        Task<CaptureThankYouOfferDto> GetAsync(
            int locationId,
            CancellationToken cancellationToken = default
        );

        Task<CaptureThankYouOfferSetResult> SetAsync(
            int locationId,
            int? offerId,
            CancellationToken cancellationToken = default
        );
    }
}
