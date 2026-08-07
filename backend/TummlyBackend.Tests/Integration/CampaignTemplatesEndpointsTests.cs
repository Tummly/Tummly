using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignTemplatesEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private static readonly string[] ExpectedIds =
        [
            "thank-recent-guests",
            "quiet-time-boost",
            "we-miss-you",
            "new-item-announcement",
            "bring-a-friend",
            "recovery-follow-up",
        ];

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignTemplatesEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCampaignTemplates_ReturnsSixLaunchTemplates()
        {
            var jwt = await SeedOperatorJwtAsync("campaign-templates-list");

            using var request = AuthorizedGet("/api/campaign-templates", jwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var items = body.GetProperty("items");
            Assert.Equal(6, items.GetArrayLength());

            var ids = items
                .EnumerateArray()
                .Select(item => item.GetProperty("id").GetString())
                .ToArray();
            Assert.Equal(ExpectedIds, ids);

            var first = items[0];
            Assert.Equal(1, first.GetProperty("version").GetInt32());
            Assert.Equal(
                "Thank recent guests",
                first.GetProperty("title").GetString()
            );
            Assert.Equal(
                "Thank recent guests",
                first.GetProperty("goalLabel").GetString()
            );
            Assert.Equal(
                "New guests",
                first.GetProperty("audienceLabel").GetString()
            );
            Assert.Equal(
                "Email",
                first.GetProperty("channelLabel").GetString()
            );
            Assert.Equal(
                "Optional",
                first.GetProperty("offerLabel").GetString()
            );
            Assert.True(first.GetProperty("suggestsGoal").GetBoolean());
            Assert.True(first.GetProperty("suggestsAudience").GetBoolean());
            Assert.True(first.GetProperty("suggestsChannel").GetBoolean());
            Assert.True(first.GetProperty("suggestsOffer").GetBoolean());
            Assert.False(first.TryGetProperty("suggestions", out _));
        }

        [Fact]
        public async Task GetCampaignTemplateById_ReturnsSuggestionDefaults()
        {
            var jwt = await SeedOperatorJwtAsync("campaign-templates-by-id");

            using var request = AuthorizedGet(
                "/api/campaign-templates/we-miss-you",
                jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var template = body.GetProperty("template");
            Assert.Equal("we-miss-you", template.GetProperty("id").GetString());
            Assert.Equal(1, template.GetProperty("version").GetInt32());
            Assert.Equal(
                "We miss you",
                template.GetProperty("title").GetString()
            );

            var suggestions = template.GetProperty("suggestions");
            Assert.Equal(
                "re-engage-inactive",
                suggestions.GetProperty("goalId").GetString()
            );
            Assert.Equal(
                "no-recent-tummly-activity",
                suggestions.GetProperty("audienceKey").GetString()
            );
            Assert.Equal(
                "email",
                suggestions.GetProperty("channel").GetString()
            );
            Assert.Equal(
                "optional",
                suggestions.GetProperty("offerStance").GetString()
            );
        }

        [Fact]
        public async Task GetCampaignTemplateById_Returns404_WhenUnknown()
        {
            var jwt = await SeedOperatorJwtAsync("campaign-templates-404");

            using var request = AuthorizedGet(
                "/api/campaign-templates/not-a-template",
                jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetCampaignTemplates_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync("/api/campaign-templates");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<string> SeedOperatorJwtAsync(
            string emailLocalPart
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Templates Owner",
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

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
        }
    }
}
