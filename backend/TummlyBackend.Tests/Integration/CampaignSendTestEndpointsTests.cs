using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignSendTestEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;

        public CampaignSendTestEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
        }

        [Fact]
        public async Task SendCampaignTest_SendsToNominatedEmail_CreatesNoCampaign()
        {
            var tracking = new TrackingGuestResponseEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-send-test-tok",
                email: "operator-campaign-send@example.com"
            );
            var campaignCountBefore = await CountCampaignsAsync();

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/campaigns/send-test"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                locationId = seeded.LocationId,
                toEmail = "team@example.com",
                subject = "Thanks for visiting",
                body = "Hi guest, thanks for joining us.",
            });

            var response = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, tracking.CallCount);
            Assert.Equal("team@example.com", tracking.LastToEmail);
            Assert.Equal(campaignCountBefore, await CountCampaignsAsync());
        }

        [Fact]
        public async Task SendCampaignTest_Returns400_WhenEmailInvalid()
        {
            var tracking = new TrackingGuestResponseEmailService();
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-send-test-invalid-tok",
                email: "operator-invalid@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/campaigns/send-test"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                locationId = seeded.LocationId,
                toEmail = "not-an-email",
                subject = "Subject",
                body = "Body text",
            });

            var response = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(0, tracking.CallCount);
        }

        [Fact]
        public async Task SendCampaignTest_Returns502_WhenResendFails()
        {
            var tracking = new TrackingGuestResponseEmailService
            {
                ThrowOnSend = true,
            };
            var client = CreateClientWithEmail(tracking);
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-send-test-fail-tok",
                email: "operator-fail@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/campaigns/send-test"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                locationId = seeded.LocationId,
                toEmail = "ok@example.com",
                subject = "Subject",
                body = "Body text",
            });

            var response = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
        }

        private HttpClient CreateClientWithEmail(
            TrackingGuestResponseEmailService tracking
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var existing = services
                        .Where(d => d.ServiceType == typeof(IEmailService))
                        .ToList();
                    foreach (var descriptor in existing)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IEmailService>(tracking);
                });
            }).CreateClient();
        }

        private async Task<int> CountCampaignsAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            return await context.Campaigns.CountAsync();
        }

        private async Task<(
            string Jwt,
            int LocationId
        )> SeedOwnerWithLocationAsync(string linkToken, string email)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var operatorEmail = email + "-" + linkToken;
            var user = new User
            {
                FullName = "Campaign Send Test Owner",
                Email = operatorEmail,
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
                Name = "Campaign Send Test Venue",
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

        private sealed class TrackingGuestResponseEmailService : EmailServiceStubBase
        {
            public int CallCount { get; private set; }

            public string? LastToEmail { get; private set; }

            public bool ThrowOnSend { get; set; }

            public override Task SendGuestResponseEmailAsync(
                string toEmail,
                string subject,
                string brandTitle,
                string? brandSubtitle,
                string? locationAddress,
                string message,
                string giveFeedbackUrl,
                string? brandLogoUrl = null,
                GuestResponseEmailOfferBlock? offer = null
            )
            {
                CallCount++;
                LastToEmail = toEmail;

                if (ThrowOnSend)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                return Task.CompletedTask;
            }
        }
    }
}
