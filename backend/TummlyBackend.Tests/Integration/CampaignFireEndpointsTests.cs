using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// API seam for Campaign fire (ticket 31).
    /// </summary>
    public class CampaignFireEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public CampaignFireEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Fire_AfterSendNowCommit_SettlesAndSetsSent()
        {
            var reserve = new LiveBillingReserve();
            var outbound = new AcceptingOutboundSender();
            var client = CreateClient(reserve, outbound);
            var seeded = await SeedSendingCampaignAsync("fire-success");

            using var request = Authorized(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/fire",
                seeded.Jwt
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var campaign = body.GetProperty("campaign");
            Assert.Equal("sent", campaign.GetProperty("status").GetString());
            Assert.Equal(1, campaign.GetProperty("acceptedCount").GetInt32());
            Assert.Single(outbound.Calls);
            Assert.Single(reserve.SettleCalls);
        }

        [Fact]
        public async Task Fire_ZeroEligible_SetsFailedAndReleases()
        {
            var reserve = new LiveBillingReserve();
            var outbound = new AcceptingOutboundSender();
            var client = CreateClient(reserve, outbound);
            var seeded = await SeedSendingCampaignAsync("fire-failed-zero");

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var guest = await context.LocationGuests
                    .Include(lg => lg.MasterGuest)
                    .Where(lg => lg.RestaurantLocationId == seeded.LocationId)
                    .FirstAsync();
                guest.OffersOptOut = true;
                await context.SaveChangesAsync();
            }

            using var request = Authorized(
                HttpMethod.Post,
                $"/api/campaigns/{seeded.CampaignId}/fire",
                seeded.Jwt
            );
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "failed",
                body.GetProperty("campaign").GetProperty("status").GetString()
            );
            Assert.Empty(outbound.Calls);
            Assert.Single(reserve.ReleaseCalls);
        }

        private HttpClient CreateClient(
            LiveBillingReserve reserve,
            AcceptingOutboundSender outbound
        )
        {
            return _factory
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        var reserveDescriptors = services
                            .Where(d => d.ServiceType == typeof(ICampaignBillingReserve))
                            .ToList();
                        foreach (var descriptor in reserveDescriptors)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddSingleton<ICampaignBillingReserve>(reserve);

                        var outboundDescriptors = services
                            .Where(d => d.ServiceType == typeof(ICampaignOutboundSender))
                            .ToList();
                        foreach (var descriptor in outboundDescriptors)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddSingleton<ICampaignOutboundSender>(outbound);
                    });
                })
                .CreateClient();
        }

        private async Task<(
            int CampaignId,
            int LocationId,
            string Jwt
        )> SeedSendingCampaignAsync(string suffix)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Fire Owner",
                Email = $"fire-{suffix}@example.com",
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
                Name = $"Fire Rest {suffix}",
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
                Address = "1 High St",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"guest-{suffix}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = location.Id,
                MasterGuestId = master.Id,
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();

            var campaign = new Campaign
            {
                RestaurantLocationId = location.Id,
                Status = CampaignFireService.SendingStatus,
                Name = $"Fire {suffix}",
                GoalId = "thank-recent-guests",
                AudienceKey = "all-eligible-guests",
                Channel = "email",
                OfferStance = "no-offer",
                MessageSubject = "Thanks",
                MessageBody = "Hello",
                ScheduleMode = "send-now",
                ScheduleTimeZone = "Europe/London",
                BillingReservationRef = "res-fire-api-1",
                ReservedEstimate = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            context.CampaignFrozenRecipients.Add(
                new CampaignFrozenRecipient
                {
                    CampaignId = campaign.Id,
                    LocationGuestId = lg.Id,
                    FrozenAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
            return (campaign.Id, location.Id, jwt);
        }

        private static HttpRequestMessage Authorized(
            HttpMethod method,
            string url,
            string jwt
        )
        {
            var request = new HttpRequestMessage(method, url);
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

        private sealed class AcceptingOutboundSender : ICampaignOutboundSender
        {
            public List<CampaignOutboundSendRequest> Calls { get; } = [];

            public Task<CampaignOutboundSendResult> SendAsync(
                CampaignOutboundSendRequest request,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(request);
                return Task.FromResult<CampaignOutboundSendResult>(
                    new CampaignOutboundSendResult.Accepted()
                );
            }
        }

        private sealed class LiveBillingReserve : ICampaignBillingReserve
        {
            public bool IsLive => true;

            public List<CampaignBillingSettleRequest> SettleCalls { get; } = [];

            public List<CampaignBillingReleaseRequest> ReleaseCalls { get; } = [];

            public Task<CampaignBillingReserveResult> ReserveAsync(
                CampaignBillingReserveRequest request,
                CancellationToken cancellationToken = default
            )
            {
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
                SettleCalls.Add(request);
                return Task.FromResult<CampaignBillingSettleResult>(
                    new CampaignBillingSettleResult.Ok()
                );
            }

            public Task<CampaignBillingReleaseResult> ReleaseAsync(
                CampaignBillingReleaseRequest request,
                CancellationToken cancellationToken = default
            )
            {
                ReleaseCalls.Add(request);
                return Task.FromResult<CampaignBillingReleaseResult>(
                    new CampaignBillingReleaseResult.Ok()
                );
            }
        }
    }
}
