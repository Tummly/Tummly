using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TummlyBackend.Data;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class ShopOrderEmailNotifierTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly RecordingEmailService _email;
        private readonly ShopOrderEmailNotifier _notifier;
        private Guid _orderId;

        public ShopOrderEmailNotifierTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;

            _context = new ApplicationDbContext(options);
            _email = new RecordingEmailService();
            _notifier = new ShopOrderEmailNotifier(_context, _email);
            Seed();
        }

        [Fact]
        public async Task TryNotifyConfirmedAsync_SendsToPlacedByUser()
        {
            await _notifier.TryNotifyConfirmedAsync(_orderId);

            Assert.Single(_email.ConfirmedCalls);
            var call = _email.ConfirmedCalls[0];
            Assert.Equal("placer@example.com", call.ToEmail);
            Assert.Equal("Alex", call.FirstName);
            Assert.Equal("Camden", call.LocationName);
            Assert.Equal("ORD-9", call.OrderNumber);
            Assert.Contains("Table Tent QR × 2", call.MaterialsLinesHtml);
            Assert.Contains("12 High Street", call.DeliveryAddressHtml);
            Assert.Contains(
                "/multi-dashboard/shop?location=1&view=orders&shopOrderId=",
                call.OrderUrl
            );
        }

        [Fact]
        public async Task TryNotifyDispatchedAsync_IncludesEstimateAndTracking()
        {
            var order = await _context.ShopOrders.SingleAsync();
            order.TrackingUrl = "https://track.example/abc";
            order.DeliveryMethod = ShopDeliveryMethods.Express;
            await _context.SaveChangesAsync();

            await _notifier.TryNotifyDispatchedAsync(_orderId);

            Assert.Single(_email.DispatchedCalls);
            var call = _email.DispatchedCalls[0];
            Assert.Equal("Typically 1–2 working days", call.DeliveryEstimate);
            Assert.Equal("https://track.example/abc", call.TrackingDetails);
            Assert.Contains(
                "/multi-dashboard/shop?location=1&view=orders&shopOrderId=",
                call.OrderUrl
            );
        }

        [Fact]
        public void ShopOrderUrl_UsesAccountTypeRoot()
        {
            Assert.Equal(
                "/single-dashboard/shop?location=3&view=orders&shopOrderId=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                EmailFrontendUrls.ShopOrder(
                    "Single",
                    3,
                    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
                )
            );
            Assert.Equal(
                "/multi-dashboard/shop?location=3&view=orders&shopOrderId=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
                EmailFrontendUrls.ShopOrder(
                    "Multi",
                    3,
                    Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
                )
            );
        }

        private void Seed()
        {
            var owner = new User
            {
                Email = "owner@example.com",
                FullName = "Owner Person",
                PasswordHash = "x",
                Role = "Restaurant",
                AccountType = "Multi",
            };
            var placer = new User
            {
                Email = "placer@example.com",
                FullName = "Alex Placer",
                PasswordHash = "x",
                Role = "Restaurant",
                AccountType = "Multi",
            };
            _context.Users.AddRange(owner, placer);
            _context.SaveChanges();

            var restaurant = new Restaurant
            {
                Name = "Test Co",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
            };
            _context.Restaurants.Add(restaurant);
            _context.SaveChanges();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                Address = "12 High Street",
                City = "London",
                Postcode = "NW1 8AB",
                LifecycleStatus = LocationLifecycleStatus.Active,
            };
            _context.RestaurantLocations.Add(location);
            _context.SaveChanges();

            _orderId = Guid.NewGuid();
            _context.ShopOrders.Add(
                new ShopOrder
                {
                    Id = _orderId,
                    OrderNumber = "ORD-9",
                    RestaurantId = restaurant.Id,
                    LocationId = location.Id,
                    LocationNameSnapshot = "Camden",
                    PlacedByUserId = placer.Id,
                    PlacedByNameSnapshot = "Alex Placer",
                    MaterialsNetPence = 0,
                    VatPence = 0,
                    DeliveryNetPence = 0,
                    GrossPence = 0,
                    DeliveryMethod = ShopDeliveryMethods.Standard,
                    PaymentStatus = ShopPaymentStatuses.Paid,
                    FulfilmentStatus = ShopFulfilmentStatuses.Processing,
                    ShipToContactName = "Alex",
                    ShipToAddressLine1 = "12 High Street",
                    ShipToAddressLine2 = "Camden",
                    ShipToPostcode = "NW1 8AB",
                    ShipToCountry = "United Kingdom",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    Lines =
                    {
                        new ShopOrderLine
                        {
                            Id = Guid.NewGuid(),
                            ShopOrderId = _orderId,
                            CatalogSkuId = "table-tents",
                            TitleSnapshot = "Table Tent QR",
                            MaterialType = "table-tents",
                            Quantity = 2,
                            UnitNetPence = 0,
                            LineNetPence = 0,
                        },
                    },
                }
            );
            _context.SaveChanges();
        }

        public void Dispose() => _context.Dispose();

        private sealed class RecordingEmailService : EmailServiceStubBase
        {
            public List<(
                string ToEmail,
                string FirstName,
                string LocationName,
                string OrderNumber,
                string MaterialsLinesHtml,
                string DeliveryAddressHtml,
                string OrderUrl
            )> ConfirmedCalls { get; } = [];

            public List<(
                string ToEmail,
                string FirstName,
                string LocationName,
                string OrderNumber,
                string DeliveryEstimate,
                string TrackingDetails,
                string OrderUrl
            )> DispatchedCalls { get; } = [];

            public override Task SendShopOrderConfirmedEmailAsync(
                string toEmail,
                string firstName,
                string locationName,
                string orderNumber,
                string materialsLinesHtml,
                string deliveryAddressHtml,
                string orderUrl
            )
            {
                ConfirmedCalls.Add(
                    (
                        toEmail,
                        firstName,
                        locationName,
                        orderNumber,
                        materialsLinesHtml,
                        deliveryAddressHtml,
                        orderUrl
                    )
                );
                return Task.CompletedTask;
            }

            public override Task SendShopOrderDispatchedEmailAsync(
                string toEmail,
                string firstName,
                string locationName,
                string orderNumber,
                string deliveryEstimate,
                string trackingDetails,
                string orderUrl
            )
            {
                DispatchedCalls.Add(
                    (
                        toEmail,
                        firstName,
                        locationName,
                        orderNumber,
                        deliveryEstimate,
                        trackingDetails,
                        orderUrl
                    )
                );
                return Task.CompletedTask;
            }
        }
    }
}
