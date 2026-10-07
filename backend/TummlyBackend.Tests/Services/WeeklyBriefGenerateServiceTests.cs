using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam under test: <see cref="IWeeklyBriefGenerateService.GenerateAsync"/>
    /// (ticket 03). Provider Fake is a collaborator; persistence observed via
    /// the generate result and <see cref="ApplicationDbContext.WeeklyBriefs"/>.
    /// </summary>
    public class WeeklyBriefGenerateServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeWeeklyBriefProvider _provider;
        private readonly WeeklyBriefGenerateService _service;

        private static readonly WeeklyBriefClosedWeek ClosedWeek = new(
            WeekKey: "2026-W33",
            CoverageStartUtc: new DateTime(2026, 8, 9, 23, 0, 0, DateTimeKind.Utc),
            CoverageEndUtcExclusive: new DateTime(
                2026,
                8,
                16,
                23,
                0,
                0,
                DateTimeKind.Utc
            )
        );

        public WeeklyBriefGenerateServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _provider = new FakeWeeklyBriefProvider();
            _service = new WeeklyBriefGenerateService(
                _context,
                _provider,
                NullLogger<WeeklyBriefGenerateService>.Instance
            );
        }

        [Fact]
        public async Task GenerateAsync_FirstCall_PersistsSucceededRow()
        {
            var locationId = await SeedLocationAsync();
            _provider.UseDefaultFixtures();

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var ok = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(result);
            Assert.True(ok.Created);
            Assert.Equal(WeeklyBriefStatus.Succeeded, ok.Brief.Status);
            Assert.Equal("2026-W33", ok.Brief.WeekKey);
            Assert.False(string.IsNullOrWhiteSpace(ok.Brief.BodyJson));
            Assert.False(string.IsNullOrWhiteSpace(ok.Brief.MetricsJson));
            Assert.False(string.IsNullOrWhiteSpace(ok.Brief.EnrichmentJson));
            var enrichment = JsonSerializer.Deserialize<WeeklyBriefEnrichment>(
                ok.Brief.EnrichmentJson!,
                WeeklyBriefStoreJson.Options
            );
            Assert.NotNull(enrichment);
            Assert.False(string.IsNullOrWhiteSpace(enrichment!.ExecutiveSummary));
            Assert.Equal(1, _provider.CallCount);
            Assert.Equal(
                1,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId && row.WeekKey == "2026-W33"
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_SecondCall_ReturnsExistingWithoutProvider()
        {
            var locationId = await SeedLocationAsync();
            _provider.UseDefaultFixtures();

            var first = await _service.GenerateAsync(locationId, ClosedWeek);
            var firstOk = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(first);
            Assert.True(firstOk.Created);
            _provider.ResetCallCount();

            var second = await _service.GenerateAsync(locationId, ClosedWeek);

            var secondOk = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(
                second
            );
            Assert.False(secondOk.Created);
            Assert.Equal(firstOk.Brief.Id, secondOk.Brief.Id);
            Assert.Equal(firstOk.Brief.BodyJson, secondOk.Brief.BodyJson);
            Assert.Equal(0, _provider.CallCount);
            Assert.Equal(
                1,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId && row.WeekKey == "2026-W33"
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_ProviderFail_LeavesNoReadyRow()
        {
            var locationId = await SeedLocationAsync();
            _provider.Fail(retryable: true);

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var failed = Assert.IsType<WeeklyBriefGenerateResult.Failed>(result);
            Assert.True(failed.Retryable);
            Assert.Equal(1, _provider.CallCount);
            Assert.Equal(
                0,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId && row.WeekKey == "2026-W33"
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_PilotPlan_ReturnsFailedWithoutProvider()
        {
            var locationId = await SeedLocationAsync(
                subscriptionPlan: BillingSubscriptionPlans.Pilot
            );
            _provider.UseDefaultFixtures();

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var failed = Assert.IsType<WeeklyBriefGenerateResult.Failed>(result);
            Assert.False(failed.Retryable);
            Assert.Equal(0, _provider.CallCount);
            Assert.Equal(
                0,
                await _context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == locationId
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_DoesNotDebitAiCredits()
        {
            // Free call: generate service has no credit / billing collaborator.
            // Guard: type surface must not reference campaign billing reserve.
            var ctor = typeof(WeeklyBriefGenerateService).GetConstructors().Single();
            Assert.DoesNotContain(
                ctor.GetParameters(),
                parameter =>
                    parameter.ParameterType.Name.Contains(
                        "Billing",
                        StringComparison.Ordinal
                    )
                    || parameter.ParameterType.Name.Contains(
                        "Credit",
                        StringComparison.Ordinal
                    )
            );
        }

        [Fact]
        public async Task GenerateAsync_PersistsInsightCandidatesAndNarratives()
        {
            var locationId = await SeedLocationAsync();
            _provider.UseDefaultFixtures();

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var ok = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(result);
            Assert.NotNull(_provider.LastInput?.InsightCandidates);
            Assert.NotEmpty(_provider.LastInput!.InsightCandidates!.Candidates);
            var enrichment = JsonSerializer.Deserialize<WeeklyBriefEnrichment>(
                ok.Brief.EnrichmentJson!,
                WeeklyBriefStoreJson.Options
            );
            Assert.NotNull(enrichment);
            Assert.NotNull(enrichment!.InsightCandidates);
            Assert.NotEmpty(enrichment.InsightCandidates!);
            Assert.NotNull(enrichment.InsightNarratives);
            Assert.NotEmpty(enrichment.InsightNarratives!);
            Assert.All(
                enrichment.InsightNarratives,
                narrative =>
                    Assert.Contains(
                        enrichment.InsightCandidates,
                        candidate => candidate.Id == narrative.CandidateId
                    )
            );
        }

        [Fact]
        public async Task GenerateAsync_StableMetrics_EmptyWatchNextAndNoActionWording()
        {
            var locationId = await SeedLocationAsync();
            await SeedStableCaptureActivityAsync(locationId, needsAttention: false);
            _provider.UseDefaultFixtures();

            var result = await _service.GenerateAsync(locationId, ClosedWeek);

            var ok = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(result);
            var body = JsonSerializer.Deserialize<WeeklyBriefBody>(
                ok.Brief.BodyJson,
                WeeklyBriefStoreJson.Options
            );
            var enrichment = JsonSerializer.Deserialize<WeeklyBriefEnrichment>(
                ok.Brief.EnrichmentJson!,
                WeeklyBriefStoreJson.Options
            );
            Assert.NotNull(body);
            Assert.NotNull(enrichment);
            Assert.Empty(body!.WatchNext);
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                body.WatchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                body.WatchNext
            );
            Assert.Empty(enrichment!.ActionWording);
            Assert.True(
                WeeklyBriefInsightNarrativeValidation.IsSoleNoMaterialChange(
                    _provider.LastInput?.InsightCandidates
                )
            );
        }

        [Fact]
        public async Task GenerateAsync_NeedsAttention_DistinctWatchNextAndActionWording()
        {
            var locationId = await SeedLocationAsync();
            await SeedStableCaptureActivityAsync(locationId, needsAttention: false);
            _provider.UseDefaultFixtures();
            var thin = await _service.GenerateAsync(locationId, ClosedWeek);
            var thinOk = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(thin);
            var thinBody = JsonSerializer.Deserialize<WeeklyBriefBody>(
                thinOk.Brief.BodyJson,
                WeeklyBriefStoreJson.Options
            );
            Assert.NotNull(thinBody);
            Assert.Empty(thinBody!.WatchNext);

            // Second location / week: needs-attention signal on the same shape.
            var locationId2 = await SeedLocationAsync();
            await SeedStableCaptureActivityAsync(locationId2, needsAttention: true);
            _provider.UseDefaultFixtures();
            _provider.ResetCallCount();

            var result = await _service.GenerateAsync(locationId2, ClosedWeek);

            var ok = Assert.IsType<WeeklyBriefGenerateResult.Succeeded>(result);
            var body = JsonSerializer.Deserialize<WeeklyBriefBody>(
                ok.Brief.BodyJson,
                WeeklyBriefStoreJson.Options
            );
            var enrichment = JsonSerializer.Deserialize<WeeklyBriefEnrichment>(
                ok.Brief.EnrichmentJson!,
                WeeklyBriefStoreJson.Options
            );
            Assert.NotNull(body);
            Assert.NotNull(enrichment);
            Assert.NotEmpty(body!.WatchNext);
            Assert.NotEqual(thinBody.WatchNext, body.WatchNext);
            Assert.Contains(
                body.WatchNext,
                line => line.Contains("Needs attention", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                body.WatchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                body.WatchNext
            );
            Assert.Single(enrichment!.ActionWording);
            Assert.Equal(
                WeeklyBriefEnrichmentActionKinds.FeedbackNeedsAttention,
                enrichment.ActionWording[0].Kind
            );
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task SeedStableCaptureActivityAsync(
            int locationId,
            bool needsAttention
        )
        {
            Assert.True(
                WeeklyBriefWeekKey.TryPriorWeekKey(
                    ClosedWeek.WeekKey,
                    out var priorKey
                )
            );
            Assert.True(
                WeeklyBriefWeekKey.TryCoverageWindow(
                    priorKey,
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    out var priorFrom,
                    out var priorTo
                )
            );

            var qr = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = QrType.CounterCard,
                Token = $"wb-stable-{locationId}-{Guid.NewGuid():N}"[..32],
                Status = QrCodeStatus.Active,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();

            for (var i = 0; i < 5; i++)
            {
                _context.LocationGuests.Add(
                    new LocationGuest
                    {
                        RestaurantLocationId = locationId,
                        Name = $"Prior Guest {i}",
                        CreatedAt = priorFrom.AddHours(i + 1),
                    }
                );
                _context.LocationGuests.Add(
                    new LocationGuest
                    {
                        RestaurantLocationId = locationId,
                        Name = $"Current Guest {i}",
                        CreatedAt = ClosedWeek.CoverageStartUtc.AddHours(i + 1),
                    }
                );
                _context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        CreatedAt = priorFrom.AddHours(i + 1),
                    }
                );
                _context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        CreatedAt = ClosedWeek.CoverageStartUtc.AddHours(i + 1),
                    }
                );
            }

            // Mirror feedback volume WoW so sole no-material-change stays valid
            // when needsAttention is false; when true, keep count matched.
            for (var i = 0; i < 3; i++)
            {
                _context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        GuestName = $"Prior Feedback {i}",
                        GuestContact = $"prior-{locationId}-{i}@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Steady prior week",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Positive,
                        WorkflowStatus = FeedbackWorkflowStatus.Resolved,
                        CreatedAt = priorFrom.AddHours(i + 2),
                    }
                );
                _context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        GuestName = $"Current Feedback {i}",
                        GuestContact = $"current-{locationId}-{i}@example.com",
                        ContactType = ContactType.Email,
                        Comment = needsAttention
                            ? "Needs follow-up"
                            : "Steady current week",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = needsAttention
                            ? FeedbackSentiment.Negative
                            : FeedbackSentiment.Positive,
                        WorkflowStatus = needsAttention
                            ? FeedbackWorkflowStatus.New
                            : FeedbackWorkflowStatus.Resolved,
                        CreatedAt = ClosedWeek.CoverageStartUtc.AddHours(i + 2),
                    }
                );
            }

            await _context.SaveChangesAsync();
            _ = priorTo;
        }

        private async Task<int> SeedLocationAsync(string? subscriptionPlan = null)
        {
            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Test Restaurant",
                AccountType = "Single",
                OwnerUserId = 7,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            if (subscriptionPlan is not null)
            {
                var billing = BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                );
                billing.SubscriptionPlan = subscriptionPlan;
                _context.BillingAccounts.Add(billing);
            }

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Harbour Kitchen",
                Address = "1 Harbour Way",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }
    }
}
