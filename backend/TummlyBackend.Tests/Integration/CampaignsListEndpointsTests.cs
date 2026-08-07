using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignsListEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignsListEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("all")]
        [InlineData("needs-attention")]
        [InlineData("drafts")]
        [InlineData("in-flight")]
        [InlineData("sent")]
        public async Task GetCampaigns_EmptyViews_ReturnSuccessEmptyEnvelope(
            string view
        )
        {
            var seeded = await SeedOwnerWithLocationAsync(
                $"campaigns-list-{view}"
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, view),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());

            var tabCounts = body.GetProperty("tabCounts");
            Assert.Equal(0, tabCounts.GetProperty("all").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("drafts").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("needsAttention").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("inFlight").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("sent").GetInt32());
        }

        [Fact]
        public async Task GetCampaigns_AfterCreate_ListsDraftProjectionOnAllAndDrafts()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaigns-list-after-create"
            );

            using (var createRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Tuesday lunch reminder",
                    goalId = "boost-quieter-time",
                    audienceKey = "all-eligible-guests",
                    channel = "sms",
                    offerStance = "no-offer",
                }
            ))
            {
                var createResponse = await _client.SendAsync(createRequest);
                Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            }

            using var allRequest = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "all"),
                seeded.Jwt
            );
            var allResponse = await _client.SendAsync(allRequest);
            Assert.Equal(HttpStatusCode.OK, allResponse.StatusCode);
            var allBody = await ReadJsonAsync(allResponse);
            Assert.Equal(1, allBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, allBody.GetProperty("items").GetArrayLength());

            var item = allBody.GetProperty("items")[0];
            Assert.Equal("Tuesday lunch reminder", item.GetProperty("name").GetString());
            Assert.Equal("draft", item.GetProperty("status").GetString());
            Assert.Equal("boost-quieter-time", item.GetProperty("goalId").GetString());
            Assert.Equal(seeded.LocationId, item.GetProperty("locationId").GetInt32());
            Assert.Equal("Main", item.GetProperty("locationName").GetString());
            Assert.Equal("sms", item.GetProperty("channel").GetString());
            Assert.Equal("all-eligible-guests", item.GetProperty("audienceKey").GetString());
            Assert.Equal("no-offer", item.GetProperty("offerStance").GetString());
            Assert.True(item.TryGetProperty("updatedAt", out _));
            Assert.Equal(JsonValueKind.Null, item.GetProperty("sendDate").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("delivery").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("engagement").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("redemptions").ValueKind);

            var tabCounts = allBody.GetProperty("tabCounts");
            Assert.Equal(1, tabCounts.GetProperty("all").GetInt32());
            Assert.Equal(1, tabCounts.GetProperty("drafts").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("needsAttention").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("inFlight").GetInt32());
            Assert.Equal(0, tabCounts.GetProperty("sent").GetInt32());

            using var draftsRequest = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "drafts"),
                seeded.Jwt
            );
            var draftsBody = await ReadJsonAsync(await _client.SendAsync(draftsRequest));
            Assert.Equal(1, draftsBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, draftsBody.GetProperty("items").GetArrayLength());

            using var needsRequest = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "needs-attention"),
                seeded.Jwt
            );
            var needsBody = await ReadJsonAsync(await _client.SendAsync(needsRequest));
            Assert.Equal(0, needsBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, needsBody.GetProperty("items").GetArrayLength());
            Assert.Equal(1, needsBody.GetProperty("tabCounts").GetProperty("all").GetInt32());
            Assert.Equal(1, needsBody.GetProperty("tabCounts").GetProperty("drafts").GetInt32());
        }

        [Fact]
        public async Task GetCampaigns_SupportsNameSearchAndUpdatedAtDesc()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaigns-list-search-sort"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var older = new Campaign
            {
                RestaurantLocationId = seeded.LocationId,
                Status = "draft",
                Name = "Alpha thank you",
                GoalId = "thank-recent-guests",
                Channel = "email",
                OfferStance = "no-offer",
                RowVersion = 1,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-2),
            };
            var newer = new Campaign
            {
                RestaurantLocationId = seeded.LocationId,
                Status = "draft",
                Name = "Beta lunch push",
                GoalId = "boost-quieter-time",
                Channel = "sms",
                OfferStance = "no-offer",
                RowVersion = 1,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-1),
            };
            context.Campaigns.AddRange(older, newer);
            await context.SaveChangesAsync();

            using var listRequest = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "all"),
                seeded.Jwt
            );
            var listBody = await ReadJsonAsync(await _client.SendAsync(listRequest));
            Assert.Equal(2, listBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Beta lunch push",
                listBody.GetProperty("items")[0].GetProperty("name").GetString()
            );
            Assert.Equal(
                "Alpha thank you",
                listBody.GetProperty("items")[1].GetProperty("name").GetString()
            );

            using var searchRequest = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "all") + "&q=Alpha",
                seeded.Jwt
            );
            var searchBody = await ReadJsonAsync(await _client.SendAsync(searchRequest));
            Assert.Equal(1, searchBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                "Alpha thank you",
                searchBody.GetProperty("items")[0].GetProperty("name").GetString()
            );
            Assert.Equal(2, searchBody.GetProperty("tabCounts").GetProperty("all").GetInt32());
        }

        [Fact]
        public async Task GetCampaigns_PaginatesUpdatedAtDesc()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaigns-list-page"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            for (var i = 0; i < 26; i++)
            {
                context.Campaigns.Add(
                    new Campaign
                    {
                        RestaurantLocationId = seeded.LocationId,
                        Status = "draft",
                        Name = $"Draft {i:D2}",
                        GoalId = "thank-recent-guests",
                        Channel = "email",
                        OfferStance = "no-offer",
                        RowVersion = 1,
                        CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                        UpdatedAt = DateTime.UtcNow.AddMinutes(-i),
                    }
                );
            }

            await context.SaveChangesAsync();

            using var page1Request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "all") + "&page=1",
                seeded.Jwt
            );
            var page1 = await ReadJsonAsync(await _client.SendAsync(page1Request));
            Assert.Equal(26, page1.GetProperty("totalCount").GetInt32());
            Assert.Equal(25, page1.GetProperty("items").GetArrayLength());
            Assert.Equal(1, page1.GetProperty("page").GetInt32());
            Assert.Equal(
                "Draft 00",
                page1.GetProperty("items")[0].GetProperty("name").GetString()
            );

            using var page2Request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "all") + "&page=2",
                seeded.Jwt
            );
            var page2 = await ReadJsonAsync(await _client.SendAsync(page2Request));
            Assert.Equal(26, page2.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, page2.GetProperty("items").GetArrayLength());
            Assert.Equal(2, page2.GetProperty("page").GetInt32());
            Assert.Equal(
                "Draft 25",
                page2.GetProperty("items")[0].GetProperty("name").GetString()
            );
        }

        [Fact]
        public async Task GetCampaigns_ListThenGetById_SupportsContinueEditingLoad()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaigns-list-continue"
            );

            using var createRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Resume me",
                    goalId = "thank-recent-guests",
                    audienceKey = "all-eligible-guests",
                    channel = "email",
                    offerStance = "no-offer",
                    messageBody = "Thanks for visiting",
                }
            );
            var created = (await ReadJsonAsync(await _client.SendAsync(createRequest)))
                .GetProperty("campaign");
            var id = created.GetProperty("id").GetInt32();

            using var listRequest = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "drafts"),
                seeded.Jwt
            );
            var listBody = await ReadJsonAsync(await _client.SendAsync(listRequest));
            Assert.Equal(1, listBody.GetProperty("items").GetArrayLength());
            Assert.Equal(
                id,
                listBody.GetProperty("items")[0].GetProperty("id").GetInt32()
            );

            using var getRequest = AuthorizedGet(
                $"/api/campaigns/{id}",
                seeded.Jwt
            );
            var getBody = await ReadJsonAsync(await _client.SendAsync(getRequest));
            var campaign = getBody.GetProperty("campaign");
            Assert.Equal(id, campaign.GetProperty("id").GetInt32());
            Assert.Equal("Resume me", campaign.GetProperty("name").GetString());
            Assert.Equal("thank-recent-guests", campaign.GetProperty("goalId").GetString());
            Assert.Equal("email", campaign.GetProperty("channel").GetString());
            Assert.Equal(
                "Thanks for visiting",
                campaign.GetProperty("messageBody").GetString()
            );
            Assert.Equal(
                created.GetProperty("rowVersion").GetInt32(),
                campaign.GetProperty("rowVersion").GetInt32()
            );
        }

        [Fact]
        public async Task GetCampaigns_RejectsAwaitingApprovalView()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaigns-list-awaiting"
            );

            using var request = AuthorizedGet(
                CampaignsUrl(seeded.LocationId, "awaiting-approval"),
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaigns_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/campaigns?locationId=1&view=all"
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaigns_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerWithLocationAsync("campaigns-list-a");
            var other = await SeedOwnerWithLocationAsync("campaigns-list-b");

            using var request = AuthorizedGet(
                CampaignsUrl(other.LocationId, "all"),
                owner.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private static HttpRequestMessage AuthorizedGet(
            string url,
            string jwt
        )
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

        private static string CampaignsUrl(int locationId, string view)
        {
            return $"/api/campaigns?locationId={locationId}&view={view}";
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
                FullName = "Campaigns Owner",
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
                Name = "Campaigns Venue",
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
