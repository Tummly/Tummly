using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public sealed class FeedbackClassificationWebApplicationFactory
        : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        public FakeFeedbackClassificationProvider FakeClassifier { get; } =
            new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                var dbDescriptors = services
                    .Where(service =>
                        service.ServiceType ==
                            typeof(DbContextOptions<ApplicationDbContext>)
                        || service.ServiceType == typeof(ApplicationDbContext)
                    )
                    .ToList();

                foreach (var descriptor in dbDescriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(warning =>
                        warning.Ignore(InMemoryEventId.TransactionIgnoredWarning)
                    );
                });

                foreach (var descriptor in services
                    .Where(service =>
                        service.ServiceType ==
                            typeof(IFeedbackClassificationProvider)
                        || service.ServiceType ==
                            typeof(FakeFeedbackClassificationProvider)
                    )
                    .ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(FakeClassifier);
                services.AddSingleton<IFeedbackClassificationProvider>(
                    FakeClassifier
                );
            });
        }
    }

    public class FeedbackClassificationEndpointsTests
        : IClassFixture<FeedbackClassificationWebApplicationFactory>
    {
        private readonly FeedbackClassificationWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackClassificationEndpointsTests(
            FeedbackClassificationWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SubmitFeedback_StartsPending_ThenFakeProviderCanSucceed()
        {
            _factory.FakeClassifier.SucceedWith(
                FeedbackSentiment.Negative,
                DetectedIssue.FoodQuality,
                DetectedIssue.WaitTime
            );

            var seeded = await SeedLocationAsync(
                "classify-succeed-token-1234567"
            );

            var submitResponse = await _client.PostAsJsonAsync(
                $"/api/scan/{seeded.LinkToken}/feedback",
                new
                {
                    guestName = "Sam Guest",
                    guestContact = "sam@example.com",
                    comment = "Food was cold and the wait was too long."
                }
            );

            Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

            var feedbackId = await GetLatestFeedbackIdAsync(seeded);

            var pendingDetails = await GetDetailsAsOwnerAsync(
                seeded,
                feedbackId
            );
            Assert.Equal(
                "Pending",
                pendingDetails.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                pendingDetails.GetProperty("sentiment").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                pendingDetails.GetProperty("detectedIssues").ValueKind
            );

            await _factory.Services
                .GetRequiredService<IFeedbackClassificationWork>()
                .DrainAsync();

            var succeededDetails = await GetDetailsAsOwnerAsync(
                seeded,
                feedbackId
            );
            Assert.Equal(
                "Succeeded",
                succeededDetails.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                "negative",
                succeededDetails.GetProperty("sentiment").GetString()
            );
            Assert.Equal(
                new[] { "FoodQuality", "WaitTime" },
                succeededDetails.GetProperty("detectedIssues")
                    .EnumerateArray()
                    .Select(item => item.GetString())
                    .ToArray()
            );

            var listBody = await GetListAsOwnerAsync(seeded);
            var recent = listBody.GetProperty("recent").EnumerateArray().First();
            Assert.Equal(
                "Succeeded",
                recent.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                "negative",
                recent.GetProperty("sentiment").GetString()
            );
        }

        [Fact]
        public async Task SubmitFeedback_FakeProviderCanFail_WithoutInventingFields()
        {
            _factory.FakeClassifier.Fail();

            var seeded = await SeedLocationAsync(
                "classify-fail-token-1234567890"
            );

            var submitResponse = await _client.PostAsJsonAsync(
                $"/api/scan/{seeded.LinkToken}/feedback",
                new
                {
                    guestName = "Sam Guest",
                    guestContact = "sam@example.com",
                    comment = "Hola, la comida estaba fría."
                }
            );

            Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);

            var feedbackId = await GetLatestFeedbackIdAsync(seeded);
            await _factory.Services
                .GetRequiredService<IFeedbackClassificationWork>()
                .DrainAsync();

            var failedDetails = await GetDetailsAsOwnerAsync(
                seeded,
                feedbackId
            );
            Assert.Equal(
                "Failed",
                failedDetails.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                JsonValueKind.Null,
                failedDetails.GetProperty("sentiment").ValueKind
            );
            Assert.Equal(
                JsonValueKind.Null,
                failedDetails.GetProperty("detectedIssues").ValueKind
            );
        }

        [Fact]
        public async Task SubmitFeedback_SucceededWithEmptyIssues_ReturnsEmptyArray()
        {
            _factory.FakeClassifier.SucceedWith(FeedbackSentiment.Positive);

            var seeded = await SeedLocationAsync(
                "classify-empty-issues-token-123"
            );

            await _client.PostAsJsonAsync(
                $"/api/scan/{seeded.LinkToken}/feedback",
                new
                {
                    guestName = "Sam Guest",
                    guestContact = "sam@example.com",
                    comment = "Lovely meal, thank you!"
                }
            );

            var feedbackId = await GetLatestFeedbackIdAsync(seeded);
            await _factory.Services
                .GetRequiredService<IFeedbackClassificationWork>()
                .DrainAsync();

            var details = await GetDetailsAsOwnerAsync(seeded, feedbackId);
            Assert.Equal(
                "Succeeded",
                details.GetProperty("classificationStatus").GetString()
            );
            Assert.Equal(
                "positive",
                details.GetProperty("sentiment").GetString()
            );
            Assert.Empty(
                details.GetProperty("detectedIssues").EnumerateArray()
            );
        }

        private async Task<int> GetLatestFeedbackIdAsync(
            (string Jwt, int LocationId, string LinkToken) seeded
        )
        {
            var list = await GetListAsOwnerAsync(seeded);
            var recent = list.GetProperty("recent").EnumerateArray().ToList();
            Assert.NotEmpty(recent);
            return recent[0].GetProperty("id").GetInt32();
        }

        private async Task<(
            string Jwt,
            int LocationId,
            string LinkToken
        )> SeedLocationAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Classify Owner",
                Email = $"{linkToken}@example.com",
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
                Name = "Classify Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LinkToken = linkToken,
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

            return (jwt, location.Id, linkToken);
        }

        private async Task<JsonElement> GetDetailsAsOwnerAsync(
            (string Jwt, int LocationId, string LinkToken) seeded,
            int? feedbackId = null
        )
        {
            if (feedbackId is null)
            {
                var list = await GetListAsOwnerAsync(seeded);
                feedbackId = list.GetProperty("recent")
                    .EnumerateArray()
                    .First()
                    .GetProperty("id")
                    .GetInt32();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{feedbackId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadJsonAsync(response);
        }

        private async Task<JsonElement> GetListAsOwnerAsync(
            (string Jwt, int LocationId, string LinkToken) seeded
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback?locationId={seeded.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadJsonAsync(response);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }
    }
}
