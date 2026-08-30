using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class AssistantEndpointsTests : IClassFixture<TummlyWebApplicationFactory>
    {
        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AssistantEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendTurn_Returns401_WhenUnauthenticated()
        {
            var response = await _client.PostAsJsonAsync(
                "/api/assistant/turns",
                new
                {
                    message = "Summarise recent feedback",
                    analysisScope = new
                    {
                        ownedLocationId = 1,
                        reportingPeriod = new { kind = "preset", presetId = "last7" },
                    },
                }
            );

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SendTurn_Returns403_ForNonOwnedLocation()
        {
            var owner = await SeedOwnerAsync("assistant-owner-a-token-1234");
            var other = await SeedOwnerAsync(
                "assistant-owner-b-token-1234",
                email: "assistant-other@example.com"
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = other.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SendTurn_PersistsPersonalThread_WithFakeStub()
        {
            var owner = await SeedOwnerAsync("assistant-send-token-123456");
            ResetFake();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            var conversation = body.GetProperty("conversation");
            Assert.Equal("Summarise recent feedback", conversation.GetProperty("title").GetString());
            Assert.Equal(
                owner.LocationId,
                conversation.GetProperty("analysisScope").GetProperty("ownedLocationId").GetInt32()
            );
            var messages = conversation.GetProperty("messages");
            Assert.Equal(2, messages.GetArrayLength());
            Assert.Equal("grounded", messages[1].GetProperty("class").GetString());
            Assert.Contains(
                "nothing to summarise",
                messages[1].GetProperty("body").GetString()
            );
            Assert.Contains(
                "Change Scope",
                messages[1].GetProperty("body").GetString()
            );
            Assert.Contains(
                "Camden",
                messages[1].GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task GetConversation_Returns404_ForTeammate()
        {
            var owner = await SeedOwnerAsync("assistant-get-owner-token-12");
            var other = await SeedOwnerAsync(
                "assistant-get-other-token-12",
                email: "assistant-get-other@example.com"
            );
            ResetFake();

            using var send = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns");
            send.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Jwt);
            send.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            var created = await _client.SendAsync(send);
            var createdBody = await ReadJsonAsync(created);
            var conversationId = createdBody
                .GetProperty("conversation")
                .GetProperty("id")
                .GetInt32();

            using var get = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/assistant/conversations/{conversationId}"
            );
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other.Jwt);

            var response = await _client.SendAsync(get);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ListConversations_ReturnsOnlyOwnerThreads()
        {
            var owner = await SeedOwnerAsync("assistant-list-owner-token1");
            var other = await SeedOwnerAsync(
                "assistant-list-other-token1",
                email: "assistant-list-other@example.com"
            );
            ResetFake();

            using var send = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/turns");
            send.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Jwt);
            send.Content = JsonContent.Create(new
            {
                message = "Owner thread",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            await _client.SendAsync(send);

            using var list = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/assistant/conversations?archived=false"
            );
            list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other.Jwt);
            var response = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal(0, body.GetProperty("conversations").GetArrayLength());
        }

        private void ResetFake()
        {
            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeAssistantLiveAnswerProvider>();
            fake.ResetToCannedStub();
        }

        private static JsonElement LastMessage(JsonElement conversation)
        {
            var messages = conversation.GetProperty("messages");
            return messages[messages.GetArrayLength() - 1];
        }

        private FakeAssistantLiveAnswerProvider FakeLive =>
            _factory.Services.GetRequiredService<FakeAssistantLiveAnswerProvider>();

        private FakeSpeechToTextProvider FakeStt =>
            _factory.Services.GetRequiredService<FakeSpeechToTextProvider>();

        private async Task<JsonElement> SendTurnAsync(
            string jwt,
            int locationId,
            string message,
            int? conversationId = null
        )
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Content = JsonContent.Create(new
            {
                message,
                conversationId,
                analysisScope = new
                {
                    ownedLocationId = locationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            return body.GetProperty("conversation");
        }

        [Fact]
        public async Task SendTurn_MultiTurnOfferThread_GapsOnceThenPersistsWithReviewAction()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-offer-token-1");
            ResetFake();
            FakeLive.SucceedWith(
                AssistantMessageClass.Grounded,
                "Lunch offer",
                "How long should the lunch offer stay valid?",
                AssistantTask.OfferPath
            );

            var started = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Create a 25% off lunch offer"
            );
            var gap = LastMessage(started);
            Assert.Equal(
                "user",
                started.GetProperty("messages")[0].GetProperty("role").GetString()
            );
            Assert.Equal("assistant", gap.GetProperty("role").GetString());
            Assert.True(
                gap.GetProperty("id").GetInt32()
                > started.GetProperty("messages")[0].GetProperty("id").GetInt32()
            );
            Assert.Equal("gap", gap.GetProperty("class").GetString());
            Assert.Equal(
                AssistantGapAsk.EndDateAsk,
                gap.GetProperty("body").GetString()
            );
            Assert.Equal(
                0,
                gap.GetProperty("actions").GetArrayLength()
            );
            Assert.False(started.GetProperty("draftInterviewActive").GetBoolean());
            var conversationId = started.GetProperty("id").GetInt32();

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, await context.CatalogOffers.CountAsync());

            FakeLive.SucceedWith(
                AssistantMessageClass.Grounded,
                "Lunch discount",
                "Saving the lunch discount.",
                AssistantTask.OfferPath,
                null,
                new AssistantOfferPathTermsState
                {
                    OfferType = "percentage_discount",
                    DiscountPercentage = 25m,
                    Validity = "30_days_after_issue",
                }
            );
            var answered = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "30 days after issue",
                conversationId
            );
var reply = LastMessage(answered);
            Assert.Equal("grounded", reply.GetProperty("class").GetString());
            var actions = reply.GetProperty("actions");
            Assert.Equal(1, actions.GetArrayLength());
            Assert.Equal(
                "review-offer",
                actions[0].GetProperty("type").GetString()
            );

            var offers = await context.CatalogOffers.ToListAsync();
            var offer = Assert.Single(offers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(25m, offer.DiscountPercentage);
            Assert.Equal(
                CatalogOfferValidity.Days30AfterIssue,
                offer.Validity
            );
        }

        [Fact]
        public async Task SendTurn_MultiTurnThread_KeepsTitleFromFirstTurn()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-title-token-1");
            ResetFake();

            var started = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Summarise recent feedback"
            );
            await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Show me recent feedback again",
                started.GetProperty("id").GetInt32()
            );

            using var listRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/assistant/conversations"
            );
            listRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            var listed = await _client.SendAsync(listRequest);
            Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
            var listBody = await ReadJsonAsync(listed);
            var conversation = listBody.GetProperty("conversations")[0];
            Assert.Equal(
                "Summarise recent feedback",
                conversation.GetProperty("title").GetString()
            );
        }

        [Fact]
        public async Task SendTurn_RetrieveAnswer_KeepsGroundedMarkdownAllowListShape()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-grounded-token-");
            ResetFake();

            var conversation = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Summarise recent feedback"
            );
var reply = LastMessage(conversation);

            Assert.Equal("grounded", reply.GetProperty("class").GetString());
            var body = reply.GetProperty("body").GetString() ?? string.Empty;
            Assert.Contains("nothing to summarise", body, StringComparison.Ordinal);
            Assert.DoesNotContain("###", body, StringComparison.Ordinal);
            Assert.DoesNotContain("<", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_CompareLocationsAsk_BehavesAsBefore()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-compare-token-");
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurantId = context.RestaurantLocations.Single(
                    location => location.Id == owner.LocationId
                ).RestaurantId;
                context.RestaurantLocations.Add(
                    new RestaurantLocation
                    {
                        RestaurantId = restaurantId,
                        LocationName = "Soho",
                        Address = "2 High Street",
                        CreatedAt = DateTime.UtcNow,
                    }
                );
                context.Restaurants.Single(
                    restaurant => restaurant.Id == restaurantId
                ).AccountType = "Multi";
                await context.SaveChangesAsync();
            }
            ResetFake();

            // Warm-up turn: keeps the one-shot title generation off the
            // compare turn, so the fake's last input belongs to it.
            var warmUp = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Summarise recent feedback"
            );
            var conversation = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Compare Camden and Soho",
                warmUp.GetProperty("id").GetInt32()
            );
            var reply = LastMessage(conversation);

            // The ask routes as a two-location compare before the model call.
            var compareIds = FakeLive.LastInput?.CompareLocations?
                .Select(location => location.OwnedLocationId)
                .OrderBy(id => id)
                .ToList();
            Assert.NotNull(compareIds);
            Assert.Equal(2, compareIds!.Count);

            Assert.Equal("grounded", reply.GetProperty("class").GetString());
        }

        [Fact]
        public async Task SendTurn_ProductExpertAsk_BehavesAsBefore()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-product-token-");
            ResetFake();

            var conversation = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "What can you do?"
            );
var reply = LastMessage(conversation);

            Assert.Equal("grounded", reply.GetProperty("class").GetString());
            var body = reply.GetProperty("body").GetString() ?? string.Empty;
            Assert.Contains("**", body, StringComparison.Ordinal);
            Assert.Contains("- ", body, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Offer type catalogue",
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("###", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_AttentionAsk_BehavesAsBefore()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-attention-token");
            ResetFake();

            var conversation = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "How many guests came last week?"
            );
var reply = LastMessage(conversation);

            Assert.Equal("grounded", reply.GetProperty("class").GetString());
            Assert.Contains(
                "Weekly brief covers the closed prior week",
                reply.GetProperty("body").GetString(),
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Camden",
                reply.GetProperty("body").GetString(),
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_RecoveryAsk_BehavesAsBefore()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-recovery-token");
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                context.Feedbacks.Add(
                    new Feedback
                    {
                        RestaurantLocationId = owner.LocationId,
                        GuestName = "Pat Guest",
                        GuestContact = "pat@example.com",
                        ContactType = ContactType.Email,
                        Comment = "Slow service at dinner",
                        OffersOptOut = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        ClassificationStatus = ClassificationStatus.Succeeded,
                        Sentiment = FeedbackSentiment.Negative,
                        DetectedTagsJson = "[\"Service\"]",
                        WorkflowStatus = FeedbackWorkflowStatus.New,
                    }
                );
                await context.SaveChangesAsync();
            }
            ResetFake();

            var conversation = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Respond to these guests"
            );
var reply = LastMessage(conversation);

            Assert.Equal(
                "open-recovery",
                reply.GetProperty("actions")[0].GetProperty("type").GetString()
            );
            Assert.NotNull(
                conversation.GetProperty("pendingRecoveryDraft")
            );
        }

        [Fact]
        public async Task SpeechTranscript_FeedANormalAssistantReply()
        {
            var owner = await SeedOwnerAsync("assistant-e2e-voice-token-12");
            ResetFake();
            FakeStt.Reset();
            FakeStt.SucceedWith("Summarise recent feedback");

            var transcript = await TranscribeAsync(owner.Jwt);

            var conversation = await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                transcript
            );
            var messages = conversation.GetProperty("messages");
            Assert.Equal(2, messages.GetArrayLength());
            Assert.Equal(
                transcript,
                messages[0].GetProperty("body").GetString()
            );
            Assert.Equal(
                "grounded",
                messages[1].GetProperty("class").GetString()
            );
        }

        [Fact]
        public async Task SendTurn_SuccessfulSend_BurnsOneAiCredit()
        {
            var owner = await SeedOwnerAsync("assistant-billing-burn-token");
            ResetFake();

            await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Summarise recent feedback"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await RestaurantIdForLocationAsync(owner.LocationId);
            var consumption = await context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == restaurantId
                    && row.Channel == CreditChannels.Ai
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
                .SingleAsync();
            Assert.Equal(1, consumption.Quantity);
            Assert.Equal(owner.LocationId, consumption.LocationId);
        }

        [Fact]
        public async Task SendTurn_ProviderFailure_BurnsZero()
        {
            var owner = await SeedOwnerAsync("assistant-billing-fail-token");
            ResetFake();
            FakeLive.Fail();

            await SendTurnAsync(
                owner.Jwt,
                owner.LocationId,
                "Summarise recent feedback"
            );

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await RestaurantIdForLocationAsync(owner.LocationId);
            Assert.False(
                await context.CreditLedgerEntries.AnyAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
            );
        }

        [Fact]
        public async Task SendTurn_Timeout_BurnsZero()
        {
            var owner = await SeedOwnerAsync("assistant-billing-timeout-tok");
            ResetFake();
            FakeLive.Delay = TimeSpan.FromSeconds(30);

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            try
            {
                await _client.SendAsync(request, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Client cancel / empty controller result.
            }

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await RestaurantIdForLocationAsync(owner.LocationId);
            Assert.False(
                await context.CreditLedgerEntries.AnyAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
            );
        }

        [Fact]
        public async Task SendTurn_RemainingZero_DoesNotCallLiveAnswer()
        {
            var owner = await SeedOwnerAsync("assistant-billing-zero-token");
            ResetFake();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();
                var restaurantId = await context.RestaurantLocations
                    .Where(location => location.Id == owner.LocationId)
                    .Select(location => location.RestaurantId)
                    .SingleAsync();
                var grant = await context.CreditLedgerEntries
                    .Where(row =>
                        row.RestaurantId == restaurantId
                        && row.Channel == CreditChannels.Ai
                        && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
                    )
                    .SingleAsync();
                context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = restaurantId,
                        Channel = CreditChannels.Ai,
                        EntryType = CreditLedgerEntryTypes.Consumption,
                        Quantity = grant.Quantity,
                        AllocationId = grant.Id,
                        LocationId = owner.LocationId,
                        CreatedAtUtc = DateTime.UtcNow,
                    }
                );
                await context.SaveChangesAsync();
            }

            var callsBefore = FakeLive.CompleteCount;
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            request.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.Equal("channel_hard_stopped", body.GetProperty("code").GetString());
            Assert.Equal(0, body.GetProperty("remaining").GetInt32());
            Assert.Equal(1, body.GetProperty("requested").GetInt32());
            Assert.Equal(callsBefore, FakeLive.CompleteCount);
        }

        [Fact]
        public async Task SendTurn_SameIdempotencyKey_BurnsOnce()
        {
            var owner = await SeedOwnerAsync("assistant-billing-idem-token");
            ResetFake();
            const string key = "22222222-2222-2222-2222-222222222222";

            using var firstRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            firstRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            firstRequest.Headers.Add("Idempotency-Key", key);
            firstRequest.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            var firstResponse = await _client.SendAsync(firstRequest);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            using var secondRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/turns"
            );
            secondRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", owner.Jwt);
            secondRequest.Headers.Add("Idempotency-Key", key);
            secondRequest.Content = JsonContent.Create(new
            {
                message = "Summarise recent feedback",
                analysisScope = new
                {
                    ownedLocationId = owner.LocationId,
                    reportingPeriod = new { kind = "preset", presetId = "last7" },
                },
            });
            var secondResponse = await _client.SendAsync(secondRequest);
            Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var restaurantId = await RestaurantIdForLocationAsync(owner.LocationId);
            var burns = await context.CreditLedgerEntries.CountAsync(row =>
                row.RestaurantId == restaurantId
                && row.EntryType == CreditLedgerEntryTypes.Consumption
            );
            Assert.Equal(1, burns);
        }

        private async Task<int> RestaurantIdForLocationAsync(int locationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            return await context.RestaurantLocations
                .Where(location => location.Id == locationId)
                .Select(location => location.RestaurantId)
                .SingleAsync();
        }

        private async Task<string> TranscribeAsync(string jwt)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(
                Encoding.UTF8.GetBytes("fake-webm-audio-bytes")
            );
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("audio/webm");
            content.Add(fileContent, "audio", "clip.webm");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/assistant/stt"
            )
            {
                Content = content,
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);

            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            return body.GetProperty("text").GetString()!;
        }

        private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(string Jwt, int LocationId)> SeedOwnerAsync(
            string unusedToken,
            string email = "assistant-owner@example.com"
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Assistant Owner",
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
                Name = "Assistant Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Camden",
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
            context.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurant.Id,
                    Channel = CreditChannels.Ai,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = 100,
                    PricebookVersion = "TUMMLY-UK-GBP-2026-08-V3",
                    CreatedAtUtc = DateTime.UtcNow,
                }
            );
            await context.SaveChangesAsync();

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id);
        }
    }
}
