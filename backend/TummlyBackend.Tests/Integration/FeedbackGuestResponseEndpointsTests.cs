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

namespace TummlyBackend.Tests.Integration
{
    public class FeedbackGuestResponseEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackGuestResponseEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendGuestResponse_PersistsFact_KeepsInProgress_EmitsActivity()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-send-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress
            );

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

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "in_progress",
                postBody.GetProperty("workflowStatus").GetString()
            );

            var activityEvent = postBody.GetProperty("activityEvent");
            Assert.Equal(
                "guest_response_sent",
                activityEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "email",
                activityEvent.GetProperty("channel").GetString()
            );
            Assert.Equal(
                "a••••@example.com",
                activityEvent.GetProperty("maskedDestination").GetString()
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
                FeedbackGuestResponseChannel.Email,
                guestResponse.Channel
            );
            Assert.Equal("Sorry about your visit", guestResponse.Subject);
            Assert.Equal("Thank you for telling us.", guestResponse.Body);

            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            Assert.False(
                await context.CreditLedgerEntries.AnyAsync(
                    row => row.RestaurantId == restaurantId
                )
            );

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getResponse = await _client.SendAsync(get);
            var getBody = await ReadJsonAsync(getResponse);
            var activity = getBody.GetProperty("activityHistory");
            Assert.Equal(2, activity.GetArrayLength());
            Assert.Equal(
                "guest_response_sent",
                activity[1].GetProperty("kind").GetString()
            );
        }

        [Fact]
        public async Task SendGuestResponse_Sms_OmitsSubject()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-tok",
                ContactType.Phone,
                "+447700900123",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-owner@example.com"
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

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.Equal(
                "••••0123",
                postBody
                    .GetProperty("activityEvent")
                    .GetProperty("maskedDestination")
                    .GetString()
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.False(string.IsNullOrWhiteSpace(guestResponse.BillingReservationRef));

            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            Assert.Equal(
                1,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                    && row.Channel == CreditChannels.Sms
                    && row.ReservationRef == guestResponse.BillingReservationRef
                )
            );
            Assert.False(
                await context.RecoverySmsSendIdempotencies.AnyAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.ReservationRef == guestResponse.BillingReservationRef
                    && row.CompletedGuestResponseId == null
                )
            );
        }

        [Fact]
        public async Task SendGuestResponse_Returns400_WhenEmailMissingSubject()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-no-subject-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "nosubject-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                body = "Hello",
                intent = "respond_to_guest",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.False(
                await context.FeedbackGuestResponses.AnyAsync(
                    r => r.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task SendGuestResponse_Returns409_WhenAlreadyResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-resolved-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.Resolved,
                email: "resolved-owner@example.com"
            );

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
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task SendGuestResponse_Returns400_WhenNoContact()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-no-contact-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.InProgress,
                email: "no-contact-owner@example.com"
            );

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
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SendGuestResponse_Returns403_ForNonOwner()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-cross-tenant-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "cross-tenant-owner@example.com"
            );
            var otherJwt = await SeedOtherOwnerJwtAsync(
                "guest-response-cross-tenant-other-tok"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", otherJwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry about your visit",
                body = "Thank you for telling us.",
                intent = "respond_to_guest",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SendGuestResponse_Sms_Returns503_WhenBillingReserveNotLive()
        {
            var client = CreateClientWithUnavailableRecoveryBilling();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-unavailable-tok",
                ContactType.Phone,
                "+447700900555",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-unavailable-owner@example.com"
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
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "billing_reserve_unavailable",
                body.GetProperty("code").GetString()
            );
        }

        [Fact]
        public async Task SendGuestResponse_Sms_Returns403_WhenRemainingZero()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-hard-stop-tok",
                ContactType.Phone,
                "+447700900777",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-hard-stop-owner@example.com",
                smsCredits: 0
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

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal("channel_hard_stopped", body.GetProperty("code").GetString());
            Assert.Equal(CreditChannels.Sms, body.GetProperty("channel").GetString());
        }

        [Fact]
        public async Task SendGuestResponse_Email_WritesNoLedgerRow()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-email-no-ledger-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "email-no-ledger-owner@example.com"
            );

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
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            Assert.False(
                await context.CreditLedgerEntries.AnyAsync(
                    row => row.RestaurantId == restaurantId
                )
            );
        }

        [Fact]
        public async Task SendGuestResponse_Sms_AcceptedLessThanReserved_ReleasesLeftoverHold()
        {
            // GSM estimate for 161 basic chars is 2 segments; delivery accepts 1.
            var body = new string('a', 161);
            Assert.Equal(2, CampaignSmsSegmentCalculator.CountSegments(body));

            await using var customized = CreateFactoryWithFixedAcceptedSmsSegments(
                acceptedSegments: 1
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-leftover-tok",
                ContactType.Phone,
                "+447700900777",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-leftover-owner@example.com",
                smsCredits: 10,
                services: customized.Services
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
                body,
                intent = "respond_to_guest",
            });

            var postResponse = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var snapshot = scope.ServiceProvider
                .GetRequiredService<ICreditBalanceSnapshot>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);

            Assert.Equal(
                1,
                await context.CreditLedgerEntries
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.EntryType == CreditLedgerEntryTypes.Consumption
                        && row.Channel == CreditChannels.Sms
                        && row.ReservationRef == guestResponse.BillingReservationRef
                    )
                    .SumAsync(row => row.Quantity)
            );
            Assert.Equal(
                1,
                await context.CreditLedgerEntries
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.EntryType == CreditLedgerEntryTypes.Release
                        && row.Channel == CreditChannels.Sms
                        && row.ReservationRef == guestResponse.BillingReservationRef
                    )
                    .SumAsync(row => row.Quantity)
            );

            var account = await snapshot.GetAccountAsync(restaurantId);
            var sms = Assert.Single(
                account!.Channels,
                row => row.Channel == CreditChannels.Sms
            );
            Assert.Equal(0, sms.Held);
            Assert.Equal(9, sms.Remaining);
        }

        [Fact]
        public async Task SendGuestResponse_Sms_SameIdempotencyKeyBeforeTtlDoesNotDoubleBurn()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-idempotency-tok",
                ContactType.Phone,
                "+447700900888",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-idempotency-owner@example.com",
                smsCredits: 5
            );
            const string idempotencyKey = "11111111-2222-3333-4444-555555555555";

            using var first = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            first.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            first.Headers.Add("Idempotency-Key", idempotencyKey);
            first.Content = JsonContent.Create(new
            {
                channel = "sms",
                body = "Thanks for your message.",
                intent = "respond_to_guest",
            });

            var firstResponse = await _client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            using var second = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            second.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            second.Headers.Add("Idempotency-Key", idempotencyKey);
            second.Content = JsonContent.Create(new
            {
                channel = "sms",
                body = "Thanks for your message.",
                intent = "respond_to_guest",
            });

            var secondResponse = await _client.SendAsync(second);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            Assert.Equal(
                1,
                await context.FeedbackGuestResponses.CountAsync(
                    r => r.FeedbackId == seeded.FeedbackId
                )
            );
            Assert.Equal(
                1,
                await context.CreditLedgerEntries.CountAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                    && row.Channel == CreditChannels.Sms
                )
            );
        }

        [Fact]
        public async Task CompleteRecovery_Returns409_WhenAlreadyResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-complete-resolved-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.Resolved,
                email: "complete-resolved-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "respond_to_guest",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CompleteRecovery_Resolves_EmitsRecoveryCompleted_NotBareStatusChange()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-complete-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "complete-owner@example.com"
            );

            using var send = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            send.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            send.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry",
                body = "Thanks",
                intent = "respond_to_guest",
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(send)).StatusCode
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "respond_to_guest",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.Equal(
                "resolved",
                postBody.GetProperty("workflowStatus").GetString()
            );
            Assert.Equal(
                "recovery_completed",
                postBody.GetProperty("activityEvent").GetProperty("kind").GetString()
            );
            Assert.Equal(
                "respond_to_guest",
                postBody
                    .GetProperty("activityEvent")
                    .GetProperty("recoveryIntent")
                    .GetString()
            );

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
            Assert.Contains("recovery_completed", kinds);
            Assert.DoesNotContain("workflow_status_changed", kinds);
            Assert.DoesNotContain("feedback_closed_out", kinds);
        }

        [Fact]
        public async Task CompleteRecovery_Returns400_WhenNoGuestResponse()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-complete-no-response-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "complete-noreq-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                intent = "respond_to_guest",
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
            string email = "guest-response-owner@example.com",
            int smsCredits = 20,
            IServiceProvider? services = null
        )
        {
            using var scope = (services ?? _factory.Services).CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Guest Response Owner",
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
                Name = "Guest Response Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
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
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Great food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = workflowStatus,
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            if (contactType == ContactType.Phone && smsCredits > 0)
            {
                context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = restaurant.Id,
                        Channel = CreditChannels.Sms,
                        EntryType = CreditLedgerEntryTypes.PilotAllocation,
                        Quantity = smsCredits,
                        PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, feedback.Id);
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
                FullName = "Other Guest Response Owner",
                Email = $"other-guest-response-owner-{linkToken}@example.com",
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
                Name = "Other Guest Response Venue",
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
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body;
        }

        private HttpClient CreateClientWithUnavailableRecoveryBilling()
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(IRecoverySmsBillingReserve)
                    );
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddScoped<
                        IRecoverySmsBillingReserve,
                        UnavailableRecoverySmsBillingReserve
                    >();
                });
            }).CreateClient();
        }

        private WebApplicationFactory<Program> CreateFactoryWithFixedAcceptedSmsSegments(
            int acceptedSegments
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services
                        .Where(d =>
                            d.ServiceType == typeof(IRecoveryGuestSmsDelivery)
                        )
                        .ToList();
                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IRecoveryGuestSmsDelivery>(
                        new FixedAcceptedRecoveryGuestSmsDelivery(acceptedSegments)
                    );
                });
            });
        }

        private sealed class FixedAcceptedRecoveryGuestSmsDelivery
            : IRecoveryGuestSmsDelivery
        {
            private readonly int _acceptedSegments;

            public FixedAcceptedRecoveryGuestSmsDelivery(int acceptedSegments)
            {
                _acceptedSegments = acceptedSegments;
            }

            public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
                string phoneNumber,
                string body,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<RecoveryGuestSmsDeliveryResult>(
                    new RecoveryGuestSmsDeliveryResult.Accepted
                    {
                        AcceptedSegments = _acceptedSegments,
                    }
                );
        }
    }
}
