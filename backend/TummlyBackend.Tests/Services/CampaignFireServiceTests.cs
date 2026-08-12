using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="ICampaignFireService"/> — drop-only revalidate, settle
    /// accepted, cannot-start Failed (ticket 31).
    /// </summary>
    public class CampaignFireServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CampaignEligibilityService _eligibility;
        private readonly RecordingBillingReserve _reserve;
        private readonly RecordingOutboundSender _outbound;
        private readonly ControllableSendStartGate _gate;
        private readonly CampaignFireService _fire;
        private readonly DateTime _now = new(2026, 8, 9, 14, 0, 0, DateTimeKind.Utc);

        public CampaignFireServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _eligibility = new CampaignEligibilityService(_context);
            _reserve = new RecordingBillingReserve { IsLive = true };
            _outbound = new RecordingOutboundSender();
            _gate = new ControllableSendStartGate();
            _fire = new CampaignFireService(
                _context,
                _eligibility,
                _reserve,
                _outbound,
                _gate,
                utcNow: () => _now
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task FireAsync_ZeroEligibleAfterRevalidate_SetsFailedAndReleases()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 1);
            // Opt out the only frozen guest so revalidate drops to zero.
            var guest = await _context.LocationGuests.SingleAsync(
                lg => lg.Id == seeded.FrozenGuestIds[0]
            );
            guest.OffersOptOut = true;
            await _context.SaveChangesAsync();

            var result = await _fire.FireAsync(seeded.CampaignId);

            var failed = Assert.IsType<CampaignFireResult.CannotStart>(result);
            Assert.Equal(CampaignFireService.FailedStatus, failed.Campaign.Status);
            Assert.Equal(0, failed.Campaign.AcceptedCount);
            Assert.Null(failed.Campaign.BillingReservationRef);

            Assert.Single(_reserve.ReleaseCalls);
            Assert.Equal("res-fire-1", _reserve.ReleaseCalls[0].ReservationRef);
            Assert.Empty(_reserve.SettleCalls);
            Assert.Empty(_outbound.Calls);

            var campaign = await _context.Campaigns.SingleAsync(
                c => c.Id == seeded.CampaignId
            );
            Assert.Equal(CampaignFireService.FailedStatus, campaign.Status);
            Assert.Null(campaign.BillingReservationRef);
        }

        [Fact]
        public async Task FireAsync_SoftLockAfterSchedule_SetsFailedAndReleases()
        {
            var seeded = await SeedScheduledCampaignAsync(
                frozenEligibleCount: 2,
                scheduledAtUtc: _now.AddMinutes(-5)
            );
            _gate.Next = new CampaignSendStartGateResult.SoftLocked();

            var result = await _fire.FireAsync(seeded.CampaignId);

            var failed = Assert.IsType<CampaignFireResult.CannotStart>(result);
            Assert.Equal(CampaignFireService.FailedStatus, failed.Campaign.Status);
            Assert.Single(_reserve.ReleaseCalls);
            Assert.Empty(_outbound.Calls);
        }

        [Fact]
        public async Task FireAsync_Success_SettlesAcceptedOnlyAndSetsSent()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 2);
            // Drop one frozen guest as newly ineligible — settle only the accepted one.
            var drop = await _context.LocationGuests.SingleAsync(
                lg => lg.Id == seeded.FrozenGuestIds[1]
            );
            drop.OffersOptOut = true;
            await _context.SaveChangesAsync();

            var result = await _fire.FireAsync(seeded.CampaignId);

            var ok = Assert.IsType<CampaignFireResult.Ok>(result);
            Assert.Equal(CampaignFireService.SentStatus, ok.Campaign.Status);
            Assert.Equal(1, ok.Campaign.AcceptedCount);
            Assert.Equal(1, ok.Campaign.SkippedIneligibleCount);
            Assert.Equal(0, ok.Campaign.RemainingUnsentCount);

            Assert.Single(_outbound.Calls);
            Assert.Equal(seeded.FrozenGuestIds[0], _outbound.Calls[0].LocationGuestId);

            Assert.Single(_reserve.SettleCalls);
            Assert.Equal(1, _reserve.SettleCalls[0].AcceptedUnits);
            Assert.Single(_reserve.ReleaseCalls);
            Assert.Equal("res-fire-1", _reserve.ReleaseCalls[0].ReservationRef);

            var deliveries = await _context.CampaignRecipientDeliveries
                .Where(row => row.CampaignId == seeded.CampaignId)
                .ToListAsync();
            Assert.Equal(2, deliveries.Count);
            Assert.Contains(
                deliveries,
                d =>
                    d.LocationGuestId == seeded.FrozenGuestIds[0]
                    && d.Outcome == CampaignFireService.AcceptedOutcome
            );
            Assert.Contains(
                deliveries,
                d =>
                    d.LocationGuestId == seeded.FrozenGuestIds[1]
                    && d.Outcome == CampaignFireService.SkippedIneligibleOutcome
            );
        }

        [Fact]
        public async Task FireAsync_MidSendStopWithAccept_SetsPartiallySent()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 3);
            using var cts = new CancellationTokenSource();
            _outbound.OnAfterAccept = acceptedSoFar =>
            {
                if (acceptedSoFar >= 1)
                {
                    cts.Cancel();
                }
            };

            var result = await _fire.FireAsync(seeded.CampaignId, cts.Token);

            var ok = Assert.IsType<CampaignFireResult.Ok>(result);
            Assert.Equal(CampaignFireService.PartiallySentStatus, ok.Campaign.Status);
            Assert.True(ok.Campaign.AcceptedCount >= 1);
            Assert.True(ok.Campaign.RemainingUnsentCount >= 1);

            Assert.Single(_reserve.SettleCalls);
            Assert.Equal(ok.Campaign.AcceptedCount, _reserve.SettleCalls[0].AcceptedUnits);
            // Remaining reservation held for retry / lifecycle release — not full release.
            Assert.Empty(_reserve.ReleaseCalls);
        }

        [Fact]
        public async Task FireAsync_IdempotentRetry_SkipsAlreadyAcceptedRecipients()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 2);

            var first = await _fire.FireAsync(seeded.CampaignId);
            Assert.IsType<CampaignFireResult.Ok>(first);

            // Simulate Partially sent leftover: reset to sending with one accepted.
            var campaign = await _context.Campaigns.SingleAsync(
                c => c.Id == seeded.CampaignId
            );
            campaign.Status = CampaignFireService.SendingStatus;
            campaign.BillingReservationRef = "res-fire-1";
            campaign.ReservedEstimate = 2;
            await _context.SaveChangesAsync();

            _outbound.Calls.Clear();
            _reserve.SettleCalls.Clear();

            var second = await _fire.FireAsync(seeded.CampaignId);
            var ok = Assert.IsType<CampaignFireResult.Ok>(second);
            Assert.Equal(CampaignFireService.SentStatus, ok.Campaign.Status);
            Assert.Empty(_outbound.Calls);
            Assert.Empty(_reserve.SettleCalls);
        }

        [Fact]
        public async Task FireAsync_ScheduledNotYetDue_ReturnsNotDue()
        {
            var seeded = await SeedScheduledCampaignAsync(
                frozenEligibleCount: 1,
                scheduledAtUtc: _now.AddHours(2)
            );

            var result = await _fire.FireAsync(seeded.CampaignId);

            Assert.IsType<CampaignFireResult.NotDue>(result);
            Assert.Empty(_outbound.Calls);
            Assert.Empty(_reserve.ReleaseCalls);
        }

        [Fact]
        public async Task FireAsync_AcceptedCounts_AreQueryableForOverviewMessagesSent()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 2);

            var result = await _fire.FireAsync(seeded.CampaignId);
            Assert.IsType<CampaignFireResult.Ok>(result);

            var count = await CampaignAcceptedMessageCounts.CountAcceptedAsync(
                _context,
                fromUtcInclusive: _now.AddMinutes(-1),
                toUtcExclusive: _now.AddMinutes(1)
            );
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task FireAsync_AcceptedWithOfferId_CreatesOfferIssue()
        {
            var seeded = await SeedSendingCampaignAsync(
                frozenEligibleCount: 1,
                withCatalogOffer: true
            );

            var result = await _fire.FireAsync(seeded.CampaignId);

            Assert.IsType<CampaignFireResult.Ok>(result);
            var issue = Assert.Single(await _context.OfferIssues.ToListAsync());
            Assert.Equal(seeded.CampaignId, issue.CampaignId);
            Assert.Equal(seeded.FrozenGuestIds[0], issue.LocationGuestId);
            Assert.Equal(seeded.CatalogOfferId, issue.CatalogOfferId);
            Assert.Equal(_now, issue.IssuedAtUtc);
            Assert.Equal(_now, issue.ClaimedAtUtc);
            Assert.Equal(OfferIssueSources.Campaign, issue.Source);
            Assert.Matches(@"^TUM-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$", issue.ClaimCode);

            var outboundOffer = Assert.Single(_outbound.Calls).Offer;
            Assert.NotNull(outboundOffer);
            Assert.Equal(issue.ClaimCode, outboundOffer.RedemptionCode);
            Assert.Equal(issue.Title, outboundOffer.Title);
            Assert.StartsWith("Expires:", outboundOffer.ExpiryLabel);
        }

        [Fact]
        public async Task FireAsync_Rejected_DoesNotCreateOfferIssue()
        {
            var seeded = await SeedSendingCampaignAsync(
                frozenEligibleCount: 1,
                withCatalogOffer: true
            );
            _outbound.NextResult = new CampaignOutboundSendResult.Rejected
            {
                Message = "provider rejected",
            };

            var result = await _fire.FireAsync(seeded.CampaignId);

            Assert.IsType<CampaignFireResult.CannotStart>(result);
            Assert.Empty(await _context.OfferIssues.ToListAsync());
            var outboundOffer = Assert.Single(_outbound.Calls).Offer;
            Assert.NotNull(outboundOffer);
            Assert.Matches(
                @"^TUM-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{6}$",
                outboundOffer.RedemptionCode
            );
        }

        [Fact]
        public async Task FireAsync_AcceptedWithoutOfferId_DoesNotCreateOfferIssue()
        {
            var seeded = await SeedSendingCampaignAsync(frozenEligibleCount: 1);

            var result = await _fire.FireAsync(seeded.CampaignId);

            Assert.IsType<CampaignFireResult.Ok>(result);
            Assert.Empty(await _context.OfferIssues.ToListAsync());
        }

        private async Task<(
            int CampaignId,
            IReadOnlyList<int> FrozenGuestIds,
            int? CatalogOfferId
        )> SeedSendingCampaignAsync(
            int frozenEligibleCount,
            bool withCatalogOffer = false
        )
        {
            return await SeedCommittedCampaignAsync(
                status: CampaignFireService.SendingStatus,
                scheduleMode: "send-now",
                scheduledAtUtc: null,
                frozenEligibleCount: frozenEligibleCount,
                withCatalogOffer: withCatalogOffer
            );
        }

        private async Task<(
            int CampaignId,
            IReadOnlyList<int> FrozenGuestIds,
            int? CatalogOfferId
        )> SeedScheduledCampaignAsync(
            int frozenEligibleCount,
            DateTime scheduledAtUtc
        )
        {
            return await SeedCommittedCampaignAsync(
                status: CampaignFireService.ScheduledStatus,
                scheduleMode: "schedule-later",
                scheduledAtUtc: scheduledAtUtc,
                frozenEligibleCount: frozenEligibleCount,
                withCatalogOffer: false
            );
        }

        private async Task<(
            int CampaignId,
            IReadOnlyList<int> FrozenGuestIds,
            int? CatalogOfferId
        )> SeedCommittedCampaignAsync(
            string status,
            string scheduleMode,
            DateTime? scheduledAtUtc,
            int frozenEligibleCount,
            bool withCatalogOffer
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
                Name = "Fire Test Restaurant",
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

            int? catalogOfferId = null;
            if (withCatalogOffer)
            {
                var offer = new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "Fire offer",
                    Description = "Catalog attach for fire",
                    Validity = CatalogOfferValidity.Days7AfterIssue,
                    DiscountPercentage = 15m,
                    CreatedAt = _now,
                    UpdatedAt = _now,
                };
                _context.CatalogOffers.Add(offer);
                await _context.SaveChangesAsync();
                catalogOfferId = offer.Id;
            }

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

                var lg = new LocationGuest
                {
                    RestaurantLocationId = location.Id,
                    MasterGuestId = master.Id,
                    OffersOptOut = false,
                    CreatedAt = _now,
                };
                _context.LocationGuests.Add(lg);
                await _context.SaveChangesAsync();
                frozenIds.Add(lg.Id);
            }

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = status,
                Name = "Thank recent guests",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = withCatalogOffer ? "include-offer" : "no-offer",
                OfferId = catalogOfferId,
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                ScheduleMode = scheduleMode,
                ScheduledAtUtc = scheduledAtUtc,
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = "res-fire-1",
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
                        FrozenAtUtc = _now.AddHours(-1),
                    }
                );
            }

            await _context.SaveChangesAsync();
            return (campaign.Id, frozenIds, catalogOfferId);
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

            public CampaignOutboundSendResult NextResult { get; set; } =
                new CampaignOutboundSendResult.Accepted();

            public Task<CampaignOutboundSendResult> SendAsync(
                CampaignOutboundSendRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(request);
                var acceptedSoFar = Calls.Count;
                OnAfterAccept?.Invoke(acceptedSoFar);
                return Task.FromResult(NextResult);
            }
        }

        private sealed class RecordingBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive { get; set; }

            public List<CampaignBillingSettleRequest> SettleCalls { get; } = [];

            public List<CampaignBillingReleaseRequest> ReleaseCalls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
                throw new NotSupportedException();
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
    }
}
