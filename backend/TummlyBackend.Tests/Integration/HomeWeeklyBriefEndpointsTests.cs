using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: <c>GET /api/home/weekly-brief</c> — auth, ownership, week default / explicit,
    /// ready vs missing envelope. Must not generate on GET.
    /// </summary>
    public class HomeWeeklyBriefEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string ExplicitWeek = "2026-W33";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeWeeklyBriefEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_ReturnsBodyAndMetrics()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready");
            var metrics = EmptyMetrics() with { GuestsJoined = 4, FeedbackCount = 2 };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-08-18T09:00:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                metrics,
                generatedAt
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.ResetCallCount();

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.LocationId, json.GetProperty("locationId").GetInt32());
            Assert.Equal(ExplicitWeek, json.GetProperty("week").GetString());
            Assert.Equal(
                "succeeded",
                json.GetProperty("status").GetString()
            );
            Assert.Equal(
                "Steady week across capture and feedback.",
                json.GetProperty("body").GetProperty("headline").GetString()
            );
            Assert.Equal(
                4,
                json.GetProperty("metrics").GetProperty("guestsJoined").GetInt32()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_ReturnsPhase1MetaAndExecutiveSummary()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-meta");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 10,
                QrScanEvents = 8,
                FeedbackCount = 5,
                ClaimsInWeek = 2,
                CampaignsSentInWeek = 1,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(
                "Steady week across capture and feedback.",
                json.GetProperty("body").GetProperty("headline").GetString()
            );
            Assert.Equal(
                10,
                json.GetProperty("metrics").GetProperty("guestsJoined").GetInt32()
            );

            var meta = json.GetProperty("meta");
            Assert.Equal("6–12 July", meta.GetProperty("period").GetString());
            Assert.Equal(
                "Based on enough activity to show useful patterns.",
                meta.GetProperty("confidence").GetString()
            );
            Assert.Equal("high", meta.GetProperty("confidenceLevel").GetString());

            var dataSources = meta.GetProperty("dataSources")
                .EnumerateArray()
                .Select(el => el.GetString())
                .ToArray();
            Assert.Equal(
                new[] { "Capture", "Feedback", "Offers", "Campaigns" },
                dataSources
            );

            Assert.Equal(
                "Steady week across capture and feedback. "
                    + "10 guests joined; 8 QR scans. "
                    + "5 feedback submissions this week. "
                    + "2 claims and 0 redemptions. "
                    + "1 campaigns reached 0 recipients.",
                json.GetProperty("executiveSummary").GetString()
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_ReturnsWhatChangedAndFeedbackSummary()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-sections");
            var weekKey = "monday:2026-07-06";
            var priorWeekKey = "monday:2026-06-29";
            var current = EmptyMetrics() with
            {
                GuestsJoined = 46,
                QrScanEvents = 112,
                FeedbackCount = 54,
                PositiveFeedbackCount = 40,
                NeutralFeedbackCount = 8,
                NegativeFeedbackCount = 6,
                NeedsAttentionCount = 6,
                RedemptionsInWeek = 24,
                CampaignsSentInWeek = 2,
            };
            var prior = EmptyMetrics() with
            {
                GuestsJoined = 40,
                QrScanEvents = 100,
                FeedbackCount = 50,
                RedemptionsInWeek = 25,
                CampaignsSentInWeek = 2,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(current);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                priorWeekKey,
                FakeWeeklyBriefProvider.FixtureFor(prior),
                prior,
                generatedAt.AddDays(-7)
            );
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                current,
                generatedAt
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());

            var whatChanged = json.GetProperty("whatChanged").EnumerateArray().ToList();
            Assert.Equal(4, whatChanged.Count);
            Assert.Equal("QR scans", whatChanged[0].GetProperty("area").GetString());
            Assert.Equal("+12%", whatChanged[0].GetProperty("change").GetString());

            var feedbackSummary = json.GetProperty("feedbackSummary");
            Assert.Equal(
                JsonValueKind.Object,
                feedbackSummary.ValueKind
            );
            Assert.Equal(6, feedbackSummary.GetProperty("needsAttentionCount").GetInt32());
            Assert.Contains(
                "54 private feedback messages",
                feedbackSummary.GetProperty("text").GetString()
            );
            Assert.Equal(
                "Based on private feedback submitted between 6–12 July.",
                feedbackSummary.GetProperty("subtitle").GetString()
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_EmitsFeedbackNeedsAttentionRecommendedAction()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-ra-feedback");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics() with { NeedsAttentionCount = 6 };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            var actions = json.GetProperty("recommendedActions").EnumerateArray().ToList();
            Assert.Single(actions);
            Assert.Equal(
                "feedback-needs-attention",
                actions[0].GetProperty("kind").GetString()
            );
            Assert.Equal(6, actions[0].GetProperty("count").GetInt32());
            Assert.Equal(
                "feedback-needs-attention",
                actions[0].GetProperty("target").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                json.GetProperty("suggestedCampaign").ValueKind
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_OmitsEmptyRecommendedActionsAndSuggestedCampaign()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-ra-empty");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics();
            var body = new WeeklyBriefBody(
                Headline: "Quiet week.",
                Capture: new WeeklyBriefSection(false, "", null),
                Feedback: new WeeklyBriefSection(false, "", null),
                Offers: new WeeklyBriefSection(false, "", null),
                Campaigns: new WeeklyBriefSection(false, "", null),
                WatchNext: []
            );
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.Empty(json.GetProperty("recommendedActions").EnumerateArray());
            Assert.Equal(
                JsonValueKind.Null,
                json.GetProperty("suggestedCampaign").ValueKind
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_EmitsRepeatedInvalidAndLowRedemptionFacts()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-ra-offers");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics();
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Quiet-day treat"
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Lee");
            // Coverage window for monday:2026-07-06 Europe/London:
            // 2026-07-05T23:00:00Z .. 2026-07-12T23:00:00Z
            for (var i = 0; i < 5; i++)
            {
                await SeedOfferIssueAsync(
                    offerId,
                    guestId,
                    $"TUM-WB{i:D2}",
                    issuedAt: new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc),
                    claimedAt: new DateTime(2026, 7, 8, 12, i, 0, DateTimeKind.Utc),
                    redeemedAt: i == 0
                        ? new DateTime(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc)
                        : null
                );
            }

            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
                OfferRedeemFailureReasons.AlreadyUsed
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
                OfferRedeemFailureReasons.Expired
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            var actions = json.GetProperty("recommendedActions").EnumerateArray().ToList();
            Assert.Equal(2, actions.Count);
            Assert.Equal(
                "repeated-invalid",
                actions[0].GetProperty("kind").GetString()
            );
            Assert.Equal(2, actions[0].GetProperty("count").GetInt32());
            Assert.Equal(
                "redemption-log",
                actions[0].GetProperty("target").GetString()
            );
            Assert.Equal(
                "low-redemption",
                actions[1].GetProperty("kind").GetString()
            );
            Assert.Equal(offerId, actions[1].GetProperty("offerId").GetInt32());
            Assert.Equal(
                "Quiet-day treat",
                actions[1].GetProperty("offerTitle").GetString()
            );
            Assert.Equal(5, actions[1].GetProperty("claims").GetInt32());
            Assert.Equal(1, actions[1].GetProperty("redemptions").GetInt32());
            Assert.Equal(
                "offers",
                actions[1].GetProperty("target").GetString()
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_CapsRecommendedActionsAtThree()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-ra-cap");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics() with { NeedsAttentionCount = 3 };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                "Cap offer"
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Pat");
            for (var i = 0; i < 5; i++)
            {
                await SeedOfferIssueAsync(
                    offerId,
                    guestId,
                    $"TUM-CAP{i:D2}",
                    issuedAt: new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc),
                    claimedAt: new DateTime(2026, 7, 8, 12, i, 0, DateTimeKind.Utc)
                );
            }

            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc),
                OfferRedeemFailureReasons.AlreadyUsed
            );
            await SeedFailedAttemptAsync(
                offerId,
                seeded.LocationId,
                new DateTime(2026, 7, 11, 12, 0, 0, DateTimeKind.Utc),
                OfferRedeemFailureReasons.Expired
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            var actions = json.GetProperty("recommendedActions").EnumerateArray().ToList();
            Assert.Equal(3, actions.Count);
            Assert.Equal(
                "feedback-needs-attention",
                actions[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "repeated-invalid",
                actions[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "low-redemption",
                actions[2].GetProperty("kind").GetString()
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_PicksNewestSuggestedDraftInWeek()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-sc-pick");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics();
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            await SeedCampaignAsync(
                seeded.LocationId,
                CampaignDraftService.DraftStatus,
                "Older draft",
                audienceKey: "new-guests",
                createdAt: new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 7, 7, 10, 0, 0, DateTimeKind.Utc)
            );
            var newestId = await SeedCampaignAsync(
                seeded.LocationId,
                CampaignDraftService.DraftStatus,
                "Quiet-day boost",
                audienceKey: "all-eligible-guests",
                createdAt: new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 7, 11, 15, 0, 0, DateTimeKind.Utc)
            );
            await SeedCampaignAsync(
                seeded.LocationId,
                CampaignDraftService.DraftStatus,
                "Outside week",
                audienceKey: "new-guests",
                createdAt: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)
            );
            await SeedCampaignAsync(
                seeded.LocationId,
                "scheduled",
                "Not a draft",
                audienceKey: "new-guests",
                createdAt: new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            var suggested = json.GetProperty("suggestedCampaign");
            Assert.Equal(JsonValueKind.Object, suggested.ValueKind);
            Assert.Equal(newestId, suggested.GetProperty("campaignId").GetInt32());
            Assert.Equal(
                "Quiet-day boost",
                suggested.GetProperty("name").GetString()
            );
            Assert.Equal(
                "all-eligible-guests",
                suggested.GetProperty("audienceKey").GetString()
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_OmitsSuggestedCampaignWhenNoQualifyingDraft()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-sc-omit");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics();
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            await SeedCampaignAsync(
                seeded.LocationId,
                CampaignDraftService.DraftStatus,
                "Outside week",
                audienceKey: "new-guests",
                createdAt: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.Equal(
                JsonValueKind.Null,
                json.GetProperty("suggestedCampaign").ValueKind
            );
        }

        [Fact]
        public async Task GenerateWeeklyBrief_DoesNotInsertCampaignRows()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-gen-no-campaign");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                Assert.Equal(
                    0,
                    context.Campaigns.Count(c =>
                        c.RestaurantLocationId == seeded.LocationId
                    )
                );
            }

            using var genScope = _factory.Services.CreateScope();
            var fake = genScope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var afterScope = _factory.Services.CreateScope();
            var afterContext = afterScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                afterContext.Campaigns.Count(c =>
                    c.RestaurantLocationId == seeded.LocationId
                )
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_OmitsEmptyWhatChangedAndFeedbackSummary()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-empty-sections");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics();
            var body = new WeeklyBriefBody(
                Headline: "Quiet week.",
                Capture: new WeeklyBriefSection(false, "", null),
                Feedback: new WeeklyBriefSection(false, "", null),
                Offers: new WeeklyBriefSection(false, "", null),
                Campaigns: new WeeklyBriefSection(false, "", null),
                WatchNext: []
            );
            var generatedAt = DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                generatedAt
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Empty(json.GetProperty("whatChanged").EnumerateArray());
            Assert.Equal(
                JsonValueKind.Null,
                json.GetProperty("feedbackSummary").ValueKind
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_MissingRow_ReturnsNotReadyEnvelope()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-missing");

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.LocationId, json.GetProperty("locationId").GetInt32());
            Assert.Equal(ExplicitWeek, json.GetProperty("week").GetString());
            Assert.False(json.TryGetProperty("body", out _));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetWeeklyBrief_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("wb-owner-a");
            var other = await SeedOwnerWithLocationAsync("wb-owner-b");

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={other.LocationId}&week={ExplicitWeek}",
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetWeeklyBrief_OmittingWeek_UsesClosedPriorWeekInLocationTz()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-default-week");
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow
            );
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                closed.WeekKey,
                body,
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
        }

        [Fact]
        public async Task GetWeeklyBrief_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                $"/api/home/weekly-brief?locationId=1&week={ExplicitWeek}"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_MissingRow_CreatesReadyEnvelope()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-gen-create");
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();
            fake.ResetCallCount();

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.LocationId, json.GetProperty("locationId").GetInt32());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(
                "succeeded",
                json.GetProperty("status").GetString()
            );
            Assert.True(
                json.GetProperty("body").TryGetProperty("headline", out _)
            );
            Assert.Equal(1, fake.CallCount);

            using var getRequest = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var getJson = await ReadJsonAsync(getResponse);
            Assert.True(getJson.GetProperty("ready").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_SecondCall_IsIdempotentWithoutReProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-gen-idem");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();
            fake.ResetCallCount();

            using var first = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(1, fake.CallCount);

            using var second = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var secondResponse = await _client.SendAsync(second);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

            var json = await ReadJsonAsync(secondResponse);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("wb-gen-owner-a");
            var other = await SeedOwnerWithLocationAsync("wb-gen-owner-b");

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={other.LocationId}",
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetWeeklyBrief_StillDoesNotGenerate()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-get-no-gen");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.ResetCallCount();

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task MarkWeeklyBriefReviewed_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsync(
                $"/api/home/weekly-brief/mark-reviewed?locationId=1&week={ExplicitWeek}",
                null
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task MarkWeeklyBriefReviewed_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("wb-mark-owner-a");
            var other = await SeedOwnerWithLocationAsync("wb-mark-owner-b");
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());

            await SeedSucceededBriefAsync(
                other.LocationId,
                ExplicitWeek,
                body,
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/mark-reviewed?locationId={other.LocationId}&week={ExplicitWeek}",
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MarkWeeklyBriefReviewed_FirstMark_PersistsReviewedFields()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-mark-first");
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());
            var generatedAt = DateTime.Parse("2026-08-18T09:00:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                EmptyMetrics(),
                generatedAt
            );

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/mark-reviewed?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var before = DateTime.UtcNow;
            var response = await _client.SendAsync(request);
            var after = DateTime.UtcNow;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.UserId, json.GetProperty("reviewedByUserId").GetInt32());
            var reviewedAt = json.GetProperty("reviewedAtUtc").GetDateTime().ToUniversalTime();
            Assert.InRange(reviewedAt, before.AddSeconds(-2), after.AddSeconds(2));

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var row = await context.WeeklyBriefs.SingleAsync(brief =>
                brief.LocationId == seeded.LocationId
                && brief.WeekKey == ExplicitWeek
            );
            Assert.Equal(seeded.UserId, row.ReviewedByUserId);
            Assert.NotNull(row.ReviewedAtUtc);
        }

        [Fact]
        public async Task MarkWeeklyBriefReviewed_AllowsSoftLock()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-mark-softlock",
                softLock: true
            );
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/mark-reviewed?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.UserId, json.GetProperty("reviewedByUserId").GetInt32());
            Assert.True(json.TryGetProperty("reviewedAtUtc", out var reviewedAt));
            Assert.NotEqual(JsonValueKind.Null, reviewedAt.ValueKind);
        }

        [Fact]
        public async Task MarkWeeklyBriefReviewed_ReMark_RefreshesTimestampAndReviewer()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-mark-idem");
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());
            var firstReviewedAt = DateTime.Parse("2026-08-10T10:00:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                EmptyMetrics(),
                DateTime.UtcNow,
                reviewedAtUtc: firstReviewedAt,
                reviewedByUserId: seeded.UserId
            );

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/mark-reviewed?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var before = DateTime.UtcNow;
            var response = await _client.SendAsync(request);
            var after = DateTime.UtcNow;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(seeded.UserId, json.GetProperty("reviewedByUserId").GetInt32());
            var reviewedAt = json.GetProperty("reviewedAtUtc").GetDateTime().ToUniversalTime();
            Assert.True(reviewedAt > firstReviewedAt);
            Assert.InRange(reviewedAt, before.AddSeconds(-2), after.AddSeconds(2));
        }

        private async Task SeedSucceededBriefAsync(
            int locationId,
            string weekKey,
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics,
            DateTime generatedAtUtc,
            DateTime? reviewedAtUtc = null,
            int? reviewedByUserId = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.WeeklyBriefs.Add(
                new WeeklyBrief
                {
                    LocationId = locationId,
                    WeekKey = weekKey,
                    Status = WeeklyBriefStatus.Succeeded,
                    GeneratedAtUtc = generatedAtUtc,
                    BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options),
                    MetricsJson = JsonSerializer.Serialize(metrics, WeeklyBriefStoreJson.Options),
                    ErrorInfo = null,
                    ReviewedAtUtc = reviewedAtUtc,
                    ReviewedByUserId = reviewedByUserId,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task<int> SeedCatalogOfferAsync(
            int locationId,
            string title
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Seeded for weekly brief tests.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<int> SeedLocationGuestAsync(
            int locationId,
            string name
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var location = await context.RestaurantLocations.FindAsync(locationId);
            Assert.NotNull(location);

            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"wb-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = name,
                CreatedAt = now,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();
            return lg.Id;
        }

        private async Task SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            DateTime issuedAt,
            DateTime? claimedAt,
            DateTime? redeemedAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.OfferIssues.Add(
                new OfferIssue
                {
                    CatalogOfferId = catalogOfferId,
                    LocationGuestId = locationGuestId,
                    ClaimCode = claimCode,
                    IssuedAtUtc = issuedAt,
                    ClaimedAtUtc = claimedAt,
                    RedeemedAtUtc = redeemedAt,
                    Source = OfferIssueSources.Campaign,
                    ExpiryAtUtc = issuedAt.AddDays(14),
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "Weekly brief seed",
                    Description = "Seeded issue",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task SeedFailedAttemptAsync(
            int catalogOfferId,
            int locationId,
            DateTime attemptedAt,
            string reason
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            context.OfferRedeemFailedAttempts.Add(
                new OfferRedeemFailedAttempt
                {
                    CatalogOfferId = catalogOfferId,
                    RestaurantLocationId = locationId,
                    AttemptedAtUtc = attemptedAt,
                    ClaimCode = "TUM-XXXXXX",
                    Reason = reason,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task<int> SeedCampaignAsync(
            int locationId,
            string status,
            string name,
            string? audienceKey,
            DateTime createdAt,
            DateTime updatedAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Status = status,
                Name = name,
                AudienceKey = audienceKey,
                GoalId = "thank-recent-guests",
                RowVersion = [0, 0, 0, 0, 0, 0, 0, 1],
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
            return campaign.Id;
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
                CampaignRecipientsReached: 0
            );

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedPost(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int UserId
        )> SeedOwnerWithLocationAsync(
            string emailLocalPart,
            bool softLock = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Weekly Brief Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Weekly Brief Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            if (softLock)
            {
                billing.BillingStatus = BillingStatuses.SoftLock;
                billing.SoftLockEnteredAt = DateTime.UtcNow.AddDays(-1);
            }

            context.BillingAccounts.Add(billing);

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, user.Id);
        }
    }
}
