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
    /// <summary>
    /// Seam: void request create / approve / reject + open attention (ticket 39).
    /// </summary>
    public class OffersVoidEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersVoidEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateVoidRequest_PendingExists_OnSecondCreate()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-void-pending");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Maya");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var issueId = await SeedRedeemedIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-VOID01"
            );

            var body = new
            {
                issueId,
                offerId,
                locationId = seeded.LocationId,
                reasonId = "redeemed_by_mistake",
                explanation = (string?)null,
                correctionId = "keep_unusable",
            };

            using var first = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/void-requests",
                seeded.Jwt,
                body
            );
            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            var firstBody = await ReadJsonAsync(firstResponse);
            Assert.True(firstBody.GetProperty("success").GetBoolean());

            using var second = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/void-requests",
                seeded.Jwt,
                body
            );
            var secondResponse = await _client.SendAsync(second);
            var secondBody = await ReadJsonAsync(secondResponse);
            Assert.False(secondBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "pending_exists",
                secondBody.GetProperty("reason").GetString()
            );
        }

        [Fact]
        public async Task ApproveVoidRequest_KeepUnusable_VoidsPassAndExcludesKpi()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-void-approve");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Alex");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var redeemedAt = DateTime.UtcNow.AddDays(-1);
            var issueId = await SeedRedeemedIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-VOID02",
                redeemedAt: redeemedAt
            );

            var requestId = await CreatePendingVoidAsync(
                seeded.Jwt,
                issueId,
                offerId,
                seeded.LocationId
            );

            using var approve = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/void-requests/{requestId}/approve",
                seeded.Jwt,
                new { }
            );
            var approveResponse = await _client.SendAsync(approve);
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issue = await context.OfferIssues.FindAsync(issueId);
            Assert.NotNull(issue);
            Assert.NotNull(issue!.RedemptionVoidedAtUtc);
            Assert.NotNull(issue.CancelledAtUtc);
            Assert.NotNull(issue.RedeemedAtUtc);

            using var metricsRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/offers/{offerId}/metrics?from={Uri.EscapeDataString(redeemedAt.AddHours(-1).ToString("o"))}&to={Uri.EscapeDataString(DateTime.UtcNow.AddHours(1).ToString("o"))}"
            );
            metricsRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var metricsResponse = await _client.SendAsync(metricsRequest);
            var metricsBody = await ReadJsonAsync(metricsResponse);
            Assert.Equal(0, metricsBody.GetProperty("redemptions").GetInt32());
        }

        [Fact]
        public async Task ListOpenVoidAttention_GroupsPendingByOffer()
        {
            var seeded = await SeedOwnerWithLocationAsync("offers-void-attention");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Sam");
            var offerId = await SeedCatalogOfferAsync(
                seeded.LocationId,
                title: "Lunch deal"
            );
            var issueId = await SeedRedeemedIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-VOID03"
            );
            await CreatePendingVoidAsync(
                seeded.Jwt,
                issueId,
                offerId,
                seeded.LocationId
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/offers/void-requests/open-attention?locationId={seeded.LocationId}"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal(offerId, items[0].GetProperty("offerId").GetInt32());
            Assert.Equal(1, items[0].GetProperty("pendingCount").GetInt32());
            Assert.True(
                items[0].TryGetProperty(
                    "newestPendingRequestedAtUtc",
                    out var requestedAt
                )
            );
            Assert.False(requestedAt.ValueKind == JsonValueKind.Null);
        }

        private async Task<int> CreatePendingVoidAsync(
            string jwt,
            int issueId,
            int offerId,
            int locationId
        )
        {
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/void-requests",
                jwt,
                new
                {
                    issueId,
                    offerId,
                    locationId,
                    reasonId = "redeemed_by_mistake",
                    explanation = (string?)null,
                    correctionId = "keep_unusable",
                }
            );
            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);
            return int.Parse(
                body.GetProperty("requestId").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture
            );
        }

        private async Task<int> SeedRedeemedIssueAsync(
            int offerId,
            int guestId,
            string claimCode,
            DateTime? redeemedAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issue = new OfferIssue
            {
                CatalogOfferId = offerId,
                LocationGuestId = guestId,
                ClaimCode = claimCode,
                IssuedAtUtc = DateTime.UtcNow.AddDays(-2),
                ClaimedAtUtc = DateTime.UtcNow.AddDays(-2),
                RedeemedAtUtc = redeemedAt ?? DateTime.UtcNow.AddDays(-1),
                Source = OfferIssueSources.Campaign,
                ExpiryAtUtc = DateTime.UtcNow.AddDays(30),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "Test offer",
                Description = "Test description",
                Validity = CatalogOfferValidity.Days14AfterIssue,
            };
            context.OfferIssues.Add(issue);
            await context.SaveChangesAsync();
            return issue.Id;
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string path,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, path)
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

        private async Task<(
            string Jwt,
            int UserId,
            int RestaurantId,
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
                FullName = "Offers Void Owner",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900456",
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
                Name = "Offers Void Venue",
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

            return (jwt, user.Id, restaurant.Id, location.Id);
        }

        private async Task<int> SeedCatalogOfferAsync(
            int locationId,
            string title = "£5 off"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Seeded for void tests.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                StaffInstructions = "Apply £5 off before payment.",
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<int> SeedLocationGuestAsync(
            int locationId,
            string name
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var location = await context.RestaurantLocations
                .FindAsync(locationId);
            Assert.NotNull(location);

            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"void-guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var lg = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = name,
                CreatedAt = now,
            };
            context.LocationGuests.Add(lg);
            await context.SaveChangesAsync();
            return lg.Id;
        }
    }
}
