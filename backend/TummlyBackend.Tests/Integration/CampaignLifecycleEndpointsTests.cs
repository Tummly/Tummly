using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignLifecycleEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignLifecycleEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Unschedule_ReturnsDraft_AndClearsFreeze()
        {
            var reserve = new LiveBillingReserve();
            var client = CreateClientWithReserve(reserve);
            var seeded = await SeedScheduledAsync("lifecycle-unschedule");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/unschedule",
                seeded.Jwt,
                new { rowVersion = Convert.ToBase64String(seeded.RowVersion) }
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "draft",
                body.GetProperty("campaign").GetProperty("status").GetString()
            );
            Assert.Single(reserve.ReleaseCalls);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Empty(
                await context.CampaignFrozenRecipients
                    .Where(row => row.CampaignId == seeded.CampaignId)
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task Pause_FromPartiallySent_ReturnsConflict()
        {
            var seeded = await SeedScheduledAsync(
                "lifecycle-pause-reject",
                status: "partially-sent"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/pause",
                seeded.Jwt,
                new { rowVersion = Convert.ToBase64String(seeded.RowVersion) }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("invalid_status", body.GetProperty("code").GetString());
        }

        private HttpClient CreateClientWithReserve(LiveBillingReserve reserve)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var existing = services
                        .Where(d => d.ServiceType == typeof(ICampaignBillingReserve))
                        .ToList();
                    foreach (var descriptor in existing)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<ICampaignBillingReserve>(reserve);
                });
            }).CreateClient();
        }

        private async Task<(
            string Jwt,
            int CampaignId,
            byte[] RowVersion
        )> SeedScheduledAsync(
            string emailLocalPart,
            string status = "scheduled"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var now = DateTime.UtcNow;
            var user = new User
            {
                FullName = "Lifecycle Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = now,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Lifecycle Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"{emailLocalPart}-guest@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                OffersOptOut = false,
                CreatedAt = now,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = status,
                Name = "Lifecycle API campaign",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                ScheduleMode = "schedule-later",
                ScheduledAtUtc = now.AddHours(2),
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = "res-api-1",
                ReservedEstimate = 1,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            context.CampaignFrozenRecipients.Add(
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = locationGuest.Id,
                    FrozenAtUtc = now,
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
            return (jwt, campaign.Id, campaign.RowVersion);
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
                Content = JsonContent.Create(body),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private sealed class LiveBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive => true;

            public List<int> ReserveCalls { get; } = [];

            public List<string> ReleaseCalls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReserveCalls.Add(request.Units);
                return Task.FromResult<CampaignBillingReserveResult>(
                    new CampaignBillingReserveResult.Ok
                    {
                        ReservationRef = "res-live-1",
                    }
                );
            }

            public Task<CampaignBillingReleaseResult> ReleaseAsync(
                CampaignBillingReleaseRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReleaseCalls.Add(request.ReservationRef);
                return Task.FromResult<CampaignBillingReleaseResult>(
                    new CampaignBillingReleaseResult.Ok()
                );
            }
        }
    }
}
