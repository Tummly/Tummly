using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Integration;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class GuestReplyConcurrentSqlTests
    {
        private readonly SqlServerFixture _fixture;

        public GuestReplyConcurrentSqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task ConcurrentIdenticalReplies_OneRow_OneDuplicateRefusal()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var factory = _fixture.CreateFactory();
            var seeded = await SeedFeedbackAsync(factory.Services, withCatalogAttach: false);

            var responses = await PostConcurrentAsync(
                factory,
                seeded,
                new
                {
                    channel = "email",
                    subject = "Sorry about your visit",
                    body = "Thank you for telling us.",
                    intent = "respond_to_guest",
                    purpose = "acknowledge_feedback",
                    tone = "warm_and_apologetic",
                },
                path: "guest-responses"
            );

            Assert.Equal(
                new[] { HttpStatusCode.OK, HttpStatusCode.Conflict },
                responses.Select(r => r.StatusCode).OrderBy(c => (int)c).ToArray()
            );

            var refused = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
            var refusedBody = await ReadJsonAsync(refused);
            Assert.Equal("duplicate_reply", refusedBody.GetProperty("code").GetString());

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.FeedbackGuestResponses.CountAsync(r =>
                    r.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [SkippableFact]
        public async Task ConcurrentRecoverySends_OneRecoveryIssue()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            await using var factory = _fixture.CreateFactory();
            var seeded = await SeedFeedbackAsync(factory.Services, withCatalogAttach: true);

            var responses = await PostConcurrentAsync(
                factory,
                seeded,
                new
                {
                    channel = "email",
                    subject = "A recovery offer from us",
                    body = "Please enjoy 20% off your next visit.",
                    intent = "respond_with_recovery_offer",
                    purpose = "include_a_recovery_offer",
                    tone = "warm_and_apologetic",
                },
                path: "recovery-offers"
            );

            Assert.Equal(
                new[] { HttpStatusCode.OK, HttpStatusCode.Conflict },
                responses.Select(r => r.StatusCode).OrderBy(c => (int)c).ToArray()
            );

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.OfferIssues.CountAsync(o =>
                    o.FeedbackId == seeded.FeedbackId
                    && o.Source == OfferIssueSources.Recovery
                )
            );
        }

        private static async Task<HttpResponseMessage[]> PostConcurrentAsync(
            WebApplicationFactory<Program> factory,
            (string Jwt, int FeedbackId) seeded,
            object payload,
            string path
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
                using var post = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"/api/feedback/{seeded.FeedbackId}/{path}"
                );
                post.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", seeded.Jwt);
                post.Content = JsonContent.Create(payload);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                return await client.SendAsync(post, cts.Token);
            }

            return await Task.WhenAll(
                PostOnceAsync(clientA),
                PostOnceAsync(clientB)
            );
        }

        private static async Task<(string Jwt, int FeedbackId)> SeedFeedbackAsync(
            IServiceProvider services,
            bool withCatalogAttach
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            var now = DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N");

            var user = new User
            {
                FullName = "Owner",
                Email = $"reply-sql-owner-{suffix}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900123",
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
                Name = $"Reply SQL {suffix}",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = now,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
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

            var contact = $"reply-sql-{suffix}@example.com";
            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = contact,
                NormalizedEmail = contact,
                CreatedAt = now,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Alex Guest",
                MarketingPreference = LocationGuestMarketingPreference.NotRecorded,
                CreatedAt = now,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var ledger = new LocationGuestPermissionLedgerService(context);
            ledger.RecordEvent(
                locationGuest.Id,
                location.Id,
                LocationGuestPermissionKind.FeedbackFollowUp,
                LocationGuestPermissionLedgerEventKinds.Grant,
                LocationGuestPermissionLedgerSources.GuestForm,
                now
            );
            await context.SaveChangesAsync();

            int? catalogOfferId = null;
            if (withCatalogAttach)
            {
                var catalogOffer = new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "20% off next visit",
                    Description = "Recovery offer",
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    DiscountPercentage = 20m,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                context.CatalogOffers.Add(catalogOffer);
                await context.SaveChangesAsync();
                catalogOfferId = catalogOffer.Id;
            }

            var qrCode = new QrCode
            {
                RestaurantLocationId = location.Id,
                QrType = QrType.SmartGuest,
                Token = $"rp{suffix}"[..32],
                Status = QrCodeStatus.Active,
                CreatedAt = now,
            };
            context.QrCodes.Add(qrCode);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                QrCodeId = qrCode.Id,
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest.Id,
                GuestName = "Alex Guest",
                GuestContact = contact,
                ContactType = ContactType.Email,
                Comment = "Great food",
                CreatedAt = now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
                RecoveryOfferId = catalogOfferId,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            return (jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role), feedback.Id);
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
