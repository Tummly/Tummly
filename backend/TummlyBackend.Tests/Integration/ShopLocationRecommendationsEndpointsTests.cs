using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class ShopLocationRecommendationsEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ShopLocationRecommendationsEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetRecommendations_ReturnsNeedsLocationDetails_WhenNoDetailsSaved()
        {
            var seeded = await SeedWorkspaceAsync();

            using var request = AuthorizedGet(
                $"/api/shop/locations/{seeded.LocationId}/recommendations",
                seeded.MemberJwt
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("needsLocationDetails").GetBoolean());
            Assert.Equal(0, body.GetProperty("lines").GetArrayLength());
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );
            Assert.True(body.TryGetProperty("window", out var window));
            Assert.False(string.IsNullOrWhiteSpace(
                window.GetProperty("from").GetString()
            ));
            Assert.False(string.IsNullOrWhiteSpace(
                window.GetProperty("to").GetString()
            ));
        }

        [Fact]
        public async Task PutDetailsThenGetRecommendations_ReturnsBaselineKit_WithEmptyMetrics()
        {
            var seeded = await SeedWorkspaceAsync();

            var put = await PutDetailsAsync(
                seeded.MemberJwt,
                seeded.LocationId,
                new
                {
                    tableCount = 18,
                    counterCount = 2,
                    entranceCount = 2,
                    secondaryEntranceCount = 0,
                    takeawayVolume = "100-249",
                    promptLocations = "tables,counters,windows",
                    existingMaterials = "yes",
                }
            );
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            using var getRequest = AuthorizedGet(
                $"/api/shop/locations/{seeded.LocationId}/recommendations",
                seeded.MemberJwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var body = await ReadJsonAsync(getResponse);
            Assert.False(body.GetProperty("needsLocationDetails").GetBoolean());
            var lines = body.GetProperty("lines");
            Assert.True(lines.GetArrayLength() >= 3);

            var tableLine = FindLine(lines, "table-tents");
            Assert.NotNull(tableLine);
            Assert.Equal(20, tableLine.Value.GetProperty("quantity").GetInt32());

            var counterLine = FindLine(lines, "counter-cards");
            Assert.NotNull(counterLine);
            Assert.Equal(3, counterLine.Value.GetProperty("quantity").GetInt32());

            var windowLine = FindLine(lines, "window-stickers");
            Assert.NotNull(windowLine);
            Assert.Equal(3, windowLine.Value.GetProperty("quantity").GetInt32());

            var summary = body.GetProperty("summary");
            Assert.True(summary.GetProperty("materialTypeCount").GetInt32() >= 3);
            Assert.True(summary.GetProperty("totalPieces").GetInt32() >= 26);
            Assert.Equal("GBP", summary.GetProperty("currency").GetString());
        }

        [Fact]
        public async Task GetRecommendations_AppliesActivityFloor_WhenFeedbackAtLeastFive()
        {
            var seeded = await SeedWorkspaceAsync();
            var smartGuestQrId = await SeedSmartGuestQrWithFeedbackAsync(
                seeded.LocationId,
                feedbackCount: 6
            );
            Assert.True(smartGuestQrId > 0);

            await PutDetailsAsync(
                seeded.MemberJwt,
                seeded.LocationId,
                new
                {
                    tableCount = 18,
                    counterCount = 2,
                    entranceCount = 2,
                    secondaryEntranceCount = 0,
                    takeawayVolume = "100-249",
                    promptLocations = "tables",
                    existingMaterials = "no",
                }
            );

            using var getRequest = AuthorizedGet(
                $"/api/shop/locations/{seeded.LocationId}/recommendations",
                seeded.MemberJwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            var body = await ReadJsonAsync(getResponse);

            var tableLine = FindLine(body.GetProperty("lines"), "table-tents");
            Assert.NotNull(tableLine);
            var quantity = tableLine.Value.GetProperty("quantity").GetInt32();
            var baseline = 20;
            var uncappedFloor = (int)Math.Ceiling(baseline * 1.1) + 2;
            var cappedFloor = Math.Min(uncappedFloor, baseline * 2);
            Assert.Equal(Math.Max(baseline, cappedFloor), quantity);
            Assert.True(uncappedFloor < baseline * 2);
        }

        [Fact]
        public async Task GetRecommendations_CapsActivityFloor_AtTwiceBaseline()
        {
            // baseline = tableCount + 2 = 2; uncapped floor = ceil(2.2)+2 = 5;
            // cap = 2 * baseline = 4 — so the 2× baseline cap must bind.
            var seeded = await SeedWorkspaceAsync();
            var smartGuestQrId = await SeedSmartGuestQrWithFeedbackAsync(
                seeded.LocationId,
                feedbackCount: 6
            );
            Assert.True(smartGuestQrId > 0);

            await PutDetailsAsync(
                seeded.MemberJwt,
                seeded.LocationId,
                new
                {
                    tableCount = 0,
                    counterCount = 0,
                    entranceCount = 0,
                    secondaryEntranceCount = 0,
                    takeawayVolume = "not-sure",
                    promptLocations = "tables",
                    existingMaterials = "no",
                }
            );

            using var getRequest = AuthorizedGet(
                $"/api/shop/locations/{seeded.LocationId}/recommendations",
                seeded.MemberJwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            var body = await ReadJsonAsync(getResponse);

            var tableLine = FindLine(body.GetProperty("lines"), "table-tents");
            Assert.NotNull(tableLine);
            var baseline = 2;
            var uncappedFloor = (int)Math.Ceiling(baseline * 1.1) + 2;
            Assert.True(uncappedFloor > baseline * 2);
            Assert.Equal(
                baseline * 2,
                tableLine.Value.GetProperty("quantity").GetInt32()
            );
        }

        [Fact]
        public async Task SoftLock_PutDetailsAndGetRecommendations200_CartPut403()
        {
            var seeded = await SeedWorkspaceAsync();
            await SetBillingStatusAsync(
                seeded.RestaurantId,
                BillingStatuses.SoftLock
            );

            var put = await PutDetailsAsync(
                seeded.MemberJwt,
                seeded.LocationId,
                new
                {
                    tableCount = 10,
                    counterCount = 1,
                    entranceCount = 1,
                    secondaryEntranceCount = 0,
                    takeawayVolume = "not-sure",
                    promptLocations = "tables",
                    existingMaterials = "no",
                }
            );
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            using var getRequest = AuthorizedGet(
                $"/api/shop/locations/{seeded.LocationId}/recommendations",
                seeded.MemberJwt
            );
            var getResponse = await _client.SendAsync(getRequest);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var recommendations = await ReadJsonAsync(getResponse);
            Assert.False(
                recommendations.GetProperty("needsLocationDetails").GetBoolean()
            );

            var cartPut = await PutCartLineAsync(
                seeded.MemberJwt,
                seeded.LocationId,
                "table-tents",
                12
            );
            Assert.Equal(HttpStatusCode.Forbidden, cartPut.StatusCode);
            var cartBody = await ReadJsonAsync(cartPut);
            Assert.Equal("soft_lock", cartBody.GetProperty("code").GetString());
        }

        private static JsonElement? FindLine(JsonElement lines, string skuId)
        {
            foreach (var line in lines.EnumerateArray())
            {
                if (
                    string.Equals(
                        line.GetProperty("skuId").GetString(),
                        skuId,
                        StringComparison.Ordinal
                    )
                )
                {
                    return line;
                }
            }

            return null;
        }

        private async Task<int> SeedSmartGuestQrWithFeedbackAsync(
            int locationId,
            int feedbackCount
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var qr = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = QrType.SmartGuest,
                Token = $"sg-{Guid.NewGuid():N}"[..32],
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.QrCodes.Add(qr);
            await context.SaveChangesAsync();

            for (var i = 0; i < feedbackCount; i++)
            {
                context.Feedbacks.Add(new Feedback
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = qr.Id,
                    GuestName = $"Guest {i}",
                    GuestContact = $"guest{i}@example.com",
                    Comment = "Great meal",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                });
            }

            await context.SaveChangesAsync();
            return qr.Id;
        }

        private async Task SetBillingStatusAsync(
            int restaurantId,
            string billingStatus
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var account = await context.BillingAccounts
                .SingleAsync(row => row.RestaurantId == restaurantId);
            account.BillingStatus = billingStatus;
            await context.SaveChangesAsync();
        }

        private async Task<HttpResponseMessage> PutDetailsAsync(
            string jwt,
            int locationId,
            object body
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/shop/locations/{locationId}/details"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PutCartLineAsync(
            string jwt,
            int locationId,
            string skuId,
            int quantity
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "/api/shop/cart/lines"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    locationId,
                    skuId,
                    quantity,
                }),
                Encoding.UTF8,
                "application/json"
            );
            return await _client.SendAsync(request);
        }

        private async Task<ShopSeed> SeedWorkspaceAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Shop Rec Owner",
                Email = $"owner-shop-19-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Shop Rec Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(new BillingAccount
            {
                RestaurantId = restaurant.Id,
                RevolutCustomerId = "cust_shop_19",
                SubscriptionPlan = BillingSubscriptionPlans.Starter,
                BillingCycle = BillingCycles.Monthly,
                BillingStatus = BillingStatuses.Active,
                ContractedPricebookId = "default",
            });

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                City = "London",
                Postcode = "SE1 1TQ",
                LocalContact = "Site Contact",
                LocationPhone = "+442074071234",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "Shop Rec Member",
                Email = $"member-shop-19-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900113",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Admin,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new ShopSeed(memberJwt, location.Id, restaurant.Id);
        }

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
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
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record ShopSeed(
            string MemberJwt,
            int LocationId,
            int RestaurantId
        );
    }
}
