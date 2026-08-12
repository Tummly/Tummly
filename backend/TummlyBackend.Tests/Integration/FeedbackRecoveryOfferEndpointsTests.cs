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
        public async Task SendAndIssue_CreatesCatalogOfferIssue_KeepsInProgress_NoOneOffRow()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-send-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
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
                channel = "email",
                subject = "A recovery offer from us",
                body = "Please enjoy 20% off your next visit.",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "warm_and_apologetic",
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
            Assert.Equal(
                "20% off next visit",
                postBody.GetProperty("recoveryOffer").GetProperty("title").GetString()
            );

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

            Assert.Equal(
                0,
                await context.FeedbackRecoveryOffers
                    .AsNoTracking()
                    .CountAsync(o => o.FeedbackId == seeded.FeedbackId)
            );

            var issue = await context.OfferIssues
                .AsNoTracking()
                .SingleAsync(o => o.FeedbackId == seeded.FeedbackId);
            Assert.Equal(OfferIssueSources.Recovery, issue.Source);
            Assert.Equal(code, issue.ClaimCode);
            Assert.Equal(seeded.CatalogOfferId, issue.CatalogOfferId);

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
            // Ticket 06 owns GET activityHistory rewrite for Offer issue beats.
        }

        [Fact]
        public async Task CompleteRecovery_OfferIntent_Resolves_AfterSendAndIssue()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-complete-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-complete-owner@example.com",
                withCatalogAttach: true
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
        public async Task SendAndIssue_Returns409_WhenAlreadyResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-resolved-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.Resolved,
                email: "offer-resolved-owner@example.com",
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
                channel = "email",
                subject = "Offer",
                body = "Body",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "appreciative",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task SendAndIssue_Returns400_WhenNoContact()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-no-contact-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-no-contact-owner@example.com",
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
                channel = "email",
                subject = "Offer",
                body = "Body",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "appreciative",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SendAndIssue_Returns400_WhenNoCatalogAttach()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-no-attach-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-no-attach-owner@example.com",
                withCatalogAttach: false
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
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SendAndIssue_Returns403_ForNonOwner()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-cross-tenant-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-cross-tenant-owner@example.com",
                withCatalogAttach: true
            );
            var otherJwt = await SeedOtherOwnerJwtAsync(
                "recovery-offer-cross-tenant-other-tok"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", otherJwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Offer",
                body = "Body",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "appreciative",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
                offersOptOut: true,
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
                channel = "email",
                subject = "Offer",
                body = "Body",
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "appreciative",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int FeedbackId,
            int? CatalogOfferId
        )> SeedOwnerWithFeedbackAsync(
            string linkToken,
            ContactType contactType,
            string guestContact,
            FeedbackWorkflowStatus workflowStatus,
            string email = "recovery-offer-owner@example.com",
            bool offersOptOut = false,
            bool withCatalogAttach = false
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
            int? catalogOfferId = null;

            if (withCatalogAttach || offersOptOut)
            {
                var masterGuest = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Email = string.IsNullOrWhiteSpace(guestContact)
                        ? null
                        : guestContact.ToLowerInvariant(),
                    NormalizedEmail = string.IsNullOrWhiteSpace(guestContact)
                        ? null
                        : guestContact.ToLowerInvariant(),
                    CreatedAt = DateTime.UtcNow,
                };
                context.MasterGuests.Add(masterGuest);
                await context.SaveChangesAsync();

                locationGuest = new LocationGuest
                {
                    MasterGuestId = masterGuest.Id,
                    RestaurantLocationId = location.Id,
                    Name = "Alex Guest",
                    OffersOptOut = offersOptOut,
                    CreatedAt = DateTime.UtcNow,
                };
                context.LocationGuests.Add(locationGuest);
                await context.SaveChangesAsync();
            }

            if (withCatalogAttach)
            {
                var catalogOffer = new CatalogOffer
                {
                    RestaurantLocationId = location.Id,
                    Status = "active",
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "20% off next visit",
                    Description = "Thanks for your feedback — enjoy 20% off.",
                    Validity = CatalogOfferValidity.Days30AfterIssue,
                    DiscountPercentage = 20m,
                    StaffInstructions = "Redeem once at the till.",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                };
                context.CatalogOffers.Add(catalogOffer);
                await context.SaveChangesAsync();
                catalogOfferId = catalogOffer.Id;
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest?.Id,
                RecoveryOfferId = catalogOfferId,
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

            return (jwt, location.Id, feedback.Id, catalogOfferId);
        }

        private async Task<string> SeedOtherOwnerJwtAsync(string linkToken)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Other Recovery Offer Owner",
                Email = $"other-recovery-offer-owner-{linkToken}@example.com",
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
                Name = "Other Recovery Offer Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            return jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
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
