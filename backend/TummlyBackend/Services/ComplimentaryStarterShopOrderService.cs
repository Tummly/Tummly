using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.PrintReadyQrMaterials;

namespace TummlyBackend.Services
{
    public sealed class ComplimentaryStarterShopOrderService
        : IComplimentaryStarterShopOrderService
    {
        private const string UkCountry = "United Kingdom";

        private readonly ApplicationDbContext _context;
        private readonly IMaterialsCatalog _catalog;
        private readonly IShopOrderNumberAllocator _orderNumbers;
        private readonly IShopOrderEmailNotifier _emailNotifier;

        public ComplimentaryStarterShopOrderService(
            ApplicationDbContext context,
            IMaterialsCatalog catalog,
            IShopOrderNumberAllocator orderNumbers,
            IShopOrderEmailNotifier? emailNotifier = null
        )
        {
            _context = context;
            _catalog = catalog;
            _orderNumbers = orderNumbers;
            _emailNotifier = emailNotifier ?? NoOpShopOrderEmailNotifier.Instance;
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

            var subscriptionPlan = await _context.BillingAccounts
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .Select(row => row.SubscriptionPlan)
                .FirstOrDefaultAsync(cancellationToken);
            if (
                string.IsNullOrWhiteSpace(subscriptionPlan)
                || string.Equals(
                    subscriptionPlan,
                    BillingSubscriptionPlans.Free,
                    StringComparison.Ordinal
                )
            )
            {
                return new ComplimentaryStarterShopOrderResult(
                    Guid.Empty,
                    Created: false
                );
            }

            var starterSkus = await LoadComplimentaryStarterSkusAsync(
                cancellationToken
            );
            if (starterSkus.Count == 0)
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

            foreach (var sku in starterSkus)
            {
                var detail = _catalog.TryBuildDetail(sku.SkuId);
                var title = detail?.Title ?? sku.Title;
                var materialType = sku.PlacementRecipe?.Slug ?? sku.SkuId;
                order.Lines.Add(
                    new ShopOrderLine
                    {
                        Id = Guid.NewGuid(),
                        ShopOrderId = order.Id,
                        CatalogSkuId = sku.SkuId,
                        TitleSnapshot = title,
                        MaterialType = materialType,
                        Quantity = 1,
                        UnitNetPence = 0,
                        LineNetPence = 0,
                    }
                );
            }

            // Full Workspace pool: the starter code minted Paused, and the print
            // path only takes an Active code. Do not exceed the pool; flag the
            // order so Admin sees why it has no asset yet.
            if (
                await HasPausedStarterCodeWithoutActiveAsync(
                    location.Id,
                    starterSkus,
                    cancellationToken
                )
            )
            {
                order.ReviewReason = ShopOrderReviewReasons.QrCapacityUnavailable;
            }

            _context.ShopOrders.Add(order);

            await _context.SaveChangesAsync(cancellationToken);

            await _emailNotifier.TryNotifyConfirmedAsync(
                order.Id,
                cancellationToken
            );

            return new ComplimentaryStarterShopOrderResult(
                order.Id,
                Created: true
            );
        }

        public async Task<IReadOnlyList<Guid>> EnsureAllActiveLocationsAsync(
            int restaurantId,
            int placedByUserId,
            string placedByName,
            CancellationToken cancellationToken = default
        )
        {
            var locationIds = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.LifecycleStatus == LocationLifecycleStatus.Active
                )
                .OrderBy(row => row.Id)
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);

            var shopOrderIds = new List<Guid>();
            foreach (var locationId in locationIds)
            {
                var result = await EnsureForLocationAsync(
                    restaurantId,
                    locationId,
                    placedByUserId,
                    placedByName,
                    cancellationToken
                );
                if (result.ShopOrderId != Guid.Empty)
                {
                    shopOrderIds.Add(result.ShopOrderId);
                }
            }

            return shopOrderIds;
        }

        /// <summary>
        /// True when a starter QR type has a Paused code at the Location and no
        /// Active code of that type (the pool-full mint).
        /// </summary>
        private async Task<bool> HasPausedStarterCodeWithoutActiveAsync(
            int locationId,
            IReadOnlyList<ShopMaterialSku> starterSkus,
            CancellationToken cancellationToken
        )
        {
            var starterTypes = new HashSet<QrType>();
            foreach (var sku in starterSkus)
            {
                if (
                    sku.PlacementRecipe != null
                    && Enum.TryParse<QrType>(
                        sku.PlacementRecipe.PlacementKey,
                        ignoreCase: false,
                        out var qrType
                    )
                )
                {
                    starterTypes.Add(qrType);
                }
            }

            if (starterTypes.Count == 0)
            {
                return false;
            }

            var codes = await _context.QrCodes
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantLocationId == locationId
                    && starterTypes.Contains(row.QrType)
                    && (
                        row.Status == QrCodeStatus.Active
                        || row.Status == QrCodeStatus.Paused
                    )
                )
                .Select(row => new { row.QrType, row.Status })
                .ToListAsync(cancellationToken);
            var activeTypes = codes
                .Where(row => row.Status == QrCodeStatus.Active)
                .Select(row => row.QrType)
                .ToHashSet();
            return codes.Any(row =>
                row.Status == QrCodeStatus.Paused
                && !activeTypes.Contains(row.QrType)
            );
        }

        /// <summary>
        /// One published, available-in-shop SKU per starter placement recipe (Admin
        /// can replace commerce while keeping the recipe slot). Filtering lives in
        /// <see cref="StarterQrMaterialTypes.QueryPublishedStarterSkus"/>.
        /// </summary>
        private async Task<IReadOnlyList<ShopMaterialSku>> LoadComplimentaryStarterSkusAsync(
            CancellationToken cancellationToken
        )
        {
            return await StarterQrMaterialTypes
                .QueryPublishedStarterSkus(_context)
                .Include(row => row.PlacementRecipe)
                .ToListAsync(cancellationToken);
        }
    }
}
