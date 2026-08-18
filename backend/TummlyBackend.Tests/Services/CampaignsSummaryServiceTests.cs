using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ICampaignsSummaryService"/> — KPI in-flight membership
    /// (Scheduled + Sending, not Paused) and accepted messages in window.
    /// </summary>
    public class CampaignsSummaryServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CampaignsSummaryService _summary;

        public CampaignsSummaryServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _summary = new CampaignsSummaryService(_context);
        }

        [Fact]
        public async Task GetSummaryAsync_InFlight_CountsScheduledAndSending_ExcludesPaused()
        {
            var (locationId, _) = await SeedLocationAsync();
            await AddCampaignAsync(locationId, CampaignsListService.ScheduledStatus);
            await AddCampaignAsync(locationId, CampaignsListService.ScheduledStatus);
            await AddCampaignAsync(locationId, CampaignsListService.SendingStatus);
            await AddCampaignAsync(locationId, CampaignsListService.PausedStatus);
            await AddCampaignAsync(locationId, CampaignsListService.DraftStatus);

            var result = await _summary.GetSummaryAsync(
                new CampaignsSummaryQuery { LocationId = locationId }
            );

            Assert.Equal(2, result.CampaignsInFlightScheduled);
            Assert.Equal(1, result.CampaignsInFlightSending);
            // KPI in-flight = 3; list In flight tab would be 4 (includes Paused).
            Assert.Equal(
                3,
                result.CampaignsInFlightScheduled + result.CampaignsInFlightSending
            );
        }

        [Fact]
        public async Task GetSummaryAsync_MessagesSent_CountsAcceptedEmailInWindowOnly()
        {
            var (locationId, restaurantId) = await SeedLocationAsync();
            var campaignId = await AddCampaignAsync(
                locationId,
                CampaignsListService.SentStatus
            );
            var guestIn = await AddGuestAsync(locationId, restaurantId);
            var guestOut = await AddGuestAsync(locationId, restaurantId);
            var guestSms = await AddGuestAsync(locationId, restaurantId);

            var windowStart = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var windowEnd = new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

            _context.CampaignRecipientDeliveries.AddRange(
                new CampaignRecipientDelivery
                {
                    CampaignId = campaignId,
                    LocationGuestId = guestIn,
                    Channel = "email",
                    Outcome = CampaignFireService.AcceptedOutcome,
                    AcceptedAtUtc = windowStart.AddDays(1),
                    UpdatedAtUtc = windowStart.AddDays(1),
                },
                new CampaignRecipientDelivery
                {
                    CampaignId = campaignId,
                    LocationGuestId = guestOut,
                    Channel = "email",
                    Outcome = CampaignFireService.AcceptedOutcome,
                    AcceptedAtUtc = windowEnd.AddDays(1),
                    UpdatedAtUtc = windowEnd.AddDays(1),
                },
                new CampaignRecipientDelivery
                {
                    CampaignId = campaignId,
                    LocationGuestId = guestSms,
                    Channel = "sms",
                    Outcome = CampaignFireService.AcceptedOutcome,
                    AcceptedAtUtc = windowStart.AddDays(2),
                    UpdatedAtUtc = windowStart.AddDays(2),
                }
            );
            await _context.SaveChangesAsync();

            var result = await _summary.GetSummaryAsync(
                new CampaignsSummaryQuery
                {
                    LocationId = locationId,
                    OverviewDateFrom = windowStart,
                    OverviewDateTo = windowEnd,
                }
            );

            Assert.Equal(1, result.MessagesSentAccepted);
            Assert.Equal(1, result.MessagesSentAcceptedEmail);
        }

        [Fact]
        public async Task GetSummaryAsync_InFlight_IgnoresOverviewDateWindow()
        {
            var (locationId, _) = await SeedLocationAsync();
            await AddCampaignAsync(locationId, CampaignsListService.ScheduledStatus);
            await AddCampaignAsync(locationId, CampaignsListService.SendingStatus);

            var narrowWindowStart = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var narrowWindowEnd = new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            var result = await _summary.GetSummaryAsync(
                new CampaignsSummaryQuery
                {
                    LocationId = locationId,
                    OverviewDateFrom = narrowWindowStart,
                    OverviewDateTo = narrowWindowEnd,
                }
            );

            Assert.Equal(1, result.CampaignsInFlightScheduled);
            Assert.Equal(1, result.CampaignsInFlightSending);
            Assert.Equal(0, result.MessagesSentAccepted);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<(int LocationId, int RestaurantId)> SeedLocationAsync()
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return (location.Id, restaurant.Id);
        }

        private async Task<int> AddCampaignAsync(int locationId, string status)
        {
            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Status = status,
                Name = $"Campaign {Guid.NewGuid():N}",
                Channel = "email",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign.Id;
        }

        private async Task<int> AddGuestAsync(int locationId, int restaurantId)
        {
            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = $"{Guid.NewGuid():N}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = "Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();
            return guest.Id;
        }
    }
}
