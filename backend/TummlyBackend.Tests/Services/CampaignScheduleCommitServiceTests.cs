using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ICampaignScheduleCommitService"/> — freeze + Billing
    /// Reserve + status (ticket 26).
    /// </summary>
    public class CampaignScheduleCommitServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CampaignEligibilityService _eligibility;
        private readonly RecordingBillingReserve _reserve;
        private readonly CampaignScheduleCommitService _commit;
        private readonly DateTime _now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

        public CampaignScheduleCommitServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _eligibility = new CampaignEligibilityService(_context);
            _reserve = new RecordingBillingReserve { IsLive = true };
            _commit = new CampaignScheduleCommitService(
                _context,
                _eligibility,
                _reserve,
                () => _now
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CommitAsync_HardBlocks_WhenBillingReserveIsNotLive()
        {
            var seeded = await SeedReviewReadyDraftAsync();
            _reserve.IsLive = false;

            var result = await _commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "send-now",
                    ScheduleTimeZone = "Europe/London",
                }
            );

            Assert.IsType<CampaignScheduleCommitResult.BillingReserveUnavailable>(
                result
            );
            Assert.Empty(_reserve.Calls);
            var campaign = await _context.Campaigns.SingleAsync(c => c.Id == seeded.CampaignId);
            Assert.Equal(CampaignScheduleCommitService.DraftStatus, campaign.Status);
        }

        [Fact]
        public async Task CommitAsync_SendNow_FreezesReservesAndSetsSending()
        {
            var seeded = await SeedReviewReadyDraftAsync(emailEligibleCount: 2);
            _reserve.NextReservationRef = "res-send-1";

            var result = await _commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "send-now",
                    ScheduleTimeZone = "Europe/London",
                }
            );

            var ok = Assert.IsType<CampaignScheduleCommitResult.Ok>(result);
            Assert.Equal(CampaignScheduleCommitService.SendingStatus, ok.Campaign.Status);
            Assert.Equal("send-now", ok.Campaign.ScheduleMode);
            Assert.Null(ok.Campaign.ScheduledAtUtc);
            Assert.Equal("Europe/London", ok.Campaign.ScheduleTimeZone);
            Assert.Equal("res-send-1", ok.Campaign.BillingReservationRef);
            Assert.Equal(2, ok.Campaign.ReservedEstimate);
            Assert.Equal(2, ok.Campaign.FrozenRecipientCount);

            Assert.Single(_reserve.Calls);
            Assert.Equal(2, _reserve.Calls[0].Units);
            Assert.Equal("email", _reserve.Calls[0].Channel);

            var frozen = await _context.CampaignFrozenRecipients
                .Where(row => row.CampaignId == seeded.CampaignId)
                .ToListAsync();
            Assert.Equal(2, frozen.Count);
        }

        [Fact]
        public async Task CommitAsync_ScheduleLater_RequiresFutureScheduledAtUtc()
        {
            var seeded = await SeedReviewReadyDraftAsync();

            var past = await _commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "schedule-later",
                    ScheduledAtUtc = _now,
                    ScheduleTimeZone = "Europe/London",
                }
            );
            Assert.IsType<CampaignScheduleCommitResult.InvalidSchedule>(past);

            var future = _now.AddHours(2);
            _reserve.NextReservationRef = "res-sched-1";
            var okResult = await _commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "schedule-later",
                    ScheduledAtUtc = future,
                    ScheduleTimeZone = "Europe/London",
                }
            );

            var ok = Assert.IsType<CampaignScheduleCommitResult.Ok>(okResult);
            Assert.Equal(CampaignScheduleCommitService.ScheduledStatus, ok.Campaign.Status);
            Assert.Equal(future, ok.Campaign.ScheduledAtUtc);
            Assert.Equal("schedule-later", ok.Campaign.ScheduleMode);
        }

        [Fact]
        public async Task CommitAsync_ReturnsReserveFailed_WhenBillingReserveRejects()
        {
            var seeded = await SeedReviewReadyDraftAsync();
            _reserve.FailNext = true;

            var result = await _commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "send-now",
                    ScheduleTimeZone = "Europe/London",
                }
            );

            Assert.IsType<CampaignScheduleCommitResult.ReserveFailed>(result);
            var campaign = await _context.Campaigns.SingleAsync(c => c.Id == seeded.CampaignId);
            Assert.Equal(CampaignScheduleCommitService.DraftStatus, campaign.Status);
            Assert.Empty(
                await _context.CampaignFrozenRecipients
                    .Where(row => row.CampaignId == seeded.CampaignId)
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task CommitAsync_Blocks_WhenZeroSelectedChannelEligible()
        {
            var seeded = await SeedReviewReadyDraftAsync(emailEligibleCount: 0);

            var result = await _commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "send-now",
                    ScheduleTimeZone = "Europe/London",
                }
            );

            Assert.IsType<CampaignScheduleCommitResult.ZeroEligible>(result);
            Assert.Empty(_reserve.Calls);
        }

        private async Task<(
            int CampaignId,
            byte[] RowVersion
        )> SeedReviewReadyDraftAsync(int emailEligibleCount = 1)
        {
            var user = new User
            {
                FullName = "Owner",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = _now,
                ActivatedAt = _now,
                ActivationExpiresAt = _now.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Commit Test Restaurant",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            for (var i = 0; i < emailEligibleCount; i++)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = $"guest{i}@example.com",
                    CreatedAt = _now,
                };
                _context.MasterGuests.Add(master);
                await _context.SaveChangesAsync();

                _context.LocationGuests.Add(
                    new LocationGuest
                    {
                        RestaurantLocationId = location.Id,
                        MasterGuestId = master.Id,
                        OffersOptOut = false,
                        CreatedAt = _now,
                    }
                );
            }

            var optedMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "opted@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(optedMaster);
            await _context.SaveChangesAsync();
            _context.LocationGuests.Add(
                new LocationGuest
                {
                    RestaurantLocationId = location.Id,
                    MasterGuestId = optedMaster.Id,
                    OffersOptOut = true,
                    CreatedAt = _now,
                }
            );
            await _context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = CampaignScheduleCommitService.DraftStatus,
                Name = "Thank recent guests",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return (campaign.Id, campaign.RowVersion);
        }

        private sealed class RecordingBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive { get; set; }

            public bool FailNext { get; set; }

            public string NextReservationRef { get; set; } = "res-default";

            public List<(
                int CampaignId,
                string Channel,
                int Units,
                int LocationId
            )> Calls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(
                    (
                        request.CampaignId,
                        request.Channel,
                        request.Units,
                        request.LocationId
                    )
                );

                if (FailNext)
                {
                    return Task.FromResult<CampaignBillingReserveResult>(
                        new CampaignBillingReserveResult.Failed
                        {
                            Message = "Insufficient credits.",
                        }
                    );
                }

                return Task.FromResult<CampaignBillingReserveResult>(
                    new CampaignBillingReserveResult.Ok
                    {
                        ReservationRef = NextReservationRef,
                    }
                );
            }
        }
    }
}
