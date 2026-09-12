using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GlobalSearchEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GlobalSearchEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetSearch_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/search?q=mo&locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSearch_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("gs-owner-a-token-1234567890");
            var other = await SeedOwnerAsync(
                "gs-owner-b-token-1234567890",
                email: "gs-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(other.LocationId, "mo")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetSearch_Returns404_ForUnknownLocation()
        {
            var owner = await SeedOwnerAsync("gs-unknown-loc-token-12345");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(999_999, "mo")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyGroups_WhenQueryShorterThanTwo()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "gs-short-q-token-123456789",
                guestName: "Mohamed",
                email: "mo@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "m")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("m", body.GetProperty("q").GetString());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("guests", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ReturnsGuestHits_ForOwnerMatch()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "gs-owner-match-token-123456",
                guestName: "Mohamed",
                email: "a@example.com",
                locationName: "Camden"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "mo")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("mo", body.GetProperty("q").GetString());
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );

            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("guests", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.LocationGuestId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("guest", hit.GetProperty("entityType").GetString());
            Assert.Equal("Mohamed", hit.GetProperty("title").GetString());
            Assert.Equal("a@example.com", hit.GetProperty("subtitle").GetString());
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
            Assert.Equal(
                "Eligible — Email",
                hit.GetProperty("status").GetString()
            );
            Assert.False(hit.TryGetProperty("email", out _));
            Assert.False(hit.TryGetProperty("mobile", out _));
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantGuests()
        {
            var ownerA = await SeedOwnerWithGuestAsync(
                "gs-tenant-a-token-123456789",
                guestName: "Mohamed",
                email: "a-mo@example.com"
            );
            var ownerB = await SeedOwnerWithGuestAsync(
                "gs-tenant-b-token-123456789",
                guestName: "Mohamed",
                email: "b-mo@example.com",
                emailUser: "gs-tenant-b-owner@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "mo")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", ownerA.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                ownerA.LocationGuestId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.Equal(
                "a-mo@example.com",
                hits[0].GetProperty("subtitle").GetString()
            );
            Assert.NotEqual(
                ownerB.LocationGuestId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopesHitsToShellLocation_NotSiblingLocation()
        {
            var seeded = await SeedMultiLocationGuestsAsync(
                "gs-multi-loc-token-12345678"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationAId, "Guest")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hits = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            Assert.Equal(
                "Location A Guest",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationAId,
                hits[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyGuestHits_WhenStaffHasGuestsNoAccess()
        {
            var seeded = await SeedOwnerAndStaffMemberAsync(
                seedMatchingGuest: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.InScopeLocationId, "Scope")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var hits = body.GetProperty("groups")[0].GetProperty("hits");
            Assert.Equal(0, hits.GetArrayLength());
        }

        private static string SearchUrl(
            int locationId,
            string q,
            string? types = null,
            int? limit = null
        )
        {
            var url =
                $"/api/search?locationId={locationId}&q={Uri.EscapeDataString(q)}";
            if (types != null)
            {
                url += $"&types={Uri.EscapeDataString(types)}";
            }
            if (limit != null)
            {
                url += $"&limit={limit.Value}";
            }
            return url;
        }

        private async Task<(string Jwt, int LocationId, int RestaurantId)> SeedOwnerAsync(
            string linkToken,
            string email = "gs-owner@example.com",
            string locationName = "Camden Street"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Global Search Owner",
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
                Name = "Global Search Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            // linkToken reserved for uniqueness / future activation seeds
            _ = linkToken;

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, restaurant.Id);
        }

        private async Task<OwnerGuestSeed> SeedOwnerWithGuestAsync(
            string linkToken,
            string guestName,
            string email,
            string locationName = "Camden Street",
            string? emailUser = null
        )
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: emailUser ?? $"{Guid.NewGuid():N}@example.com",
                locationName: locationName
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                RestaurantId = owner.RestaurantId,
                Email = email,
                NormalizedEmail = email.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = owner.LocationId,
                Name = guestName,
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            return new OwnerGuestSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                locationGuest.Id
            );
        }

        private async Task<MultiLocationSeed> SeedMultiLocationGuestsAsync(
            string linkTokenPrefix
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "GS Multi Owner",
                Email = $"{linkTokenPrefix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900111",
                Role = "Owner",
                AccountType = "Multi",
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "GS Multi Venue",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var locationA = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var locationB = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Second Street",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(locationA, locationB);
            await context.SaveChangesAsync();

            var masterA = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "a@example.com",
                NormalizedEmail = "a@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            var masterB = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "b@example.com",
                NormalizedEmail = "b@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(masterA, masterB);
            await context.SaveChangesAsync();

            context.LocationGuests.AddRange(
                new LocationGuest
                {
                    MasterGuestId = masterA.Id,
                    RestaurantLocationId = locationA.Id,
                    Name = "Location A Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                },
                new LocationGuest
                {
                    MasterGuestId = masterB.Id,
                    RestaurantLocationId = locationB.Id,
                    Name = "Location B Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow.AddDays(-4),
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new MultiLocationSeed(jwt, locationA.Id, locationB.Id);
        }

        private async Task<StaffSeed> SeedOwnerAndStaffMemberAsync(
            bool seedMatchingGuest
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "GS Scope Owner",
                Email = $"gs-owner-12-{Guid.NewGuid():N}@example.com",
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
                Name = "GS Scope Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope);
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
                FullName = "GS Staff Member",
                Email = $"gs-staff-12-{Guid.NewGuid():N}@example.com",
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
                PermissionRole = PermissionRoles.Staff,
                LocationScope = LocationScopeKind.NamedList,
                NamedLocationIdsJson =
                    MembershipLocationScope.SerializeNamedIds([inScope.Id]),
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            if (seedMatchingGuest)
            {
                var email = $"{Guid.NewGuid():N}@example.com";
                var master = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = email,
                    NormalizedEmail = email,
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(master);
                await context.SaveChangesAsync();

                context.LocationGuests.Add(new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = inScope.Id,
                    Name = "In Scope Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                });
                await context.SaveChangesAsync();
            }

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new StaffSeed(memberJwt, inScope.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private sealed record OwnerGuestSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int LocationGuestId
        );

        private sealed record MultiLocationSeed(
            string Jwt,
            int LocationAId,
            int LocationBId
        );

        private sealed record StaffSeed(
            string MemberJwt,
            int InScopeLocationId
        );
    }
}
