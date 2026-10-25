using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Claims / Campaign rows with null LocationGuestId (WS2a erasure reads).
    /// </summary>
    public sealed class UnlinkedGuestRowsTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly OfferIssueService _offerIssues;
        private readonly OfferVoidRequestService _voidRequests;
        private readonly OfferLifecycleService _lifecycle;
        private readonly ReportsOffersService _reports;
        private readonly CampaignEligibilityService _eligibility;
        private readonly RecordingBillingReserve _reserve;
        private readonly RecordingOutboundSender _outbound;
        private readonly ControllableSendStartGate _gate;
        private readonly CampaignFireService _fire;
        private readonly DateTime _now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

        public UnlinkedGuestRowsTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _offerIssues = new OfferIssueService(_context);
            _voidRequests = new OfferVoidRequestService(
                _context,
                new TrackingOperatorNotificationsService()
            );
            _lifecycle = new OfferLifecycleService(_context);
            _reports = new ReportsOffersService(_context);
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
        public async Task Redeem_UnlinkedClaim_UsesOfferLocation()
        {
            var seeded = await SeedUnlinkedClaimAsync(claimCode: "TUM-UNL001");
            var otherLocationId = await SeedSecondLocationAsync(seeded.RestaurantId);

            var checkOk = await _offerIssues.CheckClaimCodeAsync(
                seeded.LocationId,
                "TUM-UNL001",
                _now
            );
            var ok = Assert.IsType<OfferRedeemCheckResult.Ok>(checkOk);
            Assert.Equal(DeletedGuestDisplay.Name, ok.Preview.GuestName);

            var redeem = await _offerIssues.RedeemClaimCodeAsync(
                seeded.LocationId,
                "TUM-UNL001",
                seeded.IssueId.ToString(),
                _now
            );
            Assert.IsType<OfferRedeemMarkResult.Ok>(redeem);

            var checkWrong = await _offerIssues.CheckClaimCodeAsync(
                otherLocationId,
                "TUM-UNL001",
                _now.AddMinutes(1)
            );
            var failed = Assert.IsType<OfferRedeemCheckResult.Failed>(checkWrong);
            Assert.Equal(OfferRedeemFailureReasons.WrongLocation, failed.Reason);
        }

        [Fact]
        public async Task VoidRequest_UnlinkedClaim_UsesOfferLocation()
        {
            var seeded = await SeedUnlinkedClaimAsync(
                claimCode: "TUM-UNL002",
                redeemedAt: _now.AddHours(-1)
            );

            var result = await _voidRequests.CreateAsync(
                seeded.UserId,
                new CreateOfferVoidRequestBody
                {
                    IssueId = seeded.IssueId,
                    OfferId = seeded.CatalogOfferId,
                    LocationId = seeded.LocationId,
                    ReasonId = OfferVoidRequestReasonIds.RedeemedByMistake,
                    CorrectionId = OfferVoidRequestCorrectionIds.KeepUnusable,
                },
                _now
            );

            Assert.NotEqual(
                OfferVoidCreateResultStatus.NotFound,
                result.Status
            );
            Assert.Equal(OfferVoidCreateResultStatus.Created, result.Status);
        }

        [Fact]
        public async Task ClaimList_UnlinkedClaim_ShowsDeletedGuest()
        {
            var seeded = await SeedUnlinkedClaimAsync(claimCode: "TUM-UNL003");

            var list = await _lifecycle.ListClaimsAsync(
                seeded.CatalogOfferId,
                _now
            );

            Assert.NotNull(list);
            var item = Assert.Single(list!.Items);
            Assert.Equal(DeletedGuestDisplay.Name, item.GuestName);
            Assert.Null(item.GuestId);
        }

        [Fact]
        public async Task RecentRedemptions_IncludesUnlinkedClaim()
        {
            var seeded = await SeedUnlinkedClaimAsync(
                claimCode: "TUM-UNL004",
                redeemedAt: _now.AddMinutes(-30)
            );

            var report = await _reports.GetOffersReportAsync(
                seeded.LocationId,
                _now.Date,
                _now.Date.AddDays(1)
            );

            Assert.False(report.LifetimeEmpty);
            var recent = Assert.Single(report.RecentRedemptions!);
            Assert.Equal(DeletedGuestDisplay.Name, recent.GuestName);
            Assert.Equal(seeded.IssueId, recent.Id);
        }

        [Fact]
        public async Task Fire_SkipsUnlinkedFrozenRecipient_KeepsAcceptedDeliveryCount()
        {
            var seeded = await SeedFireCampaignWithOneLinkedAndOneUnlinkedAsync();

            var result = await _fire.FireAsync(seeded.CampaignId);

            var ok = Assert.IsType<CampaignFireResult.Ok>(result);
            Assert.Equal(2, ok.Campaign.AcceptedCount);
            Assert.Single(_outbound.Calls);
            Assert.Equal(seeded.LinkedGuestId, _outbound.Calls[0].LocationGuestId);

            Assert.True(_reserve.SettleCalls.Count >= 1);
            Assert.Equal(
                2,
                _reserve.SettleCalls.Sum(call => call.AcceptedUnits)
            );
        }

        private async Task<(
            int LocationId,
            int RestaurantId,
            int UserId,
            int CatalogOfferId,
            int IssueId
        )> SeedUnlinkedClaimAsync(
            string claimCode,
            DateTime? redeemedAt = null
        )
        {
            var baseSeed = await SeedRestaurantLocationOfferAsync();
            var issue = new OfferIssue
            {
                CatalogOfferId = baseSeed.CatalogOfferId,
                LocationGuestId = null,
                ClaimCode = claimCode,
                IssuedAtUtc = _now.AddDays(-1),
                ClaimedAtUtc = _now.AddDays(-1),
                RedeemedAtUtc = redeemedAt,
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = _now.AddDays(7),
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off next visit",
                Description = "Come back soon",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                StaffInstructions = "Apply 10%.",
            };
            _context.OfferIssues.Add(issue);
            await _context.SaveChangesAsync();

            return (
                baseSeed.LocationId,
                baseSeed.RestaurantId,
                baseSeed.UserId,
                baseSeed.CatalogOfferId,
                issue.Id
            );
        }

        private async Task<(
            int CampaignId,
            int LinkedGuestId
        )> SeedFireCampaignWithOneLinkedAndOneUnlinkedAsync()
        {
            var baseSeed = await SeedRestaurantLocationOfferAsync(
                withOffer: false
            );

            var master = new MasterGuest
            {
                RestaurantId = baseSeed.RestaurantId,
                Email = $"linked-{Guid.NewGuid():N}@example.com",
                CreatedAt = _now,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var linked = new LocationGuest
            {
                RestaurantLocationId = baseSeed.LocationId,
                MasterGuestId = master.Id,
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                Name = "Linked Guest",
                CreatedAt = _now,
            };
            _context.LocationGuests.Add(linked);
            await _context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = baseSeed.LocationId,
                Status = CampaignFireService.SendingStatus,
                Name = "Unlinked fire",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                ScheduleMode = "send-now",
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = "res-unlinked-1",
                ReservedEstimate = 2,
                CreatedAt = _now,
                UpdatedAt = _now,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            _context.CampaignFrozenRecipients.AddRange(
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = linked.Id,
                    FrozenAtUtc = _now.AddHours(-1),
                },
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = null,
                    FrozenAtUtc = _now.AddHours(-1),
                }
            );

            _context.CampaignRecipientDeliveries.Add(
                new CampaignRecipientDelivery
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = null,
                    Channel = "email",
                    Outcome = CampaignFireService.AcceptedOutcome,
                    AcceptedAtUtc = _now.AddHours(-2),
                    AcceptedUnits = 1,
                    UpdatedAtUtc = _now.AddHours(-2),
                }
            );
            await _context.SaveChangesAsync();

            return (campaign.Id, linked.Id);
        }

        private async Task<(
            int LocationId,
            int RestaurantId,
            int UserId,
            int CatalogOfferId
        )> SeedRestaurantLocationOfferAsync(bool withOffer = true)
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
                Name = "Unlinked Restaurant",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = _now,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();

            var offerId = 0;
            if (withOffer)
            {
                var offer = new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "10% off next visit",
                    Description = "Come back soon",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                    StaffInstructions = "Apply 10%.",
                    CreatedAt = _now,
                    UpdatedAt = _now,
                };
                _context.CatalogOffers.Add(offer);
                await _context.SaveChangesAsync();
                offerId = offer.Id;
            }

            return (location.Id, restaurant.Id, user.Id, offerId);
        }

        private async Task<int> SeedSecondLocationAsync(int restaurantId)
        {
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = "Camden",
                CreatedAt = _now,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private sealed class ControllableSendStartGate : ICampaignSendStartGate
        {
            public Task<CampaignSendStartGateResult> EvaluateAsync(
                int campaignId,
                int locationId,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<CampaignSendStartGateResult>(
                    new CampaignSendStartGateResult.Clear()
                );
            }
        }

        private sealed class RecordingOutboundSender : ICampaignOutboundSender
        {
            public List<CampaignOutboundSendRequest> Calls { get; } = [];

            public Task<CampaignOutboundSendResult> SendAsync(
                CampaignOutboundSendRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(request);
                return Task.FromResult<CampaignOutboundSendResult>(
                    new CampaignOutboundSendResult.Accepted()
                );
            }
        }

        private sealed class RecordingBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive { get; set; }

            public List<CampaignBillingSettleRequest> SettleCalls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            ) =>
                Task.FromResult<CampaignBillingReserveResult>(
                    new CampaignBillingReserveResult.Ok
                    {
                        ReservationRef = "res",
                    }
                );

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
            ) =>
                Task.FromResult<CampaignBillingReleaseResult>(
                    new CampaignBillingReleaseResult.Ok()
                );
        }
    }
}
