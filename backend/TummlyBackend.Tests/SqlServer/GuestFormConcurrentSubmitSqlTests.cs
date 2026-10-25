using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class GuestFormConcurrentSubmitSqlTests
    {
        private readonly SqlServerFixture _fixture;

        public GuestFormConcurrentSubmitSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task ConcurrentFirstSubmits_NewContact_OneFeedback_No500()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var factory = _fixture.CreateFactory();

            for (var iteration = 0; iteration < 5; iteration++)
            {
                // QrCode.Token max length is 32.
                var token = $"cn{iteration}{Guid.NewGuid():N}"[..32];
                var contact = $"cn{iteration}-{Guid.NewGuid():N}@example.com";
                const string comment = "Concurrent first submit.";
                var locationId = await SeedLiveThankYouLocationAsync(
                    factory.Services,
                    token,
                    restaurantName: $"Conc New {iteration}"
                );

                var payload = new
                {
                    guestName = "Alex Guest",
                    guestContact = contact,
                    comment,
                    offersOptOut = false,
                };

                var responses = await PostConcurrentAsync(factory, token, payload);

                Assert.All(
                    responses,
                    response => Assert.Equal(HttpStatusCode.OK, response.StatusCode)
                );

                var bodies = await Task.WhenAll(
                    responses.Select(ReadJsonAsync)
                );
                var codes = bodies
                    .Select(body =>
                        body.GetProperty("offer").GetProperty("claimCode").GetString()
                    )
                    .ToArray();
                Assert.False(string.IsNullOrWhiteSpace(codes[0]));
                Assert.Equal(codes[0], codes[1]);

                using var scope = factory.Services.CreateScope();
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                Assert.Equal(
                    1,
                    await context.Feedbacks.CountAsync(f =>
                        f.RestaurantLocationId == locationId
                    )
                );
                Assert.Equal(
                    1,
                    await context.MasterGuests.CountAsync(g =>
                        g.NormalizedEmail == contact.ToLowerInvariant()
                    )
                );
                Assert.Equal(
                    1,
                    await context.OfferIssues.CountAsync(issue =>
                        issue.LocationGuest!.RestaurantLocationId == locationId
                    )
                );
            }
        }

        [SkippableFact]
        public async Task ConcurrentSubmits_ExistingGuest_OneFeedback()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var factory = _fixture.CreateFactory();
            using var seedClient = factory.CreateClient();

            for (var iteration = 0; iteration < 5; iteration++)
            {
                // QrCode.Token max length is 32.
                var token = $"ce{iteration}{Guid.NewGuid():N}"[..32];
                var contact =
                    $"ce{iteration}-{Guid.NewGuid():N}@example.com";
                const string firstComment = "Seed visit.";
                const string raceComment = "Concurrent existing submit.";
                var locationId = await SeedLiveThankYouLocationAsync(
                    factory.Services,
                    token,
                    restaurantName: $"Conc Exist {iteration}"
                );

                var seedResponse = await seedClient.PostAsJsonAsync(
                    $"/api/scan/{token}/feedback",
                    new
                    {
                        guestName = "Alex Guest",
                        guestContact = contact,
                        comment = firstComment,
                        offersOptOut = false,
                    }
                );
                Assert.Equal(HttpStatusCode.OK, seedResponse.StatusCode);

                int feedbackBefore;
                using (var scope = factory.Services.CreateScope())
                {
                    var context = scope.ServiceProvider
                        .GetRequiredService<ApplicationDbContext>();
                    feedbackBefore = await context.Feedbacks.CountAsync(f =>
                        f.RestaurantLocationId == locationId
                    );
                    Assert.Equal(1, feedbackBefore);
                }

                var payload = new
                {
                    guestName = "Alex Guest",
                    guestContact = contact,
                    comment = raceComment,
                    offersOptOut = false,
                };

                var responses = await PostConcurrentAsync(factory, token, payload);
                Assert.All(
                    responses,
                    response => Assert.Equal(HttpStatusCode.OK, response.StatusCode)
                );

                var bodies = await Task.WhenAll(
                    responses.Select(ReadJsonAsync)
                );
                var codes = bodies
                    .Select(body =>
                        body.GetProperty("offer").GetProperty("claimCode").GetString()
                    )
                    .ToArray();
                Assert.False(string.IsNullOrWhiteSpace(codes[0]));
                Assert.Equal(codes[0], codes[1]);

                using (var scope = factory.Services.CreateScope())
                {
                    var context = scope.ServiceProvider
                        .GetRequiredService<ApplicationDbContext>();
                    Assert.Equal(
                        feedbackBefore + 1,
                        await context.Feedbacks.CountAsync(f =>
                            f.RestaurantLocationId == locationId
                        )
                    );
                    Assert.Equal(
                        1,
                        await context.OfferIssues.CountAsync(issue =>
                            issue.LocationGuest!.RestaurantLocationId
                                == locationId
                        )
                    );
                }
            }
        }

        private static async Task<HttpResponseMessage[]> PostConcurrentAsync(
            WebApplicationFactory<Program> factory,
            string token,
            object payload
        )
        {
            using var clientA = factory.CreateClient();
            using var clientB = factory.CreateClient();
            var ready = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );
            var arrived = 0;

            async Task<HttpResponseMessage> PostOnceAsync(HttpClient client)
            {
                if (Interlocked.Increment(ref arrived) == 2)
                {
                    ready.TrySetResult();
                }

                await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                return await client.PostAsJsonAsync(
                    $"/api/scan/{token}/feedback",
                    payload,
                    cts.Token
                );
            }

            return await Task.WhenAll(
                PostOnceAsync(clientA),
                PostOnceAsync(clientB)
            );
        }

        private static async Task<int> SeedLiveThankYouLocationAsync(
            IServiceProvider services,
            string linkToken,
            string restaurantName
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N");

            var user = new User
            {
                FullName = "Owner",
                Email = $"conc-owner-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900000",
                Role = "Owner",
                AccountType = "Single",
                CreatedAt = now,
                ActivatedAt = now,
                ActivationExpiresAt = now.AddDays(30),
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = restaurantName,
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var offer = new CatalogOffer
            {
                RestaurantLocationId = location.Id,
                Status = "active",
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Thanks for visiting",
                Description = "Guest form thank-you",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = now,
                UpdatedAt = now,
            };
            context.CatalogOffers.Add(offer);
            await context.SaveChangesAsync();
            location.ThankYouCatalogOfferId = offer.Id;

            context.QrCodes.Add(
                new QrCode
                {
                    RestaurantLocationId = location.Id,
                    QrType = QrType.SmartGuest,
                    Token = linkToken,
                    Status = QrCodeStatus.Active,
                    CreatedAt = now,
                }
            );
            await context.SaveChangesAsync();

            return location.Id;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            return document.RootElement.Clone();
        }
    }
}
