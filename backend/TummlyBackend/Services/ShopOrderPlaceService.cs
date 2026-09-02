using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class ShopOrderPlaceService : IShopOrderPlaceService
    {
        private const string UkCountry = "United Kingdom";
        private const int ExpressDeliveryNetPence = 2000;

        private readonly ApplicationDbContext _context;
        private readonly IMaterialsCatalog _catalog;
        private readonly IShopCartService _carts;
        private readonly IShopOrderNumberAllocator _orderNumbers;

        public ShopOrderPlaceService(
            ApplicationDbContext context,
            IMaterialsCatalog catalog,
            IShopCartService carts,
            IShopOrderNumberAllocator orderNumbers
        )
        {
            _context = context;
            _catalog = catalog;
            _carts = carts;
            _orderNumbers = orderNumbers;
        }

        public async Task<ShopDeliveryDefaultsDto?> GetDeliveryDefaultsAsync(
            int restaurantId,
            int locationId,
            string placingUserDisplayName,
            CancellationToken cancellationToken = default
        )
        {
            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == locationId
                        && row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (location == null)
            {
                return null;
            }

            var contactName = string.IsNullOrWhiteSpace(location.LocalContact)
                ? placingUserDisplayName.Trim()
                : location.LocalContact.Trim();

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

            return new ShopDeliveryDefaultsDto
            {
                LocationId = location.Id,
                LocationName = location.LocationName,
                ContactName = contactName,
                ContactPhone = contactPhone,
                AddressLine1 = location.Address ?? string.Empty,
                AddressLine2 = string.IsNullOrWhiteSpace(location.City)
                    ? null
                    : location.City.Trim(),
                Postcode = string.IsNullOrWhiteSpace(location.Postcode)
                    ? string.Empty
                    : UkPostcode.FormatForDisplay(location.Postcode),
                Country = UkCountry,
            };
        }

        public async Task<ShopOrderPlaceResult> PlaceAsync(
            int restaurantId,
            int userId,
            string placedByName,
            PlaceShopOrderRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var modeError = ValidateExclusiveMode(request);
            if (modeError != null)
            {
                return modeError;
            }

            if (
                !string.Equals(
                    request.DeliveryMethod,
                    ShopDeliveryMethods.Standard,
                    StringComparison.Ordinal
                )
                && !string.Equals(
                    request.DeliveryMethod,
                    ShopDeliveryMethods.Express,
                    StringComparison.Ordinal
                )
            )
            {
                return ShopOrderPlaceResult.Fail(
                    "invalid_delivery_method",
                    "deliveryMethod must be standard or express."
                );
            }

            var shipError = ValidateShipTo(request.ShipTo, out var shipTo);
            if (shipError != null)
            {
                return shipError;
            }

            var location = await _context.RestaurantLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == request.LocationId
                        && row.RestaurantId == restaurantId,
                    cancellationToken
                );
            if (location == null)
            {
                return ShopOrderPlaceResult.Fail(
                    "location_not_found",
                    "Owned location was not found."
                );
            }

            var fromCart = request.FromCart == true;
            IReadOnlyList<(string SkuId, int Quantity)> rawLines;
            if (fromCart)
            {
                var cart = await _carts.GetCartAsync(
                    restaurantId,
                    request.LocationId,
                    userId,
                    cancellationToken
                );
                if (cart.Lines.Count == 0)
                {
                    return ShopOrderPlaceResult.Fail(
                        "empty_cart",
                        "Cart has no lines to place."
                    );
                }

                rawLines = cart.Lines
                    .Select(line => (line.SkuId, line.Quantity))
                    .ToList();
            }
            else
            {
                rawLines = request.Lines!
                    .Select(line => (line.SkuId.Trim(), line.Quantity))
                    .ToList();
            }

            var priced = PriceLines(rawLines);
            if (priced.Error != null)
            {
                return priced.Error;
            }

            var deliveryNetPence = string.Equals(
                request.DeliveryMethod,
                ShopDeliveryMethods.Express,
                StringComparison.Ordinal
            )
                ? ExpressDeliveryNetPence
                : 0;

            var materialsNetPence = priced.Lines!.Sum(line => line.LineNetPence);
            var vatPence = TummlyVatMath.VatPenceFromNetPence(materialsNetPence);
            var grossPence = checked(
                materialsNetPence + vatPence + deliveryNetPence
            );

            if (grossPence != request.ExpectedGrossPence)
            {
                return ShopOrderPlaceResult.Fail(
                    "totals_mismatch",
                    "expectedGrossPence does not match server totals."
                );
            }

            var now = DateTime.UtcNow;
            var orderNumber = await _orderNumbers.AllocateNextOrderNumberAsync(
                restaurantId,
                cancellationToken
            );

            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                RestaurantId = restaurantId,
                LocationId = location.Id,
                LocationNameSnapshot = location.LocationName,
                PlacedByUserId = userId,
                PlacedByNameSnapshot = placedByName.Trim(),
                MaterialsNetPence = materialsNetPence,
                VatPence = vatPence,
                DeliveryNetPence = deliveryNetPence,
                GrossPence = grossPence,
                DeliveryMethod = request.DeliveryMethod,
                PaymentStatus = ShopPaymentStatuses.AwaitingPayment,
                FulfilmentStatus = null,
                ShipToContactName = shipTo.ContactName,
                ShipToContactPhone = shipTo.ContactPhone,
                ShipToAddressLine1 = shipTo.AddressLine1,
                ShipToAddressLine2 = shipTo.AddressLine2,
                ShipToPostcode = shipTo.Postcode,
                ShipToCountry = shipTo.Country,
                DeliveryInstructions = shipTo.DeliveryInstructions,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            foreach (var line in priced.Lines!)
            {
                order.Lines.Add(new ShopOrderLine
                {
                    Id = Guid.NewGuid(),
                    ShopOrderId = order.Id,
                    CatalogSkuId = line.SkuId,
                    TitleSnapshot = line.Title,
                    MaterialType = line.MaterialType,
                    Quantity = line.Quantity,
                    UnitNetPence = line.UnitNetPence,
                    LineNetPence = line.LineNetPence,
                });
            }

            _context.ShopOrders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            if (fromCart)
            {
                await _carts.ClearCartAsync(
                    restaurantId,
                    request.LocationId,
                    userId,
                    cancellationToken
                );
            }

            return ShopOrderPlaceResult.Ok(ShopOrderDtoMapper.Map(order));
        }

        private static ShopOrderPlaceResult? ValidateExclusiveMode(
            PlaceShopOrderRequest request
        )
        {
            var fromCart = request.FromCart == true;
            var hasLines = request.Lines is { Count: > 0 };

            if (fromCart && hasLines)
            {
                return ShopOrderPlaceResult.Fail(
                    "exclusive_mode",
                    "fromCart and lines are mutually exclusive."
                );
            }

            if (!fromCart && !hasLines)
            {
                return ShopOrderPlaceResult.Fail(
                    "exclusive_mode",
                    "Provide fromCart: true or a non-empty lines array."
                );
            }

            return null;
        }

        private static ShopOrderPlaceResult? ValidateShipTo(
            PlaceShopOrderShipToRequest shipTo,
            out ValidatedShipTo validated
        )
        {
            validated = default!;

            var contactName = (shipTo.ContactName ?? string.Empty).Trim();
            if (contactName.Length is 0 or > 200)
            {
                return ShopOrderPlaceResult.Fail(
                    "invalid_ship_to",
                    "shipTo.contactName is required (max 200)."
                );
            }

            string? contactPhone = null;
            if (!string.IsNullOrWhiteSpace(shipTo.ContactPhone))
            {
                if (
                    !PhoneNumberHelper.TryNormalizeToE164(
                        shipTo.ContactPhone,
                        PhoneNumberHelper.DefaultRegion,
                        out var e164
                    )
                )
                {
                    return ShopOrderPlaceResult.Fail(
                        "invalid_ship_to",
                        "shipTo.contactPhone must be a valid UK number."
                    );
                }

                contactPhone = e164;
            }

            var addressLine1 = (shipTo.AddressLine1 ?? string.Empty).Trim();
            if (addressLine1.Length is 0 or > 500)
            {
                return ShopOrderPlaceResult.Fail(
                    "invalid_ship_to",
                    "shipTo.addressLine1 is required (max 500)."
                );
            }

            string? addressLine2 = null;
            if (!string.IsNullOrWhiteSpace(shipTo.AddressLine2))
            {
                addressLine2 = shipTo.AddressLine2.Trim();
                if (addressLine2.Length > 100)
                {
                    return ShopOrderPlaceResult.Fail(
                        "invalid_ship_to",
                        "shipTo.addressLine2 max length is 100."
                    );
                }
            }

            if (!UkPostcode.IsValidFormat(shipTo.Postcode))
            {
                return ShopOrderPlaceResult.Fail(
                    "invalid_ship_to",
                    "shipTo.postcode must be a valid UK postcode."
                );
            }

            var postcode = UkPostcode.FormatForDisplay(shipTo.Postcode);

            var country = (shipTo.Country ?? string.Empty).Trim();
            if (!string.Equals(country, UkCountry, StringComparison.Ordinal))
            {
                return ShopOrderPlaceResult.Fail(
                    "invalid_ship_to",
                    "shipTo.country must be United Kingdom."
                );
            }

            string? instructions = null;
            if (!string.IsNullOrWhiteSpace(shipTo.DeliveryInstructions))
            {
                instructions = shipTo.DeliveryInstructions.Trim();
                if (instructions.Length > 500)
                {
                    return ShopOrderPlaceResult.Fail(
                        "invalid_ship_to",
                        "shipTo.deliveryInstructions max length is 500."
                    );
                }
            }

            validated = new ValidatedShipTo(
                contactName,
                contactPhone,
                addressLine1,
                addressLine2,
                postcode,
                country,
                instructions
            );
            return null;
        }

        private (
            List<PricedLine>? Lines,
            ShopOrderPlaceResult? Error
        ) PriceLines(IReadOnlyList<(string SkuId, int Quantity)> rawLines)
        {
            var priced = new List<PricedLine>();
            foreach (var raw in rawLines)
            {
                if (string.IsNullOrWhiteSpace(raw.SkuId))
                {
                    return (
                        null,
                        ShopOrderPlaceResult.Fail(
                            "unknown_sku",
                            "skuId is required."
                        )
                    );
                }

                var detail = _catalog.TryBuildDetail(raw.SkuId);
                if (detail == null)
                {
                    return (
                        null,
                        ShopOrderPlaceResult.Fail(
                            "unknown_sku",
                            $"Unknown catalog skuId '{raw.SkuId}'."
                        )
                    );
                }

                if (raw.Quantity < detail.MinOrderQty)
                {
                    return (
                        null,
                        ShopOrderPlaceResult.Fail(
                            "invalid_quantity",
                            $"Quantity for '{raw.SkuId}' is below minOrderQty."
                        )
                    );
                }

                var lineNet = checked(detail.UnitNetPence * raw.Quantity);
                priced.Add(new PricedLine(
                    detail.SkuId,
                    detail.Title,
                    MapMaterialType(detail.SkuId, detail.Category),
                    raw.Quantity,
                    detail.UnitNetPence,
                    lineNet
                ));
            }

            return (priced, null);
        }

        private static string MapMaterialType(string skuId, string category)
        {
            return skuId switch
            {
                "table-tents" => "table-tents",
                "packaging-stickers" => "packaging-stickers",
                "receipt-stickers" => "receipt-stickers",
                "window-stickers" => "window-stickers",
                "package-seal-stickers" => "package-seal-stickers",
                "starter-kits" => "starter-kits",
                _ => category,
            };
        }

        private readonly record struct ValidatedShipTo(
            string ContactName,
            string? ContactPhone,
            string AddressLine1,
            string? AddressLine2,
            string Postcode,
            string Country,
            string? DeliveryInstructions
        );

        private sealed record PricedLine(
            string SkuId,
            string Title,
            string MaterialType,
            int Quantity,
            int UnitNetPence,
            int LineNetPence
        );
    }
}
