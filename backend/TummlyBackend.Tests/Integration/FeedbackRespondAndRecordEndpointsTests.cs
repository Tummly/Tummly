using TummlyBackend.Tests.Helpers;
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
    public class FeedbackRespondAndRecordEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FeedbackRespondAndRecordEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendAndRecord_DualWrites_KeepsInProgress_EmitsBothActivities()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Regarding your visit",
                body = "Thank you — we briefed the team.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });

            var postResponse = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);

            var postBody = await ReadJsonAsync(postResponse);
            Assert.True(postBody.GetProperty("success").GetBoolean());
            Assert.Equal(
                "in_progress",
                postBody.GetProperty("workflowStatus").GetString()
            );

            var guestEvent = postBody.GetProperty("guestResponseActivityEvent");
            Assert.Equal(
                "guest_response_sent",
                guestEvent.GetProperty("kind").GetString()
            );

            var internalEvent =
                postBody.GetProperty("internalActionActivityEvent");
            Assert.Equal(
                "internal_action_recorded",
                internalEvent.GetProperty("kind").GetString()
            );
            Assert.Equal(
                "team_briefed",
                internalEvent.GetProperty("category").GetString()
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

            Assert.Equal(
                1,
                await context.FeedbackGuestResponses.CountAsync(
                    r => r.FeedbackId == seeded.FeedbackId
                )
            );
            Assert.Equal(
                1,
                await context.FeedbackInternalActions.CountAsync(
                    a => a.FeedbackId == seeded.FeedbackId
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
            var kinds = activity.EnumerateArray()
                .Select(e => e.GetProperty("kind").GetString())
                .ToList();
            Assert.Contains("guest_response_sent", kinds);
            Assert.Contains("internal_action_recorded", kinds);
            Assert.DoesNotContain("recovery_completed", kinds);
        }

        [Fact]
        public async Task SendAndRecord_Returns409_WhenAlreadyResolved()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-resolved-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.Resolved,
                email: "resolved-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Regarding your visit",
                body = "Thank you — we briefed the team.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task SendAndRecord_Returns400_WhenNoContact()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-no-contact-tok",
                ContactType.Unknown,
                "",
                FeedbackWorkflowStatus.InProgress,
                email: "no-contact-owner@example.com"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Regarding your visit",
                body = "Thank you — we briefed the team.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SendAndRecord_Returns403_ForNonOwner()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-cross-tenant-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "cross-tenant-owner@example.com"
            );
            var otherJwt = await SeedOtherOwnerJwtAsync(
                "respond-and-record-cross-tenant-other-tok"
            );

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", otherJwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Regarding your visit",
                body = "Thank you — we briefed the team.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CompleteRecovery_RequiresBothFacts_ThenResolves()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-complete-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-complete@example.com"
            );

            using var completeBefore = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            completeBefore.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            completeBefore.Content = JsonContent.Create(new
            {
                intent = "respond_and_record_internal_action",
            });

            var beforeResponse = await _client.SendAsync(completeBefore);
            Assert.Equal(HttpStatusCode.BadRequest, beforeResponse.StatusCode);

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                subject = "Sorry",
                body = "We followed up internally.",
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });
            Assert.Equal(
                HttpStatusCode.OK,
                (await _client.SendAsync(post)).StatusCode
            );

            using var complete = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-completion"
            );
            complete.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            complete.Content = JsonContent.Create(new
            {
                intent = "respond_and_record_internal_action",
            });

            var completeResponse = await _client.SendAsync(complete);
            Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
            var completeBody = await ReadJsonAsync(completeResponse);
            Assert.Equal(
                "resolved",
                completeBody.GetProperty("workflowStatus").GetString()
            );
            Assert.Equal(
                "respond_and_record_internal_action",
                completeBody
                    .GetProperty("activityEvent")
                    .GetProperty("recoveryIntent")
                    .GetString()
            );
        }

        [Fact]
        public async Task PrepareDraft_AcceptsConfirmedInternalActionContext()
        {
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-draft-tok",
                ContactType.Email,
                "alex@example.com",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-draft@example.com"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<TummlyBackend.Services.FakeFeedbackRecoveryDraftProvider>();

            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/recovery-draft"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
            post.Content = JsonContent.Create(new
            {
                channel = "email",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                mode = "prepare",
                confirmedInternalActionCategory = "team_briefed",
                confirmedInternalActionNote = "Briefed the floor team.",
            });

            var response = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.NotNull(fake.LastInput);
            Assert.Equal(
                "team_briefed",
                fake.LastInput!.ConfirmedInternalActionCategory
            );
            Assert.Equal(
                "Briefed the floor team.",
                fake.LastInput.ConfirmedInternalActionNote
            );
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
            string email = "respond-and-record-owner@example.com",
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
                FullName = "Respond And Record Owner",
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
                Name = "Respond And Record Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                PilotBillingAccountSeed.Create(
                    restaurant.Id,
                    PricebookId
                )
            );

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Main",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };

            context.RestaurantLocations.Add(location);
            await context.SaveChangesAsync();

            // SMS replies need an eligible Location Guest (follow-up granted).
            LocationGuest? locationGuest = null;
            if (contactType == ContactType.Phone)
            {
                var masterGuest = new MasterGuest
                {
                    RestaurantId = restaurant.Id,
                    Mobile = guestContact,
                    NormalizedPhone = new string(guestContact.Where(char.IsDigit).ToArray()),
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

                new LocationGuestPermissionLedgerService(context).RecordEvent(
                    locationGuest.Id,
                    location.Id,
                    LocationGuestPermissionKind.FeedbackFollowUp,
                    LocationGuestPermissionLedgerEventKinds.Grant,
                    LocationGuestPermissionLedgerSources.GuestForm,
                    DateTime.UtcNow
                );
                await context.SaveChangesAsync();
            }

            var feedback = new Feedback
            {
                RestaurantLocationId = location.Id,
                LocationGuestId = locationGuest?.Id,
                GuestName = "Alex Guest",
                GuestContact = guestContact,
                ContactType = contactType,
                Comment = "Cold food",
                CreatedAt = DateTime.UtcNow,
                ClassificationStatus = ClassificationStatus.Succeeded,
                Sentiment = FeedbackSentiment.Negative,
                DetectedTagsJson = "[]",
                WorkflowStatus = workflowStatus,
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync();

            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Ai,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 20,
                    PricebookVersion = PricebookId,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                }
            );
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
                        PricebookVersion = PricebookId,
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

        [Fact]
        public async Task RespondAndRecordSms_CallsProvider_StoresSidAndAccepted()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-sid-tok",
                ContactType.Phone,
                "+447911123480",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-sid-owner@example.com",
                services: customized.Services
            );

            var response = await SendRespondAndRecordSmsAsync(
                client,
                seeded,
                "Thanks for telling us. We have briefed the team."
            );
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var sent = Assert.Single(delivery.Calls);
            Assert.Equal("+447911123480", sent.PhoneNumber);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var guestResponse = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            // The provider text has the restaurant prefix. The stored reply keeps the operator text.
            Assert.Equal("Respond And Record Venue: " + guestResponse.Body, sent.Body);
            Assert.StartsWith("SMrecorded-", guestResponse.ProviderMessageId);
            Assert.Equal(
                GuestReplyDeliveryState.Accepted,
                guestResponse.DeliveryState
            );
            Assert.Equal(
                1,
                await context.FeedbackInternalActions.CountAsync(a =>
                    a.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task RespondAndRecordSms_InsufficientCredits_Refused()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-hard-stop-tok",
                ContactType.Phone,
                "+447911123481",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-hard-stop-owner@example.com",
                smsCredits: 0,
                services: customized.Services
            );

            var response = await SendRespondAndRecordSmsAsync(
                client,
                seeded,
                "Thanks for telling us. We have briefed the team."
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
                await context.FeedbackGuestResponses.CountAsync(r => r.FeedbackId == seeded.FeedbackId)
            );
            Assert.Equal(
                0,
                await context.FeedbackInternalActions.CountAsync(a => a.FeedbackId == seeded.FeedbackId)
            );
        }

        [Fact]
        public async Task RespondAndRecordSms_ProviderFails_ReleasesCredits()
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
                "respond-and-record-sms-failed-tok",
                ContactType.Phone,
                "+447911123482",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-failed-owner@example.com",
                services: customized.Services
            );

            var response = await SendRespondAndRecordSmsAsync(
                client,
                seeded,
                "Thanks for telling us. We have briefed the team."
            );
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            var failedBody = await ReadJsonAsync(response);
            Assert.False(failedBody.GetProperty("success").GetBoolean());
            Assert.Equal("sms_send_failed", failedBody.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                failedBody.GetProperty("message").GetString()
            );
            Assert.Single(delivery.Calls);

            using var scope = customized.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var snapshot = scope.ServiceProvider
                .GetRequiredService<ICreditBalanceSnapshot>();
            var failedReply = await context.FeedbackGuestResponses
                .AsNoTracking()
                .SingleAsync(r => r.FeedbackId == seeded.FeedbackId);
            Assert.Equal(GuestReplyDeliveryState.Failed, failedReply.DeliveryState);
            Assert.Null(failedReply.ProviderMessageId);
            Assert.Equal(
                0,
                await context.FeedbackInternalActions.CountAsync(a => a.FeedbackId == seeded.FeedbackId)
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

        [Fact]
        public async Task RespondAndRecordSms_SettleFailsAfterAccept_ReturnsSuccess_ReplySaved()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                settleFails: true
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-settle-fail-tok",
                ContactType.Phone,
                "+447911123496",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-settle-fail-owner@example.com",
                services: customized.Services
            );

            var response = await SendRespondAndRecordSmsAsync(
                client,
                seeded,
                "Thanks for telling us. We have briefed the team."
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
            Assert.Equal(
                1,
                await context.FeedbackInternalActions.CountAsync(a =>
                    a.FeedbackId == seeded.FeedbackId
                )
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
        public async Task RespondAndRecordSms_ProviderThrows_ReplyUnavailable_Returns502_IdenticalRetryBlocked()
        {
            var delivery = new ThrowingRecoveryGuestSmsDelivery();
            await using var customized = CreateFactoryWithSmsDelivery(delivery);
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-provider-throws-tok",
                ContactType.Phone,
                "+447911123497",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-provider-throws-owner@example.com",
                services: customized.Services
            );

            const string body = "Thanks for telling us. We have briefed the team.";
            var first = await SendRespondAndRecordSmsAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);
            var firstBody = await ReadJsonAsync(first);
            Assert.Equal("sms_send_failed", firstBody.GetProperty("code").GetString());
            Assert.Equal(
                "The reply was not confirmed as sent. Check its status before trying again.",
                firstBody.GetProperty("message").GetString()
            );

            var retry = await SendRespondAndRecordSmsAsync(client, seeded, body);
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
        public async Task RespondAndRecordSms_CompleteFailsAfterAccept_ReturnsSuccess()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailComplete = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-complete-fail-tok",
                ContactType.Phone,
                "+447911123504",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-complete-fail-owner@example.com",
                services: customized.Services
            );

            var response = await SendRespondAndRecordSmsAsync(
                client,
                seeded,
                "Thanks for telling us. We have briefed the team."
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
            Assert.Equal(
                1,
                await context.FeedbackInternalActions.CountAsync(a =>
                    a.FeedbackId == seeded.FeedbackId
                )
            );
        }

        [Fact]
        public async Task RespondAndRecordSms_FinalSaveFailsAfterAccept_Returns502NotUnauthorized()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-final-save-fail-tok",
                ContactType.Phone,
                "+447911123505",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-final-save-fail-owner@example.com",
                services: customized.Services
            );

            var response = await SendRespondAndRecordSmsAsync(
                client,
                seeded,
                "Thanks for telling us. We have briefed the team."
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
            Assert.Equal(
                0,
                await context.FeedbackInternalActions.CountAsync(a => a.FeedbackId == seeded.FeedbackId)
            );
        }

        [Fact]
        public async Task RespondAndRecordSms_FinalSaveFailsAfterAccept_IdenticalRetryBlocked()
        {
            var delivery = new RecordingRecoveryGuestSmsDelivery();
            var faults = new SmsStepFaults { FailSave = true };
            await using var customized = CreateFactoryWithSmsDelivery(
                delivery,
                faults: faults
            );
            var client = customized.CreateClient();
            var seeded = await SeedOwnerWithFeedbackAsync(
                "respond-and-record-sms-final-save-retry-tok",
                ContactType.Phone,
                "+447911123513",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-final-save-retry-owner@example.com",
                services: customized.Services
            );

            const string body = "Thanks for telling us. We have briefed the team.";
            var first = await SendRespondAndRecordSmsAsync(client, seeded, body);
            Assert.Equal(HttpStatusCode.BadGateway, first.StatusCode);

            // The save works again. The identical retry must not send a second text.
            faults.FailSave = false;
            var retry = await SendRespondAndRecordSmsAsync(client, seeded, body);
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
        public async Task RespondAndRecordSms_ProviderFailed_RowFailed_RetryAllowed()
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
                "respond-and-record-sms-failed-row-tok",
                ContactType.Phone,
                "+447911123514",
                FeedbackWorkflowStatus.InProgress,
                email: "respond-and-record-sms-failed-row-owner@example.com",
                services: customized.Services
            );

            const string body = "Thanks for telling us. We have briefed the team.";
            var failed = await SendRespondAndRecordSmsAsync(client, seeded, body);
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

            var retry = await SendRespondAndRecordSmsAsync(client, seeded, body);
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
                });
            });
        }

        private static async Task<HttpResponseMessage> SendRespondAndRecordSmsAsync(
            HttpClient client,
            (string Jwt, int LocationId, int FeedbackId) seeded,
            string body
        )
        {
            using var post = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/feedback/{seeded.FeedbackId}/respond-and-record-internal-action"
            );
            post.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", seeded.Jwt);
            post.Content = JsonContent.Create(new
            {
                channel = "sms",
                body,
                intent = "respond_and_record_internal_action",
                purpose = "acknowledge_feedback",
                tone = "warm_and_apologetic",
                category = "team_briefed",
                note = "Briefed the floor team.",
            });

            return await client.SendAsync(post);
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
                FullName = "Other Respond And Record Owner",
                Email =
                    $"other-respond-and-record-owner-{linkToken}@example.com",
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
                Name = "Other Respond And Record Venue",
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
