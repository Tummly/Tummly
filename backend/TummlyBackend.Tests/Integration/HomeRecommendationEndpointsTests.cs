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
    public class HomeRecommendationEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public HomeRecommendationEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostRecommendation_WeakSignals_ReturnsNoneWithoutCallingProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("home-rec-weak");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
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
        public async Task PostRecommendation_HomeNativeSuccess_ReturnsAllowListedTypeAndAction()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync(
                "home-rec-success"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var recommendation = body.GetProperty("recommendation");
            Assert.Equal(
                "review-open-feedback",
                recommendation.GetProperty("type").GetString()
            );
            Assert.Equal(
                "Review open feedback",
                recommendation.GetProperty("title").GetString()
            );
            Assert.Equal("Main", recommendation.GetProperty("locationName").GetString());

            var action = recommendation.GetProperty("action");
            Assert.Equal("open-feedback", action.GetProperty("kind").GetString());

            Assert.Equal(1, fake.CallCount);
            Assert.NotNull(fake.LastInput);
            Assert.Equal("review-open-feedback", fake.LastInput!.SelectedType);
            var serialized = JsonSerializer.Serialize(fake.LastInput);
            Assert.DoesNotContain("Alex Guest", serialized);
            Assert.DoesNotContain("alex@example.com", serialized);
            Assert.DoesNotContain("Great food", serialized);
        }

        [Fact]
        public async Task PostRecommendation_ProviderFail_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync("home-rec-fail");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.Fail(retryable: true);

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_ProviderNoneAfterNativeSelect_ReturnsRetryableFailure()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync(
                "home-rec-provider-none"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("none")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
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
            var seeded = await SeedOwnerWithOpenFeedbackAsync("home-rec-cache");

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using (var first = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            ))
            {
                var firstResponse = await _client.SendAsync(first);
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            }

            Assert.Equal(1, fake.CallCount);

            using (var second = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            ))
            {
                var secondResponse = await _client.SendAsync(second);
                Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
            }

            Assert.Equal(1, fake.CallCount);

            var refreshBody = Last7Body(seeded.LocationId);
            refreshBody["refresh"] = true;
            using (var refresh = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
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
        public async Task PostRecommendation_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsync(
                "/api/home/recommendation",
                new StringContent("{}", Encoding.UTF8, "application/json")
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("home-rec-a");
            var other = await SeedOwnerWithLocationAsync("home-rec-b");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                owner.Jwt,
                Last7Body(other.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_FakeRegistered_NoLiveAzureRequired()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync("home-rec-fake");

            using var scope = _factory.Services.CreateScope();
            var provider = scope.ServiceProvider
                .GetRequiredService<IHomeRecommendationProvider>();
            Assert.IsType<FakeHomeRecommendationProvider>(provider);

            var fake = (FakeHomeRecommendationProvider)provider;
            fake.ResetCallCount();
            // Default fixtures per type — no SucceedWith required.
            fake.UseDefaultFixtures();

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "review-open-feedback",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_CampaignType_ReturnsDraftPrefillViaCampaignsService()
        {
            var seeded = await SeedOwnerWithRecoveryGuestAsync(
                "home-rec-campaign"
            );

            using var scope = _factory.Services.CreateScope();
            var homeFake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            var campaignFake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            homeFake.ResetCallCount();
            campaignFake.ResetCallCount();
            campaignFake.SucceedWith(
                new CampaignRecommendationModelOutput(
                    Type: "recovery-follow-up",
                    Title: "Follow up on recovery guests",
                    Opportunity:
                        "Guests who left negative feedback are ready for recovery.",
                    EligibleAudience:
                        "Guests with negative feedback who need recovery.",
                    WhyBullets:
                    [
                        "Have negative feedback on file",
                        "Are eligible for a recovery follow-up",
                    ],
                    SuggestedChannel: "email",
                    EstimatedUsage: "About 1 email",
                    DraftPrefill: new CampaignRecommendationDraftPrefillOutput(
                        GoalId: "follow-up-completed-recovery",
                        AudienceKey: "completed-recovery-follow-up",
                        Channel: "email",
                        OfferStance: "no-offer",
                        CampaignName: "Recovery follow-up",
                        MessageSubject: "We want to make this right",
                        MessageBody: "Thanks for your feedback — we would love to help."
                    )
                )
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var recommendation = body.GetProperty("recommendation");
            Assert.Equal(
                "recovery-follow-up",
                recommendation.GetProperty("type").GetString()
            );
            Assert.Equal(
                "Follow up on recovery guests",
                recommendation.GetProperty("title").GetString()
            );
            Assert.Equal(
                "email",
                recommendation.GetProperty("suggestedChannel").GetString()
            );

            var draft = recommendation.GetProperty("draftPrefill");
            Assert.Equal(
                "follow-up-completed-recovery",
                draft.GetProperty("goalId").GetString()
            );
            Assert.Equal(
                "completed-recovery-follow-up",
                draft.GetProperty("audienceKey").GetString()
            );
            Assert.Equal("email", draft.GetProperty("channel").GetString());
            Assert.Equal(
                "Recovery follow-up",
                draft.GetProperty("campaignName").GetString()
            );

            var echoed = recommendation.GetProperty("echoedCounts");
            Assert.True(echoed.TryGetProperty("needsRecovery", out _));
            Assert.True(echoed.TryGetProperty("marketingEligible", out _));

            Assert.Equal(0, homeFake.CallCount);
            Assert.Equal(1, campaignFake.CallCount);
            Assert.NotNull(campaignFake.LastInput);
            Assert.Equal("7days", campaignFake.LastInput!.OverviewDatePreset);
        }

        [Fact]
        public async Task PostRecommendation_HomeNative_DoesNotCallCampaignsProvider()
        {
            var seeded = await SeedOwnerWithOpenFeedbackAsync(
                "home-rec-native-no-campaign"
            );

            using var scope = _factory.Services.CreateScope();
            var homeFake = scope.ServiceProvider
                .GetRequiredService<FakeHomeRecommendationProvider>();
            var campaignFake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignRecommendationProvider>();
            homeFake.ResetCallCount();
            campaignFake.ResetCallCount();
            homeFake.SucceedWith(
                FakeHomeRecommendationProvider.FixtureFor("review-open-feedback")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/home/recommendation",
                seeded.Jwt,
                Last7Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(1, homeFake.CallCount);
            Assert.Equal(0, campaignFake.CallCount);
        }

        private static Dictionary<string, object?> Last7Body(
            int locationId,
            string from = "2026-08-15T00:00:00.000Z",
            string to = "2026-08-21T15:30:00.000Z"
        )
        {
            return new Dictionary<string, object?>
            {
                ["locationId"] = locationId,
                ["overviewDatePreset"] = "last7",
                ["from"] = from,
                ["to"] = to,
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
                FullName = "Home Rec Owner",
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
                Name = "Home Rec Venue",
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
        )> SeedOwnerWithOpenFeedbackAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerWithLocationAsync(emailLocalPart);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // Inside the Last7Body window used by tests.
            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    GuestName = "Alex Guest",
                    GuestContact = "alex@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Great food",
                    WorkflowStatus = FeedbackWorkflowStatus.New,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                }
            );
            await context.SaveChangesAsync();

            return seeded;
        }

        /// <summary>
        /// Seeds a NeedsRecovery guest without Home-native signals so the domain
        /// router selects <c>recovery-follow-up</c> (ticket 06 campaign handoff).
        /// Guest is not marketing-eligible; feedback is Resolved so it does not
        /// inflate open / needs-attention Home counts.
        /// </summary>
        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithRecoveryGuestAsync(string emailLocalPart)
        {
            var seeded = await SeedOwnerWithLocationAsync(emailLocalPart);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                Email = "recovery@example.com",
                Mobile = "07700900888",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            // Outside Last7Body window so GuestsJoinedInWindow stays 0.
            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = seeded.LocationId,
                Name = "Recovery Guest",
                MarketingPreference = LocationGuestMarketingPreference.NotRecorded,
                CreatedAt = DateTime.Parse("2026-07-01T12:00:00Z").ToUniversalTime(),
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = seeded.LocationId,
                    LocationGuestId = locationGuest.Id,
                    GuestName = "Recovery Guest",
                    GuestContact = "recovery@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Cold food",
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Negative,
                    WorkflowStatus = FeedbackWorkflowStatus.Resolved,
                    CreatedAt = DateTime.Parse("2026-07-15T12:00:00Z").ToUniversalTime(),
                }
            );
            await context.SaveChangesAsync();

            return seeded;
        }
    }
}
