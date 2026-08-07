using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignDraftEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignDraftEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostCampaign_CreatesDraft_ForOwnedLocation()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-create");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Thank recent guests",
                    goalId = "thank-recent-guests",
                    audienceKey = "all-eligible-guests",
                    channel = "email",
                    offerStance = "create-new-offer",
                    messageSubject = "Thanks for visiting",
                    messageBody = "We appreciate you.",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var campaign = body.GetProperty("campaign");
            Assert.True(campaign.GetProperty("id").GetInt32() > 0);
            Assert.Equal(seeded.LocationId, campaign.GetProperty("locationId").GetInt32());
            Assert.Equal("draft", campaign.GetProperty("status").GetString());
            Assert.Equal("Thank recent guests", campaign.GetProperty("name").GetString());
            Assert.Equal("thank-recent-guests", campaign.GetProperty("goalId").GetString());
            Assert.Equal("all-eligible-guests", campaign.GetProperty("audienceKey").GetString());
            Assert.Equal("email", campaign.GetProperty("channel").GetString());
            Assert.Equal("create-new-offer", campaign.GetProperty("offerStance").GetString());
            Assert.Equal("Thanks for visiting", campaign.GetProperty("messageSubject").GetString());
            Assert.Equal("We appreciate you.", campaign.GetProperty("messageBody").GetString());
            Assert.Equal(JsonValueKind.String, campaign.GetProperty("rowVersion").ValueKind);
            Assert.False(string.IsNullOrEmpty(campaign.GetProperty("rowVersion").GetString()));
            Assert.False(campaign.TryGetProperty("templateId", out var templateId)
                && templateId.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(templateId.GetString()));
        }

        [Fact]
        public async Task PostCampaign_DefaultsNameFromGoal_WhenNameOmitted()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-default-name");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    goalId = "thank-recent-guests",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            var campaign = body.GetProperty("campaign");
            Assert.Equal("Thank recent guests", campaign.GetProperty("name").GetString());
            Assert.Equal("draft", campaign.GetProperty("status").GetString());
        }

        [Fact]
        public async Task PostCampaign_SnapshotsTemplateIdAndVersion()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-template");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    templateId = "thank-recent-guests",
                    templateVersion = 1,
                    goalId = "thank-recent-guests",
                    audienceKey = "new-guests",
                    channel = "email",
                    offerStance = "no-offer",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var campaign = (await ReadJsonAsync(response)).GetProperty("campaign");
            Assert.Equal("thank-recent-guests", campaign.GetProperty("templateId").GetString());
            Assert.Equal(1, campaign.GetProperty("templateVersion").GetInt32());
            Assert.Equal("Thank recent guests", campaign.GetProperty("name").GetString());
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenGoalIdNotInProductAllowList()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-bad-goal");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Illegal goal",
                    goalId = "quiet-time",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "goalId",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenChannelNotInProductAllowList()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-bad-channel");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Illegal channel",
                    channel = "push",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "channel",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenAudienceKeyNotInProductAllowList()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-bad-audience");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Illegal audience",
                    audienceKey = "all-eligible-or-saved-group",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "audienceKey",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenOfferStanceNotInProductAllowList()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-bad-offer");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Illegal offer",
                    offerStance = "optional",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "offerStance",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenTemplateIdIsOrphan()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-orphan-template");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Orphan template",
                    templateId = "not-a-real-template",
                    templateVersion = 1,
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "template",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PostCampaign_Returns400_WhenTemplateVersionDoesNotMatchCatalogue()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-draft-bad-template-version"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    templateId = "thank-recent-guests",
                    templateVersion = 99,
                    goalId = "thank-recent-guests",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "templateVersion",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PatchCampaign_Returns400_WhenGoalIdNotInProductAllowList()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-patch-bad-goal");
            var created = await CreateDraftAsync(
                seeded,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Patch bad goal",
                    goalId = "thank-recent-guests",
                }
            );
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();

            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, goalId = "promote-something-new-illegal" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "goalId",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PatchCampaign_Returns400_WhenOfferStanceNotInProductAllowList()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-patch-bad-offer");
            var created = await CreateDraftAsync(
                seeded,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Patch bad offer",
                    offerStance = "no-offer",
                }
            );
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();

            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, offerStance = "optional" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Contains(
                "offerStance",
                body.GetProperty("message").GetString()!,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task PatchCampaign_StampsCatalogueVersion_WhenTemplateIdChangesWithoutVersion()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-draft-patch-template-stamp"
            );
            var created = await CreateDraftAsync(
                seeded,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Before template",
                    goalId = "thank-recent-guests",
                }
            );
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();

            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, templateId = "we-miss-you" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var campaign = (await ReadJsonAsync(response)).GetProperty("campaign");
            Assert.Equal("we-miss-you", campaign.GetProperty("templateId").GetString());
            Assert.Equal(1, campaign.GetProperty("templateVersion").GetInt32());
        }

        [Fact]
        public async Task GetCampaignById_ReturnsDraft_ForOwnedLocation()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-get");
            var created = await CreateDraftAsync(
                seeded,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Get me",
                    goalId = "custom-campaign",
                }
            );
            var id = created.GetProperty("id").GetInt32();

            using var request = AuthorizedGet($"/api/campaigns/{id}", seeded.Jwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var campaign = (await ReadJsonAsync(response)).GetProperty("campaign");
            Assert.Equal(id, campaign.GetProperty("id").GetInt32());
            Assert.Equal("Get me", campaign.GetProperty("name").GetString());
            Assert.Equal("draft", campaign.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetCampaignById_Returns404_WhenStatusIsNotDraft()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-get-nondraft");
            var campaignId = await SeedCampaignWithStatusAsync(
                seeded.LocationId,
                status: "sent",
                name: "Sent campaign"
            );

            using var request = AuthorizedGet(
                $"/api/campaigns/{campaignId}",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("Campaign not found.", body.GetProperty("message").GetString());
        }

        [Fact]
        public async Task PatchCampaign_UpdatesFields_AndChangesRowVersion()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-patch");
            var created = await CreateDraftAsync(
                seeded,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Before",
                    goalId = "thank-recent-guests",
                    channel = "email",
                }
            );
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();
            Assert.False(string.IsNullOrEmpty(rowVersion));

            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new
                {
                    rowVersion,
                    name = "After",
                    channel = "sms",
                    messageBody = "Updated body",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var campaign = (await ReadJsonAsync(response)).GetProperty("campaign");
            Assert.Equal("After", campaign.GetProperty("name").GetString());
            Assert.Equal("sms", campaign.GetProperty("channel").GetString());
            Assert.Equal("Updated body", campaign.GetProperty("messageBody").GetString());
            Assert.Equal(JsonValueKind.String, campaign.GetProperty("rowVersion").ValueKind);
            Assert.NotEqual(rowVersion, campaign.GetProperty("rowVersion").GetString());
            Assert.Equal("draft", campaign.GetProperty("status").GetString());
        }

        [Fact]
        public async Task PatchCampaign_Returns409_OnRowVersionConflict()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-conflict");
            var created = await CreateDraftAsync(
                seeded,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Conflict",
                    goalId = "thank-recent-guests",
                }
            );
            var id = created.GetProperty("id").GetInt32();
            var rowVersion = created.GetProperty("rowVersion").GetString();
            Assert.False(string.IsNullOrEmpty(rowVersion));

            using var first = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, name = "First writer" }
            );
            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            using var stale = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{id}",
                seeded.Jwt,
                new { rowVersion, name = "Stale writer" }
            );
            var staleResponse = await _client.SendAsync(stale);
            Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

            var body = await ReadJsonAsync(staleResponse);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "This campaign was updated elsewhere. Reload and try again.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task PatchCampaign_ReturnsDistinctConflict_WhenStatusIsNotDraft()
        {
            var seeded = await SeedOwnerWithLocationAsync("campaign-draft-patch-nondraft");
            var campaignId = await SeedCampaignWithStatusAsync(
                seeded.LocationId,
                status: "sent",
                name: "Sent campaign",
                rowVersion: [1, 2, 3, 4, 5, 6, 7, 8]
            );

            using var request = AuthorizedJson(
                HttpMethod.Patch,
                $"/api/campaigns/{campaignId}",
                seeded.Jwt,
                new { rowVersion = Convert.ToBase64String([1, 2, 3, 4, 5, 6, 7, 8]), name = "Should not apply" }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Only draft campaigns can be updated.",
                body.GetProperty("message").GetString()
            );
            Assert.NotEqual(
                "This campaign was updated elsewhere. Reload and try again.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task PostCampaign_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsJsonAsync(
                "/api/campaigns",
                new { locationId = 1, name = "Nope" }
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostCampaign_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("campaign-draft-a");
            var other = await SeedOwnerWithLocationAsync("campaign-draft-b");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                owner.Jwt,
                new
                {
                    locationId = other.LocationId,
                    name = "Cross location",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaignById_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("campaign-draft-get-a");
            var other = await SeedOwnerWithLocationAsync("campaign-draft-get-b");
            var created = await CreateDraftAsync(
                owner,
                new
                {
                    locationId = owner.LocationId,
                    name = "Owned draft",
                }
            );
            var id = created.GetProperty("id").GetInt32();

            using var request = AuthorizedGet($"/api/campaigns/{id}", other.Jwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<JsonElement> CreateDraftAsync(
            (string Jwt, int LocationId) seeded,
            object body
        )
        {
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                body
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ReadJsonAsync(response)).GetProperty("campaign");
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
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

        private async Task<int> SeedCampaignWithStatusAsync(
            int locationId,
            string status,
            string name,
            byte[]? rowVersion = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Status = status,
                Name = name,
                GoalId = "thank-recent-guests",
                RowVersion = rowVersion ?? [0, 0, 0, 0, 0, 0, 0, 1],
                CreatedAt = now,
                UpdatedAt = now,
            };

            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
            return campaign.Id;
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
                FullName = "Campaign Draft Owner",
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
                Name = "Campaign Draft Venue",
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
    }
}
