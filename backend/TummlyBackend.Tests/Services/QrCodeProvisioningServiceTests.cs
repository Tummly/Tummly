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
            _service = new QrCodeProvisioningService(_context, smartGuestLink);
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

        public void Dispose()
        {
            _context.Dispose();
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
