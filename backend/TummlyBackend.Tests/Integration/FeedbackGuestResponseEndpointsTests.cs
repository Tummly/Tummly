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
using TummlyBackend.DTOs.Feedback;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

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
        public async Task FeedbackDetail_GuestResponseEvent_HasDeliveryState()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-delivery-state-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress
            );

            var deliveryStateAt = new DateTime(2026, 10, 26, 9, 30, 0, DateTimeKind.Utc);
            int guestResponseId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var row = new FeedbackGuestResponse
                {
                    FeedbackId = seeded.FeedbackId,
                    Channel = FeedbackGuestResponseChannel.Email,
                    Intent = FeedbackRecoveryIntent.RespondToGuest,
                    MaskedDestination = "a••••@example.com",
                    Subject = "Sorry about your visit",
                    Body = "Thank you for telling us.",
                    AuthorDisplayName = "Guest Response Owner",
                    CreatedAt = DateTime.UtcNow,
                    DeliveryState = GuestReplyDeliveryState.Delivered,
                    DeliveryStateAtUtc = deliveryStateAt,
                };
                context.FeedbackGuestResponses.Add(row);
                await context.SaveChangesAsync();
                guestResponseId = row.Id;
            }

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/feedback/{seeded.FeedbackId}"
            );
            get.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);

            var getBody = await ReadJsonAsync(await _client.SendAsync(get));
            var activity = getBody
                .GetProperty("activityHistory")
                .EnumerateArray()
                .Single(e =>
                    e.GetProperty("kind").GetString() == "guest_response_sent"
                );

            Assert.Equal(guestResponseId, activity.GetProperty("guestResponseId").GetInt32());
            Assert.Equal("delivered", activity.GetProperty("deliveryState").GetString());
            Assert.Equal(
                deliveryStateAt,
                activity.GetProperty("deliveryStateAt").GetDateTime()
            );
        }

        [Fact]
        public async Task SendGuestResponse_Sms_OmitsSubject()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-sms-tok",
                ContactType.Phone,
                "+447911123456",
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
                "••••3456",
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
                "+447911123455",
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
                "+447911123457",
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
        public async Task SendGuestResponse_Email_Succeeds_WithoutMarketingOptIn()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-email-no-marketing-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "email-no-marketing-owner@example.com",
                followUpGranted: true,
                marketing: null
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
            Assert.Equal(
                0,
                await context.CreditLedgerEntries.CountAsync(
                    row =>
                        row.RestaurantId == restaurantId
                        && row.Channel == CreditChannels.Email
                )
            );
        }

        [Theory]
        [InlineData("email", "alex@example.com", ContactType.Email)]
        [InlineData("sms", "+447911123999", ContactType.Phone)]
        public async Task SendGuestResponse_Returns400_WhenFollowUpWithdrawn(
            string channel,
            string guestContact,
            ContactType contactType
        )
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                $"guest-response-followup-withdrawn-{channel}-tok",
                contactType,
                guestContact,
                FeedbackWorkflowStatus.InProgress,
                email: $"followup-withdrawn-{channel}-owner@example.com",
                followUpGranted: false,
                smsCredits: contactType == ContactType.Phone ? 20 : 0
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = channel == "email"
                ? JsonContent.Create(new
                {
                    channel,
                    subject = "Sorry about your visit",
                    body = "Thank you for telling us.",
                    intent = "respond_to_guest",
                })
                : JsonContent.Create(new
                {
                    channel,
                    body = "Thanks for your message.",
                    intent = "respond_to_guest",
                });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                ServiceReplyEligibility.NotAllowedMessage,
                body.GetProperty("message").GetString()
            );
        }

        [Theory]
        [InlineData("include_a_recovery_offer")]
        [InlineData("free_dessert_promotion")]
        [InlineData("Acknowledge_Feedback")]
        public async Task SendGuestResponse_Returns400_WhenPurposeNotAllowed(string purpose)
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                $"guest-response-purpose-refused-{purpose.ToLowerInvariant().Replace('_', '-')}-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: $"purpose-refused-{purpose.ToLowerInvariant().Replace('_', '-')}-owner@example.com"
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
                purpose,
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                FeedbackGuestResponsePurposes.ErrorCode,
                body.GetProperty("code").GetString()
            );
        }

        [Theory]
        [InlineData("acknowledge_feedback")]
        [InlineData("apologise_and_confirm_follow_up")]
        [InlineData("ask_for_more_information")]
        [InlineData("confirm_operational_action")]
        [InlineData("create_custom_response")]
        public async Task SendGuestResponse_AllowsEveryServicePurpose(string purpose)
        {
            var label = purpose.Replace('_', '-');
            var seeded = await SeedOwnerWithFeedbackAsync(
                $"guest-response-purpose-ok-{label}-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: $"purpose-ok-{label}-owner@example.com"
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
                purpose,
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SendGuestResponse_Email_AllowedWhenEmailMarketingUnsubscribed()
        {
            // Marketing unsubscribe and genuine service email are separate purposes.
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-email-unsubscribed-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "email-unsubscribed-owner@example.com"
            );
            await StageEmailSuppressionAsync(
                seeded.LocationId,
                "email:alex@example.com",
                LocationGuestPermissionKind.EmailMarketing,
                GuestContactSuppressionSources.EmailUnsubscribe
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
        }

        [Fact]
        public async Task SendGuestResponse_Returns400_WhenEmailHardBounced_WithSafeMessage()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-email-bounced-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "email-bounced-owner@example.com"
            );
            await StageEmailSuppressionAsync(
                seeded.LocationId,
                "email:alex@example.com",
                LocationGuestPermissionKind.EmailDelivery,
                GuestContactSuppressionSources.EmailBounce
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

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                ServiceReplyEligibility.EmailDeliveryUnavailableMessage,
                body.GetProperty("message").GetString()
            );
        }

        private async Task StageEmailSuppressionAsync(
            int locationId,
            string contactKey,
            LocationGuestPermissionKind kind,
            string source
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var suppressions = scope.ServiceProvider
                .GetRequiredService<IGuestContactSuppressionService>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            await suppressions.StageAsync(
                restaurantId,
                contactKey,
                kind,
                DateTime.UtcNow,
                source
            );
            await context.SaveChangesAsync();
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
                "+447911123457",
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
        public async Task ReplySms_Accepted_StoresSidAndAccepted()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-sid-tok",
                ContactType.Phone,
                "+447911123462",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-sid-owner@example.com"
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

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.StartsWith("SM-test-", guestResponse.ProviderMessageId);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                guestResponse.DeliveryState
            );
            Assert.NotNull(guestResponse.DeliveryStateAtUtc);
        }

        [Fact]
        public async Task ReplySms_PrefixCrossesSegment_ReservesTwo()
        {
            // 150 characters is one segment. The restaurant prefix makes the sent text
            // 172 characters, so the reserve must be two segments.
            var body = new string('a', 150);
            var delivery = new RecordingRecoveryGuestSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = 1,
                    ProviderMessageId = "SMprefix-1",
                    StatusCallbackSet = true,
                }
            );
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-prefix-tok",
                ContactType.Phone,
                "+447911123496",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-prefix-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var sent = Assert.Single(delivery.Calls);
            Assert.Equal("Guest Response Venue: " + body, sent.Body);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);

            // Reserved 2 segments, accepted 1: one consumed, one released.
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
        }

        [Fact]
        public async Task ReplySms_DuplicateRuleStillComparesOperatorBody()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-dup-prefix-tok",
                ContactType.Phone,
                "+447911123497",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-dup-prefix-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var first = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            var secondBody = await ReadJsonAsync(second);
            Assert.Equal("duplicate_reply", secondBody.GetProperty("code").GetString());
            Assert.Single(delivery.Calls);
        }

        [Fact]
        public async Task ReplySms_NoCallbackConfigured_Unavailable()
        {
            await using var customized = CreateFactoryWithScriptedSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Accepted
                {
                    AcceptedSegments = 1,
                    ProviderMessageId = "SMnocallback0000000000000000000001",
                    StatusCallbackSet = false,
                }
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-nocallback-tok",
                ContactType.Phone,
                "+447911123463",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-nocallback-owner@example.com",
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
                body = "Thanks for your message.",
                intent = "respond_to_guest",
            });

            var postResponse = await client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(
                "SMnocallback0000000000000000000001",
                guestResponse.ProviderMessageId
            );
            Assert.Equal(
                GuestReplyDeliveryState.Unavailable,
                guestResponse.DeliveryState
            );
        }

        [Fact]
        public async Task ReplySms_ProviderFailed_StoresFailedReply()
        {
            // The reply row is saved before the provider call. A failed send
            // moves that row to Failed.
            await using var customized = CreateFactoryWithScriptedSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Unable to send Recovery SMS.",
                }
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-failed-tok",
                ContactType.Phone,
                "+447911123464",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-failed-owner@example.com",
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
                body = "Thanks for your message.",
                intent = "respond_to_guest",
            });

            var postResponse = await client.SendAsync(post);
            Assert.NotEqual(HttpStatusCode.OK, postResponse.StatusCode);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var failedReply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Failed, failedReply.DeliveryState);
        }

        [Fact]
        public async Task ReplySms_ProviderFailed_Returns502SmsSendFailed()
        {
            await using var customized = CreateFactoryWithSmsDelivery(
                new RecordingRecoveryGuestSmsDelivery(
                    new RecoveryGuestSmsDeliveryResult.Failed
                    {
                        Message = "Provider down.",
                    }
                )
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-502-tok",
                ContactType.Phone,
                "+447911123490",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-502-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("sms_send_failed", body.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task ReplySms_AfterProviderFailure_IdenticalRetryAllowed()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Provider down.",
                }
            );
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-identical-retry-tok",
                ContactType.Phone,
                "+447911123491",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-identical-retry-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            const string body = "Thanks for your message.";
            var failed = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);

            var retry = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(2, delivery.Calls.Count);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var replies = await context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(r => r.FeedbackId == seeded.FeedbackId)
                .ToListAsync();
            Assert.Equal(2, replies.Count);
            Assert.Contains(replies, r => r.DeliveryState == GuestReplyDeliveryState.Failed);
            Assert.Contains(replies, r => r.DeliveryState == GuestReplyDeliveryState.Accepted);

            var snapshot = scope.ServiceProvider
                .GetRequiredService<ICreditBalanceSnapshot>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            var account = await snapshot.GetAccountAsync(restaurantId);
            var sms = Assert.Single(
                account!.Channels,
                row => row.Channel == CreditChannels.Sms
            );
            Assert.Equal(0, sms.Held);
            Assert.Equal(9, sms.Remaining);
        }

        [Fact]
        public async Task ReplySms_SettleFailsAfterAccept_ReturnsSuccess_ReplySaved()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                settleFails: true
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-settle-fail-tok",
                ContactType.Phone,
                "+447911123492",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-settle-fail-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var reply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Accepted, reply.DeliveryState);
            Assert.StartsWith("SMrecorded-", reply.ProviderMessageId);

            // Settle failed, so the hold stays open for the sweeper.
            Assert.Equal(
                1,
                await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                    row.FeedbackId == seeded.FeedbackId
                    && row.CompletedGuestResponseId == null
                )
            );
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            var account = await scope.ServiceProvider
                .GetRequiredService<ICreditBalanceSnapshot>()
                .GetAccountAsync(restaurantId);
            var sms = Assert.Single(
                account!.Channels,
                row => row.Channel == CreditChannels.Sms
            );
            Assert.Equal(1, sms.Held);
            Assert.Equal(9, sms.Remaining);
        }

        [Fact]
        public async Task ReplySms_ProviderThrows_ReplyUnavailable_Returns502_IdenticalRetryBlocked()
        {
            var delivery = new ThrowingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-provider-throws-tok",
                ContactType.Phone,
                "+447911123493",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-provider-throws-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            const string body = "Thanks for your message.";
            var first = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);
            var firstBody = await ReadJsonAsync(first);
            Assert.Equal("sms_send_failed", firstBody.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                firstBody.GetProperty("message").GetString()
            );

            var retry = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
            var retryBody = await ReadJsonAsync(retry);
            Assert.Equal("duplicate_reply", retryBody.GetProperty("code").GetString());
            Assert.Equal(1, delivery.CallCount);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var reply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Unavailable, reply.DeliveryState);
            Assert.Null(reply.ProviderMessageId);
            Assert.Equal(
                1,
                await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                    row.FeedbackId == seeded.FeedbackId
                    && row.CompletedGuestResponseId == null
                )
            );
        }

        [Fact]
        public async Task ReplySms_CompleteFailsAfterAccept_ReturnsSuccess()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailComplete = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-complete-fail-tok",
                ContactType.Phone,
                "+447911123500",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-complete-fail-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var reply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Accepted, reply.DeliveryState);
            Assert.StartsWith("SMrecorded-", reply.ProviderMessageId);
        }

        [Fact]
        public async Task ReplySms_FinalSaveFailsAfterAccept_Returns502NotUnauthorized()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-final-save-fail-tok",
                ContactType.Phone,
                "+447911123501",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-final-save-fail-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("sms_send_failed", body.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                body.GetProperty("message").GetString()
            );
            Assert.Single(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            // The reply row was saved before the provider call and stays Pending.
            var pendingReply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Pending, pendingReply.DeliveryState);
            Assert.Null(pendingReply.ProviderMessageId);
        }

        [Fact]
        public async Task ReplySms_FinalSaveFailsAfterAccept_IdenticalRetryBlocked()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-final-save-retry-tok",
                ContactType.Phone,
                "+447911123511",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-final-save-retry-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            const string body = "Thanks for your message.";
            var first = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);

            // The save works again. The identical retry must not send a second text.
            faults.FailSave = false;
            var retry = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
            var retryBody = await ReadJsonAsync(retry);
            Assert.Equal("duplicate_reply", retryBody.GetProperty("code").GetString());
            Assert.Single(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var reply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Pending, reply.DeliveryState);
            Assert.Null(reply.ProviderMessageId);
        }

        [Fact]
        public async Task ReplySms_ProviderFailed_RowFailed_RetryAllowed()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Provider down.",
                }
            );
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-failed-row-tok",
                ContactType.Phone,
                "+447911123512",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-failed-row-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            const string body = "Thanks for your message.";
            var failed = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);

            using (var scope = customized.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var reply = await context.FeedbackGuestResponses
                    .AsNoTracking()
                    .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
                Assert.Equal(GuestReplyDeliveryState.Failed, reply.DeliveryState);
                Assert.Null(reply.ProviderMessageId);
                Assert.Equal(
                    0,
                    await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                        row.FeedbackId == seeded.FeedbackId
                        && row.CompletedGuestResponseId == null
                    )
                );
            }

            var retry = await PostSmsReplyAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(2, delivery.Calls.Count);

            using var scope2 = customized.Services.CreateScope();
            var context2 = scope2.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var states = await context2.FeedbackGuestResponses
                .AsNoTracking()
                .Where(r => r.FeedbackId == seeded.FeedbackId)
                .Select(r => r.DeliveryState)
                .ToListAsync();
            Assert.Equal(2, states.Count);
            Assert.Contains(GuestReplyDeliveryState.Failed, states);
            Assert.Contains(GuestReplyDeliveryState.Accepted, states);
        }

        [Fact]
        public async Task ReplySms_ProviderFailed_FailedSaveThrows_Returns502()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Provider down.",
                }
            );
            var faults = new SmsStepFaults { FailFailedSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-failed-save-throws-tok",
                ContactType.Phone,
                "+447911123515",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-failed-save-throws-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("sms_send_failed", body.GetProperty("code").GetString());
            Assert.Single(delivery.Calls);
        }

        [Fact]
        public async Task ReplySms_PendingSaveFails_ReleasesHold_Returns502()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailPendingSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-pending-save-fails-tok",
                ContactType.Phone,
                "+447911123516",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-pending-save-fails-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            var response = await PostSmsReplyAsync(
                client,
                seeded,
                "Thanks for your message."
            );
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("sms_send_failed", body.GetProperty("code").GetString());
            Assert.Empty(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.FeedbackGuestResponses.CountAsync(r =>
                    r.FeedbackId == seeded.FeedbackId
                )
            );
            Assert.Equal(
                0,
                await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                    row.FeedbackId == seeded.FeedbackId
                )
            );

            var snapshot = scope.ServiceProvider
                .GetRequiredService<ICreditBalanceSnapshot>();
            var restaurantId = await context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == seeded.LocationId)
                .Select(row => row.RestaurantId)
                .SingleAsync();
            var account = await snapshot.GetAccountAsync(restaurantId);
            var sms = Assert.Single(
                account!.Channels,
                row => row.Channel == CreditChannels.Sms
            );
            Assert.Equal(0, sms.Held);
            Assert.Equal(10, sms.Remaining);
        }

        [Fact]
        public async Task ReplySms_RequestCancelledAfterAccept_ReplyAcceptedWithSid_Charged()
        {
            // The controller passes no request token today. This test calls the
            // service with a token that is cancelled after the provider accepts, so
            // a later change that passes RequestAborted cannot lose an accepted send.
            var delivery = new AbortingAfterAcceptRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            using var cts = new CancellationTokenSource();
            delivery.OnAccepted = cts.Cancel;
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-reply-sms-cancelled-tok",
                ContactType.Phone,
                "+447911123517",
                FeedbackWorkflowStatus.InProgress,
                email: "sms-cancelled-owner@example.com",
                smsCredits: 10,
                services: customized.Services
            );

            using (var sendScope = customized.Services.CreateScope())
            {
                var sendContext = sendScope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurantOwnerId = await sendContext.RestaurantLocations
                    .AsNoTracking()
                    .Where(row => row.Id == seeded.LocationId)
                    .Select(row => row.Restaurant!.OwnerUserId)
                    .SingleAsync();

                var service = sendScope.ServiceProvider
                    .GetRequiredService<IFeedbackGuestResponsesService>();
                var result = await service.SendAsync(
                    seeded.FeedbackId,
                    restaurantOwnerId,
                    FeedbackGuestResponseChannel.Sms,
                    FeedbackRecoveryIntent.RespondToGuest,
                    null,
                    "Thanks for your message.",
                    null,
                    null,
                    null,
                    null,
                    cts.Token
                );
                Assert.NotNull(result);
            }

            Assert.Equal(1, delivery.CallCount);
            Assert.True(delivery.TokenCancelledAfterAccept);
            using var pollScope = customized.Services.CreateScope();
            var pollContext = pollScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var reply = await pollContext.FeedbackGuestResponses
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.NotNull(reply);
            Assert.Equal(GuestReplyDeliveryState.Accepted, reply!.DeliveryState);
            Assert.StartsWith("SMaborted-", reply.ProviderMessageId);

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
                "+447911123458",
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

        [Fact]
        public async Task GuestResponse_IdenticalRepeatWithin10Min_Returns409DuplicateReply()
        {
            var notifier = new CountingGuestResponseEmailDeliveryWork();
            await using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IGuestResponseEmailDeliveryWork>();
                    services.AddSingleton<IGuestResponseEmailDeliveryWork>(notifier);
                })
            );
            using var client = factory.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-duplicate-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "duplicate-owner@example.com",
                services: factory.Services
            );

            var first = await SendGuestReplyAsync(
                client,
                seeded,
                "Sorry about your visit",
                "Thank you for telling us."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await SendGuestReplyAsync(
                client,
                seeded,
                "Sorry about your visit",
                "Thank you for telling us."
            );
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

            var secondBody = await ReadJsonAsync(second);
            Assert.False(secondBody.GetProperty("success").GetBoolean());
            Assert.Equal("duplicate_reply", secondBody.GetProperty("code").GetString());
            Assert.Equal(
                "This reply has already been submitted. Check its delivery status.",
                secondBody.GetProperty("message").GetString()
            );

            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.FeedbackGuestResponses.CountAsync(r =>
                    r.FeedbackId == seeded.FeedbackId
                )
            );
            Assert.Equal(1, notifier.Notified);
        }

        [Fact]
        public async Task GuestResponse_SameTextWithExtraSpaces_Returns409()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-spaces-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "spaces-owner@example.com"
            );

            var first = await SendGuestReplyAsync(
                _client,
                seeded,
                "Sorry about your visit",
                "Thank you for telling us."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await SendGuestReplyAsync(
                _client,
                seeded,
                "Sorry about your visit",
                "  Thank   you\n for   telling us.  "
            );
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            var body = await ReadJsonAsync(second);
            Assert.Equal("duplicate_reply", body.GetProperty("code").GetString());
        }

        [Fact]
        public async Task GuestResponse_DifferentText_Allowed()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-different-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "different-owner@example.com"
            );

            var first = await SendGuestReplyAsync(
                _client,
                seeded,
                "Sorry about your visit",
                "Thank you for telling us."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await SendGuestReplyAsync(
                _client,
                seeded,
                "Sorry about your visit",
                "We have passed your comments to the kitchen."
            );
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                2,
                await context.FeedbackGuestResponses.CountAsync(r =>
                    r.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task GuestResponse_SameTextAfter10Min_Allowed()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-aged-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "aged-owner@example.com"
            );

            var first = await SendGuestReplyAsync(
                _client,
                seeded,
                "Sorry about your visit",
                "Thank you for telling us."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var row = await context.FeedbackGuestResponses.SingleAsync(r =>
                    r.FeedbackId == seeded.FeedbackId
                );
                row.CreatedAt = DateTime.UtcNow.AddMinutes(-11);
                await context.SaveChangesAsync();
            }

            var second = await SendGuestReplyAsync(
                _client,
                seeded,
                "Sorry about your visit",
                "Thank you for telling us."
            );
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        }

        [Fact]
        public async Task GuestResponse_LockTimeout_Returns503()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "guest-response-lock-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "lock-owner@example.com"
            );

            _factory.ReplyLock.RejectAll = true;
            try
            {
                var response = await SendGuestReplyAsync(
                    _client,
                    seeded,
                    "Sorry about your visit",
                    "Thank you for telling us."
                );
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

                var body = await ReadJsonAsync(response);
                Assert.False(body.GetProperty("success").GetBoolean());
                Assert.Equal("Please try again.", body.GetProperty("message").GetString());
            }
            finally
            {
                _factory.ReplyLock.RejectAll = false;
            }
        }

        private static async Task<HttpResponseMessage> SendGuestReplyAsync(
            HttpClient client,
            (string Jwt, int LocationId, int FeedbackId) seeded,
            string subject,
            string body
        )
        {
            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/guest-responses"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject,
                body,
                intent = "respond_to_guest",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
            });

            return await client.SendAsync(post);
        }

        private sealed class CountingGuestResponseEmailDeliveryWork
            : IGuestResponseEmailDeliveryWork
        {
            public int Notified { get; private set; }

            public ValueTask NotifyAsync(
                int guestResponseId,
                CancellationToken cancellationToken = default
            )
            {
                Notified += 1;
                return ValueTask.CompletedTask;
            }

            public Task RunAsync(CancellationToken stoppingToken) =>
                Task.CompletedTask;

            public Task DrainAsync(
                CancellationToken cancellationToken = default
            ) => Task.CompletedTask;
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
            IServiceProvider? services = null,
            bool followUpGranted = true,
            LocationGuestPermissionState? marketing = null
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

            var master = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = contactType == ContactType.Email
                    ? guestContact.Trim().ToLowerInvariant()
                    : null,
                NormalizedEmail = contactType == ContactType.Email
                    ? guestContact.Trim().ToLowerInvariant()
                    : null,
                Mobile = contactType == ContactType.Phone ? guestContact : null,
                NormalizedPhone = contactType == ContactType.Phone
                    ? new string(guestContact.Where(char.IsDigit).ToArray())
                    : null,
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.Add(master);
            await context.SaveChangesAsync();

            var locationGuest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = location.Id,
                Name = "Alex Guest",
                MarketingPreference = LocationGuestMarketingPreference.NotRecorded,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.Add(locationGuest);
            await context.SaveChangesAsync();

            var ledger = new LocationGuestPermissionLedgerService(context);
            var at = DateTime.UtcNow;
            ledger.RecordEvent(
                locationGuest.Id,
                location.Id,
                LocationGuestPermissionKind.FeedbackFollowUp,
                followUpGranted
                    ? LocationGuestPermissionLedgerEventKinds.Grant
                    : LocationGuestPermissionLedgerEventKinds.Withdraw,
                LocationGuestPermissionLedgerSources.GuestForm,
                at
            );

            if (marketing is { } marketingState)
            {
                var marketingKind = contactType == ContactType.Email
                    ? LocationGuestPermissionKind.EmailMarketing
                    : LocationGuestPermissionKind.SmsMarketing;
                ledger.RecordEvent(
                    locationGuest.Id,
                    location.Id,
                    marketingKind,
                    marketingState == LocationGuestPermissionState.Granted
                        ? LocationGuestPermissionLedgerEventKinds.Grant
                        : LocationGuestPermissionLedgerEventKinds.Withdraw,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    at
                );
            }

            await context.SaveChangesAsync();

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest.Id,
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

        private async Task<HttpResponseMessage> PostSmsReplyAsync(
            HttpClient client,
            (string Jwt, int LocationId, int FeedbackId) seeded,
            string body
        )
        {
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

            return await client.SendAsync(post);
        }

        private WebApplicationFactory<Program> CreateFactoryWithSmsDelivery(
            IRecoveryGuestSmsDelivery delivery,
            bool settleFails = false,
            SmsStepFaults? faults = null
        )
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    foreach (
                        var descriptor in services
                            .Where(d => d.ServiceType == typeof(IRecoveryGuestSmsDelivery))
                            .ToList()
                    )
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<IRecoveryGuestSmsDelivery>(delivery);
                    if (faults != null)
                    {
                        RecoverySmsTestServices.UseFaultySmsStep(services, faults);
                    }
                    if (settleFails)
                    {
                        ReplaceWithSettleFailingBilling(services);
                    }
                });
            });
        }

        private static void ReplaceWithSettleFailingBilling(
            IServiceCollection services
        )
        {
            foreach (
                var descriptor in services
                    .Where(d => d.ServiceType == typeof(IRecoverySmsBillingReserve))
                    .ToList()
            )
            {
                services.Remove(descriptor);
            }

            services.AddScoped<IRecoverySmsBillingReserve>(sp =>
                new SettleFailingRecoverySmsBilling(
                    new LiveRecoverySmsBillingReserve(
                        sp.GetRequiredService<ApplicationDbContext>(),
                        sp.GetRequiredService<ICreditLedger>(),
                        sp.GetRequiredService<ICreditBalanceSnapshot>()
                    )
                )
            );
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

        private WebApplicationFactory<Program> CreateFactoryWithScriptedSmsDelivery(
            RecoveryGuestSmsDeliveryResult result
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
                        new ScriptedRecoveryGuestSmsDelivery(result)
                    );
                });
            });
        }

        private sealed class ScriptedRecoveryGuestSmsDelivery
            : IRecoveryGuestSmsDelivery
        {
            private readonly RecoveryGuestSmsDeliveryResult _result;

            public ScriptedRecoveryGuestSmsDelivery(RecoveryGuestSmsDeliveryResult result)
            {
                _result = result;
            }

            public Task<RecoveryGuestSmsDeliveryResult> SendAsync(
                string phoneNumber,
                string body,
                CancellationToken cancellationToken = default
            ) => Task.FromResult(_result);
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
