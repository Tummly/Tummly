using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// HTTP seam: Active Offer cap on promote / Resume (credit-ledger ticket 30).
    /// </summary>
    public class OffersActiveEntitlementEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string CurrentPricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OffersActiveEntitlementEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateOffer_AtActiveCap_SucceedsAsDraft()
        {
            var seeded = await SeedPilotWithActiveOfferAsync("offer-cap-create");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                seeded.Jwt,
                SampleOfferBody(seeded.LocationId, "Still a Draft")
            );
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var offer = (await ReadJsonAsync(response)).GetProperty("offer");
            Assert.Equal("draft", offer.GetProperty("status").GetString());
        }

        [Fact]
        public async Task PromoteToActive_AtCap_Returns409_WithCapAndCurrent()
        {
            var seeded = await SeedPilotWithActiveOfferAsync("offer-cap-promote");
            var draftId = await CreateOfferAsync(seeded, "Second offer");

            using var attach = AuthorizedJson(
                HttpMethod.Put,
                $"/api/capture/locations/{seeded.LocationId}/thank-you-offer",
                seeded.Jwt,
                new { offerId = draftId }
            );
            var response = await _client.SendAsync(attach);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "active_offer_cap_reached",
                body.GetProperty("code").GetString()
            );
            Assert.Equal(1, body.GetProperty("cap").GetInt32());
            Assert.Equal(1, body.GetProperty("current").GetInt32());

            using var get = AuthorizedGet($"/api/offers/{draftId}", seeded.Jwt);
            var getResponse = await _client.SendAsync(get);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(
                "draft",
                (await ReadJsonAsync(getResponse))
                    .GetProperty("offer")
                    .GetProperty("status")
                    .GetString()
            );
        }

        [Fact]
        public async Task ResumeToActive_AtCap_Returns409_WithCapAndCurrent()
        {
            var seeded = await SeedPilotOwnerAsync("offer-cap-resume");
            var pausedId = await CreateOfferAsync(seeded, "Paused with attach");
            using var campaign = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns",
                seeded.Jwt,
                new
                {
                    locationId = seeded.LocationId,
                    name = "Hold attach",
                    goalId = "thank-recent-guests",
                    offerStance = "create-new-offer",
                    offerId = pausedId,
                }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(campaign)).StatusCode
            );

            using var pause = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{pausedId}/pause",
                seeded.Jwt,
                new { }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(pause)).StatusCode
            );

            var activeId = await CreateOfferAsync(seeded, "Fills the cap");
            using var thankYou = AuthorizedJson(
                HttpMethod.Put,
                $"/api/capture/locations/{seeded.LocationId}/thank-you-offer",
                seeded.Jwt,
                new { offerId = activeId }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(thankYou)).StatusCode
            );

            using var resume = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{pausedId}/resume",
                seeded.Jwt,
                new { }
            );
            var response = await _client.SendAsync(resume);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "active_offer_cap_reached",
                body.GetProperty("code").GetString()
            );
            Assert.Equal(1, body.GetProperty("cap").GetInt32());
            Assert.Equal(1, body.GetProperty("current").GetInt32());
        }

        [Fact]
        public async Task ReattachAlreadyActive_AtCap_Succeeds()
        {
            var seeded = await SeedPilotWithActiveOfferAsync("offer-cap-reattach");

            using var first = AuthorizedJson(
                HttpMethod.Put,
                $"/api/capture/locations/{seeded.LocationId}/thank-you-offer",
                seeded.Jwt,
                new { offerId = seeded.ActiveOfferId }
            );
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(first)).StatusCode
            );

            using var again = AuthorizedJson(
                HttpMethod.Put,
                $"/api/capture/locations/{seeded.LocationId}/thank-you-offer",
                seeded.Jwt,
                new { offerId = seeded.ActiveOfferId }
            );
            var response = await _client.SendAsync(again);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var get = AuthorizedGet(
                $"/api/offers/{seeded.ActiveOfferId}",
                seeded.Jwt
            );
            var getResponse = await _client.SendAsync(get);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(
                "active",
                (await ReadJsonAsync(getResponse))
                    .GetProperty("offer")
                    .GetProperty("status")
                    .GetString()
            );
        }

        private sealed record SeededOwner(
            string Jwt,
            int LocationId,
            int RestaurantId
        );

        private sealed record SeededWithActive(
            string Jwt,
            int LocationId,
            int RestaurantId,
            int ActiveOfferId
        );

        private async Task<SeededWithActive> SeedPilotWithActiveOfferAsync(
            string emailLocalPart
        )
        {
            var seeded = await SeedPilotOwnerAsync(emailLocalPart);
            var offerId = await SeedActiveOfferDirectAsync(
                seeded.LocationId,
                "Already Active"
            );
            return new SeededWithActive(
                seeded.Jwt,
                seeded.LocationId,
                seeded.RestaurantId,
                offerId
            );
        }

        private async Task<SeededOwner> SeedPilotOwnerAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Offer Cap Owner",
                Email = $"{emailLocalPart}@example.com",
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
                Name = "Offer Cap Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    CurrentPricebookId
                )
            );

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

            return new SeededOwner(jwt, location.Id, restaurant.Id);
        }

        private async Task<int> SeedActiveOfferDirectAsync(
            int locationId,
            string title
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var entity = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.FixedDiscount,
                Title = title,
                Description = "Seeded stored Active offer.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(entity);
            await context.SaveChangesAsync();
            return entity.Id;
        }

        private async Task<int> CreateOfferAsync(
            SeededOwner seeded,
            string title
        ) => await CreateOfferAsync(seeded.Jwt, seeded.LocationId, title);

        private async Task<int> CreateOfferAsync(
            SeededWithActive seeded,
            string title
        ) => await CreateOfferAsync(seeded.Jwt, seeded.LocationId, title);

        private async Task<int> CreateOfferAsync(
            string jwt,
            int locationId,
            string title
        )
        {
            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers",
                jwt,
                SampleOfferBody(locationId, title)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ReadJsonAsync(response))
                .GetProperty("offer")
                .GetProperty("id")
                .GetInt32();
        }

        private static object SampleOfferBody(int locationId, string title) =>
            new
            {
                locationId,
                offerType = "fixed_discount",
                title,
                description = "A reusable campaign offer definition.",
                validity = "14_days_after_issue",
                discountAmount = 5m,
            };

        private static HttpRequestMessage AuthorizedGet(string url, string jwt)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            return request;
        }

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body
        )
        {
            var request = new HttpRequestMessage(method, url)
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
    }
}
