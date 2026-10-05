using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Persist / read Offer Card print catalog OfferId on
    /// <see cref="Models.RestaurantLocation"/>.
    /// </summary>
    public class ShopOfferCardOfferService : IShopOfferCardOfferService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOffersCatalogService _offers;
        private readonly IPrintReadyQrMaterialsWork _printReadyWork;
        private readonly Func<DateTime> _utcNow;

        public ShopOfferCardOfferService(
            ApplicationDbContext context,
            IOffersCatalogService offers,
            IPrintReadyQrMaterialsWork printReadyWork,
            Func<DateTime>? utcNow = null
        )
        {
            _context = context;
            _offers = offers;
            _printReadyWork = printReadyWork;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public async Task<ShopOfferCardOfferDto> GetAsync(
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
                || location.OfferCardCatalogOfferId is not int offerId)
            {
                return Empty();
            }

            return await BuildDtoAsync(locationId, offerId, cancellationToken);
        }

        public async Task<bool> HasAttachAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.Id == locationId
                        && row.OfferCardCatalogOfferId != null,
                    cancellationToken
                );
        }

        public async Task<ShopOfferCardOfferSetResult> SetAsync(
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
                return new ShopOfferCardOfferSetResult.LocationNotFound();
            }

            if (offerId is null)
            {
                var previousOfferId = location.OfferCardCatalogOfferId;
                location.OfferCardCatalogOfferId = null;
                await _context.SaveChangesAsync(cancellationToken);
                await _offers.SyncInFlightStoredStatusForAttachChangeAsync(
                    previousOfferId,
                    nextOfferId: null,
                    cancellationToken
                );
                return new ShopOfferCardOfferSetResult.Ok(Empty());
            }

            var attachable = await _offers.IsAttachableForLocationAsync(
                offerId.Value,
                locationId,
                cancellationToken
            );
            if (!attachable)
            {
                return new ShopOfferCardOfferSetResult.InvalidOffer(
                    "Offer must be an attachable Draft or Active catalog offer for this location."
                );
            }

            var replacedOfferId = location.OfferCardCatalogOfferId;
            location.OfferCardCatalogOfferId = offerId.Value;
            await _context.SaveChangesAsync(cancellationToken);
            var sync = await _offers.SyncInFlightStoredStatusForAttachChangeAsync(
                replacedOfferId,
                offerId.Value,
                cancellationToken
            );
            if (sync is CatalogOfferInFlightSyncResult.CapReached cap)
            {
                location.OfferCardCatalogOfferId = replacedOfferId;
                await _context.SaveChangesAsync(cancellationToken);
                return new ShopOfferCardOfferSetResult.CapReached(
                    cap.Cap,
                    cap.Current
                );
            }

            if (sync is CatalogOfferInFlightSyncResult.FailClosed)
            {
                location.OfferCardCatalogOfferId = replacedOfferId;
                await _context.SaveChangesAsync(cancellationToken);
                return new ShopOfferCardOfferSetResult.FailClosed();
            }

            await _printReadyWork.RequestEnsureAsync(
                locationId,
                cancellationToken
            );

            var dto = await BuildDtoAsync(
                locationId,
                offerId.Value,
                cancellationToken
            );
            return new ShopOfferCardOfferSetResult.Ok(dto);
        }

        private async Task<ShopOfferCardOfferDto> BuildDtoAsync(
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

            return new ShopOfferCardOfferDto
            {
                OfferCardOfferId = offer.Id,
                OfferCardOfferTitle = offer.Title,
                OfferCardOfferLive = live,
            };
        }

        private static ShopOfferCardOfferDto Empty()
            => new()
            {
                OfferCardOfferId = null,
                OfferCardOfferTitle = null,
                OfferCardOfferLive = false,
            };
    }
}
