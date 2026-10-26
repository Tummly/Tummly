using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Tests.Integration.Support;

namespace TummlyBackend.Tests.Integration
{
    public class AssistantConversationScopeEndpointsTests
        : IClassFixture<TummlyWebApplicationFactory>
    {
        private const string InScopeTitle = "In scope thread";
        private const string OutOfScopeTitle = "Out of scope thread";
        private const string AllScopeTitle = "All locations thread";

        private readonly TummlyWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AssistantConversationScopeEndpointsTests(TummlyWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ConversationList_HidesSingleScopeForUnassignedLocation()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );
            await SeedConversationAsync(seeded.MemberUserId, seeded.InScopeLocationId, InScopeTitle);
            await SeedConversationAsync(seeded.MemberUserId, seeded.OutOfScopeLocationId, OutOfScopeTitle);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/assistant/conversations?archived=false"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var titles = body.RootElement
                .GetProperty("conversations")
                .EnumerateArray()
                .Select(row => row.GetProperty("title").GetString())
                .ToList();

            Assert.Contains(InScopeTitle, titles);
            Assert.DoesNotContain(OutOfScopeTitle, titles);
        }

        [Fact]
        public async Task ConversationList_HidesAllScopeForPartialAccessUser()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );
            await SeedConversationAsync(seeded.MemberUserId, seeded.InScopeLocationId, InScopeTitle);
            await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var titles = await ListTitlesAsync(seeded.MemberJwt);

            Assert.Contains(InScopeTitle, titles);
            Assert.DoesNotContain(AllScopeTitle, titles);
        }

        [Fact]
        public async Task ConversationList_OwnerSeesOwnAllAndSingleScope()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );
            await SeedConversationAsync(seeded.OwnerUserId, seeded.InScopeLocationId, InScopeTitle);
            await SeedConversationAsync(seeded.OwnerUserId, seeded.OutOfScopeLocationId, OutOfScopeTitle);
            await SeedAllScopeConversationAsync(seeded.OwnerUserId, AllScopeTitle);

            var titles = await ListTitlesAsync(seeded.OwnerJwt);

            Assert.Contains(AllScopeTitle, titles);
            Assert.Contains(InScopeTitle, titles);
            Assert.Contains(OutOfScopeTitle, titles);
        }

        [Fact]
        public async Task GetConversation_AllScope_PartialAccess_Denied()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(
                _factory,
                PermissionRoles.LocationManager
            );
            var conversationId = await SeedAllScopeConversationAsync(
                seeded.MemberUserId,
                AllScopeTitle
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/assistant/conversations/{conversationId}"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", seeded.MemberJwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(AllScopeTitle, body);
        }

        [Fact]
        public async Task Send_AllScope_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var response = await SendAsync(HttpMethod.Post, "/api/assistant/turns", seeded.MemberJwt, new
            {
                conversationId,
                message = "Summarise recent feedback",
                analysisScope = InScopeAnalysisScope(seeded.InScopeLocationId),
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Retry_AllScope_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var response = await SendAsync(
                HttpMethod.Post,
                $"/api/assistant/conversations/{conversationId}/retry",
                seeded.MemberJwt,
                body: null
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApplyScope_AllScope_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var response = await SendAsync(
                HttpMethod.Patch,
                $"/api/assistant/conversations/{conversationId}/scope",
                seeded.MemberJwt,
                new { analysisScope = InScopeAnalysisScope(seeded.InScopeLocationId) }
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Archive_AllScope_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var response = await SendAsync(
                HttpMethod.Patch,
                $"/api/assistant/conversations/{conversationId}/archive",
                seeded.MemberJwt,
                body: null
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ClearDraft_AllScope_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var response = await SendAsync(
                HttpMethod.Post,
                $"/api/assistant/conversations/{conversationId}/draft-interview/clear",
                seeded.MemberJwt,
                body: null
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_AllScope_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);

            var response = await SendAsync(
                HttpMethod.Delete,
                $"/api/assistant/conversations/{conversationId}",
                seeded.MemberJwt,
                body: null
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SwitchToAll_PartialUser_403ScopeNotAllowed()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedConversationAsync(seeded.MemberUserId, seeded.InScopeLocationId, InScopeTitle);

            var response = await SendAsync(
                HttpMethod.Patch,
                $"/api/assistant/conversations/{conversationId}/scope",
                seeded.MemberJwt,
                new { analysisScope = AllAnalysisScope() }
            );

            await AssertScopeNotAllowedAsync(response);
        }

        [Fact]
        public async Task NewConversationAll_PartialUser_403ScopeNotAllowed()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);

            var response = await SendAsync(HttpMethod.Post, "/api/assistant/turns", seeded.MemberJwt, new
            {
                message = "Summarise all locations",
                analysisScope = AllAnalysisScope(),
            });

            await AssertScopeNotAllowedAsync(response);
        }

        [Fact]
        public async Task OwnerAllScope_ActionsStillWork()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var conversationId = await SeedAllScopeConversationAsync(seeded.OwnerUserId, AllScopeTitle);

            var get = await SendAsync(
                HttpMethod.Get,
                $"/api/assistant/conversations/{conversationId}",
                seeded.OwnerJwt,
                body: null
            );
            var archive = await SendAsync(
                HttpMethod.Patch,
                $"/api/assistant/conversations/{conversationId}/archive",
                seeded.OwnerJwt,
                body: null
            );

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        }

        [Fact]
        public async Task SendTurn_ReplayOnAllScope_PartialUser_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var allId = await SeedAllScopeConversationAsync(seeded.MemberUserId, AllScopeTitle);
            var key = Guid.NewGuid().ToString();
            await SeedCachedOutcomeAsync(seeded.RestaurantId, key, allId);

            var response = await SendAsync(HttpMethod.Post, "/api/assistant/turns", seeded.MemberJwt, new
            {
                conversationId = allId,
                message = "Summarise recent feedback",
                analysisScope = InScopeAnalysisScope(seeded.InScopeLocationId),
            }, idempotencyKey: key);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(AllScopeTitle, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SendTurn_ReplayOfAnotherConversation_PartialUser_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var allId = await SeedAllScopeConversationAsync(seeded.OwnerUserId, AllScopeTitle);
            var ownId = await SeedConversationAsync(seeded.MemberUserId, seeded.InScopeLocationId, InScopeTitle);
            var key = Guid.NewGuid().ToString();
            await SeedCachedOutcomeAsync(seeded.RestaurantId, key, allId);

            var response = await SendAsync(HttpMethod.Post, "/api/assistant/turns", seeded.MemberJwt, new
            {
                conversationId = ownId,
                message = "Summarise recent feedback",
                analysisScope = InScopeAnalysisScope(seeded.InScopeLocationId),
            }, idempotencyKey: key);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(AllScopeTitle, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SendTurn_ReplaySameConversation_OwnerGetsCachedOutcome()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var ownerConvId = await SeedConversationAsync(seeded.OwnerUserId, seeded.InScopeLocationId, InScopeTitle);
            var key = Guid.NewGuid().ToString();
            await SeedCachedOutcomeAsync(seeded.RestaurantId, key, ownerConvId);

            var response = await SendAsync(HttpMethod.Post, "/api/assistant/turns", seeded.OwnerJwt, new
            {
                conversationId = ownerConvId,
                message = "Summarise recent feedback",
                analysisScope = InScopeAnalysisScope(seeded.InScopeLocationId),
            }, idempotencyKey: key);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(ownerConvId, body.RootElement.GetProperty("conversation").GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task RetryTurn_ReplayOfAnotherConversation_PartialUser_NotFound()
        {
            var seeded = await OperatorScopeSeed.SeedAsync(_factory, PermissionRoles.LocationManager);
            var allId = await SeedAllScopeConversationAsync(seeded.OwnerUserId, AllScopeTitle);
            var failedId = await SeedFailedTurnAsync(seeded.MemberUserId, seeded.InScopeLocationId, InScopeTitle);
            var key = Guid.NewGuid().ToString();
            await SeedCachedOutcomeAsync(seeded.RestaurantId, key, allId);

            var response = await SendAsync(
                HttpMethod.Post,
                $"/api/assistant/conversations/{failedId}/retry",
                seeded.MemberJwt,
                body: null,
                idempotencyKey: key
            );

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain(AllScopeTitle, await response.Content.ReadAsStringAsync());
        }

        private async Task SeedCachedOutcomeAsync(int restaurantId, string key, int conversationId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            if (!context.BillingAccounts.Any(row => row.RestaurantId == restaurantId))
            {
                context.BillingAccounts.Add(new BillingAccount
                {
                    RestaurantId = restaurantId,
                    BillingStatus = BillingStatuses.Pilot,
                    ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
                });
            }

            var dto = new AssistantConversationDto
            {
                Id = conversationId,
                Title = AllScopeTitle,
                AnalysisScope = new AssistantAnalysisScopeDto
                {
                    ScopeKind = AssistantAnalysisScope.ScopeKindAll,
                },
            };
            context.AssistantAiActionOutcomes.Add(new AssistantAiActionOutcome
            {
                Id = Guid.NewGuid(),
                RestaurantId = restaurantId,
                IdempotencyKey = key,
                ResponseJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }),
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddHours(1),
            });
            await context.SaveChangesAsync();
        }

        private async Task<int> SeedFailedTurnAsync(int ownerUserId, int locationId, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var location = context.RestaurantLocations.Single(row => row.Id == locationId);
            var now = DateTime.UtcNow;
            var analysisScope = new AssistantAnalysisScopeDto
            {
                ScopeKind = "single",
                OwnedLocationId = locationId,
                OwnedLocationName = location.LocationName,
                ReportingPeriod = new AssistantReportingPeriodDto { Kind = "preset", PresetId = "last7" },
            };

            var conversation = new AssistantConversation
            {
                OwnerUserId = ownerUserId,
                Title = title,
                OwnedLocationId = locationId,
                OwnedLocationName = location.LocationName,
                ScopeKind = "single",
                ReportingPeriodKind = "preset",
                ReportingPeriodPresetId = "last7",
                CreatedAt = now,
                LastActivityAt = now,
            };
            var userMessage = new AssistantMessage
            {
                Role = AssistantMessageRole.User,
                Body = "Summarise recent feedback",
                CreatedAt = now,
            };
            AssistantAnalysisScope.CopyToUserMessage(userMessage, analysisScope, location.LocationName);
            conversation.Messages.Add(userMessage);
            conversation.Messages.Add(new AssistantMessage
            {
                Role = AssistantMessageRole.Assistant,
                Class = AssistantMessageClass.Failure,
                Body = "The answer failed.",
                CreatedAt = now.AddSeconds(1),
            });
            context.AssistantConversations.Add(conversation);
            await context.SaveChangesAsync();
            return conversation.Id;
        }

        private static object InScopeAnalysisScope(int locationId)
        {
            return new
            {
                scopeKind = "single",
                ownedLocationId = locationId,
                reportingPeriod = new { kind = "preset", presetId = "last7" },
            };
        }

        private static object AllAnalysisScope()
        {
            return new
            {
                scopeKind = AssistantAnalysisScope.ScopeKindAll,
                reportingPeriod = new { kind = "preset", presetId = "last7" },
            };
        }

        private async Task AssertScopeNotAllowedAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("scope_not_allowed", body.RootElement.GetProperty("code").GetString());
            Assert.Equal(
                "This item is outside your authorised Location scope.",
                body.RootElement.GetProperty("message").GetString()
            );
        }

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method,
            string path,
            string jwt,
            object? body,
            string? idempotencyKey = null
        )
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            if (idempotencyKey is not null)
            {
                request.Headers.Add("Idempotency-Key", idempotencyKey);
            }
            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }
            return await _client.SendAsync(request);
        }

        private async Task<List<string?>> ListTitlesAsync(string jwt)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "/api/assistant/conversations?archived=false"
            );
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement
                .GetProperty("conversations")
                .EnumerateArray()
                .Select(row => row.GetProperty("title").GetString())
                .ToList();
        }

        private async Task<int> SeedAllScopeConversationAsync(int ownerUserId, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTime.UtcNow;

            var conversation = new AssistantConversation
            {
                OwnerUserId = ownerUserId,
                Title = title,
                OwnedLocationId = null,
                OwnedLocationName = AssistantAnalysisScope.AllLocationsChromeName,
                ScopeKind = AssistantAnalysisScope.ScopeKindAll,
                ReportingPeriodKind = "preset",
                ReportingPeriodPresetId = "last7",
                CreatedAt = now,
                LastActivityAt = now,
            };
            context.AssistantConversations.Add(conversation);
            await context.SaveChangesAsync();
            return conversation.Id;
        }

        private async Task<int> SeedConversationAsync(int ownerUserId, int locationId, string title)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var location = context.RestaurantLocations.Single(row => row.Id == locationId);
            var now = DateTime.UtcNow;

            var conversation = new AssistantConversation
            {
                OwnerUserId = ownerUserId,
                Title = title,
                OwnedLocationId = locationId,
                OwnedLocationName = location.LocationName,
                ScopeKind = "single",
                ReportingPeriodKind = "preset",
                ReportingPeriodPresetId = "last7",
                CreatedAt = now,
                LastActivityAt = now,
            };
            context.AssistantConversations.Add(conversation);
            await context.SaveChangesAsync();
            return conversation.Id;
        }
    }
}
