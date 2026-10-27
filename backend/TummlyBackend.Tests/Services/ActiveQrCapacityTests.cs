using Microsoft.EntityFrameworkCore;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class ActiveQrCapacityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ActiveQrCapacity _capacity;
        private int _restaurantId;
        private int _locationId;

        public ActiveQrCapacityTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _capacity = new ActiveQrCapacity(
                _context,
                PricebookCatalog.LoadFromDirectory(TestPricebookPaths.PackDirectory())
            );

            SeedWorkspace();
        }

        [Fact]
        public async Task CountUsed_ActiveOnly_EqualsActiveCount()
        {
            await SeedQrAsync(QrType.CounterCard, QrCodeStatus.Active);
            await SeedQrAsync(QrType.TableTent, QrCodeStatus.Active);
            await SeedQrAsync(QrType.WindowSticker, QrCodeStatus.Paused);

            var used = await _capacity.CountUsedAsync(_locationId, CancellationToken.None);

            Assert.Equal(2, used);
        }

        [Fact]
        public async Task CountUsed_TwoHeldReservationsSameType_CountOnce()
        {
            await SeedReservationAsync(QrType.GuestCard, ShopQrSlotReservationStatuses.Open);
            await SeedReservationAsync(QrType.GuestCard, ShopQrSlotReservationStatuses.Open);

            var used = await _capacity.CountUsedAsync(_locationId, CancellationToken.None);

            Assert.Equal(1, used);
        }

        [Fact]
        public async Task CountUsed_HeldReservationWhereActiveCodeExists_NotCounted()
        {
            await SeedQrAsync(QrType.TableTent, QrCodeStatus.Active);
            await SeedReservationAsync(QrType.TableTent, ShopQrSlotReservationStatuses.Open);

            var used = await _capacity.CountUsedAsync(_locationId, CancellationToken.None);

            Assert.Equal(1, used);
        }

        [Fact]
        public async Task CountUsed_ReviewStatusCountsAsHeld()
        {
            await SeedReservationAsync(QrType.TableTalker, ShopQrSlotReservationStatuses.Review);

            var used = await _capacity.CountUsedAsync(_locationId, CancellationToken.None);

            Assert.Equal(1, used);
        }

        [Fact]
        public async Task CountUsed_ReleasedAndConverted_NotCounted()
        {
            await SeedReservationAsync(QrType.GuestCard, ShopQrSlotReservationStatuses.Released);
            await SeedReservationAsync(QrType.TableTent, ShopQrSlotReservationStatuses.Converted);

            var used = await _capacity.CountUsedAsync(_locationId, CancellationToken.None);

            Assert.Equal(0, used);
        }

        [Fact]
        public async Task EvaluateAsync_HeldReservationTakesSlot_NoLongerFitsAtCap()
        {
            var probe = await _capacity.EvaluateAsync(_restaurantId, _locationId, 1, CancellationToken.None);
            Assert.True(probe.EntitlementsAvailable);
            var cap = probe.PerLocationCap;
            Assert.True(cap >= 1);

            var beforeReservation = await _capacity.EvaluateAsync(_restaurantId, _locationId, cap, CancellationToken.None);
            Assert.True(beforeReservation.Fits);
            Assert.Equal(0, beforeReservation.Used);

            await SeedReservationAsync(QrType.GuestCard, ShopQrSlotReservationStatuses.Open);

            var afterReservation = await _capacity.EvaluateAsync(_restaurantId, _locationId, cap, CancellationToken.None);
            Assert.False(afterReservation.Fits);
            Assert.Equal(1, afterReservation.Used);
            Assert.Equal(cap, afterReservation.PerLocationCap);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedWorkspace()
        {
            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                0,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            BillingCreditsService.ApplyPilotSignupBilling(
                billing,
                DateTime.UtcNow
            );
            var restaurant = new Restaurant
            {
                Name = "Capacity Restaurant",
                AccountType = "Single",
                OwnerUserId = 0,
                CreatedAt = DateTime.UtcNow,
                BillingAccount = billing,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
                CaptureLocationStatus = CaptureLocationStatus.Active,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();

            _restaurantId = restaurant.Id;
            _locationId = location.Id;
        }

        private async Task SeedQrAsync(QrType qrType, QrCodeStatus status)
        {
            _context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = _locationId,
                QrType = qrType,
                Token = Guid.NewGuid().ToString("N"),
                Status = status,
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();
        }

        private async Task SeedReservationAsync(QrType qrType, string status)
        {
            var now = DateTime.UtcNow;
            _context.ShopQrSlotReservations.Add(new ShopQrSlotReservation
            {
                Id = Guid.NewGuid(),
                ShopOrderId = Guid.NewGuid(),
                RestaurantLocationId = _locationId,
                QrType = qrType,
                Status = status,
                ReconcileAfterUtc = now.AddMinutes(30),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            await _context.SaveChangesAsync();
        }
    }
}
