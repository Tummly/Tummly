using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Helpers
{
    public class WeeklyBriefRecommendedActionsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public WeeklyBriefRecommendedActionsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
        }

        public void Dispose()
            => _context.Dispose();

        [Fact]
        public async Task FindWorstUnderperformQrAsync_FlagsCounterBelowHalfOfPeer()
        {
            var locationId = await SeedLocationAsync();
            var tableTent = await SeedQrAsync(
                locationId,
                QrType.TableTent,
                token: "table-tent-underperform-01"
            );
            var counter = await SeedQrAsync(
                locationId,
                QrType.CounterCard,
                token: "counter-underperform-01"
            );
            var from = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(7);
            await SeedScansAsync(locationId, tableTent, count: 12, from);
            await SeedScansAsync(locationId, counter, count: 1, from);

            var fact =
                await WeeklyBriefRecommendedActions.FindWorstUnderperformQrAsync(
                    _context,
                    locationId,
                    from,
                    to,
                    CancellationToken.None
                );

            Assert.NotNull(fact);
            Assert.Equal("underperform-qr", fact!.Kind);
            Assert.Equal(counter, fact.QrCodeId);
            Assert.Equal("Counter card", fact.PlacementLabel);
            Assert.Equal(1, fact.Scans);
            Assert.Equal("capture", fact.Target);
        }

        [Fact]
        public async Task FindWorstUnderperformQrAsync_SkipsWhenPeerScansBelowMin()
        {
            var locationId = await SeedLocationAsync();
            var tableTent = await SeedQrAsync(
                locationId,
                QrType.TableTent,
                token: "table-tent-thin-01"
            );
            var counter = await SeedQrAsync(
                locationId,
                QrType.CounterCard,
                token: "counter-thin-01"
            );
            var from = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(7);
            await SeedScansAsync(locationId, tableTent, count: 4, from);
            await SeedScansAsync(locationId, counter, count: 0, from);

            var fact =
                await WeeklyBriefRecommendedActions.FindWorstUnderperformQrAsync(
                    _context,
                    locationId,
                    from,
                    to,
                    CancellationToken.None
                );

            Assert.Null(fact);
        }

        [Fact]
        public async Task FindWorstUnderperformQrAsync_IgnoresSmartGuest()
        {
            var locationId = await SeedLocationAsync();
            await SeedQrAsync(
                locationId,
                QrType.SmartGuest,
                token: "smart-guest-ignore-01"
            );
            var counter = await SeedQrAsync(
                locationId,
                QrType.CounterCard,
                token: "counter-alone-01"
            );
            var from = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(7);
            await SeedScansAsync(locationId, counter, count: 0, from);

            var fact =
                await WeeklyBriefRecommendedActions.FindWorstUnderperformQrAsync(
                    _context,
                    locationId,
                    from,
                    to,
                    CancellationToken.None
                );

            Assert.Null(fact);
        }

        [Fact]
        public async Task BuildFactsAsync_IncludesUnderperformQrBeforeOfferFacts()
        {
            var locationId = await SeedLocationAsync();
            var tableTent = await SeedQrAsync(
                locationId,
                QrType.TableTent,
                token: "table-tent-build-01"
            );
            var counter = await SeedQrAsync(
                locationId,
                QrType.CounterCard,
                token: "counter-build-01"
            );
            var from = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = from.AddDays(7);
            await SeedScansAsync(locationId, tableTent, count: 10, from);
            await SeedScansAsync(locationId, counter, count: 0, from);

            var facts = await WeeklyBriefRecommendedActions.BuildFactsAsync(
                _context,
                locationId,
                EmptyMetrics(),
                from,
                to,
                CancellationToken.None
            );

            var underperform =
                Assert.Single(
                    facts.OfType<WeeklyBriefRecommendedActions.UnderperformQrFactDto>()
                );
            Assert.Equal(counter, underperform.QrCodeId);
        }

        private async Task<int> SeedLocationAsync()
        {
            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Capture Cafe",
                OwnerUserId = 1,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedQrAsync(
            int locationId,
            QrType qrType,
            string token
        )
        {
            var qr = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = qrType,
                Token = token,
                Status = QrCodeStatus.Active,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();
            return qr.Id;
        }

        private async Task SeedScansAsync(
            int locationId,
            int qrCodeId,
            int count,
            DateTime fromUtc
        )
        {
            for (var i = 0; i < count; i++)
            {
                _context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qrCodeId,
                        CreatedAt = fromUtc.AddHours(i + 1),
                    }
                );
            }

            await _context.SaveChangesAsync();
        }

        private static WeeklyBriefMetrics EmptyMetrics()
            => new(
                GuestsJoined: 0,
                QrScanEvents: 0,
                FeedbackCount: 0,
                PositiveFeedbackCount: 0,
                NeutralFeedbackCount: 0,
                NegativeFeedbackCount: 0,
                NeedsAttentionCount: 0,
                DetectedTagCounts: new Dictionary<string, int>(),
                ActiveOffers: 0,
                ClaimsInWeek: 0,
                RedemptionsInWeek: 0,
                CampaignsSentInWeek: 0,
                CampaignRecipientsReached: 0,
                UnsubscribesInWeek: 0
            );
    }
}
