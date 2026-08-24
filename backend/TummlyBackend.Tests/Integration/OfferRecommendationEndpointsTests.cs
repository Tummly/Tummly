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
    public class OfferRecommendationEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OfferRecommendationEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostRecommendation_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsync(
                "/api/offers/1/recommendation",
                new StringContent(
                    """{"locationId":1}""",
                    Encoding.UTF8,
                    "application/json"
                )
            );
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_Returns404_WhenOfferMissing()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-missing");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/offers/999999/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task PostRecommendation_Returns403_WhenLocationNotOwned()
        {
            var owner = await SeedOwnerWithLocationAsync("offer-rec-loc-a");
            var other = await SeedOwnerWithLocationAsync("offer-rec-loc-b");
            var offerId = await SeedOfferAsync(
                owner.LocationId,
                CatalogOfferStatus.Draft
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                owner.Jwt,
                Body(other.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task PostRecommendation_Returns400_WhenOfferBelongsToOtherOwnedLocation()
        {
            var seeded = await SeedOwnerWithTwoLocationsAsync(
                "offer-rec-two-loc"
            );
            var offerId = await SeedOfferAsync(
                seeded.FirstLocationId,
                CatalogOfferStatus.Draft
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.SecondLocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("retryable").GetBoolean());
            Assert.Equal(0, fake.CallCount);
        }

        [Theory]
        [InlineData("draft")]
        [InlineData("paused")]
        [InlineData("archived")]
        public async Task PostRecommendation_NonActive_ReturnsNoneWithoutCallingProvider(
            string status
        )
        {
            var seeded = await SeedOwnerWithLocationAsync(
                $"offer-rec-{status}"
            );
            var offerId = await SeedOfferAsync(seeded.LocationId, status);

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_Expired_ReturnsNoneWithoutCallingProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-expired");
            var offerId = await SeedExpiredOfferAsync(seeded.LocationId);

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_ActiveZeroMarketingEligible_ReturnsNoneWithoutProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-zero-me");
            var offerId = await SeedOfferAsync(
                seeded.LocationId,
                CatalogOfferStatus.Active
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_ActiveWithClaimsInPeriod_ReturnsNoneWithoutProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-claims");
            var offerId = await SeedOfferAsync(
                seeded.LocationId,
                CatalogOfferStatus.Active
            );
            await SeedMarketingEligibleGuestAsync(seeded.LocationId);
            await SeedClaimAsync(
                offerId,
                seeded.LocationId,
                DateTime.UtcNow.AddDays(-1)
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_AzureCannotChangeRuledNone()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "offer-rec-azure-none"
            );
            var offerId = await SeedOfferAsync(
                seeded.LocationId,
                CatalogOfferStatus.Active
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "none",
                body.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_CachesByOperatorLocationOfferPeriod_RefreshBypasses()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-cache");
            var offerId = await SeedOfferAsync(
                seeded.LocationId,
                CatalogOfferStatus.Active
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using (var first = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            ))
            {
                var firstResponse = await _client.SendAsync(first);
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            }

            Assert.Equal(0, fake.CallCount);

            await SeedMarketingEligibleGuestAsync(seeded.LocationId);

            using (var second = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            ))
            {
                var secondResponse = await _client.SendAsync(second);
                Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
                var secondBody = await ReadJsonAsync(secondResponse);
                Assert.Equal(
                    "none",
                    secondBody.GetProperty("recommendation").GetProperty("type").GetString()
                );
            }

            Assert.Equal(0, fake.CallCount);

            var refreshBody = Body(seeded.LocationId);
            refreshBody["refresh"] = true;
            using (var refresh = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                refreshBody
            ))
            {
                var refreshResponse = await _client.SendAsync(refresh);
                Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
                var refreshJson = await ReadJsonAsync(refreshResponse);
                Assert.Equal(
                    "promote-this-offer",
                    refreshJson.GetProperty("recommendation").GetProperty("type").GetString()
                );
            }

            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_CacheKeyIncludesDefaultReportingPeriod()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-period");
            var offerId = await SeedOfferAsync(
                seeded.LocationId,
                CatalogOfferStatus.Active
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            using (var first = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            ))
            {
                var firstResponse = await _client.SendAsync(first);
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            }

            Assert.Equal(0, fake.CallCount);

            await SetReportingPeriodAsync(seeded.LocationId, "30days");
            await SeedMarketingEligibleGuestAsync(seeded.LocationId);

            using (var second = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                Body(seeded.LocationId)
            ))
            {
                var secondResponse = await _client.SendAsync(second);
                Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
                var secondBody = await ReadJsonAsync(secondResponse);
                Assert.Equal(
                    "promote-this-offer",
                    secondBody.GetProperty("recommendation").GetProperty("type").GetString()
                );
            }

            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostRecommendation_IgnoresPostedOverviewWindow()
        {
            var seeded = await SeedOwnerWithLocationAsync("offer-rec-kpi");
            var offerId = await SeedOfferAsync(
                seeded.LocationId,
                CatalogOfferStatus.Active
            );
            await SeedMarketingEligibleGuestAsync(seeded.LocationId);
            await SeedClaimAsync(
                offerId,
                seeded.LocationId,
                DateTime.UtcNow.AddDays(-20)
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeOfferRecommendationProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                FakeOfferRecommendationProvider.FixtureFor("promote-this-offer")
            );

            var body = Body(seeded.LocationId);
            body["overviewDatePreset"] = "last30";
            body["from"] = DateTime.UtcNow.AddDays(-30).ToString("O");
            body["to"] = DateTime.UtcNow.ToString("O");

            using var request = AuthorizedJson(
                HttpMethod.Post,
                $"/api/offers/{offerId}/recommendation",
                seeded.Jwt,
                body
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var json = await ReadJsonAsync(response);
            Assert.Equal(
                "promote-this-offer",
                json.GetProperty("recommendation").GetProperty("type").GetString()
            );
            Assert.Equal(1, fake.CallCount);
            Assert.NotNull(fake.LastInput);
            Assert.Equal("7days", fake.LastInput!.ReportingPeriod);
        }

        [Fact]
        public async Task PostRecommendation_FakeRegistered_NoLiveAzureRequired()
        {
            using var scope = _factory.Services.CreateScope();
            var provider = scope.ServiceProvider
                .GetRequiredService<IOfferRecommendationProvider>();
            Assert.IsType<FakeOfferRecommendationProvider>(provider);
        }

        private static Dictionary<string, object?> Body(int locationId)
        {
            return new Dictionary<string, object?>
            {
                ["locationId"] = locationId,
                ["refresh"] = false,
            };
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

        private async Task<(
            string Jwt,
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
                FullName = "Offer Rec Owner",
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
                Name = "Offer Rec Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                DefaultReportingPeriod = WorkspaceDefaultsOptions.DefaultReportingPeriod,
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

        private async Task<(
            string Jwt,
            int FirstLocationId,
            int SecondLocationId
        )> SeedOwnerWithTwoLocationsAsync(string emailLocalPart)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Offer Rec Two Loc",
                Email = $"{emailLocalPart}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900124",
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
                Name = "Offer Rec Group",
                AccountType = "Multi",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var first = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "One",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var second = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Two",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(first, second);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, first.Id, second.Id);
        }

        private async Task<int> SeedOfferAsync(int locationId, string status)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "10% off next visit",
                Description = "Quiet card seed",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task<int> SeedExpiredOfferAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Expired 10%",
                Description = "Past expiry",
                Validity = CatalogOfferValidity.ChooseExpiryDate,
                CustomExpiryDate = DateOnly.FromDateTime(now.AddDays(-1)),
                DiscountPercentage = 10m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task SeedMarketingEligibleGuestAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                Email = $"eligible-{Guid.NewGuid():N}@example.com",
                Mobile = "07700900999",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            context.LocationGuests.Add(
                new LocationGuest
                {
                    MasterGuestId = master.Id,
                    RestaurantLocationId = locationId,
                    Name = "Eligible Guest",
                    MarketingPreference = LocationGuestMarketingPreference.Allowed,
                    CreatedAt = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task SeedClaimAsync(
            int offerId,
            int locationId,
            DateTime claimedAtUtc
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var master = new MasterGuest
            {
                Email = $"claim-{Guid.NewGuid():N}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = locationId,
                Name = "Claim Guest",
                MarketingPreference = LocationGuestMarketingPreference.NotRecorded,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(guest);
            await context.SaveChangesAsync();

            context.OfferIssues.Add(
                new OfferIssue
                {
                    CatalogOfferId = offerId,
                    LocationGuestId = guest.Id,
                    ClaimCode = Guid.NewGuid().ToString("N")[..8],
                    IssuedAtUtc = claimedAtUtc,
                    ClaimedAtUtc = claimedAtUtc,
                    Source = OfferIssueSources.Campaign,
                    ExpiryAtUtc = claimedAtUtc.AddDays(14),
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "Issued",
                    Description = "Issued",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                }
            );
            await context.SaveChangesAsync();
        }

        private async Task SetReportingPeriodAsync(
            int locationId,
            string period
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .Where(location => location.Id == locationId)
                .Select(location => location.RestaurantId)
                .FirstAsync();
            var restaurant = await context.Restaurants
                .FirstAsync(row => row.Id == restaurantId);
            restaurant.DefaultReportingPeriod = period;
            await context.SaveChangesAsync();
        }
    }
}
