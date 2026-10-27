using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    public sealed record ShopCapacityWorkspace(
        int RestaurantId,
        int LocationId,
        int OwnerUserId,
        string Jwt
    );

    /// <summary>
    /// Arrange steps for Shop QR capacity tests. Works on any
    /// <see cref="ApplicationDbContext"/> (in-memory factory store or the
    /// SQL Server fixture database). The location starts with three Active
    /// codes (Table Tent, Window Sticker, Offer Card) on a Starter plan.
    /// </summary>
    public static class ShopCapacityTestSeed
    {
        public const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        public static async Task<ShopCapacityWorkspace> SeedWorkspaceAsync(
            ApplicationDbContext context,
            IJwtService jwtService
        )
        {
            var suffix = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;

            var owner = new User
            {
                FullName = "Capacity Owner",
                Email = $"capacity-owner-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = now,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = $"Capacity Venue {suffix}",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(new BillingAccount
            {
                RestaurantId = restaurant.Id,
                RevolutCustomerId = $"cust_capacity_{suffix}",
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = PricebookId,
            });

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Capacity Site",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                LocalContact = "Site Contact",
                LocationPhone = "+442074071234",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            foreach (var type in new[]
            {
                QrType.TableTent,
                QrType.WindowSticker,
                QrType.OfferCard,
            })
            {
                context.QrCodes.Add(new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = type,
                    Token = $"cap-{type}-{Guid.NewGuid():N}"[..32],
                    Status = QrCodeStatus.Active,
                    CreatedAt = now,
                });
            }

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });
            owner.SelectedRestaurantId = restaurant.Id;
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );
            return new ShopCapacityWorkspace(
                restaurant.Id,
                location.Id,
                owner.Id,
                jwt
            );
        }

        /// <summary>
        /// Adds Active Digital Guest Link codes until exactly
        /// <paramref name="freeSlots"/> slots remain under the Workspace
        /// pool. Returns the pool.
        /// </summary>
        public static async Task<int> LeaveFreeSlotsAsync(
            ApplicationDbContext context,
            ShopCapacityWorkspace workspace,
            int freeSlots,
            int? fillLocationId = null
        )
        {
            var cap = await CapAsync(context, workspace);
            var used = await new ActiveQrCapacityProbe(context)
                .CountUsedAsync(workspace.RestaurantId);
            var toAdd = cap - freeSlots - used;
            Assert.True(toAdd >= 0, $"Seed already uses {used} of cap {cap}.");

            for (var i = 0; i < toAdd; i++)
            {
                context.QrCodes.Add(new QrCode
                {
                    RestaurantLocationId = fillLocationId ?? workspace.LocationId,
                    QrType = QrType.DigitalGuestLink,
                    Token = $"fill-{Guid.NewGuid():N}"[..32],
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await context.SaveChangesAsync();
            return cap;
        }

        /// <summary>
        /// Adds a second Location (no codes) to the same Workspace. The result
        /// has the same restaurant, owner and token; only the Location differs.
        /// </summary>
        public static async Task<ShopCapacityWorkspace> AddLocationAsync(
            ApplicationDbContext context,
            ShopCapacityWorkspace workspace,
            string name = "Second Site"
        )
        {
            var location = new RestaurantLocation
            {
                RestaurantId = workspace.RestaurantId,
                LocationName = name,
                Address = "2 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                LocalContact = "Site Contact",
                LocationPhone = "+442074071234",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            return workspace with { LocationId = location.Id };
        }

        public static async Task<int> CapAsync(
            ApplicationDbContext context,
            ShopCapacityWorkspace workspace
        )
        {
            var account = await context.BillingAccounts
                .AsNoTracking()
                .SingleAsync(row => row.RestaurantId == workspace.RestaurantId);
            var capacity = new ActiveQrCapacityProbe(context).Capacity;
            var allowance = await capacity.GetAllowanceAsync(
                account,
                CancellationToken.None
            );
            Assert.NotNull(allowance);
            return allowance!.WorkspacePool;
        }

        /// <summary>
        /// Inserts an awaiting-payment order with one line per
        /// (sku, quantity). Totals are placeholders; the pay session reads
        /// them from the row.
        /// </summary>
        public static async Task<Guid> InsertAwaitingOrderAsync(
            ApplicationDbContext context,
            ShopCapacityWorkspace workspace,
            params string[] skuIds
        )
        {
            var order = new ShopOrder
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"CAP-{Guid.NewGuid():N}"[..16],
                RestaurantId = workspace.RestaurantId,
                LocationId = workspace.LocationId,
                LocationNameSnapshot = "Capacity Site",
                PlacedByUserId = workspace.OwnerUserId,
                PlacedByNameSnapshot = "Capacity Owner",
                MaterialsNetPence = 1000 * skuIds.Length,
                VatPence = 200 * skuIds.Length,
                DeliveryNetPence = 0,
                GrossPence = 1200 * skuIds.Length,
                DeliveryMethod = ShopDeliveryMethods.Standard,
                PaymentStatus = ShopPaymentStatuses.AwaitingPayment,
                ShipToContactName = "Ada Lovelace",
                ShipToAddressLine1 = "6 Southwark Street",
                ShipToPostcode = "SE1 1TQ",
                ShipToCountry = "United Kingdom",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            foreach (var sku in skuIds)
            {
                order.Lines.Add(new ShopOrderLine
                {
                    Id = Guid.NewGuid(),
                    CatalogSkuId = sku,
                    TitleSnapshot = sku,
                    MaterialType = sku,
                    Quantity = 5,
                    UnitNetPence = 1000,
                    LineNetPence = 1000,
                });
            }

            context.ShopOrders.Add(order);
            await context.SaveChangesAsync();
            return order.Id;
        }

        /// <summary>
        /// Flips the catalog flag that decides whether a SKU mints a QR code
        /// at Shop fulfilment (Admin can do this between pay and payment).
        /// </summary>
        public static async Task SetMintOnShopFulfilmentAsync(
            IServiceProvider services,
            string skuId,
            bool mint
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var sku = await context.ShopMaterialSkus
                .SingleAsync(row => row.SkuId == skuId);
            sku.MintOnShopFulfilment = mint;
            await context.SaveChangesAsync();
        }

        public static HttpRequestMessage PayRequest(
            ShopCapacityWorkspace workspace,
            Guid orderId,
            string? idempotencyKey = null
        )
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/shop/orders/{orderId}/pay?locationId={workspace.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", workspace.Jwt);
            request.Headers.Add(
                "Idempotency-Key",
                idempotencyKey ?? Guid.NewGuid().ToString("D")
            );
            return request;
        }

        /// <summary>
        /// Makes the SKU orderable: available in the Shop and, for Guest
        /// Cards, an attached card offer.
        /// </summary>
        public static async Task MakeOrderableAsync(
            ApplicationDbContext context,
            ShopCapacityWorkspace workspace,
            params string[] skuIds
        )
        {
            foreach (var skuId in skuIds)
            {
                var sku = await context.ShopMaterialSkus
                    .SingleAsync(row => row.SkuId == skuId);
                sku.AvailableInShop = true;
            }

            if (skuIds.Contains("qr-guest-cards"))
            {
                var offer = new CatalogOffer
                {
                    RestaurantLocationId = workspace.LocationId,
                    Title = "Free Dessert Friday",
                    Description = "Guest card offer",
                    Status = CatalogOfferStatus.Active,
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    OfferType = CatalogOfferType.FreeItem,
                    FreeItemText = "Dessert",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                context.CatalogOffers.Add(offer);
                await context.SaveChangesAsync();
                var location = await context.RestaurantLocations
                    .SingleAsync(row => row.Id == workspace.LocationId);
                location.OfferCardCatalogOfferId = offer.Id;
            }

            await context.SaveChangesAsync();
        }

        private sealed class ActiveQrCapacityProbe
        {
            private readonly ApplicationDbContext _context;

            public ActiveQrCapacityProbe(ApplicationDbContext context)
            {
                _context = context;
                Capacity = new ActiveQrCapacity(
                    context,
                    PricebookCatalog.LoadFromDirectory(
                        TestPricebookPaths.PackDirectory()
                    )
                );
            }

            public ActiveQrCapacity Capacity { get; }

            public Task<int> CountUsedAsync(int restaurantId) =>
                Capacity.CountUsedForRestaurantAsync(
                    restaurantId,
                    CancellationToken.None
                );
        }
    }
}
