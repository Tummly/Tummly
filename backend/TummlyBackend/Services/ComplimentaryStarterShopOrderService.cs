using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ComplimentaryStarterShopOrderService
        : IComplimentaryStarterShopOrderService
    {
        private const string UkCountry = "United Kingdom";

        private static readonly (string SkuId, string MaterialType)[] StarterSkus =
        [
            ("table-tents", "table-tents"),
            ("window-stickers", "window-stickers"),
            ("offer-card", "offer-card"),
        ];

        private readonly ApplicationDbContext _context;
        private readonly IMaterialsCatalog _catalog;
        private readonly IShopOrderNumberAllocator _orderNumbers;

        public ComplimentaryStarterShopOrderService(
            ApplicationDbContext context,
            IMaterialsCatalog catalog,
            IShopOrderNumberAllocator orderNumbers
        )
        {
            _context = context;
            _catalog = catalog;
            _orderNumbers = orderNumbers;
        }

        public async Task<ComplimentaryStarterShopOrderResult> EnsureForLocationAsync(
            int restaurantId,
            int locationId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        )
        {
            var existingId = await _context.ShopOrders
                .AsNoTracking()
                .Where(row =>
                    row.LocationId == locationId
                    && row.IsComplimentary
                )
                .Select(row => (Guid?)row.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingId.HasValue)
            {
                return new ComplimentaryStarterShopOrderResult(
                    existingId.Value,
                    Created: false
                );
            }

            var location = await _context.RestaurantLocations
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == locationId
                        && row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (location == null)
            {
                throw new InvalidOperationException(
                    $"Owned location {locationId} was not found for restaurant {restaurantId}."
                );
            }

            if (location.LifecycleStatus != LocationLifecycleStatus.Active)
            {
                return new ComplimentaryStarterShopOrderResult(
                    Guid.Empty,
                    Created: false
                );
            }

            var now = DateTime.UtcNow;
            var orderNumber = await _orderNumbers.AllocateNextOrderNumberAsync(
                restaurantId,
                cancellationToken
            );

            var contactName = string.IsNullOrWhiteSpace(location.LocalContact)
                ? placedByName.Trim()
                : location.LocalContact.Trim();
            if (string.IsNullOrWhiteSpace(contactName))
            {
                contactName = location.LocationName;
            }

            string? contactPhone = null;
            if (
                !string.IsNullOrWhiteSpace(location.LocationPhone)
                && PhoneNumberHelper.TryNormalizeToE164(
                    location.LocationPhone,
                    PhoneNumberHelper.DefaultRegion,
                    out var e164
                )
            )
            {
                contactPhone = e164;
            }

            var postcode = string.IsNullOrWhiteSpace(location.Postcode)
                ? string.Empty
                : UkPostcode.FormatForDisplay(location.Postcode);

            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                RestaurantId = restaurantId,
                LocationId = location.Id,
                LocationNameSnapshot = location.LocationName,
                PlacedByUserId = placedByUserId,
                PlacedByNameSnapshot = string.IsNullOrWhiteSpace(placedByName)
                    ? contactName
                    : placedByName.Trim(),
                MaterialsNetPence = 0,
                VatPence = 0,
                DeliveryNetPence = 0,
                GrossPence = 0,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.Paid,
                RevolutOrderId = null,
                PaidAtUtc = now,
                FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                ProcessingStartedAtUtc = now,
                ShipToContactName = contactName,
                ShipToContactPhone = contactPhone,
                ShipToAddressLine1 = string.IsNullOrWhiteSpace(location.Address)
                    ? location.LocationName
                    : location.Address.Trim(),
                ShipToAddressLine2 = string.IsNullOrWhiteSpace(location.City)
                    ? null
                    : location.City.Trim(),
                ShipToPostcode = postcode,
                ShipToCountry = UkCountry,
                IsComplimentary = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            foreach (var (skuId, materialType) in StarterSkus)
            {
                var detail = _catalog.TryBuildDetail(skuId);
                var title = detail?.Title ?? skuId;
                order.Lines.Add(
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        ShopOrderId = order.Id,
                        CatalogSkuId = skuId,
                        TitleSnapshot = title,
                        MaterialType = materialType,
                        Quantity = 1,
                        UnitNetPence = 0,
                        LineNetPence = 0,
                    }
                );
            }

            _context.ShopOrders.Add(order);

            await _context.SaveChangesAsync(cancellationToken);

            return new ComplimentaryStarterShopOrderResult(
                order.Id,
                Created: true
            );
        }
    }
}
