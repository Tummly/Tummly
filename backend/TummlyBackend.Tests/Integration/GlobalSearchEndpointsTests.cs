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

        [Fact]
        public async Task GetSearch_ReturnsCampaignHits_ForOwnerNameMatch()
        {
            var seeded = await SeedOwnerWithCampaignAsync(
                "gs-campaign-match-token-12",
                campaignName: "Weekend brunch push",
                status: "draft",
                channel: "email",
                locationName: "Camden"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "week", types: "campaigns")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());

            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("campaigns", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.CampaignId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("campaign", hit.GetProperty("entityType").GetString());
            Assert.Equal(
                "Weekend brunch push",
                hit.GetProperty("title").GetString()
            );
            Assert.Equal("Email", hit.GetProperty("subtitle").GetString());
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
            Assert.Equal("Draft", hit.GetProperty("status").GetString());
            Assert.False(hit.TryGetProperty("billingReservationRef", out _));
            Assert.False(hit.TryGetProperty("messageBody", out _));
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantCampaigns()
        {
            var ownerA = await SeedOwnerWithCampaignAsync(
                "gs-camp-tenant-a-token-123",
                campaignName: "Shared name brunch",
                status: "scheduled",
                channel: "sms"
            );
            var ownerB = await SeedOwnerWithCampaignAsync(
                "gs-camp-tenant-b-token-123",
                campaignName: "Shared name brunch",
                status: "draft",
                channel: "email",
                emailUser: "gs-camp-tenant-b@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "Shared", types: "campaigns")
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
                ownerA.CampaignId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.NotEqual(
                ownerB.CampaignId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopesCampaignHitsToShellLocation_NotSibling()
        {
            var seeded = await SeedMultiLocationCampaignsAsync(
                "gs-multi-camp-token-12345"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationAId, "Campaign", types: "campaigns")
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
                "Location A Campaign",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationAId,
                hits[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyCampaignHits_WhenStaffHasCampaignsNoAccess()
        {
            var seeded = await SeedOwnerAndStaffMemberAsync(
                seedMatchingGuest: false,
                seedMatchingCampaign: true
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.InScopeLocationId, "Scope", types: "campaigns")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("campaigns", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ReturnsOfferHits_ForOwnerTitleMatch()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-match-token-12345",
                offerTitle: "Monday Lunch Deal",
                locationName: "Camden"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "lunch", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal("lunch", body.GetProperty("q").GetString());
            Assert.Equal(
                seeded.LocationId,
                body.GetProperty("locationId").GetInt32()
            );

            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("offers", groups[0].GetProperty("type").GetString());

            var hits = groups[0].GetProperty("hits");
            Assert.Equal(1, hits.GetArrayLength());
            var hit = hits[0];
            Assert.Equal(
                seeded.OfferId.ToString(),
                hit.GetProperty("id").GetString()
            );
            Assert.Equal("offer", hit.GetProperty("entityType").GetString());
            Assert.Equal(
                "Monday Lunch Deal",
                hit.GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationId,
                hit.GetProperty("locationId").GetInt32()
            );
            Assert.Equal("Camden", hit.GetProperty("locationName").GetString());
            Assert.Equal("Active", hit.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetSearch_DoesNotReturnOtherTenantOffers()
        {
            var ownerA = await SeedOwnerWithOfferAsync(
                "gs-offer-tenant-a-token-123",
                offerTitle: "Shared Title Deal"
            );
            var ownerB = await SeedOwnerWithOfferAsync(
                "gs-offer-tenant-b-token-123",
                offerTitle: "Shared Title Deal",
                emailUser: "gs-offer-tenant-b@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(ownerA.LocationId, "Shared", types: "offers")
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
                ownerA.OfferId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
            Assert.NotEqual(
                ownerB.OfferId.ToString(),
                hits[0].GetProperty("id").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_ScopesOfferHitsToShellLocation_NotSiblingLocation()
        {
            var seeded = await SeedMultiLocationOffersAsync(
                "gs-offer-multi-loc-token-12"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationAId, "Deal", types: "offers")
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
                "Location A Deal",
                hits[0].GetProperty("title").GetString()
            );
            Assert.Equal(
                seeded.LocationAId,
                hits[0].GetProperty("locationId").GetInt32()
            );
        }

        [Fact]
        public async Task GetSearch_ReturnsEmptyOfferHits_WhenBillingAdminHasOffersNoAccess()
        {
            var seeded = await SeedOwnerAndBillingAdminWithOfferAsync();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Lunch", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var groups = body.GetProperty("groups");
            Assert.Equal(1, groups.GetArrayLength());
            Assert.Equal("offers", groups[0].GetProperty("type").GetString());
            Assert.Equal(0, groups[0].GetProperty("hits").GetArrayLength());
        }

        [Fact]
        public async Task GetSearch_ProjectsEffectiveExpiredStatus_NotStoredActive()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-expired-token-1234",
                offerTitle: "Expired Lunch Deal",
                status: CatalogOfferStatus.Active,
                validity: CatalogOfferValidity.ChooseExpiryDate,
                customExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3))
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Expired", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var hit = (await ReadJsonAsync(response))
                .GetProperty("groups")[0]
                .GetProperty("hits")[0];
            Assert.Equal("Expired", hit.GetProperty("status").GetString());
            Assert.Equal(
                "Expired Lunch Deal",
                hit.GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task GetSearch_DoesNotMutateOfferStatus_WhenSearching()
        {
            var seeded = await SeedOwnerWithOfferAsync(
                "gs-offer-readonly-token-123",
                offerTitle: "Stable Draft Deal",
                status: CatalogOfferStatus.Draft
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                SearchUrl(seeded.LocationId, "Stable", types: "offers")
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var offer = await context.CatalogOffers.FindAsync(seeded.OfferId);
            Assert.NotNull(offer);
            Assert.Equal(CatalogOfferStatus.Draft, offer!.Status);
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

        private async Task<OwnerCampaignSeed> SeedOwnerWithCampaignAsync(
            string linkToken,
            string campaignName,
            string status,
            string channel,
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

            var campaign = new Campaign
            {
                RestaurantLocationId = owner.LocationId,
                Name = campaignName,
                Status = status,
                Channel = channel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();

            return new OwnerCampaignSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                campaign.Id
            );
        }

        private async Task<MultiLocationSeed> SeedMultiLocationCampaignsAsync(
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
                FullName = "GS Multi Camp Owner",
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
                Name = "GS Multi Camp Venue",
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

            context.Campaigns.AddRange(
                new Campaign
                {
                    RestaurantLocationId = locationA.Id,
                    Name = "Location A Campaign",
                    Status = "draft",
                    Channel = "email",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                },
                new Campaign
                {
                    RestaurantLocationId = locationB.Id,
                    Name = "Location B Campaign",
                    Status = "sent",
                    Channel = "sms",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
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
            bool seedMatchingGuest,
            bool seedMatchingCampaign = false
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

            if (seedMatchingCampaign)
            {
                context.Campaigns.Add(new Campaign
                {
                    RestaurantLocationId = inScope.Id,
                    Name = "In Scope Campaign",
                    Status = "draft",
                    Channel = "email",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
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

        private async Task<OwnerOfferSeed> SeedOwnerWithOfferAsync(
            string linkToken,
            string offerTitle,
            string status = CatalogOfferStatus.Active,
            CatalogOfferValidity validity = CatalogOfferValidity.Days14AfterIssue,
            DateOnly? customExpiryDate = null,
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

            var now = DateTime.UtcNow;
            var offer = new CatalogOffer
            {
                RestaurantLocationId = owner.LocationId,
                Status = status,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = offerTitle,
                Description = "Global Search offer seed.",
                Validity = validity,
                CustomExpiryDate = customExpiryDate,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            return new OwnerOfferSeed(
                owner.Jwt,
                owner.LocationId,
                owner.RestaurantId,
                offer.Id
            );
        }

        private async Task<MultiLocationSeed> SeedMultiLocationOffersAsync(
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
                FullName = "GS Offer Multi Owner",
                Email = $"{linkTokenPrefix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900112",
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
                Name = "GS Offer Multi Venue",
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

            var now = DateTime.UtcNow;
            context.CatalogOffers.AddRange(
                new CatalogOffer
                {
                    RestaurantLocationId = locationA.Id,
                    Status = CatalogOfferStatus.Active,
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "Location A Deal",
                    Description = "A",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now.AddDays(-2),
                },
                new CatalogOffer
                {
                    RestaurantLocationId = locationB.Id,
                    Status = CatalogOfferStatus.Active,
                    OfferType = CatalogOfferType.FixedDiscount,
                    Title = "Location B Deal",
                    Description = "B",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountAmount = 5m,
                    CreatedAt = now.AddDays(-3),
                    UpdatedAt = now.AddDays(-3),
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

        private async Task<BillingAdminOfferSeed> SeedOwnerAndBillingAdminWithOfferAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "GS Offer Scope Owner",
                Email = $"gs-offer-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900114",
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
                Name = "GS Offer Scope Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
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
                FullName = "GS Billing Admin",
                Email = $"gs-billing-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900115",
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
                PermissionRole = PermissionRoles.BillingAdmin,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var now = DateTime.UtcNow;
            context.CatalogOffers.Add(new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "Lunch Special",
                Description = "NoAccess check",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await context.SaveChangesAsync();

            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new BillingAdminOfferSeed(memberJwt, location.Id);
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

        private sealed record OwnerCampaignSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int CampaignId
        );

        private sealed record OwnerOfferSeed(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int OfferId
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

        private sealed record BillingAdminOfferSeed(
            string MemberJwt,
            int LocationId
        );
    }
}
