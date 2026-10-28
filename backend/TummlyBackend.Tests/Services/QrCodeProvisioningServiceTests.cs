using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Shop.MaterialsCatalog;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class QrCodeProvisioningServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly QrCodeProvisioningService _service;

        public QrCodeProvisioningServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            ShopMaterialsCatalogSeed
                .EnsureSeededAsync(_context)
                .GetAwaiter()
                .GetResult();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example"
                })
                .Build();

            var smartGuestLink = new SmartGuestLinkService(
                _context,
                configuration,
                new NoOpBillingAccountLifecycle()
            );
            _service = new QrCodeProvisioningService(_context, smartGuestLink,
                TestActiveQrCapacity.For(_context)
            );
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_LaunchSeed_MintsOneCounterDisplay()
        {
            var location = await CreateLocationAsync();

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            var minted = Assert.Single(qrCodes);
            Assert.Equal(QrType.OfferCard, minted.QrType);
            Assert.Equal(QrCodeStatus.Active, minted.Status);
            Assert.Equal(32, minted.Token.Length);
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_TwoStarterRecipes_MintsTwoTypes()
        {
            var tentRecipe = await _context.PlacementRecipes.SingleAsync(row =>
                row.Slug == "table-tent"
            );
            tentRecipe.IncludeInComplimentaryStarter = true;
            foreach (
                var tentSku in await _context.ShopMaterialSkus
                    .Where(row => row.PlacementRecipeId == tentRecipe.Id)
                    .ToListAsync()
            )
            {
                tentSku.AvailableInShop = true;
            }
            await _context.SaveChangesAsync();

            var location = await CreateLocationAsync();

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            Assert.Equal(2, qrCodes.Count);
            Assert.Equal(
                new[] { QrType.OfferCard, QrType.TableTent },
                qrCodes.Select(q => q.QrType).ToArray()
            );
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_ReEnsure_DoesNotDuplicate()
        {
            var location = await CreateLocationAsync();

            var first = await _service.MintDefaultQrCodesAsync(location);
            await _context.SaveChangesAsync();
            Assert.Single(first);

            var second = await _service.MintDefaultQrCodesAsync(location);
            await _context.SaveChangesAsync();

            Assert.Empty(second);
            Assert.Equal(1, await _context.QrCodes.CountAsync());
            Assert.Equal(
                QrType.OfferCard,
                await _context.QrCodes.Select(row => row.QrType).SingleAsync()
            );
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_ComingSoonSkus_NotMinted()
        {
            var location = await CreateLocationAsync();

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);
            await _context.SaveChangesAsync();

            Assert.Single(qrCodes);
            var types = await _context.QrCodes
                .Select(row => row.QrType)
                .ToListAsync();
            Assert.DoesNotContain(QrType.TableTent, types);
            Assert.DoesNotContain(QrType.WindowSticker, types);
            Assert.DoesNotContain(QrType.TableTalker, types);
            Assert.DoesNotContain(QrType.GuestCard, types);
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_TokensAreUnique()
        {
            var tentRecipe = await _context.PlacementRecipes.SingleAsync(row =>
                row.Slug == "table-tent"
            );
            tentRecipe.IncludeInComplimentaryStarter = true;
            await _context.SaveChangesAsync();

            var location = await CreateLocationAsync();

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            var tokens = qrCodes.Select(q => q.Token).ToList();
            Assert.Equal(tokens.Distinct().Count(), tokens.Count);
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_DoesNotSaveUntilCallerSaves()
        {
            var location = await CreateLocationAsync();

            await _service.MintDefaultQrCodesAsync(location);

            Assert.Equal(0, await _context.QrCodes.CountAsync());

            await _context.SaveChangesAsync();

            Assert.Equal(1, await _context.QrCodes.CountAsync());
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_AcrossTwoLocations_AllTokensUnique()
        {
            var locationA = await CreateLocationAsync("Site A");
            var locationB = await CreateLocationAsync("Site B");

            var qrCodesA = await _service.MintDefaultQrCodesAsync(locationA);
            var qrCodesB = await _service.MintDefaultQrCodesAsync(locationB);

            var tokens = qrCodesA.Concat(qrCodesB).Select(q => q.Token).ToList();
            Assert.Equal(2, tokens.Count);
            Assert.Equal(tokens.Distinct().Count(), tokens.Count);
        }

        [Fact]
        public async Task MintDefault_FreeAccount_MintsPaused()
        {
            var location = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Free,
                BillingStatuses.Free
            );

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            var minted = Assert.Single(qrCodes);
            Assert.Equal(QrCodeStatus.Paused, minted.Status);
        }

        [Fact]
        public async Task MintDefault_UnpaidCustomSignup_MintsPaused()
        {
            var location = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Custom,
                BillingStatuses.Free
            );

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            var minted = Assert.Single(qrCodes);
            Assert.Equal(QrCodeStatus.Paused, minted.Status);
        }

        [Fact]
        public async Task MintDefault_PaidActiveCustom_MintsActive()
        {
            var location = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Custom,
                BillingStatuses.Active
            );
            await SetCustomPoolAsync(location.RestaurantId, qrPlacements: 5);

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            var minted = Assert.Single(qrCodes);
            Assert.Equal(QrCodeStatus.Active, minted.Status);
        }

        [Fact]
        public async Task MintDefault_PilotAccount_MintsActive()
        {
            var location = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.Pilot
            );

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            var minted = Assert.Single(qrCodes);
            Assert.Equal(QrCodeStatus.Active, minted.Status);
        }

        [Fact]
        public async Task MintDefault_FullWorkspace_MintsStarterQrPaused()
        {
            // B5: Pilot pool is 5 and another Location already uses all five.
            var first = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.Pilot
            );
            await AddActiveCodesAsync(first, 5);
            var second = await AddLocationAsync(first.RestaurantId, "Second");

            var qrCodes = await _service.MintDefaultQrCodesAsync(second);

            var minted = Assert.Single(qrCodes);
            Assert.Equal(QrCodeStatus.Paused, minted.Status);
        }

        [Fact]
        public async Task MintDefault_Headroom_MintsAtMostHeadroomActive()
        {
            await EnableSecondStarterTypeAsync();
            var first = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.Pilot
            );
            await AddActiveCodesAsync(first, 4);
            var second = await AddLocationAsync(first.RestaurantId, "Second");

            var qrCodes = await _service.MintDefaultQrCodesAsync(second);

            Assert.Equal(2, qrCodes.Count);
            Assert.Equal(1, qrCodes.Count(q => q.Status == QrCodeStatus.Active));
            Assert.Equal(1, qrCodes.Count(q => q.Status == QrCodeStatus.Paused));
        }

        [Fact]
        public async Task MintDefault_HeldReservationAtOtherLocation_ReducesHeadroom()
        {
            var first = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.Pilot
            );
            await AddActiveCodesAsync(first, 4);
            var now = DateTime.UtcNow;
            _context.ShopQrSlotReservations.Add(new ShopQrSlotReservation
            {
                Id = Guid.NewGuid(),
                ShopOrderId = Guid.NewGuid(),
                RestaurantLocationId = first.Id,
                QrType = QrType.GuestCard,
                Status = ShopQrSlotReservationStatuses.Open,
                ReconcileAfterUtc = now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            await _context.SaveChangesAsync();
            var second = await AddLocationAsync(first.RestaurantId, "Second");

            var qrCodes = await _service.MintDefaultQrCodesAsync(second);

            Assert.Equal(QrCodeStatus.Paused, Assert.Single(qrCodes).Status);
        }

        [Fact]
        public async Task MintDefault_TwoUnsavedLocationsInOneBatch_ShareTheHeadroom()
        {
            // Signup mints for several Locations before one save: headroom is spent once.
            var first = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Pilot,
                BillingStatuses.Pilot
            );
            await AddActiveCodesAsync(first, 4);
            var second = await AddLocationAsync(first.RestaurantId, "Second");
            var third = await AddLocationAsync(first.RestaurantId, "Third");

            var fromSecond = await _service.MintDefaultQrCodesAsync(second);
            var fromThird = await _service.MintDefaultQrCodesAsync(third);

            Assert.Equal(QrCodeStatus.Active, Assert.Single(fromSecond).Status);
            Assert.Equal(QrCodeStatus.Paused, Assert.Single(fromThird).Status);
        }

        [Fact]
        public async Task MintDefault_PlanWithUnresolvablePool_MintsPaused()
        {
            var location = await CreateLocationWithPlanAsync(
                BillingSubscriptionPlans.Starter,
                BillingStatuses.Active
            );
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == location.RestaurantId);
            account.ContractedPricebookId = "NO-SUCH-PRICEBOOK";
            await _context.SaveChangesAsync();

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            Assert.Equal(QrCodeStatus.Paused, Assert.Single(qrCodes).Status);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task EnableSecondStarterTypeAsync()
        {
            var tentRecipe = await _context.PlacementRecipes.SingleAsync(row =>
                row.Slug == "table-tent"
            );
            tentRecipe.IncludeInComplimentaryStarter = true;
            foreach (
                var tentSku in await _context.ShopMaterialSkus
                    .Where(row => row.PlacementRecipeId == tentRecipe.Id)
                    .ToListAsync()
            )
            {
                tentSku.AvailableInShop = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task SetCustomPoolAsync(int restaurantId, int qrPlacements)
        {
            var account = await _context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.CustomPlanConfigJson = new TummlyBackend.Billing.CustomPlan.CustomPlanConfig
            {
                Version = 1,
                Cadence = "monthly",
                Quantities = new TummlyBackend.Billing.CustomPlan.CustomPlanQuantities
                {
                    Locations = 1,
                    QrPlacements = qrPlacements,
                },
            }.ToJson();
            await _context.SaveChangesAsync();
        }

        private async Task AddActiveCodesAsync(RestaurantLocation location, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _context.QrCodes.Add(new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.DigitalGuestLink,
                    Token = Guid.NewGuid().ToString("N"),
                    Status = QrCodeStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<RestaurantLocation> AddLocationAsync(
            int restaurantId,
            string name
        )
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = name,
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location;
        }

        private async Task<RestaurantLocation> CreateLocationWithPlanAsync(
            string plan,
            string billingStatus
        )
        {
            var location = await CreateLocationAsync();
            var account = BillingCreditsService.CreateDefaultBillingAccount(
                location.RestaurantId,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            account.SubscriptionPlan = plan;
            account.BillingStatus = billingStatus;
            _context.BillingAccounts.Add(account);
            await _context.SaveChangesAsync();
            return location;
        }

        private async Task<RestaurantLocation> CreateLocationAsync(
            string locationName = "Main"
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = "Single",
                OwnerUserId = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow
            };

            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            return location;
        }
    }
}
