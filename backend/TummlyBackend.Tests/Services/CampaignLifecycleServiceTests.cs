using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ICampaignLifecycleService"/> — list lifecycle transitions
    /// (ticket 30).
    /// </summary>
    public class CampaignLifecycleServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CampaignEligibilityService _eligibility;
        private readonly RecordingBillingReserve _reserve;
        private readonly CampaignLifecycleService _lifecycle;
        private readonly DateTime _now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

        public CampaignLifecycleServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _eligibility = new CampaignEligibilityService(_context);
            _reserve = new RecordingBillingReserve { IsLive = true };
            _lifecycle = new CampaignLifecycleService(
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
        public async Task UnscheduleAsync_Scheduled_ClearsFreezeAndReturnsDraft()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.ScheduledStatus,
                reservationRef: "res-sched"
            );

            var result = await _lifecycle.UnscheduleAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = seeded.RowVersion,
                }
            );

            var ok = Assert.IsType<CampaignLifecycleResult.Ok>(result);
            Assert.Equal(CampaignLifecycleService.DraftStatus, ok.Campaign.Status);
            Assert.Null(ok.Campaign.BillingReservationRef);
            Assert.Null(ok.Campaign.ScheduleMode);
            Assert.Equal(0, ok.Campaign.FrozenRecipientCount);
            Assert.Single(_reserve.ReleaseCalls);
            Assert.Empty(
                await _context.CampaignFrozenRecipients
                    .Where(row => row.CampaignId == seeded.CampaignId)
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task UnscheduleAsync_RejectsNonScheduled()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.SendingStatus
            );

            var result = await _lifecycle.UnscheduleAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = seeded.RowVersion,
                }
            );

            Assert.IsType<CampaignLifecycleResult.InvalidStatus>(result);
        }

        [Fact]
        public async Task PauseAsync_FromScheduledAndSending_Only()
        {
            var scheduled = await SeedCommittedAsync(
                CampaignLifecycleService.ScheduledStatus
            );
            var pauseScheduled = await _lifecycle.PauseAsync(
                scheduled.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = scheduled.RowVersion,
                }
            );
            var pausedOk = Assert.IsType<CampaignLifecycleResult.Ok>(pauseScheduled);
            Assert.Equal(
                CampaignLifecycleService.PausedStatus,
                pausedOk.Campaign.Status
            );
            Assert.Null(pausedOk.Campaign.BillingReservationRef);
            Assert.True(
                await _context.CampaignFrozenRecipients.AnyAsync(row =>
                    row.CampaignId == scheduled.CampaignId
                )
            );

            var sending = await SeedCommittedAsync(
                CampaignLifecycleService.SendingStatus,
                emailEligibleCount: 2
            );
            var pauseSending = await _lifecycle.PauseAsync(
                sending.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = sending.RowVersion,
                }
            );
            Assert.IsType<CampaignLifecycleResult.Ok>(pauseSending);

            var partial = await SeedCommittedAsync(
                CampaignLifecycleService.PartiallySentStatus
            );
            var pausePartial = await _lifecycle.PauseAsync(
                partial.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = partial.RowVersion,
                }
            );
            Assert.IsType<CampaignLifecycleResult.InvalidStatus>(pausePartial);
        }

        [Fact]
        public async Task CancelAsync_SendingWithAccept_GoesPartiallySent()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.SendingStatus,
                emailEligibleCount: 2
            );
            var accepted = await _context.CampaignFrozenRecipients
                .Where(row => row.CampaignId == seeded.CampaignId)
                .OrderBy(row => row.Id)
                .FirstAsync();
            accepted.AcceptedAtUtc = _now.AddMinutes(-1);
            await _context.SaveChangesAsync();

            var campaign = await _context.Campaigns.SingleAsync(c =>
                c.Id == seeded.CampaignId
            );

            var result = await _lifecycle.CancelAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = campaign.RowVersion,
                }
            );

            var ok = Assert.IsType<CampaignLifecycleResult.Ok>(result);
            Assert.Equal(
                CampaignLifecycleService.PartiallySentStatus,
                ok.Campaign.Status
            );
            Assert.True(ok.Campaign.FrozenRecipientCount >= 1);
        }

        [Fact]
        public async Task CancelAsync_SendingWithoutAccept_GoesCancelled()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.SendingStatus
            );

            var result = await _lifecycle.CancelAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = seeded.RowVersion,
                }
            );

            var ok = Assert.IsType<CampaignLifecycleResult.Ok>(result);
            Assert.Equal(
                CampaignLifecycleService.CancelledStatus,
                ok.Campaign.Status
            );
            Assert.Equal(0, ok.Campaign.FrozenRecipientCount);
        }

        [Fact]
        public async Task ResumeAsync_RevalidatesDropOnly_AndReReserves()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.PausedStatus,
                emailEligibleCount: 2,
                reservationRef: null
            );

            // Mark one frozen guest opted out so resume drops them.
            var frozen = await _context.CampaignFrozenRecipients
                .Where(row => row.CampaignId == seeded.CampaignId)
                .OrderBy(row => row.LocationGuestId)
                .ToListAsync();
            Assert.Equal(2, frozen.Count);
            var dropGuest = await _context.LocationGuests.SingleAsync(g =>
                g.Id == frozen[0].LocationGuestId
            );
            dropGuest.MarketingPreference = LocationGuestMarketingPreference.OptedOut;
            await _context.SaveChangesAsync();

            var campaign = await _context.Campaigns.SingleAsync(c =>
                c.Id == seeded.CampaignId
            );
            campaign.ScheduleMode = CampaignLifecycleService.SendNowMode;
            await _context.SaveChangesAsync();

            _reserve.NextReservationRef = "res-resume";
            var result = await _lifecycle.ResumeAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = campaign.RowVersion,
                }
            );

            var ok = Assert.IsType<CampaignLifecycleResult.Ok>(result);
            Assert.Equal(CampaignLifecycleService.SendingStatus, ok.Campaign.Status);
            Assert.Equal(1, ok.Campaign.FrozenRecipientCount);
            Assert.Equal("res-resume", ok.Campaign.BillingReservationRef);
            Assert.Single(_reserve.ReserveCalls);
            Assert.Equal(1, _reserve.ReserveCalls[0].Units);

            var remainingIds = await _context.CampaignFrozenRecipients
                .Where(row => row.CampaignId == seeded.CampaignId)
                .Select(row => row.LocationGuestId)
                .ToListAsync();
            Assert.Single(remainingIds);
            Assert.DoesNotContain(dropGuest.Id, remainingIds);
        }

        [Fact]
        public async Task ResumeAsync_NeverSilentAddsNewlyMatchedGuests()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.PausedStatus,
                emailEligibleCount: 1,
                reservationRef: null
            );
            var campaign = await _context.Campaigns.SingleAsync(c =>
                c.Id == seeded.CampaignId
            );

            // Add a new eligible guest after freeze — must not join on resume.
            var master = new MasterGuest
            {
                RestaurantId = (
                    await _context.RestaurantLocations.SingleAsync(l =>
                        l.Id == campaign.RestaurantLocationId
                    )
                ).RestaurantId,
                Email = "new-after-freeze@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();
            _context.LocationGuests.Add(
                new LocationGuest
                {
                    RestaurantLocationId = campaign.RestaurantLocationId,
                    MasterGuestId = master.Id,
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = _now,
                }
            );
            await _context.SaveChangesAsync();

            _reserve.NextReservationRef = "res-no-add";
            var result = await _lifecycle.ResumeAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = campaign.RowVersion,
                }
            );

            var ok = Assert.IsType<CampaignLifecycleResult.Ok>(result);
            Assert.Equal(1, ok.Campaign.FrozenRecipientCount);
            Assert.Equal(1, _reserve.ReserveCalls[0].Units);
        }

        [Fact]
        public async Task RetryRemainingAsync_OnlyFromPartiallySent()
        {
            var paused = await SeedCommittedAsync(
                CampaignLifecycleService.PausedStatus,
                reservationRef: null
            );
            var reject = await _lifecycle.RetryRemainingAsync(
                paused.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = paused.RowVersion,
                }
            );
            Assert.IsType<CampaignLifecycleResult.InvalidStatus>(reject);

            var partial = await SeedCommittedAsync(
                CampaignLifecycleService.PartiallySentStatus,
                emailEligibleCount: 2,
                reservationRef: null
            );
            var accepted = await _context.CampaignFrozenRecipients
                .Where(row => row.CampaignId == partial.CampaignId)
                .OrderBy(row => row.Id)
                .FirstAsync();
            accepted.AcceptedAtUtc = _now.AddMinutes(-5);
            await _context.SaveChangesAsync();
            var campaign = await _context.Campaigns.SingleAsync(c =>
                c.Id == partial.CampaignId
            );

            _reserve.NextReservationRef = "res-retry";
            var okResult = await _lifecycle.RetryRemainingAsync(
                partial.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = campaign.RowVersion,
                }
            );
            var ok = Assert.IsType<CampaignLifecycleResult.Ok>(okResult);
            Assert.Equal(CampaignLifecycleService.SendingStatus, ok.Campaign.Status);
            Assert.Equal(1, ok.Campaign.ReservedEstimate);
        }

        [Fact]
        public async Task DuplicateAsDraftAsync_FromFailed_CopiesContent()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.FailedStatus,
                reservationRef: null
            );
            var source = await _context.Campaigns.SingleAsync(c =>
                c.Id == seeded.CampaignId
            );
            source.MessageBody = "Recover this";
            source.MessageSubject = "Subject";
            await _context.SaveChangesAsync();

            var result = await _lifecycle.DuplicateAsDraftAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = source.RowVersion,
                }
            );

            var duplicated = Assert.IsType<CampaignLifecycleResult.Duplicated>(
                result
            );
            Assert.Equal(CampaignLifecycleService.DraftStatus, duplicated.Campaign.Status);
            Assert.Equal("Lifecycle campaign - Draft", duplicated.Campaign.Name);
            Assert.Equal("Recover this", duplicated.Campaign.MessageBody);
            Assert.NotEqual(seeded.CampaignId, duplicated.Campaign.Id);
            Assert.Equal(
                CampaignLifecycleService.FailedStatus,
                (
                    await _context.Campaigns.SingleAsync(c => c.Id == seeded.CampaignId)
                ).Status
            );
        }

        [Fact]
        public async Task DuplicateAsDraftAsync_RejectsSent()
        {
            var seeded = await SeedCommittedAsync(
                CampaignLifecycleService.SentStatus,
                reservationRef: null
            );

            var result = await _lifecycle.DuplicateAsDraftAsync(
                seeded.CampaignId,
                new CampaignLifecycleActionRequest
                {
                    RowVersion = seeded.RowVersion,
                }
            );

            Assert.IsType<CampaignLifecycleResult.InvalidStatus>(result);
        }

        [Fact]
        public void BuildDuplicateName_AppendsDraftSuffix()
        {
            Assert.Equal(
                "Weekend SMS blast - Draft",
                CampaignLifecycleService.BuildDuplicateName("Weekend SMS blast")
            );
        }

        [Fact]
        public void BuildDuplicateName_TruncatesToMax()
        {
            var original = new string('a', CampaignLifecycleService.NameMaxLength);
            var copy = CampaignLifecycleService.BuildDuplicateName(original);
            Assert.Equal(CampaignLifecycleService.NameMaxLength, copy.Length);
            Assert.EndsWith(CampaignLifecycleService.DuplicateNameSuffix, copy);
        }

        private async Task<(
            int CampaignId,
            byte[] RowVersion,
            int LocationId
        )> SeedCommittedAsync(
            string status,
            int emailEligibleCount = 1,
            string? reservationRef = "res-seed"
        )
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
                Name = "Lifecycle Test Restaurant",
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

            var guestIds = new List<int>();
            for (var i = 0; i < emailEligibleCount; i++)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = $"guest{i}-{Guid.NewGuid():N}@example.com",
                    CreatedAt = _now,
                };
                _context.MasterGuests.Add(master);
                await _context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    RestaurantLocationId = location.Id,
                    MasterGuestId = master.Id,
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = _now,
                };
                _context.LocationGuests.Add(locationGuest);
                await _context.SaveChangesAsync();
                guestIds.Add(locationGuest.Id);
            }

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = status,
                Name = "Lifecycle campaign",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                ScheduleMode =
                    status == CampaignLifecycleService.ScheduledStatus
                        ? CampaignLifecycleService.ScheduleLaterMode
                        : CampaignLifecycleService.SendNowMode,
                ScheduledAtUtc =
                    status == CampaignLifecycleService.ScheduledStatus
                        ? _now.AddHours(2)
                        : null,
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = reservationRef,
                ReservedEstimate = reservationRef == null ? null : guestIds.Count,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            foreach (var guestId in guestIds)
            {
                _context.CampaignFrozenRecipients.Add(
                    new CampaignFrozenRecipient
                    {
                        CampaignId = campaign.Id,
                        LocationGuestId = guestId,
                        FrozenAtUtc = _now,
                    }
                );
            }
            await _context.SaveChangesAsync();

            return (campaign.Id, campaign.RowVersion, location.Id);
        }

        private sealed class RecordingBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive { get; set; }

            public string NextReservationRef { get; set; } = "res-default";

            public List<(
                int CampaignId,
                string Channel,
                int Units,
                int LocationId
            )> ReserveCalls { get; } = [];

            public List<(int CampaignId, string ReservationRef)> ReleaseCalls
            {
                get;
            } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReserveCalls.Add(
                    (
                        request.CampaignId,
                        request.Channel,
                        request.Units,
                        request.LocationId
                    )
                );

                return Task.FromResult<CampaignBillingReserveResult>(
                    new CampaignBillingReserveResult.Ok
                    {
                        ReservationRef = NextReservationRef,
                    }
                );
            }

            public Task<CampaignBillingSettleResult> SettleAsync(
                CampaignBillingSettleRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<CampaignBillingSettleResult>(
                    new CampaignBillingSettleResult.Ok()
                );
            }

            public Task<CampaignBillingReleaseResult> ReleaseAsync(
                CampaignBillingReleaseRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReleaseCalls.Add((request.CampaignId, request.ReservationRef));
                return Task.FromResult<CampaignBillingReleaseResult>(
                    new CampaignBillingReleaseResult.Ok()
                );
            }
        }
    }
}
