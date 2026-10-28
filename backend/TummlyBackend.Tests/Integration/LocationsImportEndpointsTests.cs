using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Billing.PlanEntitlements;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Import contract: partial-success. Valid rows insert as Draft until the
    /// location cap; further valid rows and invalid rows return per-row errors.
    /// Fail-closed (missing billing/pricebook) rejects the whole request when
    /// nothing has been created yet.
    /// </summary>
    public class LocationsImportEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public LocationsImportEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Import_HappyPath_CreatesDrafts_AndEmitsLocationCreated()
        {
            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import Happy Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );

            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Soho Draft",
                            address = "10 Wardour Street",
                            city = "London",
                            postcode = "W1D 6QF",
                        },
                        new
                        {
                            locationName = "Shoreditch Draft",
                            address = "1 Curtain Road",
                            city = "London",
                            postcode = "EC2A 3NZ",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var created = body.GetProperty("created");
            Assert.Equal(2, created.GetArrayLength());
            Assert.Equal(0, body.GetProperty("errors").GetArrayLength());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var locations = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == seeded.RestaurantId)
                .OrderBy(row => row.Id)
                .ToListAsync();
            Assert.Equal(2, locations.Count);
            Assert.All(
                locations,
                row => Assert.Equal(LocationLifecycleStatus.Draft, row.LifecycleStatus)
            );

            var activities = await context.LocationActivities
                .AsNoTracking()
                .Where(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind == LocationActivityKinds.LocationCreated
                )
                .ToListAsync();
            Assert.Equal(2, activities.Count);
        }

        [Fact]
        public async Task Import_InvalidRows_ReturnErrors_WithoutSilentSkip()
        {
            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import Row Errors Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );

            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Valid One",
                            address = "10 Wardour Street",
                            city = "London",
                            postcode = "W1D 6QF",
                        },
                        new
                        {
                            locationName = "Missing City",
                            address = "1 High Street",
                            postcode = "LS1 1AA",
                        },
                        new
                        {
                            locationName = "",
                            address = "2 High Street",
                            city = "Leeds",
                            postcode = "LS1 2AA",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, body.GetProperty("created").GetArrayLength());
            var errors = body.GetProperty("errors");
            Assert.Equal(2, errors.GetArrayLength());
            Assert.Equal(1, errors[0].GetProperty("rowIndex").GetInt32());
            Assert.Contains(
                "City",
                errors[0].GetProperty("message").GetString(),
                StringComparison.OrdinalIgnoreCase
            );
            Assert.Equal(2, errors[1].GetProperty("rowIndex").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var count = await context.RestaurantLocations
                .CountAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task Import_Cap_PartialSuccess_ReportsRemainingRows()
        {
            // Pilot includes 1 slot. Import 2 valid rows: first creates, second
            // returns location_cap_reached (partial-success).
            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import Cap Venue",
                subscriptionPlan: BillingSubscriptionPlans.Pilot
            );

            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Fits",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                        new
                        {
                            locationName = "Overflow",
                            address = "2 High Street",
                            city = "Leeds",
                            postcode = "LS1 2AA",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(1, body.GetProperty("created").GetArrayLength());
            var errors = body.GetProperty("errors");
            Assert.Equal(1, errors.GetArrayLength());
            Assert.Equal(1, errors[0].GetProperty("rowIndex").GetInt32());
            Assert.Equal(
                LocationCap.CapReachedCode,
                errors[0].GetProperty("code").GetString()
            );
            Assert.Equal(1, errors[0].GetProperty("cap").GetInt32());
            Assert.Equal(1, errors[0].GetProperty("current").GetInt32());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var count = await context.RestaurantLocations
                .CountAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(1, count);
            var createdActivities = await context.LocationActivities
                .CountAsync(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Kind == LocationActivityKinds.LocationCreated
                );
            Assert.Equal(1, createdActivities);
        }

        [Fact]
        public async Task Import_MissingPricebook_FailsClosed_WholeRequest()
        {
            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import Fail Closed Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var billingAccount = await context.BillingAccounts
                    .SingleAsync(row => row.RestaurantId == seeded.RestaurantId);
                billingAccount.ContractedPricebookId = "missing-pricebook";
                await context.SaveChangesAsync();
            }

            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Blocked",
                            address = "1 High Street",
                            city = "Leeds",
                            postcode = "LS1 1AA",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.False(
                body.TryGetProperty("code", out var code)
                && code.GetString() == LocationCap.CapReachedCode
            );

            using var verify = _factory.Services.CreateScope();
            var verifyContext = verify.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var count = await verifyContext.RestaurantLocations
                .CountAsync(row => row.RestaurantId == seeded.RestaurantId);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Import_PossibleDuplicatePremises_SameWorkspace_ReturnsWarningWithMatch_AndKeepsDraft()
        {
            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import Duplicate Same Workspace Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );
            var existingId = await AddLocationAsync(
                seeded.RestaurantId,
                "Existing Venue",
                "21 Borough High Street",
                "SE1 1XP"
            );

            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Renamed Venue",
                            address = "21 Borough High St",
                            city = "London",
                            postcode = "SE1 1XP",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("errors").GetArrayLength());
            var created = Assert.Single(body.GetProperty("created").EnumerateArray());
            var warning = created.GetProperty("warning");
            Assert.Equal("possible_duplicate_premises", warning.GetProperty("code").GetString());
            Assert.Equal("same_workspace", warning.GetProperty("scope").GetString());
            Assert.Equal(existingId, warning.GetProperty("matchedLocationId").GetInt32());
            Assert.Equal("Existing Venue", warning.GetProperty("matchedLocationName").GetString());

            var locationId = created.GetProperty("locationId").GetInt32();
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = await context.RestaurantLocations
                .AsNoTracking()
                .SingleAsync(row => row.Id == locationId);
            Assert.Equal(LocationLifecycleStatus.Draft, location.LifecycleStatus);
            Assert.Equal(
                0,
                await context.ShopOrders.CountAsync(row => row.IsComplimentary)
            );
        }

        [Fact]
        public async Task Import_PossibleDuplicatePremises_OtherWorkspace_ReturnsGenericWarning_WithoutTenantData()
        {
            var other = await SeedWithRoomAsync(
                restaurantName: "Import Other Tenant Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );
            await AddLocationAsync(
                other.RestaurantId,
                "Competitor Venue Ltd",
                "3 Canal Walk",
                "M1 4EN"
            );

            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import Duplicate Other Workspace Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );
            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Our Venue",
                            address = "3 Canal Walk",
                            city = "Manchester",
                            postcode = "M1 4EN",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var raw = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("Competitor Venue", raw, StringComparison.Ordinal);

            var body = JsonDocument.Parse(raw).RootElement;
            var created = Assert.Single(body.GetProperty("created").EnumerateArray());
            var warning = created.GetProperty("warning");
            Assert.Equal("possible_duplicate_premises", warning.GetProperty("code").GetString());
            Assert.Equal("other_workspace", warning.GetProperty("scope").GetString());
            Assert.Equal(JsonValueKind.Null, warning.GetProperty("matchedLocationId").ValueKind);
            Assert.Equal(JsonValueKind.Null, warning.GetProperty("matchedLocationName").ValueKind);
        }

        [Fact]
        public async Task Import_NoPremisesMatch_ReturnsNullWarning()
        {
            var seeded = await SeedWithRoomAsync(
                restaurantName: "Import No Match Venue",
                subscriptionPlan: BillingSubscriptionPlans.Group
            );

            using var request = AuthorizedPost(
                "/api/locations/import",
                seeded.OwnerJwt,
                new
                {
                    rows = new object[]
                    {
                        new
                        {
                            locationName = "Quiet Venue",
                            address = "5 Quay Street",
                            city = "Bristol",
                            postcode = "BS1 4DJ",
                        },
                    },
                }
            );
            var response = await _client.SendAsync(request);

            var body = await ReadJsonAsync(response);
            var created = Assert.Single(body.GetProperty("created").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, created.GetProperty("warning").ValueKind);
        }

        private async Task<int> AddLocationAsync(
            int restaurantId,
            string name,
            string address,
            string postcode
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var location = new RestaurantLocation
            {
                RestaurantId = restaurantId,
                LocationName = name,
                Address = address,
                City = "London",
                Postcode = postcode,
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<Seeded> SeedWithRoomAsync(
            string restaurantName,
            string subscriptionPlan
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = $"Owner {restaurantName}",
                Email = $"{Guid.NewGuid():N}@example.com",
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
                Name = restaurantName,
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                BillingContactUserId = owner.Id,
                PrivacyContactUserId = owner.Id,
                SupportContactUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                CurrentPricebookId
            );
            billing.SubscriptionPlan = subscriptionPlan;
            context.BillingAccounts.Add(billing);

            owner.SelectedRestaurantId = restaurant.Id;
            context.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = owner.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
            await context.SaveChangesAsync();

            return new Seeded(
                restaurant.Id,
                owner.Id,
                jwtService.GenerateToken(
                    owner.Id.ToString(),
                    owner.Email,
                    owner.Role
                )
            );
        }

        private static HttpRequestMessage AuthorizedPost(
            string path,
            string jwt,
            object payload
        )
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt
            );
            request.Content = JsonContent.Create(payload);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private sealed record Seeded(
            int RestaurantId,
            int OwnerUserId,
            string OwnerJwt
        );
    }
}
