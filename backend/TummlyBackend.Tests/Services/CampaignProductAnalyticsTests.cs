using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ICampaignProductAnalytics"/> — minimal send events
    /// (ticket 32).
    /// </summary>
    public class CampaignProductAnalyticsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CampaignEligibilityService _eligibility;
        private readonly RecordingBillingReserve _reserve;
        private readonly RecordingCampaignProductAnalytics _analytics;
        private readonly DateTime _now = new(2026, 8, 9, 15, 0, 0, DateTimeKind.Utc);

        public CampaignProductAnalyticsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _eligibility = new CampaignEligibilityService(_context);
            _reserve = new RecordingBillingReserve { IsLive = true };
            _analytics = new RecordingCampaignProductAnalytics();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task CommitAsync_SendNow_EmitsScheduleCommitWithMode()
        {
            var seeded = await SeedReviewReadyDraftAsync();
            var commit = CreateCommitService();

            var result = await commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "send-now",
                    ScheduleTimeZone = "Europe/London",
                }
            );

            Assert.IsType<CampaignScheduleCommitResult.Ok>(result);
            Assert.Single(_analytics.ScheduleCommits);
            Assert.Equal(seeded.CampaignId, _analytics.ScheduleCommits[0].CampaignId);
            Assert.Equal("send-now", _analytics.ScheduleCommits[0].Mode);
        }

        [Fact]
        public async Task CommitAsync_ScheduleLater_EmitsScheduleCommitWithMode()
        {
            var seeded = await SeedReviewReadyDraftAsync();
            var commit = CreateCommitService();

            var result = await commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "schedule-later",
                    ScheduledAtUtc = _now.AddHours(2),
                    ScheduleTimeZone = "Europe/London",
                }
            );

            Assert.IsType<CampaignScheduleCommitResult.Ok>(result);
            Assert.Single(_analytics.ScheduleCommits);
            Assert.Equal(seeded.CampaignId, _analytics.ScheduleCommits[0].CampaignId);
            Assert.Equal(
                "schedule-later",
                _analytics.ScheduleCommits[0].Mode
            );
        }

        [Fact]
        public async Task CommitAsync_ReserveFailed_DoesNotEmitScheduleCommit()
        {
            var seeded = await SeedReviewReadyDraftAsync();
            _reserve.FailNext = true;
            var commit = CreateCommitService();

            var result = await commit.CommitAsync(
                seeded.CampaignId,
                new CommitCampaignScheduleRequest
                {
                    RowVersion = seeded.RowVersion,
                    ScheduleMode = "send-now",
                    ScheduleTimeZone = "Europe/London",
                }
            );

            Assert.IsType<CampaignScheduleCommitResult.ReserveFailed>(result);
            Assert.Empty(_analytics.ScheduleCommits);
        }

        [Fact]
        public async Task FireAsync_Success_EmitsSendStartAndTerminalSent()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 1);
            var fire = CreateFireService();

            var result = await fire.FireAsync(seeded.CampaignId);

            Assert.IsType<CampaignFireResult.Ok>(result);
            Assert.Single(_analytics.SendStarts);
            Assert.Equal(seeded.CampaignId, _analytics.SendStarts[0]);
            Assert.Single(_analytics.SendTerminals);
            Assert.Equal(seeded.CampaignId, _analytics.SendTerminals[0].CampaignId);
            Assert.Equal(
                CampaignFireService.SentStatus,
                _analytics.SendTerminals[0].Status
            );
        }

        [Fact]
        public async Task FireAsync_MidSendStop_EmitsSendStartAndTerminalPartiallySent()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 3);
            var outbound = new RecordingOutboundSender();
            using var cts = new CancellationTokenSource();
            outbound.OnAfterAccept = acceptedSoFar =>
            {
                if (acceptedSoFar >= 1)
                {
                    cts.Cancel();
                }
            };
            var fire = CreateFireService(outbound);

            var result = await fire.FireAsync(seeded.CampaignId, cts.Token);

            var ok = Assert.IsType<CampaignFireResult.Ok>(result);
            Assert.Equal(
                CampaignFireService.PartiallySentStatus,
                ok.Campaign.Status
            );
            Assert.Single(_analytics.SendStarts);
            Assert.Single(_analytics.SendTerminals);
            Assert.Equal(
                CampaignFireService.PartiallySentStatus,
                _analytics.SendTerminals[0].Status
            );
        }

        [Fact]
        public async Task FireAsync_CannotStart_EmitsTerminalFailedWithoutSendStart()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 1);
            var guest = await _context.LocationGuests.SingleAsync(
                lg => lg.Id == seeded.FrozenGuestIds[0]
            );
            guest.MarketingPreference = LocationGuestMarketingPreference.OptedOut;
            await _context.SaveChangesAsync();
            var fire = CreateFireService();

            var result = await fire.FireAsync(seeded.CampaignId);

            Assert.IsType<CampaignFireResult.CannotStart>(result);
            Assert.Empty(_analytics.SendStarts);
            Assert.Single(_analytics.SendTerminals);
            Assert.Equal(
                CampaignFireService.FailedStatus,
                _analytics.SendTerminals[0].Status
            );
            Assert.Equal(seeded.CampaignId, _analytics.SendTerminals[0].CampaignId);
        }

        [Fact]
        public async Task SendAsync_Success_EmitsSendTest()
        {
            var locationId = await SeedLocationAsync();
            var email = new TrackingGuestResponseEmailService();
            var service = new CampaignSendTestService(
                _context,
                email,
                _analytics
            );

            var result = await service.SendAsync(
                locationId,
                toEmail: "team@example.com",
                subject: "Thanks",
                body: "Hello guest"
            );

            Assert.True(result);
            Assert.Single(_analytics.SendTests);
            Assert.Equal(locationId, _analytics.SendTests[0]);
        }

        private CampaignScheduleCommitService CreateCommitService()
        {
            return new CampaignScheduleCommitService(
                _context,
                _eligibility,
                _reserve,
                new FixedCreditBalanceSnapshot(remaining: 1000),
                new NoOpCampaignFireWork(),
                _analytics,
                utcNow: () => _now
            );
        }

        private CampaignFireService CreateFireService(
            RecordingOutboundSender? outbound = null
        )
        {
            return new CampaignFireService(
                _context,
                _eligibility,
                _reserve,
                outbound ?? new RecordingOutboundSender(),
                new ControllableSendStartGate(),
                _analytics,
                utcNow: () => _now
            );
        }

        private async Task<(
            int CampaignId,
            byte[] RowVersion
        )> SeedReviewReadyDraftAsync()
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
                Name = "Analytics Commit Restaurant",
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

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "guest@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();
            _context.LocationGuests.Add(
                new LocationGuest
                {
                    RestaurantLocationId = location.Id,
                    MasterGuestId = master.Id,
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
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

        private async Task<(
            int CampaignId,
            IReadOnlyList<int> FrozenGuestIds
        )> SeedSendingCampaignAsync(int frozenEligibleCount)
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
                Name = "Analytics Fire Restaurant",
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

            var frozenIds = new List<int>();
            for (var i = 0; i < frozenEligibleCount; i++)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = $"guest{i}@example.com",
                    CreatedAt = _now,
                };
                _context.MasterGuests.Add(master);
                await _context.SaveChangesAsync();
                var guest = new LocationGuest
                {
                    RestaurantLocationId = location.Id,
                    MasterGuestId = master.Id,
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = _now,
                };
                _context.LocationGuests.Add(guest);
                await _context.SaveChangesAsync();
                frozenIds.Add(guest.Id);
            }

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = CampaignFireService.SendingStatus,
                Name = "Send now",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                ScheduleMode = "send-now",
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = "res-analytics-1",
                ReservedEstimate = frozenEligibleCount,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            foreach (var guestId in frozenIds)
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

            return (campaign.Id, frozenIds);
        }

        private async Task<int> SeedLocationAsync()
        {
            var user = new User
            {
                FullName = "Campaign Operator",
                Email = $"owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
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
                Name = "Send Test Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private sealed class RecordingCampaignProductAnalytics
            : ICampaignProductAnalytics
        {
            public List<(int CampaignId, string Mode)> ScheduleCommits { get; } =
                [];

            public List<int> SendStarts { get; } = [];

            public List<(int CampaignId, string Status)> SendTerminals { get; } =
                [];

            public List<int> SendTests { get; } = [];

            public void TrackScheduleCommit(int campaignId, string scheduleMode)
            {
                ScheduleCommits.Add((campaignId, scheduleMode));
            }

            public void TrackSendStart(int campaignId)
            {
                SendStarts.Add(campaignId);
            }

            public void TrackSendTerminal(int campaignId, string terminalStatus)
            {
                SendTerminals.Add((campaignId, terminalStatus));
            }

            public void TrackSendTest(int locationId)
            {
                SendTests.Add(locationId);
            }
        }

        private sealed class RecordingBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive { get; set; }

            public bool FailNext { get; set; }

            public List<CampaignBillingSettleRequest> SettleCalls { get; } = [];

            public List<CampaignBillingReleaseRequest> ReleaseCalls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
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
                        ReservationRef = "res-analytics-commit",
                    }
                );
            }

            public Task<CampaignBillingSettleResult> SettleAsync(
                CampaignBillingSettleRequest request,
                CancellationToken cancellationToken = default
            )
            {
                SettleCalls.Add(request);
                return Task.FromResult<CampaignBillingSettleResult>(
                    new CampaignBillingSettleResult.Ok()
                );
            }

            public Task<CampaignBillingReleaseResult> ReleaseAsync(
                CampaignBillingReleaseRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReleaseCalls.Add(request);
                return Task.FromResult<CampaignBillingReleaseResult>(
                    new CampaignBillingReleaseResult.Ok()
                );
            }
        }

        private sealed class NoOpCampaignFireWork : ICampaignFireWork
        {
            public ValueTask NotifyAsync(
                int campaignId,
                CancellationToken cancellationToken = default
            )
                => ValueTask.CompletedTask;

            public Task RunAsync(CancellationToken stoppingToken)
                => Task.CompletedTask;

            public Task DrainAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private sealed class ControllableSendStartGate : ICampaignSendStartGate
        {
            public CampaignSendStartGateResult Next { get; set; } =
                new CampaignSendStartGateResult.Clear();

            public Task<CampaignSendStartGateResult> EvaluateAsync(
                int campaignId,
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult(Next);
            }
        }

        private sealed class RecordingOutboundSender : ICampaignOutboundSender
        {
            public List<CampaignOutboundSendRequest> Calls { get; } = [];

            public Action<int>? OnAfterAccept { get; set; }

            public Task<CampaignOutboundSendResult> SendAsync(
                CampaignOutboundSendRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(request);
                OnAfterAccept?.Invoke(Calls.Count);
                return Task.FromResult<CampaignOutboundSendResult>(
                    new CampaignOutboundSendResult.Accepted()
                );
            }
        }

        private sealed class TrackingGuestResponseEmailService : EmailServiceStubBase
        {
            public override Task SendGuestResponseEmailAsync(
                string toEmail,
                string subject,
                string brandTitle,
                string? brandSubtitle,
                string? locationAddress,
                string message,
                string? brandLogoUrl = null,
                GuestResponseEmailOfferBlock? offer = null
            )
            {
                return Task.CompletedTask;
            }
        }
    }
}
