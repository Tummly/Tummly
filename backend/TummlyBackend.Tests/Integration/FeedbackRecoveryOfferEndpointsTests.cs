using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackRecoveryOfferEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackRecoveryOfferEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendAndIssue_PersistsDualFacts_KeepsInProgress_EmitsBothActivities()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-send-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "A recovery offer from us",
                body = "Please enjoy 20% off your next visit.",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "warm_and_apologetic",
                offer = new
                {
                    offerType = "percentage_discount",
                    title = "20% off",
                    description = "Thanks for your feedback — enjoy 20% off.",
                    validity = "30_days_after_issue",
                    discountPercentage = 20,
                    staffInstructions = "Redeem once at the till.",
                },
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "in_progress",
                postBody.GetProperty("workflowStatus").GetString()
            );

            Assert.Equal(
                "guest_response_sent",
                postBody.GetProperty("activityEvent").GetProperty("kind").GetString()
            );
            Assert.Equal(
                "recovery_offer_issued",
                postBody
                    .GetProperty("recoveryOfferActivityEvent")
                    .GetProperty("kind")
                    .GetString()
            );

            var code = postBody
                .GetProperty("recoveryOffer")
                .GetProperty("redemptionCode")
                .GetString();
            Assert.False(string.IsNullOrWhiteSpace(code));
            Assert.StartsWith("TUM-", code);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var feedback = await context.Feedbacks
                .AsNoTracking()
                .SingleAsync(f => f.Id == seeded.FeedbackId);
            Assert.Equal(
                FeedbackWorkflowStatus.InProgress,
                feedback.WorkflowStatus
            );

            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                FeedbackRecoveryIntent.RespondWithRecoveryOffer,
                guestResponse.Intent
            );

            var offer = await context.FeedbackRecoveryOffers
                .AsNoTracking()
                .SingleAsync(o => o.FeedbackId == seeded.FeedbackId);
            Assert.Equal("20% off", offer.Title);
            Assert.Equal(code, offer.RedemptionCode);
            Assert.Equal(20m, offer.DiscountPercentage);

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getBody = await ReadJsonAsync(await _client.SendAsync(get));
            var kinds = getBody
                .GetProperty("activityHistory")
                .EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToList();

            Assert.Contains("guest_response_sent", kinds);
            Assert.Contains("recovery_offer_issued", kinds);
        }

        [Fact]
        public async Task CompleteRecovery_OfferIntent_Resolves_AfterSendAndIssue()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-complete-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-complete-owner@example.com"
            );

            using var send = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            send.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            send.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Offer",
                body = "Body text",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "appreciative",
                offer = new
                {
                    offerType = "fixed_discount",
                    title = "£10 off",
                    description = "Ten pounds off next visit.",
                    validity = "14_days_after_issue",
                    discountAmount = 10,
                },
            });
            Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(send)).StatusCode);

            using var complete = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            complete.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            complete.Content = JsonContent.Create(new
            {
                intent = "respond_with_recovery_offer",
            });

            var completeResponse = await _client.SendAsync(complete);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
            var body = await ReadJsonAsync(completeResponse);
            Assert.Equal(
                "resolved",
                body.GetProperty("workflowStatus").GetString()
            );
            Assert.Equal(
                "respond_with_recovery_offer",
                body
                    .GetProperty("activityEvent")
                    .GetProperty("recoveryIntent")
                    .GetString()
            );
        }

        [Fact]
        public async Task SendAndIssue_Returns400_WhenGuestOffersOptOut()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-optout-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-optout-owner@example.com",
                offersOptOut: true
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Offer",
                body = "Body",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "appreciative",
                offer = new
                {
                    offerType = "percentage_discount",
                    title = "10% off",
                    description = "Desc",
                    validity = "7_days_after_issue",
                    discountPercentage = 10,
                },
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            ContactType contactType,
            string guestContact,
            FeedbackWorkflowStatus workflowStatus,
            string email = "recovery-offer-owner@example.com",
            bool offersOptOut = false
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Recovery Offer Owner",
                Email = email + "-" + linkToken,
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
                Name = "Recovery Offer Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
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

            LocationGuest? locationGuest = null;
            if (offersOptOut)
            {
                var masterGuest = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = guestContact.ToLowerInvariant(),
                    NormalizedEmail = guestContact.ToLowerInvariant(),
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(masterGuest);
                await context.SaveChangesAsync();

                locationGuest = new LocationGuest
                {
                    MasterGuestId = masterGuest.Id,
                    RestaurantLocationId = location.Id,
                    Name = "Alex Guest",
                    OffersOptOut = true,
                    CreatedAt = DateTime.UtcNow,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest?.Id,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Food was cold",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = workflowStatus,
                OffersOptOut = offersOptOut,
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, feedback.Id);
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(text).RootElement.Clone();
        }
    }
}
