using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: POST /api/scan/{token}/feedback with a live thank-you Offer.
    /// A repeat submit for the same contact and comment re-shows the existing
    /// Claim and does not issue a second one.
    /// </summary>
    public class ScanThankYouRepeatClaimEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ScanThankYouRepeatClaimEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RepeatSubmit_SameGuestAndComment_ReturnsSameClaim_WithoutNewIssue()
        {
            const string token = "scan-thank-you-repeat-claim-tok";
            const string contact = "repeat-claim@example.com";
            const string comment = "Repeat claim check.";

            var locationId = await SeedThankYouLocationAsync(token);

            var first = await PostFeedbackAsync(token, contact, comment);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var firstOffer = (await ReadJsonAsync(first)).GetProperty("offer");
            var firstClaimCode = firstOffer.GetProperty("claimCode").GetString();
            Assert.False(string.IsNullOrWhiteSpace(firstClaimCode));

            var second = await PostFeedbackAsync(token, contact, comment);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var secondOffer = (await ReadJsonAsync(second)).GetProperty("offer");
            Assert.Equal(firstClaimCode, secondOffer.GetProperty("claimCode").GetString());
            Assert.True(secondOffer.GetProperty("alreadyClaimed").GetBoolean());

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var guestIds = await context.LocationGuests
                .Where(row => row.RestaurantLocationId == locationId)
                .Select(row => (int?)row.Id)
                .ToListAsync();
            Assert.Equal(
                1,
                await context.OfferIssues.CountAsync(row =>
                    row.Source == OfferIssueSources.GuestFormThankYou
                    && guestIds.Contains(row.LocationGuestId)
                )
            );
        }

        private async Task<HttpResponseMessage> PostFeedbackAsync(
            string token,
            string guestContact,
            string comment
        )
        {
            return await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Repeat Claim",
                    guestContact,
                    comment,
                    offersOptOut = false,
                }
            );
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private async Task<int> SeedThankYouLocationAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var restaurant = new Restaurant
            {
                Name = "Repeat Claim Kitchen",
                AccountType = "Single",
                OwnerUserId = 999_201,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "2 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = linkToken,
                Status = QrCodeStatus.Active,
                CreatedAt = now,
            });

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = "active",
                OfferType = CatalogOfferType.FixedDiscount,
                Title = "£5 off",
                Description = "Thank-you offer for repeat claim test.",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountAmount = 5m,
                StaffInstructions = "Apply £5 off before payment.",
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();

            location.ThankYouCatalogOfferId = offer.Id;
            await context.SaveChangesAsync();

            return location.Id;
        }
    }
}
