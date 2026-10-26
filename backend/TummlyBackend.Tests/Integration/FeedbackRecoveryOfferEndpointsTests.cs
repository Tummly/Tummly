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
using TummlyBackend.Tests.Helpers;

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
            Assert.Contains("recovery_offer_issued", kinds);

            var offerIssued = getBody
                .GetProperty("activityHistory")
                .EnumerateArray()
                .Single(e => e.GetProperty("kind").GetString() == "recovery_offer_issued");
            Assert.Equal(code, offerIssued.GetProperty("redemptionCode").GetString());
            Assert.Equal(
                "20% off next visit",
                offerIssued.GetProperty("offerTitle").GetString()
            );
            Assert.Equal(
                "not_redeemed",
                offerIssued.GetProperty("redemptionStatus").GetString()
            );
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
                withCatalogAttach: false,
                withLocationGuest: true,
                followUpGranted: true
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

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "A Recovery catalog offer attach is required before send.",
                body.GetProperty("message").GetString()
            );
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

        [Theory]
        [InlineData("email", "alex@example.com", ContactType.Email)]
        [InlineData("sms", "+447911123460", ContactType.Phone)]
        public async Task SendAndIssue_Succeeds_WhenMarketingWithdrawnButFollowUpGranted(
            string channel,
            string guestContact,
            ContactType contactType
        )
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                $"recovery-offer-marketing-withdrawn-{channel}-tok",
                contactType,
                guestContact,
                FeedbackWorkflowStatus.InProgress,
                email: $"offer-marketing-withdrawn-{channel}-owner@example.com",
                withCatalogAttach: true,
                followUpGranted: true,
                marketing: LocationGuestPermissionState.Withdrawn
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = channel == "email"
                ? JsonContent.Create(new
                {
                    channel,
                    subject = "Offer",
                    body = "Body",
                    intent = "respond_with_recovery_offer",
                    purpose = "include_a_recovery_offer",
                    tone = "appreciative",
                })
                : JsonContent.Create(new
                {
                    channel,
                    body = "Body",
                    intent = "respond_with_recovery_offer",
                    purpose = "include_a_recovery_offer",
                    tone = "appreciative",
                });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                1,
                await context.OfferIssues.CountAsync(
                    o =>
                        o.FeedbackId == seeded.FeedbackId
                        && o.Source == OfferIssueSources.Recovery
                )
            );
        }

        [Theory]
        [InlineData("email", "alex@example.com", ContactType.Email)]
        [InlineData("sms", "+447911123461", ContactType.Phone)]
        public async Task SendAndIssue_Returns400_WhenFollowUpWithdrawn(
            string channel,
            string guestContact,
            ContactType contactType
        )
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                $"recovery-offer-followup-withdrawn-{channel}-tok",
                contactType,
                guestContact,
                FeedbackWorkflowStatus.InProgress,
                email: $"offer-followup-withdrawn-{channel}-owner@example.com",
                withCatalogAttach: true,
                followUpGranted: false
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = channel == "email"
                ? JsonContent.Create(new
                {
                    channel,
                    subject = "Offer",
                    body = "Body",
                    intent = "respond_with_recovery_offer",
                    purpose = "include_a_recovery_offer",
                    tone = "appreciative",
                })
                : JsonContent.Create(new
                {
                    channel,
                    body = "Body",
                    intent = "respond_with_recovery_offer",
                    purpose = "include_a_recovery_offer",
                    tone = "appreciative",
                });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.Equal(
                ServiceReplyEligibility.NotAllowedMessage,
                body.GetProperty("message").GetString()
            );
        }

        [Fact]
        public async Task RecoveryOffer_IdenticalRepeatWithin10Min_Returns409DuplicateReply()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-duplicate-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-duplicate-owner@example.com",
                withCatalogAttach: true
            );

            var first = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            var body = await ReadJsonAsync(second);
            Assert.Equal("duplicate_reply", body.GetProperty("code").GetString());
            Assert.Equal(
                "This reply has already been submitted. Check its delivery status.",
                body.GetProperty("message").GetString()
            );

            using var scope = _factory.Services.CreateScope();
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

        [Fact]
        public async Task RecoveryOffer_SecondWhileFirstOpen_Returns409RecoveryOfferOpen()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-open-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-open-owner@example.com",
                withCatalogAttach: true
            );

            var first = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var firstBody = await ReadJsonAsync(first);
            var claimCode = firstBody
                .GetProperty("recoveryOffer")
                .GetProperty("redemptionCode")
                .GetString();
            Assert.False(string.IsNullOrWhiteSpace(claimCode));

            var second = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "We are sorry again. Please come back soon."
            );
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            var body = await ReadJsonAsync(second);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal("recovery_offer_open", body.GetProperty("code").GetString());
            Assert.Contains(claimCode!, body.GetProperty("message").GetString());

            using var scope = _factory.Services.CreateScope();
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

        [Fact]
        public async Task RecoveryOffer_AfterFirstRedeemed_Allowed()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-redeemed-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-redeemed-owner@example.com",
                withCatalogAttach: true
            );

            var first = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            await UpdateRecoveryIssueAsync(
                seeded.FeedbackId,
                issue => issue.RedeemedAtUtc = DateTime.UtcNow
            );

            var second = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "A second offer after the first was used."
            );
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                2,
                await context.OfferIssues.CountAsync(o =>
                    o.FeedbackId == seeded.FeedbackId
                    && o.Source == OfferIssueSources.Recovery
                )
            );
        }

        [Fact]
        public async Task RecoveryOffer_AfterFirstExpired_Allowed()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-expired-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-expired-owner@example.com",
                withCatalogAttach: true
            );

            var first = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            await UpdateRecoveryIssueAsync(
                seeded.FeedbackId,
                issue => issue.ExpiryAtUtc = DateTime.UtcNow.AddDays(-1)
            );

            var second = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "A second offer after the first expired."
            );
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        }

        [Fact]
        public async Task RecoveryOffer_AfterFirstVoided_Allowed()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-voided-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-voided-owner@example.com",
                withCatalogAttach: true
            );

            var first = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            await UpdateRecoveryIssueAsync(
                seeded.FeedbackId,
                issue => issue.CancelledAtUtc = DateTime.UtcNow
            );

            var second = await SendRecoveryOfferAsync(
                _client,
                seeded,
                "A second offer after the first was voided."
            );
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                2,
                await context.OfferIssues.CountAsync(o =>
                    o.FeedbackId == seeded.FeedbackId
                    && o.Source == OfferIssueSources.Recovery
                )
            );
        }

        [Fact]
        public async Task RecoveryOffer_LockTimeout_Returns503()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-lock-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-lock-owner@example.com",
                withCatalogAttach: true
            );

            _factory.ReplyLock.RejectAll = true;
            try
            {
                var response = await SendRecoveryOfferAsync(
                    _client,
                    seeded,
                    "Please enjoy 20% off your next visit."
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

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.OfferIssues.CountAsync(o =>
                    o.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_CallsProvider_StoresSidAndAccepted()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-sid-tok",
                ContactType.Phone,
                "+447911123470",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-sid-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var sent = Assert.Single(delivery.Calls);
            Assert.Equal("+447911123470", sent.PhoneNumber);

            var body = await ReadJsonAsync(response);
            var code = body
                .GetProperty("recoveryOffer")
                .GetProperty("redemptionCode")
                .GetString();
            Assert.Contains(code!, sent.Body);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            // The provider text has the restaurant prefix. The stored reply keeps the operator text.
            Assert.Equal("Recovery Offer Venue: " + guestResponse.Body, sent.Body);
            Assert.StartsWith("SMrecorded-", guestResponse.ProviderMessageId);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                guestResponse.DeliveryState
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_ReservesAndChargesSegments()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-segments-tok",
                ContactType.Phone,
                "+447911123471",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-segments-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var sent = Assert.Single(delivery.Calls);
            var segments = CampaignSmsSegmentCalculator.CountSegments(sent.Body);

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
                segments,
                await context.CreditLedgerEntries
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.EntryType == CreditLedgerEntryTypes.Consumption
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
            Assert.Equal(20 - segments, sms.Remaining);
        }

        [Fact]
        public async Task RecoveryOfferSms_InsufficientCredits_RefusedWithoutIssueOrRow()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-hard-stop-tok",
                ContactType.Phone,
                "+447911123472",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-hard-stop-owner@example.com",
                withCatalogAttach: true,
                smsCredits: 0,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(CreditChannels.Sms, body.GetProperty("channel").GetString());

            Assert.Empty(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.OfferIssues.CountAsync(o => o.FeedbackId == seeded.FeedbackId)
            );
            Assert.Equal(
                0,
                await context.FeedbackGuestResponses.CountAsync(r => r.FeedbackId == seeded.FeedbackId)
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_BillingNotLive_Refused()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                billingLive: false
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-billing-down-tok",
                ContactType.Phone,
                "+447911123473",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-billing-down-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(
                "billing_reserve_unavailable",
                body.GetProperty("code").GetString()
            );

            Assert.Empty(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(
                0,
                await context.OfferIssues.CountAsync(o => o.FeedbackId == seeded.FeedbackId)
            );
            Assert.Equal(
                0,
                await context.FeedbackGuestResponses.CountAsync(r => r.FeedbackId == seeded.FeedbackId)
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_ProviderFails_ReleasesCredits_CancelsIssue_ReplyFailed()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery(
                new RecoveryGuestSmsDeliveryResult.Failed
                {
                    Message = "Unable to send Recovery SMS.",
                }
            );
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-failed-tok",
                ContactType.Phone,
                "+447911123474",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-failed-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var failed = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
            );
            Assert.NotEqual(HttpStatusCode.OK, failed.StatusCode);
            Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
            var failedBody = await ReadJsonAsync(failed);
            Assert.False(failedBody.GetProperty("success").GetBoolean());
            Assert.Equal("sms_send_failed", failedBody.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                failedBody.GetProperty("message").GetString()
            );
            Assert.Single(delivery.Calls);

            using (var scope = customized.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var snapshot = scope.ServiceProvider
                    .GetRequiredService<ICreditBalanceSnapshot>();

                var issue = await context.OfferIssues
                    .AsNoTracking()
                    .SingleAsync(o => o.FeedbackId == seeded.FeedbackId);
                Assert.NotNull(issue.CancelledAtUtc);

                var reply = await context.FeedbackGuestResponses
                    .AsNoTracking()
                    .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
                Assert.Equal(GuestReplyDeliveryState.Failed, reply.DeliveryState);

                Assert.Equal(
                    0,
                    await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                        row.FeedbackId == seeded.FeedbackId
                    )
                );

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
                Assert.Equal(20, sms.Remaining);
            }

            // Retry after the cancelled Offer: the open-Offer rule no longer refuses.
            var retry = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy a free dessert on your next visit."
            );
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        }

        [Fact]
        public async Task RecoveryOfferSms_AfterProviderFailure_IdenticalRetryAllowed()
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
                "recovery-offer-sms-identical-retry-tok",
                ContactType.Phone,
                "+447911123475",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-identical-retry-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            const string body = "Please enjoy 20% off your next visit.";
            var failed = await SendRecoverySmsOfferAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);

            var retry = await SendRecoverySmsOfferAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Equal(2, delivery.Calls.Count);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var replies = await context.FeedbackGuestResponses
                .AsNoTracking()
                .Where(r => r.FeedbackId == seeded.FeedbackId)
                .OrderBy(r => r.Id)
                .ToListAsync();
            Assert.Equal(2, replies.Count);
            Assert.Equal(GuestReplyDeliveryState.Failed, replies[0].DeliveryState);
            Assert.Equal(GuestReplyDeliveryState.Accepted, replies[1].DeliveryState);

            var issues = await context.OfferIssues
                .AsNoTracking()
                .Where(o =>
                    o.FeedbackId == seeded.FeedbackId
                    && o.Source == OfferIssueSources.Recovery
                )
                .ToListAsync();
            Assert.Equal(2, issues.Count);
            Assert.Single(issues, o => o.CancelledAtUtc == null);
        }

        [Fact]
        public async Task RecoveryOfferSms_SettleFailsAfterAccept_ReturnsSuccess_ReplySaved()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                settleFails: true
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-settle-fail-tok",
                ContactType.Phone,
                "+447911123494",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-settle-fail-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
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

            // The guest got the text, so the Claim stays open.
            Assert.Single(
                await context.OfferIssues
                    .AsNoTracking()
                    .Where(o =>
                        o.FeedbackId == seeded.FeedbackId
                        && o.CancelledAtUtc == null
                    )
                    .ToListAsync()
            );
            Assert.Equal(
                1,
                await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                    row.FeedbackId == seeded.FeedbackId
                    && row.CompletedGuestResponseId == null
                )
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_ProviderThrows_ReplyUnavailable_Returns502_IdenticalRetryBlocked()
        {
            var delivery = new ThrowingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-provider-throws-tok",
                ContactType.Phone,
                "+447911123495",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-provider-throws-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            const string body = "Please enjoy 20% off your next visit.";
            var first = await SendRecoverySmsOfferAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);
            var firstBody = await ReadJsonAsync(first);
            Assert.Equal("sms_send_failed", firstBody.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                firstBody.GetProperty("message").GetString()
            );

            var retry = await SendRecoverySmsOfferAsync(client, seeded, body);
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

            // The Claim stays open: the guest may already hold the code.
            Assert.Single(
                await context.OfferIssues
                    .AsNoTracking()
                    .Where(o =>
                        o.FeedbackId == seeded.FeedbackId
                        && o.CancelledAtUtc == null
                    )
                    .ToListAsync()
            );
            Assert.Equal(
                1,
                await context.RecoverySmsSendIdempotencies.CountAsync(row =>
                    row.FeedbackId == seeded.FeedbackId
                    && row.CompletedGuestResponseId == null
                )
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_CompleteFailsAfterAccept_ReturnsSuccess()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailComplete = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-complete-fail-tok",
                ContactType.Phone,
                "+447911123502",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-complete-fail-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
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
            Assert.Single(
                await context.OfferIssues
                    .AsNoTracking()
                    .Where(o =>
                        o.FeedbackId == seeded.FeedbackId
                        && o.CancelledAtUtc == null
                    )
                    .ToListAsync()
            );
        }

        [Fact]
        public async Task RecoveryOfferSms_FinalSaveFailsAfterAccept_Returns502NotUnauthorized()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "recovery-offer-sms-final-save-fail-tok",
                ContactType.Phone,
                "+447911123503",
                FeedbackWorkflowStatus.InProgress,
                email: "offer-sms-final-save-fail-owner@example.com",
                withCatalogAttach: true,
                services: customized.Services
            );

            var response = await SendRecoverySmsOfferAsync(
                client,
                seeded,
                "Please enjoy 20% off your next visit."
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
            // The reply was saved before the send, but its accepted state was not.
            var reply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Pending, reply.DeliveryState);
            Assert.Null(reply.ProviderMessageId);
        }

        private WebApplicationFactory<Program> CreateFactoryWithSmsDelivery(
            IRecoveryGuestSmsDelivery delivery,
            bool billingLive = true,
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
                            .Where(d =>
                                d.ServiceType == typeof(IRecoveryGuestSmsDelivery)
                                || (!billingLive
                                    && d.ServiceType == typeof(IRecoverySmsBillingReserve))
                            )
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
                    if (!billingLive)
                    {
                        services.AddScoped<
                            IRecoverySmsBillingReserve,
                            UnavailableRecoverySmsBillingReserve
                        >();
                    }
                    else if (settleFails)
                    {
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
                });
            });
        }

        private static async Task<HttpResponseMessage> SendRecoverySmsOfferAsync(
            HttpClient client,
            (
                string Jwt,
                int LocationId,
                int FeedbackId,
                int? CatalogOfferId
            ) seeded,
            string body
        )
        {
            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-offers"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "sms",
                body,
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "warm_and_apologetic",
            });

            return await client.SendAsync(post);
        }

        private static async Task<HttpResponseMessage> SendRecoveryOfferAsync(
            HttpClient client,
            (string Jwt, int LocationId, int FeedbackId, int? CatalogOfferId) seeded,
            string body
        )
        {
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
                body,
                intent = "respond_with_recovery_offer",
                purpose = "include_a_recovery_offer",
                tone = "warm_and_apologetic",
            });

            return await client.SendAsync(post);
        }

        private async Task UpdateRecoveryIssueAsync(
            int feedbackId,
            Action<OfferIssue> change
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var issue = await context.OfferIssues.SingleAsync(o =>
                o.FeedbackId == feedbackId && o.Source == OfferIssueSources.Recovery
            );
            change(issue);
            await context.SaveChangesAsync();
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
            bool withCatalogAttach = false,
            bool withLocationGuest = false,
            bool followUpGranted = true,
            LocationGuestPermissionState? marketing = null,
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

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
                    restaurant.Id,
                    "TUMMLY-UK-GBP-2026-08-V3"
                )
            );
            await context.SaveChangesAsync();

            LocationGuest? locationGuest = null;
            int? catalogOfferId = null;

            // Catalog attach implies a Location Guest; callers may also seed a
            // guest without a RecoveryOfferId (honest no-attach assert).
            if (withCatalogAttach || withLocationGuest)
            {
                var masterGuest = new MasterGuest
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
                context.MasterGuests.Add(masterGuest);
                await context.SaveChangesAsync();

                locationGuest = new LocationGuest
                {
                    MasterGuestId = masterGuest.Id,
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
