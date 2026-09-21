using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using TummlyBackend.Billing.Pricebook;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Locations;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class OwnedLocationInsertComplimentaryStarterTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OwnedLocationInsertService _insert;
        private int _userId;
        private int _restaurantId;

        public OwnedLocationInsertComplimentaryStarterTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);

            var pricebook = PricebookCatalog.LoadFromDirectory(ResolveBillingPackDir());
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://tummly.example",
                    }
                )
                .Build();
            var smartGuestLink = new SmartGuestLinkService(
                _context,
                configuration,
                new NoOpBillingAccountLifecycle()
            );

            _insert = new OwnedLocationInsertService(
                _context,
                pricebook,
                new QrCodeProvisioningService(_context, smartGuestLink)
            );

            SeedRestaurantWithEntitledPlan();
        }

        [Fact]
        public async Task AddAsync_DraftLocation_DoesNotCreateComplimentaryOrder()
        {
            var result = await _insert.AddAsync(
                _restaurantId,
                _userId,
                new AddOwnedLocationRequest
                {
                    LocationName = "Branch",
                    Address = "2 High Street",
                    City = "Leeds",
                    Postcode = "LS1 2AB",
                }
            );

            Assert.IsType<AddOwnedLocationResult.Created>(result);
            Assert.Equal(
                0,
                await _context.ShopOrders.CountAsync(o => o.IsComplimentary)
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedRestaurantWithEntitledPlan()
        {
            var user = new User
            {
                FullName = "Operator One",
                Email = "op-insert-kit@example.com",
                PasswordHash = "x",
                CreatedAt = DateTime.UtcNow,
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            _userId = user.Id;

            var restaurant = new Restaurant
            {
                Name = "Insert Kit Restaurant",
                AccountType = "Multi",
                OwnerUserId = _userId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();
            _restaurantId = restaurant.Id;

            _context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            _context.SaveChanges();
        }

        private static string ResolveBillingPackDir()
        {
            var packDir = Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "docs",
                    "product",
                    "billing-pack-v3.0"
                )
            );
            if (!Directory.Exists(packDir))
            {
                packDir = Path.GetFullPath(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "..",
                        "..",
                        "..",
                        "..",
                        "docs",
                        "product",
                        "billing-pack-v3.0"
                    )
                );
            }

            return packDir;
        }
    }
}
