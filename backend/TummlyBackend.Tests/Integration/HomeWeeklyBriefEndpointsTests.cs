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
        public async Task GetWeeklyBrief_ReadyRow_PrefersEnrichmentOverPhase1()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-enrich");
            var weekKey = "monday:2026-07-06";
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 10,
                FeedbackCount = 5,
                NeedsAttentionCount = 6,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary:
                    "You received more guest activity this week from delivery inserts.",
                FeedbackSummary: new WeeklyBriefEnrichmentFeedbackSummary(
                    "Several guests mentioned packaging and wait time.",
                    "Based on private feedback submitted between 6–12 July."
                ),
                ActionWording:
                [
                    new WeeklyBriefEnrichmentActionWording(
                        WeeklyBriefEnrichmentActionKinds.FeedbackNeedsAttention,
                        "Follow up with six guests this week",
                        "AI enriched needs-attention subtitle."
                    ),
                ]
            );

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                weekKey,
                body,
                metrics,
                DateTime.Parse("2026-07-13T08:30:00Z").ToUniversalTime(),
                enrichment: enrichment
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.Equal(
                "You received more guest activity this week from delivery inserts.",
                json.GetProperty("executiveSummary").GetString()
            );

            var feedbackSummary = json.GetProperty("feedbackSummary");
            Assert.Equal(
                "Several guests mentioned packaging and wait time.",
                feedbackSummary.GetProperty("text").GetString()
            );
            Assert.Equal(
                "Based on private feedback submitted between 6–12 July.",
                feedbackSummary.GetProperty("subtitle").GetString()
            );
            Assert.Equal(6, feedbackSummary.GetProperty("needsAttentionCount").GetInt32());

            var actions = json.GetProperty("recommendedActions").EnumerateArray().ToList();
            Assert.Single(actions);
            Assert.Equal(
                "feedback-needs-attention",
                actions[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Follow up with six guests this week",
                actions[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                "AI enriched needs-attention subtitle.",
                actions[0].GetProperty("subtitle").GetString()
            );
        }

        [Fact]
        public async Task GetWeeklyBrief_ReadyRow_DeserializesLegacyMetricsWithoutUnsubscribes()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-legacy-metrics");
            var weekKey = "monday:2026-07-06";
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics() with
            {
                GuestsJoined = 3,
            });
            // Pre-phase-2 MetricsJson shape (no unsubscribesInWeek).
            var legacyMetricsJson =
                """
                {"guestsJoined":3,"qrScanEvents":0,"feedbackCount":0,"positiveFeedbackCount":0,"neutralFeedbackCount":0,"negativeFeedbackCount":0,"needsAttentionCount":0,"detectedTagCounts":{},"activeOffers":0,"claimsInWeek":0,"redemptionsInWeek":0,"campaignsSentInWeek":0,"campaignRecipientsReached":0}
                """;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.WeeklyBriefs.Add(
                    new WeeklyBrief
                    {
                        LocationId = seeded.LocationId,
                        WeekKey = weekKey,
                        Status = WeeklyBriefStatus.Succeeded,
                        GeneratedAtUtc = DateTime.UtcNow,
                        BodyJson = JsonSerializer.Serialize(
                            body,
                            WeeklyBriefStoreJson.Options
                        ),
                        MetricsJson = legacyMetricsJson,
                        EnrichmentJson = null,
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={weekKey}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(
                3,
                json.GetProperty("metrics").GetProperty("guestsJoined").GetInt32()
            );
            Assert.Equal(
                0,
                json.GetProperty("metrics").GetProperty("unsubscribesInWeek").GetInt32()
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
                UnsubscribesInWeek = 4,
            };
            var prior = EmptyMetrics() with
            {
                GuestsJoined = 40,
                QrScanEvents = 100,
                FeedbackCount = 50,
                RedemptionsInWeek = 25,
                UnsubscribesInWeek = 5,
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
            Assert.Equal(5, whatChanged.Count);
            Assert.Equal("QR scans", whatChanged[0].GetProperty("area").GetString());
            Assert.Equal("+12%", whatChanged[0].GetProperty("change").GetString());
            Assert.Equal("Unsubscribes", whatChanged[4].GetProperty("area").GetString());
            Assert.Equal("-20%", whatChanged[4].GetProperty("change").GetString());
            Assert.Equal(
                "Fewer guests opted out of marketing.",
                whatChanged[4].GetProperty("meaning").GetString()
            );
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
        public async Task GetWeeklyBrief_ReadyRow_LegacyIsoWeek_PicksSuggestedDraftInCoverage()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-ready-sc-iso");
            // ExplicitWeek = 2026-W33 → Mon 2026-08-10 … Mon 2026-08-17 (London)
            var metrics = EmptyMetrics();
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var generatedAt = DateTime.Parse("2026-08-18T08:30:00Z").ToUniversalTime();

            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                metrics,
                generatedAt
            );

            var draftId = await SeedCampaignAsync(
                seeded.LocationId,
                CampaignDraftService.DraftStatus,
                "ISO-week draft",
                audienceKey: "new-guests",
                createdAt: new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            var suggested = json.GetProperty("suggestedCampaign");
            Assert.Equal(JsonValueKind.Object, suggested.ValueKind);
            Assert.Equal(draftId, suggested.GetProperty("campaignId").GetInt32());
            Assert.Equal(
                "ISO-week draft",
                suggested.GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GenerateWeeklyBrief_DoesNotInsertCampaignRows()
        {
            var seeded = await SeedEligibleForGenerateAsync("wb-gen-no-campaign");

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
        public async Task GetWeeklyBrief_FromTo_SnapsToMostRecentClosedOverlappingWeek()
        {
            var utcNow = DateTime.UtcNow;
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                "monday"
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-get-snap",
                weekStartsOn: "monday",
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Growth
            );
            var body = FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics());
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                closed.WeekKey,
                body,
                EmptyMetrics(),
                utcNow
            );

            // Range overlapping the closed prior week (and current week).
            var from = closed.CoverageStartUtc.AddDays(2).ToString("o");
            var to = utcNow.ToString("o");

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
        }

        [Fact]
        public async Task GetWeeklyBrief_FromTo_NoClosedOverlap_ReturnsNotReady()
        {
            var utcNow = DateTime.UtcNow;
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                "monday"
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-get-no-overlap",
                weekStartsOn: "monday",
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Growth
            );

            // Range entirely inside the open current week (after closed end).
            var from = closed.CoverageEndUtcExclusive.ToString("o");
            var to = utcNow > closed.CoverageEndUtcExclusive
                ? utcNow.ToString("o")
                : closed.CoverageEndUtcExclusive.AddHours(1).ToString("o");

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(string.Empty, json.GetProperty("week").GetString());
        }

        [Fact]
        public async Task GenerateWeeklyBrief_FromTo_SnapsAndCreatesReadyEnvelope()
        {
            var utcNow = DateTime.UtcNow;
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                "monday"
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-gen-snap",
                weekStartsOn: "monday",
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Growth
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();
            fake.ResetCallCount();

            var from = closed.CoverageStartUtc.AddDays(1).ToString("o");
            var to = utcNow.ToString("o");

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.True(json.GetProperty("ready").GetBoolean());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task GenerateWeeklyBrief_FromTo_NoClosedOverlap_ReturnsNotReadyWithoutProvider()
        {
            var utcNow = DateTime.UtcNow;
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                "monday"
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-gen-no-overlap",
                weekStartsOn: "monday",
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Growth
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();
            fake.ResetCallCount();

            var from = closed.CoverageEndUtcExclusive.ToString("o");
            var to = utcNow > closed.CoverageEndUtcExclusive
                ? utcNow.ToString("o")
                : closed.CoverageEndUtcExclusive.AddHours(1).ToString("o");

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(string.Empty, json.GetProperty("week").GetString());
            Assert.Equal(
                WeeklyBriefNotReadyReasons.NoClosedOverlap,
                json.GetProperty("reason").GetString()
            );
            Assert.Equal(0, fake.CallCount);
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
            var seeded = await SeedEligibleForGenerateAsync("wb-gen-create");

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
            Assert.Equal(seeded.ClosedWeek.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(
                "succeeded",
                json.GetProperty("status").GetString()
            );
            Assert.True(
                json.GetProperty("body").TryGetProperty("headline", out _)
            );
            Assert.True(
                json.TryGetProperty("insightCandidates", out var candidates)
            );
            Assert.True(candidates.GetArrayLength() > 0);
            Assert.True(
                json.TryGetProperty("insightNarratives", out var narratives)
            );
            Assert.True(narratives.GetArrayLength() > 0);
            Assert.Equal(1, fake.CallCount);

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            // Lazy / manual POST generate must not notify; Monday job first-write
            // owns weekly-brief-ready + system email (IWeeklyBriefReadyNotifier).
            Assert.Equal(
                0,
                await context.Notifications.CountAsync(n =>
                    n.UserId == seeded.UserId
                    && n.Type == WeeklyBriefReadyNotifier.NotificationType
                )
            );

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
        public async Task GenerateWeeklyBrief_NonGenerateDay_CreatesReadyEnvelope()
        {
            // Manual Generate brief is available any day; IsGenerateDay gates
            // only the scheduled Monday job, not lazy POST generate.
            var utcNow = DateTime.UtcNow;
            var weekStartsOn = NonGenerateWeekStartsOn();
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                weekStartsOn
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-gen-not-day",
                weekStartsOn: weekStartsOn,
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Growth
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
            Assert.True(
                json.GetProperty("body").TryGetProperty("headline", out _)
            );
            Assert.Equal(1, fake.CallCount);

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == seeded.LocationId
                    && row.WeekKey == closed.WeekKey
                    && row.Status == WeeklyBriefStatus.Succeeded
                )
            );
        }

        [Fact]
        public async Task GenerateWeeklyBrief_LocationTooNew_ReturnsNotReadyWithoutRow()
        {
            var utcNow = DateTime.UtcNow;
            var weekStartsOn = CurrentLondonGenerateWeekStartsOn();
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                weekStartsOn
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-gen-too-new",
                weekStartsOn: weekStartsOn,
                // Created at coverage end — never lived in the closed week.
                locationCreatedAtUtc: closed.CoverageEndUtcExclusive,
                subscriptionPlan: BillingSubscriptionPlans.Growth
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
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(
                WeeklyBriefNotReadyReasons.LocationTooNew,
                json.GetProperty("reason").GetString()
            );
            Assert.Equal(0, fake.CallCount);

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == seeded.LocationId
                )
            );
        }

        [Fact]
        public async Task GenerateWeeklyBrief_MidWeekCreate_CreatesReadyEnvelope()
        {
            var utcNow = DateTime.UtcNow;
            var weekStartsOn = CurrentLondonGenerateWeekStartsOn();
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                weekStartsOn
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-gen-mid-week",
                weekStartsOn: weekStartsOn,
                // Partial week OK — created after coverage start, before end.
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(2),
                subscriptionPlan: BillingSubscriptionPlans.Growth
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
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(1, fake.CallCount);

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == seeded.LocationId
                    && row.WeekKey == closed.WeekKey
                    && row.Status == WeeklyBriefStatus.Succeeded
                )
            );
            // Lazy generate still does not produce weekly-brief-ready.
            Assert.Equal(
                0,
                await context.Notifications.CountAsync(n =>
                    n.UserId == seeded.UserId
                    && n.Type == WeeklyBriefReadyNotifier.NotificationType
                )
            );
        }

        [Fact]
        public async Task GenerateWeeklyBrief_PilotPlan_ReturnsNotReadyWithoutRow()
        {
            var utcNow = DateTime.UtcNow;
            var weekStartsOn = CurrentLondonGenerateWeekStartsOn();
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                weekStartsOn
            );
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-gen-pilot",
                weekStartsOn: weekStartsOn,
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Pilot
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
            Assert.False(json.GetProperty("ready").GetBoolean());
            Assert.Equal(closed.WeekKey, json.GetProperty("week").GetString());
            Assert.Equal(
                WeeklyBriefNotReadyReasons.Pilot,
                json.GetProperty("reason").GetString()
            );
            Assert.Equal(0, fake.CallCount);

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.WeeklyBriefs.CountAsync(row =>
                    row.LocationId == seeded.LocationId
                )
            );
        }

        [Fact]
        public async Task GenerateWeeklyBrief_SecondCall_IsIdempotentWithoutReProvider()
        {
            var seeded = await SeedEligibleForGenerateAsync("wb-gen-idem");

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
        public async Task GenerateWeeklyBrief_StableMetrics_EmptyWatchNextAndNoRecommendedActions()
        {
            var seeded = await SeedEligibleForGenerateAsync("wb-gen-rpta-stable");
            await SeedStableWeekActivityAsync(
                seeded.LocationId,
                seeded.ClosedWeek,
                needsAttention: false
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            var watchNext = json.GetProperty("body")
                .GetProperty("watchNext")
                .EnumerateArray()
                .Select(el => el.GetString())
                .ToArray();
            Assert.Empty(watchNext);
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                watchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                watchNext
            );
            Assert.Empty(json.GetProperty("recommendedActions").EnumerateArray());
        }

        [Fact]
        public async Task GenerateWeeklyBrief_NeedsAttention_DistinctWatchNextAndRecommendedAction()
        {
            var stable = await SeedEligibleForGenerateAsync("wb-gen-rpta-thin");
            await SeedStableWeekActivityAsync(
                stable.LocationId,
                stable.ClosedWeek,
                needsAttention: false
            );

            using (var thinScope = _factory.Services.CreateScope())
            {
                var thinFake = thinScope.ServiceProvider
                    .GetRequiredService<FakeWeeklyBriefProvider>();
                thinFake.UseDefaultFixtures();
            }

            using var thinRequest = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={stable.LocationId}",
                stable.Jwt
            );
            var thinResponse = await _client.SendAsync(thinRequest);
            Assert.Equal(HttpStatusCode.OK, thinResponse.StatusCode);
            var thinJson = await ReadJsonAsync(thinResponse);
            var thinWatchNext = thinJson.GetProperty("body")
                .GetProperty("watchNext")
                .EnumerateArray()
                .Select(el => el.GetString())
                .ToArray();
            Assert.Empty(thinWatchNext);

            var seeded = await SeedEligibleForGenerateAsync("wb-gen-rpta-needs");
            await SeedStableWeekActivityAsync(
                seeded.LocationId,
                seeded.ClosedWeek,
                needsAttention: true
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeWeeklyBriefProvider>();
            fake.UseDefaultFixtures();

            using var request = AuthorizedPost(
                $"/api/home/weekly-brief/generate?locationId={seeded.LocationId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.True(json.GetProperty("ready").GetBoolean());
            var watchNext = json.GetProperty("body")
                .GetProperty("watchNext")
                .EnumerateArray()
                .Select(el => el.GetString())
                .ToArray();
            Assert.NotEmpty(watchNext);
            Assert.NotEqual(thinWatchNext, watchNext);
            Assert.Contains(
                watchNext,
                line =>
                    line is not null
                    && line.Contains("Needs attention", StringComparison.Ordinal)
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchNeedsAttention,
                watchNext
            );
            Assert.DoesNotContain(
                FakeWeeklyBriefProvider.LegacyFillerWatchOfferRate,
                watchNext
            );

            var actions = json.GetProperty("recommendedActions").EnumerateArray().ToList();
            Assert.Single(actions);
            Assert.Equal(
                "feedback-needs-attention",
                actions[0].GetProperty("kind").GetString()
            );
            Assert.True(actions[0].GetProperty("count").GetInt32() > 0);
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

        [Fact]
        public async Task DownloadWeeklyBriefPdf_ReadyUnlocked_ReturnsPdfWithSections()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-pdf-ok");
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 10,
                QrScanEvents = 20,
                FeedbackCount = 5,
                NeedsAttentionCount = 2,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                metrics,
                DateTime.Parse("2026-08-18T09:00:00Z").ToUniversalTime()
            );
            // ExplicitWeek = 2026-W33 → Mon 2026-08-10 … Mon 2026-08-17 (London)
            await SeedCampaignAsync(
                seeded.LocationId,
                CampaignDraftService.DraftStatus,
                "Quiet-day boost",
                audienceKey: "all-eligible-guests",
                createdAt: new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc),
                updatedAt: new DateTime(2026, 8, 14, 15, 0, 0, DateTimeKind.Utc)
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief/pdf?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "application/pdf",
                response.Content.Headers.ContentType?.MediaType
            );
            var fileName =
                response.Content.Headers.ContentDisposition?.FileName
                    ?.Trim('"');
            Assert.NotNull(fileName);
            Assert.StartsWith(
                $"tummly-weekly-brief-{seeded.LocationId}-",
                fileName
            );
            Assert.EndsWith("Z.pdf", fileName);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 100);
            var ascii = System.Text.Encoding.ASCII.GetString(bytes);
            Assert.Contains("%PDF", ascii);
            Assert.Contains("Weekly Brief", ascii);
            Assert.Contains("Executive summary", ascii);
            Assert.Contains("What changed", ascii);
            Assert.Contains("Feedback summary", ascii);
            Assert.Contains("Recommended actions", ascii);
            Assert.Contains("Follow up with 2 guests", ascii);
            Assert.Contains("Suggested campaign", ascii);
            Assert.Contains("Draft: Quiet-day boost", ascii);
        }

        [Fact]
        public async Task DownloadWeeklyBriefPdf_ReadyWithEnrichment_UsesEnrichedCopy()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-pdf-enrich");
            var metrics = EmptyMetrics() with
            {
                GuestsJoined = 10,
                FeedbackCount = 5,
                NeedsAttentionCount = 2,
            };
            var body = FakeWeeklyBriefProvider.FixtureFor(metrics);
            var enrichment = new WeeklyBriefEnrichment(
                ExecutiveSummary: "Enriched PDF executive summary for shipping audit.",
                FeedbackSummary: new WeeklyBriefEnrichmentFeedbackSummary(
                    "Enriched PDF feedback narrative about packaging.",
                    "Based on private feedback submitted this week."
                ),
                ActionWording:
                [
                    new WeeklyBriefEnrichmentActionWording(
                        WeeklyBriefEnrichmentActionKinds.FeedbackNeedsAttention,
                        "Follow up with two enriched guests",
                        "PDF action subtitle"
                    ),
                ]
            );
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                body,
                metrics,
                DateTime.Parse("2026-08-18T09:00:00Z").ToUniversalTime(),
                enrichment: enrichment
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief/pdf?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var ascii = System.Text.Encoding.ASCII.GetString(bytes);
            Assert.Contains("%PDF", ascii);
            Assert.Contains("Enriched PDF executive summary for shipping audit.", ascii);
            Assert.Contains(
                "Enriched PDF feedback narrative about packaging.",
                ascii
            );
            Assert.Contains("Follow up with two enriched guests", ascii);
        }

        [Fact]
        public async Task DownloadWeeklyBriefPdf_SoftLock_Returns403()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "wb-pdf-soft",
                softLock: true
            );
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics()),
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief/pdf?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("soft_lock", json.GetProperty("code").GetString());

            using var getRequest = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task DownloadWeeklyBriefPdf_Dormant_Returns403()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-pdf-dormant");
            await SetBillingStatusAsync(
                seeded.LocationId,
                BillingStatuses.Dormant
            );
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics()),
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief/pdf?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("dormant", json.GetProperty("code").GetString());

            using var getRequest = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task DownloadWeeklyBriefPdf_ChargebackRestricted_Returns403()
        {
            var seeded = await SeedOwnerWithLocationAsync("wb-pdf-cb");
            await SetChargebackRestrictedAsync(seeded.LocationId, restricted: true);
            await SeedSucceededBriefAsync(
                seeded.LocationId,
                ExplicitWeek,
                FakeWeeklyBriefProvider.FixtureFor(EmptyMetrics()),
                EmptyMetrics(),
                DateTime.UtcNow
            );

            using var request = AuthorizedGet(
                $"/api/home/weekly-brief/pdf?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal(
                "chargeback_restricted",
                json.GetProperty("code").GetString()
            );

            using var getRequest = AuthorizedGet(
                $"/api/home/weekly-brief?locationId={seeded.LocationId}&week={ExplicitWeek}",
                seeded.Jwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        private async Task SeedSucceededBriefAsync(
            int locationId,
            string weekKey,
            WeeklyBriefBody body,
            WeeklyBriefMetrics metrics,
            DateTime generatedAtUtc,
            DateTime? reviewedAtUtc = null,
            int? reviewedByUserId = null,
            WeeklyBriefEnrichment? enrichment = null
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
                    EnrichmentJson = enrichment is null
                        ? null
                        : JsonSerializer.Serialize(enrichment, WeeklyBriefStoreJson.Options),
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
                CampaignRecipientsReached: 0,
                UnsubscribesInWeek: 0
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
            bool softLock = false,
            string? weekStartsOn = null,
            DateTime? locationCreatedAtUtc = null,
            string? subscriptionPlan = null
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
                WeekStartsOn = weekStartsOn,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                "TUMMLY-UK-GBP-2026-08-V3"
            );
            if (subscriptionPlan is not null)
            {
                billing.SubscriptionPlan = subscriptionPlan;
                if (
                    !string.Equals(
                        subscriptionPlan,
                        BillingSubscriptionPlans.Pilot,
                        StringComparison.Ordinal
                    )
                )
                {
                    billing.BillingStatus = BillingStatuses.Active;
                    billing.BillingCycle = BillingCycles.Monthly;
                }
            }

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
                CreatedAt = locationCreatedAtUtc ?? DateTime.UtcNow,
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

        /// <summary>
        /// Week-starts-on matching today's London weekday so IsGenerateDay is true.
        /// </summary>
        private static string CurrentLondonGenerateWeekStartsOn()
        {
            var utcNow = DateTime.UtcNow;
            foreach (var day in WorkspaceDefaultsOptions.WeekStartsOnValues)
            {
                if (
                    WeeklyBriefWeekKey.IsGenerateDay(
                        WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                        utcNow,
                        day
                    )
                )
                {
                    return day;
                }
            }

            return WorkspaceDefaultsOptions.DefaultWeekStartsOn;
        }

        private static string NonGenerateWeekStartsOn()
        {
            var current = CurrentLondonGenerateWeekStartsOn();
            return string.Equals(current, "monday", StringComparison.Ordinal)
                ? "tuesday"
                : "monday";
        }

        /// <summary>
        /// Seed a location eligible for lazy generate (created before closed-week
        /// coverage end + non-Pilot plan). Week-starts-on may be any day; manual
        /// generate is not limited to IsGenerateDay.
        /// </summary>
        private async Task<(
            string Jwt,
            int LocationId,
            int UserId,
            WeeklyBriefClosedWeek ClosedWeek
        )> SeedEligibleForGenerateAsync(string emailLocalPart)
        {
            var utcNow = DateTime.UtcNow;
            var weekStartsOn = WorkspaceDefaultsOptions.DefaultWeekStartsOn;
            var closed = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                utcNow,
                weekStartsOn
            );
            var seeded = await SeedOwnerWithLocationAsync(
                emailLocalPart,
                weekStartsOn: weekStartsOn,
                locationCreatedAtUtc: closed.CoverageStartUtc.AddDays(-7),
                subscriptionPlan: BillingSubscriptionPlans.Growth
            );
            return (seeded.Jwt, seeded.LocationId, seeded.UserId, closed);
        }

        /// <summary>
        /// Mirror capture + feedback across prior and current closed weeks so
        /// emit yields sole no-material-change (or needs-attention control only).
        /// </summary>
        private async Task SeedStableWeekActivityAsync(
            int locationId,
            WeeklyBriefClosedWeek closedWeek,
            bool needsAttention
        )
        {
            Assert.True(
                WeeklyBriefWeekKey.TryPriorWeekKey(
                    closedWeek.WeekKey,
                    out var priorKey
                )
            );
            Assert.True(
                WeeklyBriefWeekKey.TryCoverageWindow(
                    priorKey,
                    WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                    out var priorFrom,
                    out _
                )
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations.FindAsync(locationId);
            Assert.NotNull(location);

            var qr = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = QrType.CounterCard,
                Token = $"wbrpta{locationId}{Guid.NewGuid():N}"[..32],
                Status = QrCodeStatus.Active,
            };
            context.QrCodes.Add(qr);
            await context.SaveChangesAsync();

            for (var i = 0; i < 5; i++)
            {
                await AddGuestWithMasterAsync(
                    context,
                    location!.RestaurantId,
                    locationId,
                    $"Prior Guest {i}",
                    priorFrom.AddHours(i + 1)
                );
                await AddGuestWithMasterAsync(
                    context,
                    location.RestaurantId,
                    locationId,
                    $"Current Guest {i}",
                    closedWeek.CoverageStartUtc.AddHours(i + 1)
                );
                context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        CreatedAt = priorFrom.AddHours(i + 1),
                    }
                );
                context.QrScanEvents.Add(
                    new QrScanEvent
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        CreatedAt = closedWeek.CoverageStartUtc.AddHours(i + 1),
                    }
                );
            }

            for (var i = 0; i < 3; i++)
            {
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        GuestName = $"Prior Feedback {i}",
                        GuestContact =
                            $"prior-rpta-{locationId}-{i}@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Steady prior week",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Positive,
                        WorkflowStatus = FeedbackWorkflowStatus.Resolved,
                        CreatedAt = priorFrom.AddHours(i + 2),
                    }
                );
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = locationId,
                        QrCodeId = qr.Id,
                        GuestName = $"Current Feedback {i}",
                        GuestContact =
                            $"current-rpta-{locationId}-{i}@example.com",
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
                        CreatedAt = closedWeek.CoverageStartUtc.AddHours(i + 2),
                    }
                );
            }

            await context.SaveChangesAsync();
        }

        private static async Task AddGuestWithMasterAsync(
            ApplicationDbContext context,
            int restaurantId,
            int locationId,
            string name,
            DateTime createdAt
        )
        {
            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = $"wb-rpta-{Guid.NewGuid():N}@example.com",
                CreatedAt = createdAt,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(
                new LocationGuest
                {
                    RestaurantLocationId = locationId,
                    MasterGuestId = master.Id,
                    Name = name,
                    CreatedAt = createdAt,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task SetBillingStatusAsync(
            int locationId,
            string billingStatus
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.RestaurantId)
                .FirstAsync();
            var account = await context.BillingAccounts
                .FirstAsync(a => a.RestaurantId == restaurantId);
            account.BillingStatus = billingStatus;
            if (billingStatus == BillingStatuses.Dormant)
            {
                account.DormantEnteredAt = DateTime.UtcNow.AddDays(-1);
            }

            await context.SaveChangesAsync();
        }

        private async Task SetChargebackRestrictedAsync(
            int locationId,
            bool restricted
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(l => l.Id == locationId)
                .Select(l => l.RestaurantId)
                .FirstAsync();
            var account = await context.BillingAccounts
                .FirstAsync(a => a.RestaurantId == restaurantId);
            account.ChargebackRestricted = restricted;
            await context.SaveChangesAsync();
        }
    }
}
