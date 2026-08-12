using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Capture;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Persist / read Guest form thank-you catalog OfferId on
    /// <see cref="Models.RestaurantLocation"/> (ticket 07).
    /// </summary>
    public class CaptureThankYouOfferService : ICaptureThankYouOfferService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOffersCatalogService _offers;
        private readonly Func<DateTime> _utcNow;

        public CaptureThankYouOfferService(
            ApplicationDbContext context,
            IOffersCatalogService offers,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _offers = offers;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<CaptureThankYouOfferDto> GetAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == locationId,
                    cancellationToken
                );

            if (location == null
                || location.ThankYouCatalogOfferId is not int offerId)
            {
                return Empty();
            }

            return await BuildDtoAsync(locationId, offerId, cancellationToken);
        }

        public async Task<CaptureThankYouOfferSetResult> SetAsync(
            int locationId,
            int? offerId,
            CancellationToken cancellationToken = default
        )
        {
            var location = await _context.RestaurantLocations
                .FirstOrDefaultAsync(
                    row => row.Id == locationId,
                    cancellationToken
                );

            if (location == null)
            {
                return new CaptureThankYouOfferSetResult.LocationNotFound();
            }

            if (offerId is null)
            {
                location.ThankYouCatalogOfferId = null;
                await _context.SaveChangesAsync(cancellationToken);
                return new CaptureThankYouOfferSetResult.Ok(Empty());
            }

            var attachable = await _offers.IsActiveForLocationAsync(
                offerId.Value,
                locationId,
                cancellationToken
            );
            if (!attachable)
            {
                return new CaptureThankYouOfferSetResult.InvalidOffer(
                    "Offer must be Active and belong to this location."
                );
            }

            location.ThankYouCatalogOfferId = offerId.Value;
            await _context.SaveChangesAsync(cancellationToken);

            var dto = await BuildDtoAsync(
                locationId,
                offerId.Value,
                cancellationToken
            );
            return new CaptureThankYouOfferSetResult.Ok(dto);
        }

        private async Task<CaptureThankYouOfferDto> BuildDtoAsync(
            int locationId,
            int offerId,
            CancellationToken cancellationToken
        )
        {
            var offer = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == offerId
                        && row.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (offer == null)
            {
                return Empty();
            }

            var today = CatalogOfferStatus.VenueLocalToday(_utcNow(), 0);
            var live = CatalogOfferStatus.IsAttachableActive(
                offer.Status,
                offer.Validity,
                offer.CustomExpiryDate,
                today
            );

            return new CaptureThankYouOfferDto
            {
                ThankYouOfferId = offer.Id,
                ThankYouOfferTitle = offer.Title,
                ThankYouOfferLive = live,
            };
        }

        private static CaptureThankYouOfferDto Empty()
            => new()
            {
                ThankYouOfferId = null,
                ThankYouOfferTitle = null,
                ThankYouOfferLive = false,
            };
    }
}
