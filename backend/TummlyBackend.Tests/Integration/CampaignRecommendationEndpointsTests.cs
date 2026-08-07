using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignRecommendationEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignRecommendationEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostRecommendation_WeakSignals_ReturnsNoneWithoutCallingProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-rec-weak"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "thank-recent-guests",
                    Title: "Should not be used",
                    Opportunity: "n/a",
                    EligibleAudience: "n/a",
                    WhyBullets: ["n/a"],
                    SuggestedChannel: "email",
                    EstimatedUsage: "n/a",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "thank-recent-guests",
                        AudienceKey: "new-guests",
                        Channel: "email",
                        OfferStance: "no-offer",
                        CampaignName: "n/a",
                        MessageSubject: "n/a",
                        MessageBody: "n/a"
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_Success_ReturnsAllowListedTypeAndEchoedCounts()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-success"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "thank-recent-guests",
                    Title: "Thank guests who recently joined",
                    Opportunity:
                        "Several guests joined recently and are ready for a thank-you.",
                    EligibleAudience:
                        "Guests captured in the last two weeks with marketing permission.",
                    WhyBullets:
                    [
                        "Have a valid marketing permission",
                        "Have a reachable email or mobile number",
                    ],
                    SuggestedChannel: "email",
                    EstimatedUsage: "Within current allowance",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "thank-recent-guests",
                        AudienceKey: "new-guests",
                        Channel: "email",
                        OfferStance: "no-offer",
                        CampaignName: "Thank you for joining",
                        MessageSubject: "Thanks for joining us",
                        MessageBody: "Thank you for joining our guest list."
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var recommendation = body.GetProperty("recommendation");
            Assert.Equal(
                "thank-recent-guests",
                recommendation.GetProperty("type").GetString()
            );
            Assert.Equal(
                "Thank guests who recently joined",
                recommendation.GetProperty("title").GetString()
            );
            Assert.Equal("email", recommendation.GetProperty("suggestedChannel").GetString());
            Assert.Equal("Main", recommendation.GetProperty("locationName").GetString());

            var echoed = recommendation.GetProperty("echoedCounts");
            Assert.True(echoed.GetProperty("marketingEligible").GetInt32() >= 1);
            Assert.True(echoed.GetProperty("allGuests").GetInt32() >= 1);
            Assert.True(echoed.TryGetProperty("newGuests", out _));
            Assert.True(echoed.TryGetProperty("needsRecovery", out _));
            Assert.True(echoed.TryGetProperty("positiveFeedback", out _));
            Assert.True(echoed.TryGetProperty("dormantGuests", out _));

            var prefill = recommendation.GetProperty("draftPrefill");
            Assert.Equal(
                "thank-recent-guests",
                prefill.GetProperty("goalId").GetString()
            );
            Assert.Equal("new-guests", prefill.GetProperty("audienceKey").GetString());
            Assert.False(prefill.TryGetProperty("templateId", out _));

            Assert.Equal(1, fake.CallCount);
            Assert.NotNull(fake.LastInput);
            var serialized = JsonSerializer.Serialize(fake.LastInput);
            Assert.DoesNotContain("eligible@example.com", serialized);
            Assert.DoesNotContain("07700900999", serialized);
        }

        [Fact]
        public async Task PostRecommendation_ProviderNone_ReturnsNoneEnvelope()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-none"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "none",
                    Title: null,
                    Opportunity: null,
                    EligibleAudience: null,
                    WhyBullets: null,
                    SuggestedChannel: null,
                    EstimatedUsage: null,
                    DraftPrefill: null
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_ProviderFail_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-fail"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.Fail(retryable: true);

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_CachesByOperatorLocationWindow_RefreshBypasses()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-cache"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "re-engage",
                    Title: "Re-engage dormant guests",
                    Opportunity: "Some guests have been quiet.",
                    EligibleAudience: "Dormant guests with permission.",
                    WhyBullets: ["No recent Guest Loop activity"],
                    SuggestedChannel: "sms",
                    EstimatedUsage: "Within current allowance",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "re-engage-inactive",
                        AudienceKey: "dormant-guests",
                        Channel: "sms",
                        OfferStance: "no-offer",
                        CampaignName: "We miss you",
                        MessageSubject: null,
                        MessageBody: "We would love to see you again."
                    )
                )
            );

            using (var first = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            ))
            {
                var firstResponse = await _client.SendAsync(first);
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            }

            Assert.Equal(1, fake.CallCount);

            using (var second = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            ))
            {
                var secondResponse = await _client.SendAsync(second);
                Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            }

            Assert.Equal(1, fake.CallCount);

            var refreshBody = Last30Body(seeded.LocationId);
            refreshBody["refresh"] = true;
            using (var refresh = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                refreshBody
            ))
            {
                var refreshResponse = await _client.SendAsync(refresh);
                Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
            }

            Assert.Equal(2, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_StripsDisallowedTypes_AsFailure()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-strip"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "unredeemed-offer",
                    Title: "Bad type",
                    Opportunity: "n/a",
                    EligibleAudience: "n/a",
                    WhyBullets: ["n/a"],
                    SuggestedChannel: "email",
                    EstimatedUsage: "n/a",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "custom-campaign",
                        AudienceKey: "all-eligible-guests",
                        Channel: "email",
                        OfferStance: "no-offer",
                        CampaignName: "n/a",
                        MessageSubject: "n/a",
                        MessageBody: "n/a"
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task PostRecommendation_DisallowedDraftPrefillGoal_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-bad-prefill-goal"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "thank-recent-guests",
                    Title: "Thank guests who recently joined",
                    Opportunity:
                        "Several guests joined recently and are ready for a thank-you.",
                    EligibleAudience:
                        "Guests captured in the last two weeks with marketing permission.",
                    WhyBullets:
                    [
                        "Have a valid marketing permission",
                        "Have a reachable email or mobile number",
                    ],
                    SuggestedChannel: "email",
                    EstimatedUsage: "Within current allowance",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "quiet-time",
                        AudienceKey: "new-guests",
                        Channel: "email",
                        OfferStance: "no-offer",
                        CampaignName: "Quiet time boost",
                        MessageSubject: "Come back midweek",
                        MessageBody: "We would love to see you midweek."
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task PostRecommendation_DisallowedDraftPrefillAudience_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-bad-prefill-audience"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "thank-recent-guests",
                    Title: "Thank guests who recently joined",
                    Opportunity:
                        "Several guests joined recently and are ready for a thank-you.",
                    EligibleAudience:
                        "Guests captured in the last two weeks with marketing permission.",
                    WhyBullets:
                    [
                        "Have a valid marketing permission",
                        "Have a reachable email or mobile number",
                    ],
                    SuggestedChannel: "email",
                    EstimatedUsage: "Within current allowance",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "thank-recent-guests",
                        AudienceKey: "all-eligible-or-saved-group",
                        Channel: "email",
                        OfferStance: "no-offer",
                        CampaignName: "Thank you for joining",
                        MessageSubject: "Thanks for joining us",
                        MessageBody: "Thank you for joining our guest list."
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task PostRecommendation_DisallowedDraftPrefillOffer_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithEligibleGuestAsync(
                "campaign-rec-bad-prefill-offer"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "thank-recent-guests",
                    Title: "Thank guests who recently joined",
                    Opportunity:
                        "Several guests joined recently and are ready for a thank-you.",
                    EligibleAudience:
                        "Guests captured in the last two weeks with marketing permission.",
                    WhyBullets:
                    [
                        "Have a valid marketing permission",
                        "Have a reachable email or mobile number",
                    ],
                    SuggestedChannel: "email",
                    EstimatedUsage: "Within current allowance",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "thank-recent-guests",
                        AudienceKey: "new-guests",
                        Channel: "email",
                        OfferStance: "optional",
                        CampaignName: "Thank you for joining",
                        MessageSubject: "Thanks for joining us",
                        MessageBody: "Thank you for joining our guest list."
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                seeded.Jwt,
                Last30Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task PostRecommendation_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsync(
                "/api/campaigns/recommendation",
                new StringContent(
                    "{}",
                    Encoding.UTF8,
                    "application/json"
                )
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("campaign-rec-a");
            var other = await SeedOwnerWithLocationAsync("campaign-rec-b");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/recommendation",
                owner.Jwt,
                Last30Body(other.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private static Dictionary<string, object?> Last30Body(int locationId)
        {
            return new Dictionary<string, object?>
            {
                ["locationId"] = locationId,
                ["overviewDatePreset"] = "last30",
                ["from"] = "2026-07-09T00:00:00.000Z",
                ["to"] = "2026-08-08T23:59:59.999Z",
                ["refresh"] = false,
            };
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
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
            int LocationId
        )> SeedOwnerWithLocationAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Campaign Rec Owner",
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
                Name = "Campaign Rec Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

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

            return (jwt, location.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithEligibleGuestAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerWithLocationAsync(emailLocalPart);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                Email = "eligible@example.com",
                Mobile = "07700900999",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(
                new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = seeded.LocationId,
                    Name = "Eligible Guest",
                    OffersOptOut = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                }
            );
            await context.SaveChangesAsync();

            return seeded;
        }
    }
}
