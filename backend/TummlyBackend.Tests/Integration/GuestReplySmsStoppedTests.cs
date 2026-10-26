using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Integration
{
    /// <summary>
    /// Seam: guest-reply SMS paths refuse a guest whose MasterGuest.SmsStoppedAtUtc
    /// is set (R4.5). The refusal reserves no credits and saves no row. Email is allowed.
    /// </summary>
    public class GuestReplySmsStoppedTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string SmsStoppedMessage =
            "This message cannot be sent for the selected purpose or recipient. Review the reason.";

        private readonly TummlyWebApplicationFactory _factory;

        public GuestReplySmsStoppedTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ReplySms_StoppedGuest_409SmsStopped_NoReserve()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedFeedbackAsync(
                customized.Services,
                "sms-stopped-reply",
                ContactType.Phone,
                "+447911123480",
                stopped: true,
                withCatalogAttach: false
            );
            var creditRowsBefore = await CountCreditRowsAsync(
                customized.Services,
                seeded.RestaurantId
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "sms",
                body = "Thanks for your message.",
                intent = "respond_to_guest",
            });

            var response = await client.SendAsync(post);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("sms_stopped", body.GetProperty("code").GetString());
            Assert.Equal(SmsStoppedMessage, body.GetProperty("message").GetString());

            Assert.Empty(delivery.Calls);
            Assert.Equal(
                creditRowsBefore,
                await CountCreditRowsAsync(customized.Services, seeded.RestaurantId)
            );
            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.FeedbackGuestResponses.AnyAsync(r => r.FeedbackId == seeded.FeedbackId)
            );
            Assert.False(
                await context.RecoverySmsSendIdempotencies.AnyAsync(r => r.FeedbackId == seeded.FeedbackId)
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_StoppedGuest_409_NoOfferIssued()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedFeedbackAsync(
                customized.Services,
                "sms-stopped-offer",
                ContactType.Phone,
                "+447911123481",
                stopped: true,
                withCatalogAttach: true
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "sms",
                body = "Please enjoy 20% off your next visit.",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "warm_and_apologetic",
            });

            var response = await client.SendAsync(post);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("sms_stopped", body.GetProperty("code").GetString());
            Assert.Equal(SmsStoppedMessage, body.GetProperty("message").GetString());

            Assert.Empty(delivery.Calls);
            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.OfferIssues.AnyAsync(o => o.FeedbackId == seeded.FeedbackId)
            );
            Assert.False(
                await context.FeedbackGuestResponses.AnyAsync(r => r.FeedbackId == seeded.FeedbackId)
            );
        }

        [Fact]
        public async Task ReplyEmail_StoppedGuest_Allowed()
        {
            var seeded = await SeedFeedbackAsync(
                _factory.Services,
                "email-stopped-reply",
                ContactType.Email,
                "stopped-email@example.com",
                stopped: true,
                withCatalogAttach: false
            );
            var client = _factory.CreateClient();

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry about your visit",
                body = "Thank you for telling us.",
                intent = "respond_to_guest",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
            });

            var response = await client.SendAsync(post);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
        }

        private WebApplicationFactory<Program> CreateFactoryWithSmsDelivery(
            RecordingRecoveryGuestSmsDelivery delivery
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRecoveryGuestSmsDelivery>();
                    services.AddSingleton<IRecoveryGuestSmsDelivery>(delivery);
                });
            });
        }

        private static async Task<int> CountCreditRowsAsync(
            IServiceProvider services,
            int restaurantId
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await context.CreditLedgerEntries.CountAsync(e => e.RestaurantId == restaurantId);
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        private static async Task<SeededFeedback> SeedFeedbackAsync(
            IServiceProvider services,
            string linkToken,
            ContactType contactType,
            string guestContact,
            bool stopped,
            bool withCatalogAttach
        )
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            var now = DateTime.UtcNow;

            var user = new User
            {
                FullName = "Stopped Guest Owner",
                Email = $"stopped-owner-{linkToken}@example.com",
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
                Name = "Stopped Guest Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                EmailMarketingPermissionEnabled = true,
                SmsMarketingPermissionEnabled = true,
                FeedbackFollowUpPermissionEnabled = true,
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
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Sms,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 20,
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                    CreatedAtUtc = now,
                }
            );

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = now,
            };
            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var isEmail = contactType == ContactType.Email;
            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = isEmail ? guestContact : null,
                NormalizedEmail = isEmail ? guestContact : null,
                Mobile = isEmail ? null : guestContact,
                NormalizedPhone = isEmail
                    ? null
                    : new string(guestContact.Where(char.IsDigit).ToArray()),
                SmsStoppedAtUtc = stopped ? now : null,
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

            new LocationGuestPermissionLedgerService(context).RecordEvent(
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
                var offer = new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "20% off next visit",
                    Description = "Thanks for your feedback.",
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    DiscountPercentage = 20m,
                    StaffInstructions = "Redeem once at the till.",
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                context.CatalogOffers.Add(offer);
                await context.SaveChangesAsync();
                catalogOfferId = offer.Id;
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest.Id,
                RecoveryOfferId = catalogOfferId,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Food was cold",
                CreatedAt = now,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = FeedbackWorkflowStatus.InProgress,
            };
            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(user.Id.ToString(), user.Email, user.Role);
            return new SeededFeedback(jwt, restaurant.Id, feedback.Id);
        }

        private sealed record SeededFeedback(string Jwt, int RestaurantId, int FeedbackId);
    }
}
