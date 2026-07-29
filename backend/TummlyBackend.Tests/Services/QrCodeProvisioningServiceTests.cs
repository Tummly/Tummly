using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;

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
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://tummly.example"
                })
                .Build();

            var smartGuestLink = new SmartGuestLinkService(_context, configuration);
            _service = new QrCodeProvisioningService(_context, smartGuestLink);
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_StagesFiveActiveQrCodes()
        {
            var location = await CreateLocationAsync();

            var qrCodes = await _service.MintDefaultQrCodesAsync(location);

            Assert.Equal(5, qrCodes.Count);
            Assert.All(qrCodes, q => Assert.Equal(QrCodeStatus.Active, q.Status));
            Assert.All(qrCodes, q => Assert.Equal(32, q.Token.Length));
            Assert.Equal(
                new[]
                {
                    QrType.CounterCard,
                    QrType.PackagingSticker,
                    QrType.DeliveryInsert,
                    QrType.WindowSticker,
                    QrType.SmartGuest
                },
                qrCodes.Select(q => q.QrType).ToArray()
            );
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_TokensAreUnique()
        {
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

            Assert.Equal(5, await _context.QrCodes.CountAsync());
        }

        [Fact]
        public async Task MintDefaultQrCodesAsync_AcrossTwoLocations_AllTokensUnique()
        {
            var locationA = await CreateLocationAsync("Site A");
            var locationB = await CreateLocationAsync("Site B");

            var qrCodesA = await _service.MintDefaultQrCodesAsync(locationA);
            var qrCodesB = await _service.MintDefaultQrCodesAsync(locationB);

            var tokens = qrCodesA.Concat(qrCodesB).Select(q => q.Token).ToList();
            Assert.Equal(10, tokens.Count);
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
