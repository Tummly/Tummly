using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class AssistantEndpointsTests : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AssistantEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendTurn_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsJsonAsync(
                "/api/assistant/turns",
                new
                {
                    message = "Summarise recent feedback",
                    analysisScope = new
                    {
                        ownedLocationId = 1,
                        reportingPeriod = new { kind = "preset", presetId = "last7" },
                    },
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendTurn_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("assistant-owner-a-token-1234");
            var other = await SeedOwnerAsync(
                "assistant-owner-b-token-1234",
                email: "assistant-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = other.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SendTurn_PersistsPersonalThread_WithFakeStub()
        {
            var owner = await SeedOwnerAsync("assistant-send-token-123456");
            ResetFake();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var conversation = body.GetProperty("conversation");
            Assert.Equal("Summarise recent feedback", conversation.GetProperty("title").GetString());
            Assert.Equal(
                owner.LocationId,
                conversation.GetProperty("analysisScope").GetProperty("ownedLocationId").GetInt32()
            );
            var messages = conversation.GetProperty("messages");
            Assert.Equal(2, messages.GetArrayLength());
            Assert.Equal("grounded", messages[1].GetProperty("class").GetString());
            Assert.Contains(
                "nothing to summarise",
                messages[1].GetProperty("body").GetString()
            );
            Assert.Contains(
                "Camden",
                messages[1].GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task GetConversation_Returns404_ForTeammate()
        {
            var owner = await SeedOwnerAsync("assistant-get-owner-token-12");
            var other = await SeedOwnerAsync(
                "assistant-get-other-token-12",
                email: "assistant-get-other@example.com"
            );
            ResetFake();

            using var send = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns");
            send.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Jwt);
            send.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            var created = await _client.SendAsync(send);
            var createdBody = await ReadJsonAsync(created);
            var conversationId = createdBody
                .GetProperty("conversation")
                .GetProperty("id")
                .GetInt32();

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/assistant/conversations/{conversationId}"
            );
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other.Jwt);

            var response = await _client.SendAsync(get);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ListConversations_ReturnsOnlyOwnerThreads()
        {
            var owner = await SeedOwnerAsync("assistant-list-owner-token1");
            var other = await SeedOwnerAsync(
                "assistant-list-other-token1",
                email: "assistant-list-other@example.com"
            );
            ResetFake();

            using var send = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns");
            send.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Jwt);
            send.Content = JsonContent.Create(new
            {
                message = "Owner thread",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            await _client.SendAsync(send);

            using var list = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/assistant/conversations?archived=false"
            );
            list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other.Jwt);
            var response = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("conversations").GetArrayLength());
        }

        private void ResetFake()
        {
            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeAssistantLiveAnswerProvider>();
            fake.ResetToCannedStub();
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string unusedToken,
            string email = "assistant-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Assistant Owner",
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
                Name = "Assistant Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
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
