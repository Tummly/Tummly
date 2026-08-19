using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestUpsertOnFeedbackSubmitTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestUpsertOnFeedbackSubmitTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SubmitFeedback_CreatesMasterAndLocationGuest_AndLinksFeedbackFk()
        {
            const string token = "guest-upsert-create-email";
            var seeded = await SeedRestaurantWithLocationAsync(
                token,
                locationName: "Camden Street"
            );

            var response = await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName = "Jane Doe",
                    guestContact = "Jane@Example.com",
                    comment = "Great meal.",
                    offersOptOut = false
                }
            );

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var feedback = await context.Feedbacks
                .SingleAsync(f => f.RestaurantLocationId == seeded.LocationId);

            Assert.NotNull(feedback.LocationGuestId);

            var locationGuest = await context.LocationGuests
                .Include(lg => lg.MasterGuest)
                .SingleAsync(lg => lg.Id == feedback.LocationGuestId);

            Assert.Equal(seeded.LocationId, locationGuest.RestaurantLocationId);
            Assert.Equal("Jane Doe", locationGuest.Name);
            Assert.Equal(LocationGuestMarketingPreference.Allowed, locationGuest.MarketingPreference);
            Assert.Equal(seeded.RestaurantId, locationGuest.MasterGuest!.RestaurantId);
            Assert.Equal("Jane@Example.com", locationGuest.MasterGuest.Email);
            Assert.Equal("jane@example.com", locationGuest.MasterGuest.NormalizedEmail);
            Assert.Null(locationGuest.MasterGuest.Mobile);
        }

        [Fact]
        public async Task SubmitFeedback_UpdatesNameAndOffersOptOut_OnSameLocationGuest()
        {
            const string token = "guest-upsert-update-name-optout";
            var seeded = await SeedRestaurantWithLocationAsync(
                token,
                locationName: "Main"
            );

            await PostFeedbackAsync(
                token,
                guestName: "Jane Doe",
                guestContact: "jane@example.com",
                offersOptOut: true
            );

            var second = await PostFeedbackAsync(
                token,
                guestName: "Jane D.",
                guestContact: "jane@example.com",
                offersOptOut: false
            );
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            Assert.Equal(
                1,
                await context.MasterGuests.CountAsync(
                    g => g.RestaurantId == seeded.RestaurantId
                )
            );
            Assert.Equal(
                1,
                await context.LocationGuests.CountAsync(
                    lg => lg.RestaurantLocationId == seeded.LocationId
                )
            );

            var locationGuest = await context.LocationGuests
                .SingleAsync(lg => lg.RestaurantLocationId == seeded.LocationId);

            Assert.Equal("Jane D.", locationGuest.Name);
            Assert.Equal(LocationGuestMarketingPreference.Allowed, locationGuest.MarketingPreference);

            var feedbacks = await context.Feedbacks
                .Where(f => f.RestaurantLocationId == seeded.LocationId)
                .ToListAsync();
            Assert.Equal(2, feedbacks.Count);
            Assert.All(
                feedbacks,
                f => Assert.Equal(locationGuest.Id, f.LocationGuestId)
            );
        }

        [Fact]
        public async Task SubmitFeedback_PhoneUpdatesMobileOnly_EmailUpdatesEmailOnly()
        {
            const string emailToken = "guest-upsert-channel-email";
            const string phoneToken = "guest-upsert-channel-phone";

            var restaurant = await SeedRestaurantWithLocationAsync(
                emailToken,
                locationName: "Email Lane"
            );
            await SeedRestaurantWithLocationAsync(
                phoneToken,
                locationName: "Phone Lane",
                restaurantId: restaurant.RestaurantId
            );

            await PostFeedbackAsync(
                emailToken,
                guestName: "Alex",
                guestContact: "alex@example.com",
                offersOptOut: false
            );
            await PostFeedbackAsync(
                phoneToken,
                guestName: "Alex",
                guestContact: "07700 900123",
                offersOptOut: false
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // No email↔phone merge — two Master Guests.
            Assert.Equal(2, await context.MasterGuests.CountAsync(
                g => g.RestaurantId == restaurant.RestaurantId
            ));

            var emailMaster = await context.MasterGuests
                .SingleAsync(g =>
                    g.RestaurantId == restaurant.RestaurantId
                    && g.NormalizedEmail == "alex@example.com"
                );
            Assert.Equal("alex@example.com", emailMaster.Email);
            Assert.Null(emailMaster.Mobile);

            var phoneMaster = await context.MasterGuests
                .SingleAsync(g =>
                    g.RestaurantId == restaurant.RestaurantId
                    && g.NormalizedPhone == "07700900123"
                );
            Assert.Equal("07700 900123", phoneMaster.Mobile);
            Assert.Null(phoneMaster.Email);
        }

        [Fact]
        public async Task SubmitFeedback_UnknownContactType_SetsNeitherEmailNorMobile()
        {
            const string token = "guest-upsert-unknown-contact";
            var seeded = await SeedRestaurantWithLocationAsync(
                token,
                locationName: "Main"
            );

            var response = await PostFeedbackAsync(
                token,
                guestName: "Pat",
                guestContact: "not-an-email-or-phone",
                offersOptOut: false
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var feedback = await context.Feedbacks
                .Include(f => f.LocationGuest!)
                .ThenInclude(lg => lg.MasterGuest)
                .SingleAsync(f => f.RestaurantLocationId == seeded.LocationId);

            Assert.Equal(ContactType.Unknown, feedback.ContactType);
            Assert.NotNull(feedback.LocationGuestId);
            Assert.Null(feedback.LocationGuest!.MasterGuest!.Email);
            Assert.Null(feedback.LocationGuest.MasterGuest.Mobile);
            Assert.Null(feedback.LocationGuest.MasterGuest.NormalizedEmail);
            Assert.Null(feedback.LocationGuest.MasterGuest.NormalizedPhone);
        }

        [Fact]
        public async Task SubmitFeedback_SameMasterAtSecondLocation_CreatesSecondLocationGuest()
        {
            const string firstToken = "guest-upsert-loc-a";
            const string secondToken = "guest-upsert-loc-b";

            var first = await SeedRestaurantWithLocationAsync(
                firstToken,
                locationName: "Camden"
            );
            var second = await SeedRestaurantWithLocationAsync(
                secondToken,
                locationName: "Soho",
                restaurantId: first.RestaurantId
            );

            await PostFeedbackAsync(
                firstToken,
                guestName: "Jane Doe",
                guestContact: "jane@example.com",
                offersOptOut: false
            );

            DateTime firstLocationGuestCreatedAt;
            int firstLocationGuestId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var firstLg = await context.LocationGuests
                    .SingleAsync(lg => lg.RestaurantLocationId == first.LocationId);
                firstLocationGuestCreatedAt = firstLg.CreatedAt;
                firstLocationGuestId = firstLg.Id;
            }

            await PostFeedbackAsync(
                secondToken,
                guestName: "Jane Doe",
                guestContact: "jane@example.com",
                offersOptOut: true
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                Assert.Equal(
                    1,
                    await context.MasterGuests.CountAsync(
                        g => g.RestaurantId == first.RestaurantId
                    )
                );
                Assert.Equal(
                    2,
                    await context.LocationGuests.CountAsync(lg =>
                        lg.RestaurantLocationId == first.LocationId
                        || lg.RestaurantLocationId == second.LocationId
                    )
                );

                var firstLg = await context.LocationGuests
                    .SingleAsync(lg => lg.RestaurantLocationId == first.LocationId);
                var secondLg = await context.LocationGuests
                    .SingleAsync(lg => lg.RestaurantLocationId == second.LocationId);

                Assert.Equal(firstLg.MasterGuestId, secondLg.MasterGuestId);
                Assert.Equal(firstLocationGuestId, firstLg.Id);
                Assert.Equal(firstLocationGuestCreatedAt, firstLg.CreatedAt);
                Assert.NotEqual(firstLg.Id, secondLg.Id);
                Assert.Equal(LocationGuestMarketingPreference.Allowed, firstLg.MarketingPreference);
                Assert.Equal(LocationGuestMarketingPreference.OptedOut, secondLg.MarketingPreference);
            }
        }

        private async Task<HttpResponseMessage> PostFeedbackAsync(
            string token,
            string guestName,
            string guestContact,
            bool offersOptOut
        )
        {
            return await _client.PostAsJsonAsync(
                $"/api/scan/{token}/feedback",
                new
                {
                    guestName,
                    guestContact,
                    comment = "A useful visit.",
                    offersOptOut
                }
            );
        }

        private async Task<(int RestaurantId, int LocationId)> SeedRestaurantWithLocationAsync(
            string linkToken,
            string locationName,
            int? restaurantId = null
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            int resolvedRestaurantId;
            if (restaurantId is int existingId)
            {
                resolvedRestaurantId = existingId;
            }
            else
            {
                var restaurant = new Restaurant
                {
                    Name = "The Golden Fork",
                    AccountType = "Multi",
                    OwnerUserId = 999_001,
                    CreatedAt = DateTime.UtcNow,
                };
                context.Restaurants.Add(restaurant);
                await context.SaveChangesAsync();
                resolvedRestaurantId = restaurant.Id;
            }

            var location = new RestaurantLocation
            {
                RestaurantId = resolvedRestaurantId,
                LocationName = locationName,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            context.QrCodes.Add(new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = linkToken,
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();

            return (resolvedRestaurantId, location.Id);
        }
    }
}
