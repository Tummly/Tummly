using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Shop;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;

namespace TummlyBackend.Tests.Services
{
    public class ShopOrderPlaceServiceTests
    {
        private const int UnitNetPence = 1000;

        [Fact]
        public async Task PlaceAsync_WhenVatModeOff_VatPenceZero_AndGrossEqualsNet()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantAsync(context);
            var service = CreateService(
                context,
                new TummlySellerVatSettings { IsActive = false }
            );

            var result = await service.PlaceAsync(
                seeded.RestaurantId,
                seeded.UserId,
                "Place Owner",
                BuildRequest(seeded.LocationId, expectedGrossPence: UnitNetPence)
            );

            Assert.Null(result.ErrorCode);
            Assert.NotNull(result.Order);
            Assert.Equal(UnitNetPence, result.Order!.MaterialsNetPence);
            Assert.Equal(0, result.Order.VatPence);
            Assert.Equal(UnitNetPence, result.Order.GrossPence);
        }

        [Fact]
        public async Task PlaceAsync_WhenVatModeActive_Applies20PercentVat()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantAsync(context);
            var expectedVat = TummlyVatMath.VatPenceFromNetPence(
                UnitNetPence,
                TummlyVatMath.DefaultVatRateBps
            );
            var expectedGross = UnitNetPence + expectedVat;
            var service = CreateService(
                context,
                new TummlySellerVatSettings { IsActive = true }
            );

            var result = await service.PlaceAsync(
                seeded.RestaurantId,
                seeded.UserId,
                "Place Owner",
                BuildRequest(seeded.LocationId, expectedGrossPence: expectedGross)
            );

            Assert.Null(result.ErrorCode);
            Assert.NotNull(result.Order);
            Assert.Equal(UnitNetPence, result.Order!.MaterialsNetPence);
            Assert.Equal(expectedVat, result.Order.VatPence);
            Assert.Equal(expectedGross, result.Order.GrossPence);
        }

        [Fact]
        public async Task PlaceAsync_LocksPackPrice_NotUnitTimesQty()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantAsync(context);
            const int packNetPence = 1899;
            const int deliveryNetPence = 299;
            var service = CreateService(
                context,
                new TummlySellerVatSettings { IsActive = false },
                new PackCatalog(
                    packs:
                    [
                        new ShopMaterialPackRow { Qty = 1, NetPence = 999 },
                        new ShopMaterialPackRow { Qty = 2, NetPence = packNetPence },
                    ],
                    deliveryNetPence: deliveryNetPence
                )
            );

            var result = await service.PlaceAsync(
                seeded.RestaurantId,
                seeded.UserId,
                "Place Owner",
                BuildRequest(
                    seeded.LocationId,
                    expectedGrossPence: packNetPence + deliveryNetPence,
                    quantity: 2
                )
            );

            Assert.Null(result.ErrorCode);
            Assert.NotNull(result.Order);
            Assert.Equal(packNetPence, result.Order!.MaterialsNetPence);
            Assert.Equal(deliveryNetPence, result.Order.DeliveryNetPence);
            Assert.Equal(packNetPence + deliveryNetPence, result.Order.GrossPence);
            var line = Assert.Single(result.Order.Lines);
            Assert.Equal(2, line.Quantity);
            Assert.Equal(packNetPence, line.UnitNetPence);
            Assert.Equal(packNetPence, line.LineNetPence);
        }

        [Fact]
        public async Task PlaceAsync_RejectsInvalidPackQty()
        {
            await using var context = CreateContext();
            var seeded = await SeedRestaurantAsync(context);
            var service = CreateService(
                context,
                new TummlySellerVatSettings { IsActive = false },
                new PackCatalog(
                    packs:
                    [
                        new ShopMaterialPackRow { Qty = 1, NetPence = 999 },
                        new ShopMaterialPackRow { Qty = 5, NetPence = 4499 },
                    ],
                    deliveryNetPence: 299
                )
            );

            var result = await service.PlaceAsync(
                seeded.RestaurantId,
                seeded.UserId,
                "Place Owner",
                BuildRequest(
                    seeded.LocationId,
                    expectedGrossPence: 999,
                    quantity: 3
                )
            );

            Assert.Equal("invalid_quantity", result.ErrorCode);
            Assert.Null(result.Order);
        }

        private static PlaceShopOrderRequest BuildRequest(
            int locationId,
            int expectedGrossPence,
            int quantity = 1
        )
        {
            return new PlaceShopOrderRequest
            {
                LocationId = locationId,
                Lines =
                [
                    new PlaceShopOrderLineRequest
                    {
                        SkuId = "sku-a",
                        Quantity = quantity,
                    },
                ],
                DeliveryMethod = ShopDeliveryMethods.Standard,
                ExpectedGrossPence = expectedGrossPence,
                ShipTo = new PlaceShopOrderShipToRequest
                {
                    ContactName = "Alex",
                    AddressLine1 = "1 High Street",
                    Postcode = "SW1A 1AA",
                    Country = "United Kingdom",
                },
            };
        }

        private static ShopOrderPlaceService CreateService(
            ApplicationDbContext context,
            TummlySellerVatSettings sellerVat,
            IMaterialsCatalog? catalog = null
        )
        {
            return new ShopOrderPlaceService(
                context,
                catalog ?? new FixedCatalog(),
                new EmptyCarts(),
                new ShopOrderNumberAllocator(context),
                Options.Create(sellerVat)
            );
        }

        private static async Task<(
            int RestaurantId,
            int UserId,
            int LocationId
        )> SeedRestaurantAsync(ApplicationDbContext context)
        {
            var owner = new User
            {
                Email = $"{Guid.NewGuid():N}@example.com",
                PasswordHash = "x",
                FullName = "Place Owner",
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Place Venue",
                AccountType = "Single",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                City = "London",
                Postcode = "SW1A1AA",
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            return (restaurant.Id, owner.Id, location.Id);
        }

        private static ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            return new ApplicationDbContext(options);
        }

        private sealed class FixedCatalog : IMaterialsCatalog
        {
            public string CurrentCatalogId => "test-catalog";

            public MaterialsCatalogSnapshot GetRequired(string catalogId)
            {
                throw new NotSupportedException();
            }

            public IReadOnlyList<ShopCatalogListItemDto> BuildList()
            {
                return [];
            }

            public ShopCatalogDetailDto? TryBuildDetail(string skuId)
            {
                if (!string.Equals(skuId, "sku-a", StringComparison.Ordinal))
                {
                    return null;
                }

                return new ShopCatalogDetailDto
                {
                    SkuId = "sku-a",
                    Title = "Test SKU",
                    Category = "print",
                    Description = "desc",
                    UnitNetPence = UnitNetPence,
                    Currency = "GBP",
                    ImageUrl = "/img.png",
                    QrType = "table",
                    Material = "card",
                    Dimensions = "A6",
                    MinOrderQty = 1,
                    Packs = ShopMaterialPacks.SinglePack(1, UnitNetPence),
                    DeliveryNetPence = 0,
                    CatalogVersion = CurrentCatalogId,
                    MintOnShopFulfilment = false,
                };
            }
        }

        private sealed class PackCatalog : IMaterialsCatalog
        {
            private readonly IReadOnlyList<ShopMaterialPackRow> _packs;
            private readonly int _deliveryNetPence;

            public PackCatalog(
                IReadOnlyList<ShopMaterialPackRow> packs,
                int deliveryNetPence
            )
            {
                _packs = packs;
                _deliveryNetPence = deliveryNetPence;
            }

            public string CurrentCatalogId => "test-catalog";

            public MaterialsCatalogSnapshot GetRequired(string catalogId)
            {
                throw new NotSupportedException();
            }

            public IReadOnlyList<ShopCatalogListItemDto> BuildList() => [];

            public ShopCatalogDetailDto? TryBuildDetail(string skuId)
            {
                if (!string.Equals(skuId, "sku-a", StringComparison.Ordinal))
                {
                    return null;
                }

                return new ShopCatalogDetailDto
                {
                    SkuId = "sku-a",
                    Title = "Pack SKU",
                    Category = "print",
                    Description = "desc",
                    UnitNetPence = ShopMaterialPacks.LowestNetPence(_packs),
                    Currency = "GBP",
                    ImageUrl = "/img.png",
                    QrType = "table",
                    Material = "card",
                    Dimensions = "A6",
                    MinOrderQty = ShopMaterialPacks.LowestQty(_packs),
                    Packs = _packs,
                    DeliveryNetPence = _deliveryNetPence,
                    CatalogVersion = CurrentCatalogId,
                    MintOnShopFulfilment = false,
                };
            }
        }

        private sealed class EmptyCarts : IShopCartService
        {
            public Task<ShopCartDto> GetCartAsync(
                int restaurantId,
                int locationId,
                int userId,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult(
                    new ShopCartDto
                    {
                        LocationId = locationId,
                        Lines = [],
                        MaterialsNetPence = 0,
                        Currency = "GBP",
                    }
                );

            public Task<ShopCartDto?> UpsertLineAsync(
                int restaurantId,
                int locationId,
                int userId,
                string skuId,
                int quantity,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task<ShopCartDto> RemoveLineAsync(
                int restaurantId,
                int locationId,
                int userId,
                string skuId,
                CancellationToken cancellationToken = default
            ) => throw new NotSupportedException();

            public Task ClearCartAsync(
                int restaurantId,
                int locationId,
                int userId,
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
        }
    }
}
