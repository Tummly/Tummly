using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Integration
{
    public class CampaignMessageDraftEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string PricebookId = "TUMMLY-UK-GBP-2026-08-V3";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CampaignMessageDraftEndpointsTests(
            TummlyWebApplicationFactory factory
        )
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostMessageDraft_Prepare_BurnsOneAiCredit()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-prepare-burn",
                aiCredits: 3
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                "Thank you for joining us recently.",
                "Thanks for visiting",
                "email"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(seeded.LocationId, mode: "prepare"),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, fake.CallCount);

            using var verifyScope = _factory.Services.CreateScope();
            var context = verifyScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var consumptions = await context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Channel == CreditChannels.Ai
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
                .ToListAsync();
            Assert.Single(consumptions);
            Assert.Equal(1, consumptions[0].Quantity);
            Assert.Equal(seeded.LocationId, consumptions[0].LocationId);
        }

        [Fact]
        public async Task PostMessageDraft_RewriteSubjectThenMessage_BurnsTwoAiCredits()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-rewrite-burn",
                aiCredits: 5
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                "Rewritten campaign body.",
                "Rewritten subject",
                "email"
            );

            using var subjectRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(
                    seeded.LocationId,
                    mode: "rewrite_subject",
                    currentBody: "Prior body",
                    currentSubject: "Prior subject"
                ),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var subjectResponse = await _client.SendAsync(subjectRequest);
            Assert.Equal(HttpStatusCode.OK, subjectResponse.StatusCode);

            using var messageRequest = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(
                    seeded.LocationId,
                    mode: "rewrite_message",
                    currentBody: "Prior body",
                    currentSubject: "Prior subject"
                ),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var messageResponse = await _client.SendAsync(messageRequest);
            Assert.Equal(HttpStatusCode.OK, messageResponse.StatusCode);
            Assert.Equal(2, fake.CallCount);

            using var verifyScope = _factory.Services.CreateScope();
            var context = verifyScope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var consumptions = await context.CreditLedgerEntries
                .Where(row =>
                    row.RestaurantId == seeded.RestaurantId
                    && row.Channel == CreditChannels.Ai
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
                .ToListAsync();
            Assert.Equal(2, consumptions.Count);
            Assert.Equal(2, consumptions.Sum(row => row.Quantity));
        }

        [Fact]
        public async Task PostMessageDraft_RemainingZero_RefusesWithoutCallingProvider()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-zero",
                aiCredits: 0
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                "Should not return",
                "Should not return",
                "email"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(seeded.LocationId, mode: "prepare"),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "channel_hard_stopped",
                body.GetProperty("code").GetString()
            );
            Assert.Equal("ai", body.GetProperty("channel").GetString());
            Assert.Equal(0, body.GetProperty("remaining").GetInt32());
            Assert.Equal(1, body.GetProperty("requested").GetInt32());
            Assert.Equal(0, fake.CallCount);
        }

        [Fact]
        public async Task PostMessageDraft_Prepare_ReturnsSubjectAndBody()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-prepare"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                "Thank you for joining us recently.",
                "Thanks for visiting",
                "email"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(seeded.LocationId, mode: "prepare"),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Thank you for joining us recently.",
                body.GetProperty("body").GetString()
            );
            Assert.Equal(
                "Thanks for visiting",
                body.GetProperty("subject").GetString()
            );
            Assert.Equal("email", body.GetProperty("channel").GetString());
            Assert.Equal(1, fake.CallCount);
            Assert.NotNull(fake.LastInput);
            Assert.Equal("prepare", fake.LastInput!.Mode);
            Assert.Equal("thank-recent-guests", fake.LastInput.GoalId);
            Assert.Equal("all-eligible-guests", fake.LastInput.AudienceKey);
            Assert.Equal("no-offer", fake.LastInput.OfferStance);
            Assert.Equal("Main", fake.LastInput.LocationName);
            Assert.Null(fake.LastInput.CurrentBody);
            Assert.DoesNotContain(
                "@",
                JsonSerializer.Serialize(fake.LastInput)
            );
        }

        [Fact]
        public async Task PostMessageDraft_RewriteMessage_ForwardsCurrentText()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-rewrite"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                "Rewritten campaign body.",
                "Prior subject",
                "email"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(
                    seeded.LocationId,
                    mode: "rewrite_message",
                    currentBody: "Prior body",
                    currentSubject: "Prior subject"
                ),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal(1, fake.CallCount);
            Assert.Equal("rewrite_message", fake.LastInput!.Mode);
            Assert.Equal("Prior body", fake.LastInput.CurrentBody);
            Assert.Equal("Prior subject", fake.LastInput.CurrentSubject);
        }

        [Fact]
        public async Task PostMessageDraft_RewriteSubject_ForwardsCurrentText()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-rewrite-subject"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.SucceedWith(
                "Prior body",
                "Rewritten subject",
                "email"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(
                    seeded.LocationId,
                    mode: "rewrite_subject",
                    currentBody: "Prior body",
                    currentSubject: "Prior subject"
                ),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.True(body.GetProperty("success").GetBoolean());
            Assert.Equal(
                "Rewritten subject",
                body.GetProperty("subject").GetString()
            );
            Assert.Equal(1, fake.CallCount);
            Assert.Equal("rewrite_subject", fake.LastInput!.Mode);
            Assert.Equal("Prior body", fake.LastInput.CurrentBody);
            Assert.Equal("Prior subject", fake.LastInput.CurrentSubject);
        }

        [Fact]
        public async Task PostMessageDraft_ProviderFail_ReturnsRetryable502()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-fail"
            );

            using var scope = _factory.Services.CreateScope();
            var fake = scope.ServiceProvider
                .GetRequiredService<FakeCampaignMessageDraftProvider>();
            fake.ResetCallCount();
            fake.Fail(retryable: true);

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(seeded.LocationId, mode: "prepare"),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.True(body.GetProperty("retryable").GetBoolean());
            Assert.Equal(1, fake.CallCount);
        }

        [Fact]
        public async Task PostMessageDraft_InvalidMode_ReturnsBadRequest()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-bad-mode"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(seeded.LocationId, mode: "invent"),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var body = await ReadJsonAsync(response);
            Assert.False(body.GetProperty("success").GetBoolean());
            Assert.False(body.GetProperty("retryable").GetBoolean());
        }

        [Fact]
        public async Task PostMessageDraft_UnownedLocation_ReturnsForbidden()
        {
            var seeded = await SeedOwnerWithLocationAsync(
                "campaign-msg-unowned"
            );

            using var request = AuthorizedJson(
                HttpMethod.Post,
                "/api/campaigns/message-draft",
                seeded.Jwt,
                PrepareBody(locationId: 999_999, mode: "prepare"),
                idempotencyKey: Guid.NewGuid().ToString("D")
            );
            var response = await _client.SendAsync(request);
            Assert.True(
                response.StatusCode is HttpStatusCode.Forbidden
                    or HttpStatusCode.NotFound
            );
        }

        private static object PrepareBody(
            int locationId,
            string mode,
            string? currentBody = null,
            string? currentSubject = null
        ) => new
        {
            locationId,
            channel = "email",
            goalId = "thank-recent-guests",
            audienceKey = "all-eligible-guests",
            offerStance = "no-offer",
            campaignName = (string?)null,
            tone = "friendly_and_clear",
            includeNotes = (string?)null,
            mode,
            currentBody,
            currentSubject,
        };

        private static HttpRequestMessage AuthorizedJson(
            HttpMethod method,
            string url,
            string jwt,
            object body,
            string idempotencyKey
        )
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"
                ),
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", jwt);
            request.Headers.Add("Idempotency-Key", idempotencyKey);
            return request;
        }

        private static async Task<JsonElement> ReadJsonAsync(
            HttpResponseMessage response
        )
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        private async Task<(
            string Jwt,
            int LocationId,
            int RestaurantId
        )> SeedOwnerWithLocationAsync(
            string emailLocalPart,
            int aiCredits = 20
        )
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var user = new User
            {
                FullName = "Campaign Msg Owner",
                Email = $"{emailLocalPart}@example.com",
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
                Name = "Campaign Msg Venue",
                AccountType = "Single",
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            context.BillingAccounts.Add(
                BillingCreditsService.CreateDefaultBillingAccount(
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

            if (aiCredits > 0)
            {
                context.CreditLedgerEntries.Add(
                    new CreditLedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        RestaurantId = restaurant.Id,
                        Channel = CreditChannels.Ai,
                        EntryType = CreditLedgerEntryTypes.PilotAllocation,
                        Quantity = aiCredits,
                        PricebookVersion = PricebookId,
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                    }
                );
                await context.SaveChangesAsync();
            }

            var jwt = jwtService.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );

            return (jwt, location.Id, restaurant.Id);
        }
    }
}
