using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestsListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestsListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGuests_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/guests?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("guests-owner-a-token-123456");
            var other = await SeedOwnerAsync(
                "guests-owner-b-token-123456",
                email: "guests-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns404_ForUnknownLocation()
        {
            var owner = await SeedOwnerAsync("guests-unknown-loc-token-12");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(999_999)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenPageSizeNot25()
        {
            var owner = await SeedOwnerAsync("guests-pagesize-token-123456");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&pageSize=50"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenInvalidSmartGroup()
        {
            var owner = await SeedOwnerAsync("guests-bad-group-token-12345");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&smartGroup=not-a-group"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenInvalidSort()
        {
            var owner = await SeedOwnerAsync("guests-bad-sort-token-123456");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&sort=not-a-sort"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenPageLessThanOne()
        {
            var owner = await SeedOwnerAsync("guests-bad-page-token-123456");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&page=0"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_ReturnsEnvelopeOverviewAndRows()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-envelope-token-123456789"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(seeded.LocationId, body.GetProperty("locationId").GetInt32());
            Assert.Equal("all-guests", body.GetProperty("smartGroup").GetString());
            Assert.Equal("", body.GetProperty("q").GetString());
            Assert.Equal("recent-activity", body.GetProperty("sort").GetString());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());
            Assert.Equal(5, body.GetProperty("totalFilteredCount").GetInt32());

            var overview = body.GetProperty("overview");
            Assert.Equal(5, overview.GetProperty("totalGuests").GetInt32());
            Assert.Equal(3, overview.GetProperty("newThisMonth").GetInt32());
            Assert.Equal(3, overview.GetProperty("marketingEligible").GetInt32());
            Assert.Equal(1, overview.GetProperty("needsRecovery").GetInt32());

            var counts = body.GetProperty("smartGroupCounts");
            Assert.Equal(5, counts.GetProperty("all-guests").GetInt32());
            Assert.Equal(3, counts.GetProperty("new-guests").GetInt32());
            Assert.Equal(1, counts.GetProperty("needs-recovery").GetInt32());
            Assert.Equal(1, counts.GetProperty("positive-feedback").GetInt32());
            Assert.Equal(0, counts.GetProperty("offer-not-redeemed").GetInt32());
            Assert.Equal(0, counts.GetProperty("recent-redeemers").GetInt32());
            Assert.Equal(1, counts.GetProperty("dormant-guests").GetInt32());

            var rows = body.GetProperty("rows").EnumerateArray().ToList();
            Assert.Equal(5, rows.Count);

            var jane = rows.Single(r =>
                r.GetProperty("name").GetString() == "Jane Doe"
            );
            Assert.Equal(seeded.JaneLocationGuestId.ToString(), jane.GetProperty("id").GetString());
            Assert.Equal("jane@example.com", jane.GetProperty("email").GetString());
            Assert.True(jane.GetProperty("mobile").ValueKind == JsonValueKind.Null);
            Assert.Equal("Eligible — Email", jane.GetProperty("marketingStatus").GetString());
            Assert.Equal("Camden Street", jane.GetProperty("locationName").GetString());
            Assert.Equal("positive", jane.GetProperty("latestFeedbackSentiment").GetString());
            Assert.Equal(2, jane.GetProperty("feedbackSubmissionCount").GetInt32());
            Assert.Equal("Feedback submitted", jane.GetProperty("lastInteractionLabel").GetString());
            Assert.NotNull(jane.GetProperty("lastInteractionAt").GetString());
            Assert.NotNull(jane.GetProperty("capturedAt").GetString());
        }

        [Fact]
        public async Task GetGuests_OmitsOverviewAndCounts_WhenIncludeAggregatesFalse()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-no-aggregates-token-1234"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=new-guests&includeAggregates=false"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("new-guests", body.GetProperty("smartGroup").GetString());
            Assert.Equal(3, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(JsonValueKind.Null, body.GetProperty("overview").ValueKind);
            Assert.Equal(
                JsonValueKind.Null,
                body.GetProperty("smartGroupCounts").ValueKind
            );
            Assert.Equal(3, body.GetProperty("rows").GetArrayLength());
        }

        [Fact]
        public async Task GetGuests_OverviewAndCountsStayLocationWide_WhenFiltered()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-wide-counts-token-123456"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=new-guests&q=jane"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(5, body.GetProperty("overview").GetProperty("totalGuests").GetInt32());
            Assert.Equal(
                3,
                body.GetProperty("smartGroupCounts").GetProperty("new-guests").GetInt32()
            );
            Assert.Single(body.GetProperty("rows").EnumerateArray());
        }

        [Fact]
        public async Task GetGuests_FiltersNewGuestsSmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-new-group-token-1234567"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=new-guests"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(3, body.GetProperty("totalFilteredCount").GetInt32());
            var names = body.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .OrderBy(n => n)
                .ToList();
            Assert.Equal(
                new[] { "Jane Doe", "No Feedback", "Opt Out Sam" },
                names
            );
        }

        [Fact]
        public async Task GetGuests_FiltersPositiveFeedbackSmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-positive-token-123456789"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=positive-feedback"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Jane Doe",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GetGuests_FiltersNeedsRecoverySmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-needs-recovery-token-123"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=needs-recovery"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Old Pat",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );
            Assert.Equal(
                1,
                body.GetProperty("smartGroupCounts")
                    .GetProperty("needs-recovery")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_FiltersDormantGuestsSmartGroup()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-dormant-token-1234567890"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=dormant-guests"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Old Pat",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Theory]
        [InlineData("offer-not-redeemed")]
        [InlineData("recent-redeemers")]
        public async Task GetGuests_DeferredSmartGroups_ReturnZeroRows(
            string smartGroup
        )
        {
            var seeded = await SeedGuestsScenarioAsync(
                $"guests-deferred-{smartGroup}-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup={smartGroup}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(0, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Empty(body.GetProperty("rows").EnumerateArray());
            Assert.Equal(
                0,
                body.GetProperty("smartGroupCounts").GetProperty(smartGroup).GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_SearchMatchesNameEmailOrMobile()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-search-token-1234567890"
            );

            using var emailRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&q=07700900456"
            );
            emailRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var emailResponse = await _client.SendAsync(emailRequest);
            var emailBody = await ReadJsonAsync(emailResponse);
            Assert.Equal(1, emailBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Bob Mobile",
                emailBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );

            using var nameRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&q=opt"
            );
            nameRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var nameResponse = await _client.SendAsync(nameRequest);
            var nameBody = await ReadJsonAsync(nameResponse);
            Assert.Equal(1, nameBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Opt Out Sam",
                nameBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GetGuests_SortsAndPaginates()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-sort-page-token-123456789"
            );

            using var sortRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=guest-name-az"
            );
            sortRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var sortResponse = await _client.SendAsync(sortRequest);
            var sortBody = await ReadJsonAsync(sortResponse);
            var sortedNames = sortBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .ToList();
            Assert.Equal(
                new[]
                {
                    "Bob Mobile",
                    "Jane Doe",
                    "No Feedback",
                    "Old Pat",
                    "Opt Out Sam",
                },
                sortedNames
            );

            using var pageRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=guest-name-az&page=2&pageSize=25"
            );
            pageRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var pageResponse = await _client.SendAsync(pageRequest);
            var pageBody = await ReadJsonAsync(pageResponse);
            Assert.Equal(5, pageBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Empty(pageBody.GetProperty("rows").EnumerateArray());
        }

        [Fact]
        public async Task GetGuests_FiltersByMarketingContactSentimentAndTags()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-filters-mix-token-123456"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var vip = new GuestTag
                {
                    RestaurantId = seeded.RestaurantId,
                    DisplayName = "VIP",
                    NormalizedName = "vip",
                    AiSourced = false,
                    CreatedAt = DateTime.UtcNow,
                };
                context.GuestTags.Add(vip);
                await context.SaveChangesAsync();
                context.LocationGuestTags.Add(
                    new LocationGuestTag
                    {
                        LocationGuestId = seeded.JaneLocationGuestId,
                        GuestTagId = vip.Id,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
                seeded = seeded with { VipTagId = vip.Id };
            }

            using var marketingRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&marketing=eligible"
            );
            marketingRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var marketingBody = await ReadJsonAsync(
                await _client.SendAsync(marketingRequest)
            );
            Assert.Equal(3, marketingBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                5,
                marketingBody.GetProperty("overview").GetProperty("totalGuests").GetInt32()
            );

            using var notOptedRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&marketing=not-opted-in"
            );
            notOptedRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var notOptedBody = await ReadJsonAsync(
                await _client.SendAsync(notOptedRequest)
            );
            Assert.Equal(2, notOptedBody.GetProperty("totalFilteredCount").GetInt32());

            using var contactRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&contact=mobile"
            );
            contactRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var contactBody = await ReadJsonAsync(
                await _client.SendAsync(contactRequest)
            );
            Assert.Equal(1, contactBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Bob Mobile",
                contactBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );

            using var sentimentRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sentiment=neutral"
            );
            sentimentRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var sentimentBody = await ReadJsonAsync(
                await _client.SendAsync(sentimentRequest)
            );
            var sentimentNames = sentimentBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .OrderBy(n => n)
                .ToList();
            // Jane has older neutral + latest positive — only latest counts.
            Assert.Equal(new[] { "Opt Out Sam" }, sentimentNames);

            using var tagRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&tagIds={seeded.VipTagId}"
            );
            tagRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var tagBody = await ReadJsonAsync(await _client.SendAsync(tagRequest));
            Assert.Equal(1, tagBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Jane Doe",
                tagBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GetGuests_FiltersByDateAxisAndPreset()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-date-filter-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&dateAxis=first-captured&datePreset=last-7"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var names = body.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("name").GetString())
                .OrderBy(n => n)
                .ToList();

            Assert.Equal(new[] { "Jane Doe", "No Feedback" }, names);
            Assert.Equal(5, body.GetProperty("overview").GetProperty("totalGuests").GetInt32());
        }

        [Fact]
        public async Task GetGuests_OverviewUsesFirstCapturedWindow_IndependentOfTableFilters()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-overview-date-token-123"
            );
            var from = Uri.EscapeDataString(
                DateTime.UtcNow.AddDays(-7).ToString("o")
            );
            var to = Uri.EscapeDataString(DateTime.UtcNow.ToString("o"));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&overviewDateFrom={from}&overviewDateTo={to}&marketing=eligible&q=jane"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            Assert.Equal(1, body.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Jane Doe",
                body.GetProperty("rows")[0].GetProperty("name").GetString()
            );

            var overview = body.GetProperty("overview");
            Assert.Equal(2, overview.GetProperty("totalGuests").GetInt32());
            Assert.Equal(1, overview.GetProperty("marketingEligible").GetInt32());
            Assert.Equal(0, overview.GetProperty("needsRecovery").GetInt32());
            Assert.Equal(
                3,
                body.GetProperty("smartGroupCounts").GetProperty("new-guests").GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_LocationOverrideScopesRowsCountsAndOverview()
        {
            var seeded = await SeedMultiLocationGuestsAsync(
                "guests-loc-override-token-12"
            );

            using var allRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationAId)}&locationScope=all"
            );
            allRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var allBody = await ReadJsonAsync(await _client.SendAsync(allRequest));
            Assert.Equal(2, allBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(2, allBody.GetProperty("overview").GetProperty("totalGuests").GetInt32());
            Assert.Equal(
                2,
                allBody.GetProperty("smartGroupCounts").GetProperty("all-guests").GetInt32()
            );

            using var idsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationAId)}&locationIds={seeded.LocationBId}"
            );
            idsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var idsBody = await ReadJsonAsync(await _client.SendAsync(idsRequest));
            Assert.Equal(1, idsBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Location B Guest",
                idsBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );
            Assert.Equal(
                "Second Street",
                idsBody.GetProperty("rows")[0].GetProperty("locationName").GetString()
            );
            Assert.Equal(1, idsBody.GetProperty("overview").GetProperty("totalGuests").GetInt32());
        }

        [Fact]
        public async Task GetGuests_Returns403_WhenLocationIdsNotOwned()
        {
            var owner = await SeedOwnerAsync("guests-loc-ids-owner-token1");
            var other = await SeedOwnerAsync(
                "guests-loc-ids-other-token1",
                email: "guests-loc-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&locationIds={other.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("recovery=open")]
        [InlineData("engagement=high")]
        [InlineData("unsubscribed=email")]
        [InlineData("suppressed=true")]
        [InlineData("invalid-contact=1")]
        [InlineData("email-and-mobile=1")]
        public async Task GetGuests_Returns400_WhenDeferredFilterPresent(
            string deferredQuery
        )
        {
            var owner = await SeedOwnerAsync($"guests-deferred-{deferredQuery[..8]}-tok");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&{deferredQuery}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_OverviewPresetLast7_ScopesFirstCapturedCohort()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-overview-preset-token1"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&overviewDatePreset=last-7"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var body = await ReadJsonAsync(await _client.SendAsync(request));
            var overview = body.GetProperty("overview");
            Assert.Equal(2, overview.GetProperty("totalGuests").GetInt32());
            Assert.Equal(1, overview.GetProperty("marketingEligible").GetInt32());
            Assert.Equal(5, body.GetProperty("totalFilteredCount").GetInt32());
        }

        [Fact]
        public async Task GetGuests_Returns400_WhenCustomDateExceeds180Days()
        {
            var owner = await SeedOwnerAsync("guests-date-180-token-123456");
            var from = Uri.EscapeDataString(
                DateTime.UtcNow.AddDays(-200).ToString("o")
            );
            var to = Uri.EscapeDataString(DateTime.UtcNow.ToString("o"));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(owner.LocationId)}&dateAxis=first-captured&dateFrom={from}&dateTo={to}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetGuests_MostRecentRedemptionSort_FallsBackToRecentActivity()
        {
            var seeded = await SeedGuestsScenarioAsync(
                "guests-redemption-sort-token-123"
            );

            using var recentRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=recent-activity"
            );
            recentRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            using var redemptionRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sort=most-recent-redemption"
            );
            redemptionRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var recentBody = await ReadJsonAsync(
                await _client.SendAsync(recentRequest)
            );
            var redemptionBody = await ReadJsonAsync(
                await _client.SendAsync(redemptionRequest)
            );

            var recentIds = recentBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("id").GetString())
                .ToList();
            var redemptionIds = redemptionBody.GetProperty("rows")
                .EnumerateArray()
                .Select(r => r.GetProperty("id").GetString())
                .ToList();

            Assert.Equal(recentIds, redemptionIds);
        }

        [Fact]
        public async Task GetGuests_DerivesMarketingStatusAndSentimentFromLatestSucceeded()
        {
            var seeded = await SeedOwnerAsync("guests-derive-token-123456789");

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = seeded.RestaurantId,
                Email = "mixed@example.com",
                NormalizedEmail = "mixed@example.com",
                Mobile = "07700900999",
                NormalizedPhone = "07700900999",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = seeded.LocationId,
                Name = "Mixed Channels",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            context.Feedbacks.AddRange(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    LocationGuestId = locationGuest.Id,
                    GuestName = "Mixed Channels",
                    GuestContact = "mixed@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Older positive",
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Positive,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                },
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    LocationGuestId = locationGuest.Id,
                    GuestName = "Mixed Channels",
                    GuestContact = "mixed@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Latest pending",
                    ClassificationStatus = ClassificationStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                }
            );
            await context.SaveChangesAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            var row = body.GetProperty("rows")
                .EnumerateArray()
                .Single();

            Assert.Equal("Eligible — Email", row.GetProperty("marketingStatus").GetString());
            Assert.Equal("positive", row.GetProperty("latestFeedbackSentiment").GetString());
        }

        [Fact]
        public async Task GetGuests_NeedsRecovery_IncludesGuestWhenOlderNegativeRemainsAfterLatestPositive()
        {
            var seeded = await SeedOwnerAsync(
                "guests-needs-both-groups-token1"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var master = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = "both-groups@example.com",
                    NormalizedEmail = "both-groups@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Both Groups",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();

                context.Feedbacks.AddRange(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = locationGuest.Id,
                        GuestName = "Both Groups",
                        GuestContact = "both-groups@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Older negative",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        CreatedAt = DateTime.UtcNow.AddDays(-8),
                    },
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = locationGuest.Id,
                        GuestName = "Both Groups",
                        GuestContact = "both-groups@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Latest positive",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Positive,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var needsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=needs-recovery"
            );
            needsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var needsBody = await ReadJsonAsync(
                await _client.SendAsync(needsRequest)
            );
            Assert.Equal(1, needsBody.GetProperty("totalFilteredCount").GetInt32());
            Assert.Equal(
                "Both Groups",
                needsBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );

            using var positiveRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&smartGroup=positive-feedback"
            );
            positiveRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var positiveBody = await ReadJsonAsync(
                await _client.SendAsync(positiveRequest)
            );
            Assert.Equal(
                1,
                positiveBody.GetProperty("totalFilteredCount").GetInt32()
            );
            Assert.Equal(
                "Both Groups",
                positiveBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );
        }

        [Theory]
        [InlineData(ClassificationStatus.Pending)]
        [InlineData(ClassificationStatus.Failed)]
        public async Task GetGuests_NeedsRecovery_ExcludesNonSucceededNegativeClassification(
            ClassificationStatus status
        )
        {
            var seeded = await SeedOwnerAsync(
                $"guests-needs-{status.ToString().ToLowerInvariant()}-token"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var master = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = $"{status.ToString().ToLowerInvariant()}-neg@example.com",
                    NormalizedEmail =
                        $"{status.ToString().ToLowerInvariant()}-neg@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = $"{status} Neg",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();

                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = locationGuest.Id,
                        GuestName = $"{status} Neg",
                        GuestContact =
                            $"{status.ToString().ToLowerInvariant()}-neg@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Non-succeeded negative",
                        ClassificationStatus = status,
                        Sentiment = FeedbackSentiment.Negative,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var body = await ReadJsonAsync(await _client.SendAsync(request));

            Assert.Equal(0, body.GetProperty("overview").GetProperty("needsRecovery").GetInt32());
            Assert.Equal(
                0,
                body.GetProperty("smartGroupCounts")
                    .GetProperty("needs-recovery")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_NeedsRecovery_ClearsWhenOnlyNegativeIsCorrectedAway()
        {
            var seeded = await SeedOwnerAsync(
                "guests-needs-corrected-token-12"
            );

            int locationGuestId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var master = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = "corrected@example.com",
                    NormalizedEmail = "corrected@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Corrected Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();
                locationGuestId = locationGuest.Id;

                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = locationGuest.Id,
                        GuestName = "Corrected Guest",
                        GuestContact = "corrected@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Was negative",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var beforeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            beforeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var beforeBody = await ReadJsonAsync(
                await _client.SendAsync(beforeRequest)
            );
            Assert.Equal(
                1,
                beforeBody.GetProperty("overview").GetProperty("needsRecovery").GetInt32()
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var feedback = context.Feedbacks.Single(f =>
                    f.LocationGuestId == locationGuestId
                );
                feedback.Sentiment = FeedbackSentiment.Neutral;
                await context.SaveChangesAsync();
            }

            using var afterRequest = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            afterRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var afterBody = await ReadJsonAsync(
                await _client.SendAsync(afterRequest)
            );
            Assert.Equal(
                0,
                afterBody.GetProperty("overview").GetProperty("needsRecovery").GetInt32()
            );
            Assert.Equal(
                0,
                afterBody.GetProperty("smartGroupCounts")
                    .GetProperty("needs-recovery")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_NeedsRecoveryOverview_UsesFeedbackSubmissionTimeNotFirstCaptured()
        {
            var seeded = await SeedOwnerAsync(
                "guests-needs-overview-fb-token"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                // Morgan: first-captured long ago; Succeeded Negative submitted recently.
                var morganMaster = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = "morgan@example.com",
                    NormalizedEmail = "morgan@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-200),
                };
                // Riley: still Needs recovery via old Negative; recent Feedback was
                // corrected away from Negative so windowed KPI should exclude them.
                var rileyMaster = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = "riley@example.com",
                    NormalizedEmail = "riley@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-150),
                };
                context.MasterGuests.AddRange(morganMaster, rileyMaster);
                await context.SaveChangesAsync();

                var morgan = new LocationGuest
                {
                    MasterGuestId = morganMaster.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Morgan",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-200),
                };
                var riley = new LocationGuest
                {
                    MasterGuestId = rileyMaster.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Riley",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-150),
                };
                context.LocationGuests.AddRange(morgan, riley);
                await context.SaveChangesAsync();

                context.Feedbacks.AddRange(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = morgan.Id,
                        GuestName = "Morgan",
                        GuestContact = "morgan@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Recent negative",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                    },
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = riley.Id,
                        GuestName = "Riley",
                        GuestContact = "riley@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Old unresolved negative",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        CreatedAt = DateTime.UtcNow.AddDays(-90),
                    },
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = riley.Id,
                        GuestName = "Riley",
                        GuestContact = "riley@example.com",
                        ContactType = ContactType.Email,
                        Comment = "In-window but corrected to neutral",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Neutral,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var allTimeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                GuestsUrl(seeded.LocationId)
            );
            allTimeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var allTimeBody = await ReadJsonAsync(
                await _client.SendAsync(allTimeRequest)
            );
            Assert.Equal(
                2,
                allTimeBody.GetProperty("overview").GetProperty("needsRecovery").GetInt32()
            );
            Assert.Equal(
                2,
                allTimeBody.GetProperty("smartGroupCounts")
                    .GetProperty("needs-recovery")
                    .GetInt32()
            );

            using var windowRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&overviewDatePreset=last-7"
            );
            windowRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var windowBody = await ReadJsonAsync(
                await _client.SendAsync(windowRequest)
            );

            // First-captured window would exclude both; Feedback-time includes Morgan only.
            Assert.Equal(
                0,
                windowBody.GetProperty("overview").GetProperty("totalGuests").GetInt32()
            );
            Assert.Equal(
                1,
                windowBody.GetProperty("overview").GetProperty("needsRecovery").GetInt32()
            );
            // Smart Group membership stays independent of overview window.
            Assert.Equal(
                2,
                windowBody.GetProperty("smartGroupCounts")
                    .GetProperty("needs-recovery")
                    .GetInt32()
            );
        }

        [Fact]
        public async Task GetGuests_SentimentFilterUsesLatestSucceededClassification()
        {
            var seeded = await SeedOwnerAsync(
                "guests-sentiment-latest-token-12"
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var master = new MasterGuest
                {
                    RestaurantId = seeded.RestaurantId,
                    Email = "mixed-sentiment@example.com",
                    NormalizedEmail = "mixed-sentiment@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Mixed Sentiment",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();

                context.Feedbacks.AddRange(
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = locationGuest.Id,
                        GuestName = "Mixed Sentiment",
                        GuestContact = "mixed-sentiment@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Older negative",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        CreatedAt = DateTime.UtcNow.AddDays(-4),
                    },
                    new Feedback
                    {
                        RestaurantLocationId = seeded.LocationId,
                        LocationGuestId = locationGuest.Id,
                        GuestName = "Mixed Sentiment",
                        GuestContact = "mixed-sentiment@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Latest positive",
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Positive,
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

            using var negativeRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sentiment=negative"
            );
            negativeRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var negativeBody = await ReadJsonAsync(
                await _client.SendAsync(negativeRequest)
            );
            Assert.Equal(
                0,
                negativeBody.GetProperty("totalFilteredCount").GetInt32()
            );

            using var positiveRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{GuestsUrl(seeded.LocationId)}&sentiment=positive"
            );
            positiveRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var positiveBody = await ReadJsonAsync(
                await _client.SendAsync(positiveRequest)
            );
            Assert.Equal(
                1,
                positiveBody.GetProperty("totalFilteredCount").GetInt32()
            );
            Assert.Equal(
                "Mixed Sentiment",
                positiveBody.GetProperty("rows")[0].GetProperty("name").GetString()
            );
            Assert.Equal(
                "positive",
                positiveBody
                    .GetProperty("rows")[0]
                    .GetProperty("latestFeedbackSentiment")
                    .GetString()
            );
        }

        private static string GuestsUrl(int locationId)
        {
            return $"/api/guests?locationId={locationId}";
        }

        private async Task<(string Jwt, int LocationId, int RestaurantId)> SeedOwnerAsync(
            string linkToken,
            string email = "guests-owner@example.com",
            string locationName = "Camden Street"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guests Owner",
                Email = email,
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
                Name = "Guests Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
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

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<GuestsScenarioSeed> SeedGuestsScenarioAsync(
            string linkToken
        )
        {
            var owner = await SeedOwnerAsync(linkToken, locationName: "Camden Street");
            var now = DateTime.UtcNow;

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            async Task<(int LocationGuestId, MasterGuest Master)> AddGuestAsync(
                string name,
                string? email,
                string? normalizedEmail,
                string? mobile,
                string? normalizedPhone,
                bool offersOptOut,
                DateTime capturedAt,
                IEnumerable<(DateTime CreatedAt, ClassificationStatus Status, FeedbackSentiment? Sentiment)> feedbacks
            )
            {
                var master = new MasterGuest
                {
                    RestaurantId = owner.RestaurantId,
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    Mobile = mobile,
                    NormalizedPhone = normalizedPhone,
                    CreatedAt = capturedAt,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                var locationGuest = new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = owner.LocationId,
                    Name = name,
                    MarketingPreference = LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(offersOptOut),

                    CreatedAt = capturedAt,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();

                foreach (var feedback in feedbacks)
                {
                    context.Feedbacks.Add(
                        new Feedback
                        {
                            RestaurantLocationId = owner.LocationId,
                            LocationGuestId = locationGuest.Id,
                            GuestName = name,
                            GuestContact = email ?? mobile ?? "unknown",
                            ContactType = email != null
                                ? ContactType.Email
                                : mobile != null
                                    ? ContactType.Phone
                                    : ContactType.Unknown,
                            Comment = "Visit note",
                            OffersOptOut = offersOptOut,
                            ClassificationStatus = feedback.Status,
                            Sentiment = feedback.Sentiment,
                            CreatedAt = feedback.CreatedAt,
                        }
                    );
                }

                await context.SaveChangesAsync();
                return (locationGuest.Id, master);
            }

            var jane = await AddGuestAsync(
                "Jane Doe",
                "jane@example.com",
                "jane@example.com",
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-5),
                feedbacks:
                [
                    (now.AddDays(-6), ClassificationStatus.Succeeded, FeedbackSentiment.Neutral),
                    (now.AddDays(-2), ClassificationStatus.Succeeded, FeedbackSentiment.Positive),
                ]
            );

            await AddGuestAsync(
                "Bob Mobile",
                null,
                null,
                "07700 900456",
                "07700900456",
                offersOptOut: false,
                capturedAt: now.AddDays(-60),
                feedbacks:
                [
                    (now.AddDays(-10), ClassificationStatus.Pending, null),
                ]
            );

            await AddGuestAsync(
                "Old Pat",
                "pat@example.com",
                "pat@example.com",
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-200),
                feedbacks:
                [
                    (now.AddDays(-100), ClassificationStatus.Succeeded, FeedbackSentiment.Negative),
                ]
            );

            await AddGuestAsync(
                "Opt Out Sam",
                "sam@example.com",
                "sam@example.com",
                null,
                null,
                offersOptOut: true,
                capturedAt: now.AddDays(-10),
                feedbacks:
                [
                    (now.AddDays(-8), ClassificationStatus.Succeeded, FeedbackSentiment.Neutral),
                ]
            );

            await AddGuestAsync(
                "No Feedback",
                null,
                null,
                null,
                null,
                offersOptOut: false,
                capturedAt: now.AddDays(-2),
                feedbacks: Array.Empty<(DateTime, ClassificationStatus, FeedbackSentiment?)>()
            );

            return new GuestsScenarioSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                jane.LocationGuestId,
                VipTagId: 0
            );
        }

        private async Task<MultiLocationGuestsSeed> SeedMultiLocationGuestsAsync(
            string linkTokenPrefix
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Multi Guests Owner",
                Email = "guests-multi@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Multi Guests Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var locationA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var locationB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Second Street",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(locationA, locationB);
            await context.SaveChangesAsync();

            var masterA = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "a@example.com",
                NormalizedEmail = "a@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            var masterB = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "b@example.com",
                NormalizedEmail = "b@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(masterA, masterB);
            await context.SaveChangesAsync();

            context.LocationGuests.AddRange(
                new LocationGuest
                {
                    MasterGuestId = masterA.Id,
                    RestaurantLocationId = locationA.Id,
                    Name = "Location A Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                },
                new LocationGuest
                {
                    MasterGuestId = masterB.Id,
                    RestaurantLocationId = locationB.Id,
                    Name = "Location B Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationGuestsSeed(
                jwt,
                locationA.Id,
                locationB.Id
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private sealed record GuestsScenarioSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int JaneLocationGuestId,
            int VipTagId
        );

        private sealed record MultiLocationGuestsSeed(
            string Jwt,
            int LocationAId,
            int LocationBId
        );
    }
}
