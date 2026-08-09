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
    public class CampaignScheduleCommitEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignScheduleCommitEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Commit_HardBlocks_WhenBillingReserveNotLive()
        {
            var seeded = await SeedReviewReadyDraftAsync(
                "commit-hard-block"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/commit",
                seeded.Jwt,
                new
                {
                    rowVersion = Convert.ToBase64String(seeded.RowVersion),
                    scheduleMode = "send-now",
                    scheduleTimeZone = "Europe/London",
                }
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "billing_reserve_unavailable",
                body.GetProperty("code").GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var campaign = await context.Campaigns.SingleAsync(
                c => c.Id == seeded.CampaignId
            );
            Assert.Equal("draft", campaign.Status);
        }

        [Fact]
        public async Task Commit_SendNow_Succeeds_WhenBillingReserveLive()
        {
            var reserve = new LiveBillingReserve();
            var client = CreateClientWithReserve(reserve);
            var seeded = await SeedReviewReadyDraftAsync(
                "commit-send-now-live",
                emailEligibleCount: 1
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/commit",
                seeded.Jwt,
                new
                {
                    rowVersion = Convert.ToBase64String(seeded.RowVersion),
                    scheduleMode = "send-now",
                    scheduleTimeZone = "Europe/London",
                }
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var campaign = body.GetProperty("campaign");
            Assert.Equal("sending", campaign.GetProperty("status").GetString());
            Assert.Equal("send-now", campaign.GetProperty("scheduleMode").GetString());
            Assert.Equal(1, campaign.GetProperty("frozenRecipientCount").GetInt32());
            Assert.Equal("res-live-1", campaign.GetProperty("billingReservationRef").GetString());
            Assert.Single(reserve.Calls);
        }

        [Fact]
        public async Task Commit_Returns422_WhenReserveFails()
        {
            var reserve = new LiveBillingReserve { FailNext = true };
            var client = CreateClientWithReserve(reserve);
            var seeded = await SeedReviewReadyDraftAsync(
                "commit-reserve-fail",
                emailEligibleCount: 1
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/commit",
                seeded.Jwt,
                new
                {
                    rowVersion = Convert.ToBase64String(seeded.RowVersion),
                    scheduleMode = "send-now",
                    scheduleTimeZone = "Europe/London",
                }
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("reserve_failed", body.GetProperty("code").GetString());
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
            int LocationId,
            int CampaignId,
            byte[] RowVersion
        )> SeedReviewReadyDraftAsync(
            string emailLocalPart,
            int emailEligibleCount = 1
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Commit Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
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
                Name = "Commit Venue",
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
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            for (var i = 0; i < emailEligibleCount; i++)
            {
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = $"{emailLocalPart}-guest{i}@example.com",
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();
                context.LocationGuests.Add(
                    new LocationGuest
                    {
                        RestaurantLocationId = location.Id,
                        MasterGuestId = master.Id,
                        OffersOptOut = false,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }

            await context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = "draft",
                Name = "Thank recent guests",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello guest",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, campaign.Id, campaign.RowVersion);
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private sealed class LiveBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive => true;

            public bool FailNext { get; set; }

            public List<int> Calls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(request.Units);
                if (FailNext)
                {
                    return Task.FromResult<CampaignBillingReserveResult>(
                        new CampaignBillingReserveResult.Failed
                        {
                            Message = "Insufficient credits.",
                        }
                    );
                }

                return Task.FromResult<CampaignBillingReserveResult>(
                    new CampaignBillingReserveResult.Ok
                    {
                        ReservationRef = "res-live-1",
                    }
                );
            }

            public Task<CampaignBillingSettleResult> SettleAsync(
                CampaignBillingSettleRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<CampaignBillingSettleResult>(
                    new CampaignBillingSettleResult.Ok()
                );
            }

            public Task<CampaignBillingReleaseResult> ReleaseAsync(
                CampaignBillingReleaseRequest request,
                CancellationToken cancellationToken = default
            )
            {
                return Task.FromResult<CampaignBillingReleaseResult>(
                    new CampaignBillingReleaseResult.Ok()
                );
            }
        }
    }
}
