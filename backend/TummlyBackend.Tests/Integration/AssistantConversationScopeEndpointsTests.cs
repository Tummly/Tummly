using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class AssistantConversationScopeEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string InScopeTitle = "In scope thread";
        private const string OutOfScopeTitle = "Out of scope thread";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AssistantConversationScopeEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ConversationList_HidesSingleScopeForUnassignedLocation()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );
            await SeedConversationAsync(seeded.MemberUserId, seeded.InScopeLocationId, InScopeTitle);
            await SeedConversationAsync(seeded.MemberUserId, seeded.OutOfScopeLocationId, OutOfScopeTitle);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/assistant/conversations?archived=false"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var titles = body.RootElement
                .GetProperty("conversations")
                .EnumerateArray()
                .Select(row => row.GetProperty("title").GetString())
                .ToList();

            Assert.Contains(InScopeTitle, titles);
            Assert.DoesNotContain(OutOfScopeTitle, titles);
        }

        private async Task SeedConversationAsync(int ownerUserId, int locationId, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var location = context.RestaurantLocations.Single(row => row.Id == locationId);
            var now = DateTime.UtcNow;

            context.AssistantConversations.Add(new AssistantConversation
            {
                OwnerUserId = ownerUserId,
                Title = title,
                OwnedLocationId = locationId,
                OwnedLocationName = location.LocationName,
                ScopeKind = "single",
                ReportingPeriodKind = "preset",
                ReportingPeriodPresetId = "last7",
                CreatedAt = now,
                LastActivityAt = now,
            });
            await context.SaveChangesAsync();
        }
    }
}
