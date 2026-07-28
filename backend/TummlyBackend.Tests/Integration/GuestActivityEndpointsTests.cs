using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class GuestActivityEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public GuestActivityEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetGuestActivity_Returns401_WhenUnauthenticated()
        {
            var response = await _client.GetAsync(
                "/api/guests/1/activity?locationId=1"
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestActivity_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("activity-owner-a-token12xx");
            var other = await SeedOwnerWithGuestAsync(
                "activity-owner-b-token12xx",
                ownerEmail: "activity-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(other.LocationGuestId, other.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetGuestActivity_Returns404_ForUnknownGuest()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "activity-unknown-gst-token"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(999_999, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task GetGuestActivity_Returns404_WhenGuestBelongsToDifferentLocation()
        {
            var seeded = await SeedOwnerWithGuestOnSecondLocationAsync(
                "activity-wrong-loc-tokenxx"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(seeded.OtherLocationGuestId, seeded.LocationId)
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                "Guest not found.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task Backfill_CreatesGuestJoinedFeedbackTagAndClassificationEvents()
        {
            var joinedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
            var feedbackAt = new DateTime(2026, 5, 2, 11, 0, 0, DateTimeKind.Utc);
            var tagAt = new DateTime(2026, 5, 3, 12, 0, 0, DateTimeKind.Utc);
            var claimAt = new DateTime(2026, 5, 2, 11, 30, 0, DateTimeKind.Utc);

            var seeded = await SeedHistoricalGuestAsync(
                "activity-backfill-token12",
                joinedAt,
                feedbackAt,
                tagAt,
                claimAt
            );

            using (var scope = _factory.Services.CreateScope())
            {
                var backfill = scope.ServiceProvider
                    .GetRequiredService<ILocationGuestActivityBackfillService>();
                await backfill.BackfillAsync();
                await backfill.BackfillAsync();
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&sort=oldest-first"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(4, body.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, body.GetProperty("page").GetInt32());
            Assert.Equal(25, body.GetProperty("pageSize").GetInt32());

            var items = body.GetProperty("items").EnumerateArray().ToList();
            Assert.Equal(4, items.Count);
            Assert.Equal(
                LocationGuestActivityKinds.GuestJoined,
                items[0].GetProperty("kind").GetString()
            );
            Assert.Equal(joinedAt, items[0].GetProperty("occurredAt").GetDateTime());
            Assert.Equal(
                LocationGuestActivityKinds.Feedback,
                items[1].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "positive",
                items[1].GetProperty("sentiment").GetString()
            );
            Assert.Equal(
                LocationGuestActivityKinds.ClassificationSucceeded,
                items[2].GetProperty("kind").GetString()
            );
            Assert.Equal(claimAt, items[2].GetProperty("occurredAt").GetDateTime());
            Assert.Equal(
                "positive",
                items[2].GetProperty("sentiment").GetString()
            );
            Assert.Equal(
                LocationGuestActivityKinds.TagApplied,
                items[3].GetProperty("kind").GetString()
            );
            Assert.Equal("Vip", items[3].GetProperty("tagName").GetString());
        }

        [Fact]
        public async Task SubmitFeedback_EmitsGuestJoinedAndFeedbackOnActivityList()
        {
            var seeded = await SeedOwnerWithLinkAsync(
                "activity-emit-feedback-tk"
            );

            var submit = await _client.PostAsJsonAsync(
                $"/api/scan/{seeded.LinkToken}/feedback",
                new
                {
                    guestName = "Emit Guest",
                    guestContact = "emit-guest@example.com",
                    comment = "Great meal",
                    offersOptOut = false,
                }
            );
            Assert.Equal(HttpStatusCode.OK, submit.StatusCode);

            int guestId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                guestId = context.LocationGuests
                    .Single(lg => lg.RestaurantLocationId == seeded.LocationId)
                    .Id;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(guestId, seeded.LocationId)
                    + "&sort=oldest-first"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var response = await _client.SendAsync(request);
            var body = await ReadJsonAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var kinds = body.GetProperty("items")
                .EnumerateArray()
                .Select(i => i.GetProperty("kind").GetString())
                .ToList();

            Assert.Contains(LocationGuestActivityKinds.GuestJoined, kinds);
            Assert.Contains(LocationGuestActivityKinds.Feedback, kinds);
        }

        [Fact]
        public async Task GetGuestActivity_FiltersByTypeAndPaginates()
        {
            var seeded = await SeedOwnerWithGuestAsync(
                "activity-filter-page-token"
            );

            var t1 = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
            var t2 = new DateTime(2026, 6, 2, 10, 0, 0, DateTimeKind.Utc);
            var t3 = new DateTime(2026, 6, 3, 10, 0, 0, DateTimeKind.Utc);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var recorder = scope.ServiceProvider
                    .GetRequiredService<ILocationGuestActivityRecorder>();

                var guest = await context.LocationGuests
                    .SingleAsync(lg => lg.Id == seeded.LocationGuestId);

                recorder.RecordGuestJoined(guest, t1);
                recorder.RecordNoteAdded(guest.Id, "Operator A", t2);
                recorder.RecordProfileEdited(guest.Id, ["name"], t3);
                await context.SaveChangesAsync();
            }

            using var filtered = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&type=note&type=profile-update&sort=oldest-first"
            );
            filtered.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var filteredResponse = await _client.SendAsync(filtered);
            var filteredBody = await ReadJsonAsync(filteredResponse);

            Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);
            Assert.Equal(2, filteredBody.GetProperty("totalCount").GetInt32());
            var filteredKinds = filteredBody.GetProperty("items")
                .EnumerateArray()
                .Select(i => i.GetProperty("kind").GetString())
                .ToList();
            Assert.Equal(
                [
                    LocationGuestActivityKinds.NoteAdded,
                    LocationGuestActivityKinds.ProfileEdited,
                ],
                filteredKinds
            );

            using var pageRequest = new HttpRequestMessage(
                HttpMethod.Get,
                ActivityUrl(seeded.LocationGuestId, seeded.LocationId)
                    + "&sort=recent-activity&page=1&pageSize=25"
            );
            pageRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var pageResponse = await _client.SendAsync(pageRequest);
            var pageBody = await ReadJsonAsync(pageResponse);

            Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
            Assert.Equal(3, pageBody.GetProperty("totalCount").GetInt32());
            Assert.Equal(
                LocationGuestActivityKinds.ProfileEdited,
                pageBody.GetProperty("items")[0].GetProperty("kind").GetString()
            );
            Assert.Equal(
                "Camden Street",
                pageBody.GetProperty("items")[0]
                    .GetProperty("locationName")
                    .GetString()
            );
        }

        private static string ActivityUrl(int guestId, int locationId)
            => $"/api/guests/{guestId}/activity?locationId={locationId}";

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string linkToken,
            string email = "activity-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Activity Owner",
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
                Name = "Activity Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
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

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            string LinkToken
        )> SeedOwnerWithLinkAsync(string linkToken)
        {
            var owner = await SeedOwnerAsync(
                linkToken,
                email: $"{linkToken}@example.com"
            );
            return (owner.Jwt, owner.LocationId, linkToken);
        }

        private async Task<GuestSeed> SeedOwnerWithGuestAsync(
            string linkToken,
            string ownerEmail = "activity-guest-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Activity Owner",
                Email = ownerEmail,
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
                Name = "Activity Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "activity-guest@example.com",
                NormalizedEmail = "activity-guest@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Activity Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int OtherLocationGuestId
        )> SeedOwnerWithGuestOnSecondLocationAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Activity Owner",
                Email = "activity-twoloc@example.com",
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
                Name = "Activity Two Loc",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var primary = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var secondary = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Soho",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(primary, secondary);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "elsewhere-activity@example.com",
                NormalizedEmail = "elsewhere-activity@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var otherGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = secondary.Id,
                Name = "Elsewhere Guest",
                OffersOptOut = false,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(otherGuest);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, primary.Id, otherGuest.Id);
        }

        private async Task<GuestSeed> SeedHistoricalGuestAsync(
            string linkToken,
            DateTime joinedAt,
            DateTime feedbackAt,
            DateTime tagAt,
            DateTime claimAt
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Activity Owner",
                Email = $"{linkToken}@example.com",
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
                Name = "Activity Backfill Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden Street",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = "historical@example.com",
                NormalizedEmail = "historical@example.com",
                CreatedAt = joinedAt,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Historical Guest",
                OffersOptOut = false,
                CreatedAt = joinedAt,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = location.Id,
                    LocationGuestId = locationGuest.Id,
                    GuestName = "Historical Guest",
                    GuestContact = "historical@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Past visit",
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = FeedbackSentiment.Positive,
                    ClassificationClaimedAt = claimAt,
                    CreatedAt = feedbackAt,
                }
            );

            var tag = new GuestTag
            {
                RestaurantId = restaurant.Id,
                DisplayName = "Vip",
                NormalizedName = "vip",
                AiSourced = false,
                CreatedAt = tagAt,
            };
            context.GuestTags.Add(tag);
            await context.SaveChangesAsync();

            context.LocationGuestTags.Add(
                new LocationGuestTag
                {
                    LocationGuestId = locationGuest.Id,
                    GuestTagId = tag.Id,
                    CreatedAt = tagAt,
                }
            );
            await context.SaveChangesAsync();

            // Clear claim stamp after "terminal" — mirrors live Succeeded rows.
            var feedback = context.Feedbacks.Single(
                f => f.LocationGuestId == locationGuest.Id
            );
            feedback.ClassificationClaimedAt = null;
            // Store claimAt on a field we can still use for backfill approximation:
            // backfill uses ClassificationClaimedAt if set, else CreatedAt.
            // For historical Succeeded rows claim is cleared — approximation falls
            // back to CreatedAt. Set claim again so the test asserts the preferred path.
            feedback.ClassificationClaimedAt = claimAt;
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return new GuestSeed(jwt, location.Id, locationGuest.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var body =
                await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private sealed record GuestSeed(
            string Jwt,
            int LocationId,
            int LocationGuestId
        );
    }
}
