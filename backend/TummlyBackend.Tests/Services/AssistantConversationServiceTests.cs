using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.OwnedLocation;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class AssistantConversationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FakeAssistantLiveAnswerProvider _fake;
        private readonly AssistantConversationService _service;

        public AssistantConversationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _fake = new FakeAssistantLiveAnswerProvider();
            _service = new AssistantConversationService(
                _context,
                new OwnedLocationService(_context),
                _fake
            );
        }

        [Fact]
        public async Task SendTurn_PersistsConversationAndStubAnswer_OnFirstSend()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Title);
            Assert.Equal(locationId, ok.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal("Camden", ok.Conversation.AnalysisScope.OwnedLocationName);
            Assert.Equal("preset", ok.Conversation.AnalysisScope.ReportingPeriod.Kind);
            Assert.Equal("last7", ok.Conversation.AnalysisScope.ReportingPeriod.PresetId);
            Assert.Equal(2, ok.Conversation.Messages.Count);
            Assert.Equal("user", ok.Conversation.Messages[0].Role);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Messages[0].Body);
            Assert.Equal(locationId, ok.Conversation.Messages[0].AnalysisScope?.OwnedLocationId);
            Assert.Equal("assistant", ok.Conversation.Messages[1].Role);
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Equal("A stub summary for Camden", ok.Conversation.Messages[1].Title);
            Assert.Contains("the last 7 days", ok.Conversation.Messages[1].Body);
            Assert.NotNull(_fake.LastInput);
            Assert.Equal("Summarise recent feedback", _fake.LastInput!.UserMessage);
            Assert.Equal("Camden", _fake.LastInput.OwnedLocationName);

            Assert.Equal(1, await _context.AssistantConversations.CountAsync());
            Assert.Equal(2, await _context.AssistantMessages.CountAsync());
        }

        [Fact]
        public async Task SendTurn_DoesNotCreateRow_WhenMessageEmpty()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "   ")
            );

            Assert.IsType<AssistantTurnOutcome.Invalid>(outcome);
            Assert.Equal(0, await _context.AssistantConversations.CountAsync());
        }

        [Fact]
        public async Task SendTurn_UsesFakeProviderFailure_AsFailureClass()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.Fail();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, ok.Conversation.Messages.Count);
            Assert.Equal("failure", ok.Conversation.Messages[1].Class);
            Assert.Equal(AssistantAnalysisScope.FailureBody, ok.Conversation.Messages[1].Body);
            Assert.Null(ok.Conversation.Messages[1].Title);
        }

        [Fact]
        public async Task SendTurn_ReturnsForbidden_WhenLocationNotOwned()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 99,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var denied = Assert.IsType<AssistantTurnOutcome.LocationDenied>(outcome);
            Assert.Equal(OwnedLocationResolveStatus.Forbidden, denied.Location.Status);
            Assert.Equal(0, await _context.AssistantConversations.CountAsync());
        }

        [Fact]
        public async Task Get_ReturnsNotFound_ForAnotherOperator()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;

            var outcome = await _service.GetAsync(ownerUserId: 99, conversationId);

            Assert.IsType<AssistantTurnOutcome.NotFound>(outcome);
        }

        [Fact]
        public async Task ApplyScope_UpdatesSavedScope_WithoutChangingLastActivity()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var second = await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            var lastActivity = ok.Conversation.LastActivityAt;

            await Task.Delay(20);

            var applied = await _service.ApplyScopeAsync(
                ownerUserId: 7,
                ok.Conversation.Id,
                new ApplyAssistantScopeRequest
                {
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        OwnedLocationId = second,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "last30",
                        },
                    },
                }
            );

            var after = Assert.IsType<AssistantTurnOutcome.Ok>(applied);
            Assert.Equal(second, after.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal("Shoreditch", after.Conversation.AnalysisScope.OwnedLocationName);
            Assert.Equal("last30", after.Conversation.AnalysisScope.ReportingPeriod.PresetId);
            Assert.Equal(lastActivity, after.Conversation.LastActivityAt);
        }

        [Fact]
        public async Task SendTurn_StoresScopeSnapshotOnEachUserSend()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var second = await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;

            await _service.ApplyScopeAsync(
                ownerUserId: 7,
                conversationId,
                new ApplyAssistantScopeRequest
                {
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        OwnedLocationId = second,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "thisMonth",
                        },
                    },
                }
            );

            var continued = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = conversationId,
                    Message = "What needs attention?",
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        OwnedLocationId = second,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "thisMonth",
                        },
                    },
                }
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(continued);
            var userTurns = ok.Conversation.Messages
                .Where(message => message.Role == "user")
                .ToList();
            Assert.Equal(2, userTurns.Count);
            Assert.Equal(first, userTurns[0].AnalysisScope?.OwnedLocationId);
            Assert.Equal("last7", userTurns[0].AnalysisScope?.ReportingPeriod.PresetId);
            Assert.Equal(second, userTurns[1].AnalysisScope?.OwnedLocationId);
            Assert.Equal("thisMonth", userTurns[1].AnalysisScope?.ReportingPeriod.PresetId);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_PersistsFailure_WhenTurnIsCancelled()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.Delay = TimeSpan.FromSeconds(5);
            using var cts = new CancellationTokenSource();
            var send = _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback"),
                cts.Token
            );
            await Task.Delay(30);
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);

            var conversation = await _context.AssistantConversations
                .Include(row => row.Messages)
                .SingleAsync();
            Assert.Equal(2, conversation.Messages.Count);
            Assert.Equal(AssistantMessageClass.Failure, conversation.Messages.Last().Class);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static SendAssistantTurnRequest FirstSendRequest(
            int locationId,
            string message
        )
            => new()
            {
                Message = message,
                AnalysisScope = new AssistantAnalysisScopeDto
                {
                    OwnedLocationId = locationId,
                    ReportingPeriod = new AssistantReportingPeriodDto
                    {
                        Kind = "preset",
                        PresetId = "last7",
                    },
                },
            };

        private async Task<int> SeedLocationAsync(int ownerUserId, string locationName)
        {
            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = "Multi",
                OwnerUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedSecondLocationAsync(int ownerUserId, string locationName)
        {
            var restaurant = await _context.Restaurants
                .SingleAsync(row => row.OwnerUserId == ownerUserId);
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }
    }
}
