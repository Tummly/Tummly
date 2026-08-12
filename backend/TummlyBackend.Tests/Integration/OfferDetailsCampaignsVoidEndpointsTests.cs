using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: GET /api/offers/{id}/linked-campaigns, /issuance-sources, /void-requests (ticket 41).
    /// </summary>
    public class OfferDetailsCampaignsVoidEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OfferDetailsCampaignsVoidEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListLinkedCampaigns_ReturnsCampaignsAttachedToOffer()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-41-linked");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var campaignId = await SeedCampaignAsync(
                seeded.LocationId,
                offerId,
                name: "Summer thank-you",
                audienceKey: "all-eligible-guests",
                channel: "email",
                status: "sent"
            );
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Maya");
            await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-LNK001",
                source: OfferIssueSources.Campaign,
                campaignId: campaignId,
                claimedAt: DateTime.UtcNow.AddDays(-2),
                redeemedAt: DateTime.UtcNow.AddDays(-1)
            );

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/linked-campaigns",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());
            var row = items[0];
            Assert.Equal(
                "Summer thank-you",
                row.GetProperty("campaignName").GetString()
            );
            Assert.Equal("sent", row.GetProperty("status").GetString());
            Assert.Equal("Sent", row.GetProperty("statusLabel").GetString());
            Assert.Equal("Camden", row.GetProperty("locationName").GetString());
            Assert.Equal("EMAIL", row.GetProperty("channelLabel").GetString());
            Assert.Equal(
                "All eligible guests",
                row.GetProperty("audienceLabel").GetString()
            );
            Assert.Equal("1", row.GetProperty("passesIssued").GetString());
            Assert.Equal("1", row.GetProperty("claims").GetString());
            Assert.Equal("1", row.GetProperty("redemptions").GetString());
        }

        [Fact]
        public async Task ListLinkedCampaigns_ReturnsEmpty_WhenNoneAttached()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-41-linked-empty");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/linked-campaigns",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task ListIssuanceSources_GroupsBySourceAndCampaign()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-41-issuance");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var campaignId = await SeedCampaignAsync(
                seeded.LocationId,
                offerId,
                name: "Welcome blast",
                audienceKey: "new-guests",
                channel: "sms",
                status: "sent"
            );
            var guestA = await SeedLocationGuestAsync(seeded.LocationId, "Ava");
            var guestB = await SeedLocationGuestAsync(seeded.LocationId, "Ben");
            await SeedOfferIssueAsync(
                offerId,
                guestA,
                claimCode: "TUM-ISS001",
                source: OfferIssueSources.Campaign,
                campaignId: campaignId,
                claimedAt: DateTime.UtcNow.AddDays(-3)
            );
            await SeedOfferIssueAsync(
                offerId,
                guestB,
                claimCode: "TUM-ISS002",
                source: OfferIssueSources.GuestFormThankYou,
                campaignId: null,
                claimedAt: DateTime.UtcNow.AddDays(-1)
            );

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/issuance-sources",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());

            var bySource = items
                .EnumerateArray()
                .ToDictionary(
                    row => row.GetProperty("sourceLabel").GetString()!,
                    row => row,
                    StringComparer.Ordinal
                );
            Assert.True(bySource.ContainsKey("Campaign"));
            Assert.True(bySource.ContainsKey("Guest form thank-you"));
            Assert.Equal(
                "Welcome blast",
                bySource["Campaign"].GetProperty("pathLabel").GetString()
            );
            Assert.Equal(
                "Thank-you screen",
                bySource["Guest form thank-you"].GetProperty("pathLabel").GetString()
            );
        }

        [Fact]
        public async Task ListVoidRequests_ReturnsPersistedRowsForOffer()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-41-void-list");
            var guestId = await SeedLocationGuestAsync(seeded.LocationId, "Maya");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);
            var issueId = await SeedOfferIssueAsync(
                offerId,
                guestId,
                claimCode: "TUM-VOD001",
                source: OfferIssueSources.GuestFormThankYou,
                campaignId: null,
                claimedAt: DateTime.UtcNow.AddDays(-2),
                redeemedAt: DateTime.UtcNow.AddDays(-1)
            );
            var requestId = await SeedPendingVoidAsync(
                issueId,
                offerId,
                seeded.LocationId,
                seeded.UserId
            );

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/void-requests",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var items = body.GetProperty("items");
            Assert.Equal(1, items.GetArrayLength());
            var row = items[0];
            Assert.Equal(
                requestId.ToString(),
                row.GetProperty("requestId").GetString()
            );
            Assert.Equal("Maya", row.GetProperty("guestName").GetString());
            Assert.Equal("pending", row.GetProperty("status").GetString());
            Assert.Equal("Pending", row.GetProperty("statusLabel").GetString());
            Assert.Equal(
                "Redeemed by mistake",
                row.GetProperty("reasonText").GetString()
            );
            Assert.Equal(
                "Keep pass unusable",
                row.GetProperty("correctionText").GetString()
            );
            Assert.Equal(
                issueId.ToString(),
                row.GetProperty("passId").GetString()
            );
        }

        [Fact]
        public async Task ListVoidRequests_ReturnsEmpty_WhenNone()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-41-void-empty");
            var offerId = await SeedCatalogOfferAsync(seeded.LocationId);

            using var request = AuthorizedGet(
                $"/api/offers/{offerId}/void-requests",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        }

        [Fact]
        public async Task ListLinkedCampaigns_ReturnsNotFound_ForMissingOffer()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-41-missing");

            using var request = AuthorizedGet(
                "/api/offers/999999/linked-campaigns",
                seeded.Jwt
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
                FullName = "Offer Details Owner",
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
                Name = "Offer Details Venue",
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
                Email = $"details-guest-{Guid.NewGuid():N}@example.com",
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

        private async Task<int> SeedCatalogOfferAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "10% off next visit",
                Description = "Details campaigns/void test offer",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task<int> SeedCampaignAsync(
            int locationId,
            int offerId,
            string name,
            string audienceKey,
            string channel,
            string status
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Status = status,
                Name = name,
                AudienceKey = audienceKey,
                Channel = channel,
                OfferId = offerId,
                OfferStance = "existing-offer",
                ScheduleMode = "send-now",
                ScheduledAtUtc = now.AddDays(-5),
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now.AddDays(-5),
            };
            context.Campaigns.Add(campaign);
            await context.SaveChangesAsync();
            return campaign.Id;
        }

        private async Task<int> SeedOfferIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            string claimCode,
            string source,
            int? campaignId,
            DateTime? claimedAt,
            DateTime? redeemedAt = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issuedAt = DateTime.UtcNow.AddDays(-7);

            var issue = new OfferIssue
            {
                CatalogOfferId = catalogOfferId,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = issuedAt,
                ClaimedAtUtc = claimedAt,
                RedeemedAtUtc = redeemedAt,
                Source = source,
                CampaignId = campaignId,
                ExpiryAtUtc = issuedAt.AddDays(14),
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "10% off next visit",
                Description = "Details test issue",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
            };
            context.OfferIssues.Add(issue);
            await context.SaveChangesAsync();
            return issue.Id;
        }

        private async Task<int> SeedPendingVoidAsync(
            int issueId,
            int offerId,
            int locationId,
            int userId
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var request = new OfferVoidRequest
            {
                OfferIssueId = issueId,
                CatalogOfferId = offerId,
                RestaurantLocationId = locationId,
                RequestedByUserId = userId,
                RequestedAtUtc = now,
                OriginalRedeemedAtUtc = now.AddDays(-1),
                ReasonId = OfferVoidRequestReasonIds.RedeemedByMistake,
                Explanation = null,
                CorrectionId = OfferVoidRequestCorrectionIds.KeepUnusable,
                Status = OfferVoidRequestStatuses.Pending,
            };
            context.OfferVoidRequests.Add(request);
            await context.SaveChangesAsync();
            return request.Id;
        }
    }
}
