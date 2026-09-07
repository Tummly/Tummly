using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public sealed class RestaurantContextSnapshotServiceTests : IDisposable
    {
        private static readonly DateTime FixedUtc =
            new(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc);

        private readonly ApplicationDbContext _context;
        private readonly FakeTimeProvider _clock;
        private readonly IMemoryCache _cache;
        private readonly FakeHomeKpiRetrieve _home;
        private readonly FakeCampaignsRetrieve _campaigns;
        private readonly FakeOffersRetrieve _offers;
        private readonly FakeFeedbackRetrieve _feedback;
        private readonly FakeCaptureRetrieve _capture;
        private readonly FakeGuestsRetrieve _guests;
        private readonly RestaurantContextSnapshotService _service;

        public RestaurantContextSnapshotServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w =>
                    w.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                )
                .Options;
            _context = new ApplicationDbContext(options);
            _clock = new FakeTimeProvider(FixedUtc);
            _cache = new MemoryCache(new MemoryCacheOptions());
            _home = new FakeHomeKpiRetrieve();
            _campaigns = new FakeCampaignsRetrieve();
            _offers = new FakeOffersRetrieve();
            _feedback = new FakeFeedbackRetrieve();
            _capture = new FakeCaptureRetrieve();
            _guests = new FakeGuestsRetrieve();
            _service = new RestaurantContextSnapshotService(
                _feedback,
                _offers,
                _campaigns,
                _capture,
                _home,
                _guests,
                _context,
                Options.Create(DefaultSettings()),
                _clock,
                _cache
            );
        }

        public void Dispose()
        {
            _cache.Dispose();
            _context.Dispose();
        }

        [Fact]
        public async Task BuildAsync_DefaultWindows_AreTrailingAndPrior30Days()
        {
            _home.Current = Home(guestsJoined: 10, guestsJoinedPrevious: 8);

            var snapshot = await _service.BuildAsync(
                ownerUserId: 1,
                scope: new SingleLocation("10"),
                currentOverride: null,
                comparisonOverride: null
            );

            Assert.Equal(new DateOnly(2026, 8, 7), snapshot.CurrentPeriod.Start);
            Assert.Equal(new DateOnly(2026, 9, 5), snapshot.CurrentPeriod.End);
            Assert.Equal(new DateOnly(2026, 7, 8), snapshot.ComparisonPeriod.Start);
            Assert.Equal(new DateOnly(2026, 8, 6), snapshot.ComparisonPeriod.End);
            Assert.Equal(10m, snapshot.Account.Covers.Current);
            Assert.Equal(8m, snapshot.Account.Covers.Prior);
            Assert.Contains(_home.Calls, call => call.OwnedLocationId == 10);
        }

        [Fact]
        public async Task BuildAsync_OverrideWindows_AreHonoured()
        {
            var current = new PeriodWindow(
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 6, 30)
            );
            var comparison = new PeriodWindow(
                new DateOnly(2026, 5, 1),
                new DateOnly(2026, 5, 31)
            );
            _home.Current = Home(guestsJoined: 3);

            var snapshot = await _service.BuildAsync(
                ownerUserId: 1,
                scope: new SingleLocation("10"),
                currentOverride: current,
                comparisonOverride: comparison
            );

            Assert.Equal(current, snapshot.CurrentPeriod);
            Assert.Equal(comparison, snapshot.ComparisonPeriod);
            Assert.Equal(
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                _home.Calls[0].FromUtc
            );
            Assert.Equal(
                new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                _home.Calls[0].ToUtc
            );
        }

        [Fact]
        public async Task BuildAsync_NewAccount_NullsPctDeltas()
        {
            await SeedLocationHistoryAsync(
                locationId: 10,
                earliestUtc: FixedUtc.AddDays(-10)
            );
            _home.Current = Home(guestsJoined: 5, guestsJoinedPrevious: 2);
            _home.PriorComparison = Home(guestsJoined: 1);

            var snapshot = await _service.BuildAsync(
                ownerUserId: 1,
                scope: new SingleLocation("10"),
                currentOverride: null,
                comparisonOverride: null
            );

            Assert.True(snapshot.Meta.IsNewAccount);
            Assert.True(snapshot.Meta.TotalDaysOfHistory < 14);
            Assert.Null(snapshot.Account.Covers.PctDelta);
            Assert.Null(snapshot.Account.Revenue.PctDelta);
        }

        [Fact]
        public async Task BuildAsync_NoActiveCampaign_SetsFlag()
        {
            await SeedLocationHistoryAsync(
                locationId: 10,
                earliestUtc: FixedUtc.AddDays(-60)
            );
            _campaigns.Current = new AssistantCampaignsEvidence(
                ListTotalCount: 1,
                ListSampleCount: 1,
                InFlightScheduled: 0,
                InFlightSending: 0,
                MessagesSentAccepted: 0,
                Rows:
                [
                    new AssistantCampaignListRow(
                        1,
                        "Past blast",
                        "sent",
                        FixedUtc.AddDays(-40),
                        FixedUtc.AddDays(-39),
                        null
                    ),
                ],
                Eligibility: [],
                Details: []
            );

            var snapshot = await _service.BuildAsync(
                ownerUserId: 1,
                scope: new SingleLocation("10"),
                currentOverride: null,
                comparisonOverride: null
            );

            Assert.Contains(
                snapshot.Campaigns.Flags,
                flag => flag.Code == "NO_ACTIVE_CAMPAIGN"
            );
        }

        [Fact]
        public async Task BuildAsync_EmptyEvidence_ListsInsufficientSections()
        {
            await SeedLocationHistoryAsync(
                locationId: 10,
                earliestUtc: FixedUtc.AddDays(-60)
            );

            var snapshot = await _service.BuildAsync(
                ownerUserId: 1,
                scope: new SingleLocation("10"),
                currentOverride: null,
                comparisonOverride: null
            );

            Assert.Contains("Account", snapshot.Meta.SectionsWithInsufficientData);
            Assert.Contains("Campaigns", snapshot.Meta.SectionsWithInsufficientData);
            Assert.Contains("Offers", snapshot.Meta.SectionsWithInsufficientData);
            Assert.Contains("Feedback", snapshot.Meta.SectionsWithInsufficientData);
            Assert.Contains("Capture", snapshot.Meta.SectionsWithInsufficientData);
            Assert.Equal(0m, snapshot.Account.Revenue.Current);
            Assert.Null(snapshot.Account.Revenue.Prior);
            Assert.Null(snapshot.Account.Revenue.PctDelta);
        }

        [Fact]
        public async Task BuildAsync_MultiLocation_DivergenceFlag_OnGuestsJoinedProxy()
        {
            await SeedLocationHistoryAsync(
                locationId: 10,
                earliestUtc: FixedUtc.AddDays(-60),
                locationName: "Alpha"
            );
            await SeedLocationHistoryAsync(
                locationId: 20,
                earliestUtc: FixedUtc.AddDays(-60),
                locationName: "Beta",
                restaurantId: 2
            );
            _home.CurrentByLocation[10] = Home(guestsJoined: 100);
            _home.CurrentByLocation[20] = Home(guestsJoined: 50);
            _home.ComparisonByLocation[10] = Home(guestsJoined: 100);
            _home.ComparisonByLocation[20] = Home(guestsJoined: 100);

            var snapshot = await _service.BuildAsync(
                ownerUserId: 1,
                scope: new AllOwnedLocations(["10", "20"]),
                currentOverride: null,
                comparisonOverride: null
            );

            Assert.NotNull(snapshot.Account.RepeatVisitRateByLocation);
            Assert.Equal(2, snapshot.Account.RepeatVisitRateByLocation!.Count);
            Assert.Contains(
                snapshot.Account.Flags,
                flag => flag.Code == "REPEAT_RATE_DIVERGENT_BY_LOCATION"
            );
        }

        [Fact]
        public async Task BuildAsync_Cache_ReturnsSameInstance()
        {
            await SeedLocationHistoryAsync(
                locationId: 10,
                earliestUtc: FixedUtc.AddDays(-60)
            );
            _home.Current = Home(guestsJoined: 4);

            var first = await _service.BuildAsync(
                ownerUserId: 7,
                scope: new SingleLocation("10"),
                currentOverride: null,
                comparisonOverride: null
            );
            var callsAfterFirst = _home.Calls.Count;
            var second = await _service.BuildAsync(
                ownerUserId: 7,
                scope: new SingleLocation("10"),
                currentOverride: null,
                comparisonOverride: null
            );

            Assert.Same(first, second);
            Assert.Equal(callsAfterFirst, _home.Calls.Count);
        }

        private async Task SeedLocationHistoryAsync(
            int locationId,
            DateTime earliestUtc,
            string locationName = "Venue",
            int restaurantId = 1
        )
        {
            if (!await _context.RestaurantLocations.AnyAsync(row => row.Id == locationId))
            {
                _context.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        Id = locationId,
                        RestaurantId = restaurantId,
                        LocationName = locationName,
                        Address = "1 High Street",
                        CreatedAt = earliestUtc,
                    }
                );
            }

            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = locationId,
                    CreatedAt = earliestUtc,
                    GuestName = "Guest",
                    GuestContact = "guest@example.com",
                    Comment = "ok",
                }
            );
            await _context.SaveChangesAsync();
        }

        private static RestaurantContextSnapshotSettings DefaultSettings()
            => new()
            {
                SchemaVersion = "2026-09-05",
                CampaignUnderperformingThresholdPct = 50,
                LocationDivergenceThresholdPts = 15,
                MinDaysForTrendClaim = 14,
                MinDataPointsForMetric = 20,
                VipAtRiskFrequencyDropPct = 30,
                OfferExpiringWindowDays = 7,
                CacheTtlSeconds = 180,
                NewAccountHistoryDays = 30,
                FlaggedFeedbackCap = 5,
            };

        private static AssistantHomeKpiEvidence Home(
            int guestsJoined = 0,
            int guestsJoinedPrevious = 0,
            int feedbackSubmitted = 0,
            int qrScans = 0
        )
            => new(
                feedbackSubmitted,
                0,
                guestsJoined,
                guestsJoinedPrevious,
                qrScans,
                0
            );

        private sealed class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;

            public FakeTimeProvider(DateTime utcNow)
            {
                _utcNow = new DateTimeOffset(utcNow);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private sealed class FakeHomeKpiRetrieve : IAssistantHomeKpiRetrieve
        {
            public AssistantHomeKpiEvidence Current { get; set; } =
                AssistantHomeKpiEvidence.Empty;

            public AssistantHomeKpiEvidence? PriorComparison { get; set; }

            public Dictionary<int, AssistantHomeKpiEvidence> CurrentByLocation { get; } = [];

            public Dictionary<int, AssistantHomeKpiEvidence> ComparisonByLocation { get; } = [];

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((ownedLocationId, fromUtc, toUtc));
                var currentStart = FixedUtc.Date.AddDays(-29);
                var isCurrentWindow = fromUtc.Date >= currentStart;
                if (isCurrentWindow
                    && CurrentByLocation.TryGetValue(ownedLocationId, out var currentByLocation))
                {
                    return Task.FromResult<AssistantHomeKpiRetrieveResult>(
                        new AssistantHomeKpiRetrieveResult.Ok(currentByLocation)
                    );
                }

                if (!isCurrentWindow
                    && ComparisonByLocation.TryGetValue(
                        ownedLocationId,
                        out var comparisonByLocation
                    ))
                {
                    return Task.FromResult<AssistantHomeKpiRetrieveResult>(
                        new AssistantHomeKpiRetrieveResult.Ok(comparisonByLocation)
                    );
                }

                if (PriorComparison is not null
                    && fromUtc < FixedUtc.AddDays(-60))
                {
                    return Task.FromResult<AssistantHomeKpiRetrieveResult>(
                        new AssistantHomeKpiRetrieveResult.Ok(PriorComparison)
                    );
                }

                if (!isCurrentWindow && Current.GuestsJoinedPrevious > 0)
                {
                    return Task.FromResult<AssistantHomeKpiRetrieveResult>(
                        new AssistantHomeKpiRetrieveResult.Ok(
                            Home(
                                guestsJoined: Current.GuestsJoinedPrevious,
                                guestsJoinedPrevious: 0
                            )
                        )
                    );
                }

                return Task.FromResult<AssistantHomeKpiRetrieveResult>(
                    new AssistantHomeKpiRetrieveResult.Ok(Current)
                );
            }
        }

        private sealed class FakeCampaignsRetrieve : IAssistantCampaignsRetrieve
        {
            public AssistantCampaignsEvidence Current { get; set; } =
                AssistantCampaignsEvidence.Empty;

            public Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                bool includeMessageCopy = false,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantCampaignsRetrieveResult>(
                    new AssistantCampaignsRetrieveResult.Ok(Current)
                );
        }

        private sealed class FakeOffersRetrieve : IAssistantOffersRetrieve
        {
            public AssistantOffersEvidence Current { get; set; } =
                AssistantOffersEvidence.Empty;

            public Task<AssistantOffersRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantOffersRetrieveResult>(
                    new AssistantOffersRetrieveResult.Ok(Current)
                );
        }

        private sealed class FakeFeedbackRetrieve : IAssistantFeedbackRetrieve
        {
            public AssistantFeedbackEvidence Current { get; set; } =
                AssistantFeedbackEvidence.Empty;

            public Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantFeedbackRetrieveResult>(
                    new AssistantFeedbackRetrieveResult.Ok(Current)
                );

            public Task<AssistantFeedbackRetrieveResult> RetrieveIdentityAsync(
                int ownedLocationId,
                string locationName,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => RetrieveAsync(ownedLocationId, fromUtc, toUtc, cancellationToken);
        }

        private sealed class FakeCaptureRetrieve : IAssistantCaptureRetrieve
        {
            public AssistantCaptureEvidence Current { get; set; } =
                AssistantCaptureEvidence.Empty;

            public Task<AssistantCaptureRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantCaptureRetrieveResult>(
                    new AssistantCaptureRetrieveResult.Ok(Current)
                );
        }

        private sealed class FakeGuestsRetrieve : IAssistantGuestsRetrieve
        {
            public AssistantGuestsEvidence Current { get; set; } =
                AssistantGuestsEvidence.Empty;

            public Task<AssistantGuestsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<AssistantGuestsRetrieveResult>(
                    new AssistantGuestsRetrieveResult.Ok(Current)
                );
        }
    }
}
