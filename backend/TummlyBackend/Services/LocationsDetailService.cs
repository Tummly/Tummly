using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class LocationsDetailService : ILocationsDetailService
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _time;
        private readonly LocationDetailOverviewComposer _overview;
        private readonly LocationDetailQrRowsComposer _qrRows;
        private readonly LocationDetailOfferCardsComposer _offerCards;

        public LocationsDetailService(
            ApplicationDbContext context,
            TimeProvider time,
            LocationDetailOverviewComposer overview,
            LocationDetailQrRowsComposer qrRows,
            LocationDetailOfferCardsComposer offerCards
        )
        {
            _context = context;
            _time = time;
            _overview = overview;
            _qrRows = qrRows;
            _offerCards = offerCards;
        }

        public async Task<LocationDetailResponseDto?> GetDetailAsync(
            LocationDetailQuery query
        )
        {
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(l =>
                    l.Id == query.LocationId
                    && l.RestaurantId == query.RestaurantId
                )
                .Select(l => new LocationSource(
                    l.Id,
                    l.LocationName,
                    l.LifecycleStatus,
                    l.City,
                    l.Postcode,
                    l.Address,
                    l.LocationPhone,
                    l.LocalContact,
                    l.ManagerUserId,
                    l.ManagerUser != null ? l.ManagerUser.FullName : null
                ))
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return null;
            }

            var privacyReadyAt = await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.Id == query.RestaurantId)
                .Select(r => r.PrivacyConsentReadyAt)
                .FirstOrDefaultAsync();
            var privacyReady = privacyReadyAt != null;

            var qrCounts = await _context.QrCodes
                .AsNoTracking()
                .Where(q => q.RestaurantLocationId == query.LocationId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    AnyCount = g.Count(),
                    ActiveCount = g.Count(q => q.Status == QrCodeStatus.Active),
                })
                .FirstOrDefaultAsync();
            var anyQrCount = qrCounts?.AnyCount ?? 0;
            var activeQrCount = qrCounts?.ActiveCount ?? 0;
            var hasActiveQr = activeQrCount > 0;

            var hasOffer = await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(o => o.RestaurantLocationId == query.LocationId);

            var (monthStartUtc, monthEndUtc) = DefaultReportingPeriodWindow.Resolve(
                "thisMonth",
                _time.GetUtcNow().UtcDateTime
            );

            var overviewMetrics = await _overview.ComposeAsync(
                query.LocationId,
                monthStartUtc,
                monthEndUtc
            );

            var qrRows = await _qrRows.ComposeAsync(
                query.LocationId,
                monthStartUtc,
                monthEndUtc
            );

            var offerCards = await _offerCards.ComposeAsync(query.LocationId);

            var lifecycleWire = ToLifecycleWire(location.LifecycleStatus);
            var setupStatus = LocationsListService.DeriveSetupStatus(
                location.LifecycleStatus,
                hasActiveQr,
                privacyReady
            );
            var city = NormalizeCity(location.City);
            var managerName = string.IsNullOrWhiteSpace(location.ManagerName)
                ? null
                : location.ManagerName.Trim();
            var postcode = string.IsNullOrWhiteSpace(location.Postcode)
                ? null
                : location.Postcode.Trim();

            var setupChecklist = LocationDetailSetupChecklistBuilder.Build(
                location.LifecycleStatus,
                location.LocationName,
                location.Address,
                city,
                postcode,
                hasActiveQr,
                anyQrCount,
                privacyReady,
                location.ManagerUserId,
                hasOffer
            );

            return new LocationDetailResponseDto
            {
                Success = true,
                Header = new LocationDetailHeaderDto
                {
                    Id = location.Id,
                    Name = location.LocationName,
                    City = city,
                    LifecycleStatus = lifecycleWire,
                    SetupStatus = setupStatus,
                    ManagerName = managerName,
                    ManagerUserId = location.ManagerUserId,
                    Address = location.Address,
                    Postcode = postcode,
                    LocationPhone = string.IsNullOrWhiteSpace(location.LocationPhone)
                        ? null
                        : location.LocationPhone.Trim(),
                    LocalContact = string.IsNullOrWhiteSpace(location.LocalContact)
                        ? null
                        : location.LocalContact.Trim(),
                    LiveQrCount = activeQrCount,
                    GuestsCapturedThisMonth = overviewMetrics.GuestsCaptured,
                },
                SetupChecklist = setupChecklist,
                OverviewMetrics = overviewMetrics,
                QrRows = qrRows,
                OfferCards = offerCards,
            };
        }

        private static string ToLifecycleWire(LocationLifecycleStatus status) =>
            status switch
            {
                LocationLifecycleStatus.Draft => "draft",
                LocationLifecycleStatus.Active => "active",
                LocationLifecycleStatus.Paused => "paused",
                LocationLifecycleStatus.Archived => "archived",
                _ => "active",
            };

        private static string? NormalizeCity(string? city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            return city.Trim();
        }

        private sealed record LocationSource(
            int Id,
            string LocationName,
            LocationLifecycleStatus LifecycleStatus,
            string? City,
            string? Postcode,
            string Address,
            string? LocationPhone,
            string? LocalContact,
            int? ManagerUserId,
            string? ManagerName
        );
    }
}
