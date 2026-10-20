using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_TurnDeadline_PersistsFailureAndBurnsZero()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 911, "Camden");
            var restaurantId = await RestaurantIdForLocationAsync(locationId);
            _fake.Delay = TimeSpan.FromSeconds(10);
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings { AssistantTurnDeadlineSeconds = 1 }
                )
            );
            var stopwatch = Stopwatch.StartNew();

            var outcome = await service.SendTurnAsync(
                ownerUserId: 911,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            stopwatch.Stop();
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[^1].Class);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
            Assert.False(
                await _context.CreditLedgerEntries.AnyAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.EntryType == CreditLedgerEntryTypes.Consumption
                )
            );
        }

        [Fact]
        public async Task SendTurn_ClientCancel_StillThrowsAndPersistsFailure()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 912, "Camden");
            _fake.Delay = TimeSpan.FromSeconds(10);
            using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                _service.SendTurnAsync(
                    ownerUserId: 912,
                    FirstSendRequest(locationId, "Summarise recent feedback"),
                    cancellationToken: cancel.Token
                )
            );

            var last = await _context.AssistantMessages
                .OrderByDescending(message => message.Id)
                .FirstAsync();
            Assert.Equal(AssistantMessageClass.Failure, last.Class);
        }

        [Fact]
        public async Task SendTurn_SlowTitleCall_KeepsFirstMessageTitle()
        {
            var camden = await SeedLocationAsync(ownerUserId: 913, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 913, "Soho");
            StubRetrieveConversationTitle("Compare locations generated");
            _fake.Delay = TimeSpan.FromSeconds(10);
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings { AssistantTitleTimeoutSeconds = 1 }
                )
            );
            var stopwatch = Stopwatch.StartNew();

            // Unnamed compare clarifies without the live answer, so the only
            // model call is the title call.
            var outcome = await service.SendTurnAsync(
                ownerUserId: 913,
                FirstSendRequest(camden, "Compare locations")
            );

            stopwatch.Stop();
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("clarify", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Compare locations", ok.Conversation.Title);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
        }

        [Fact]
        public async Task SendTurn_LiveAnswer_StoresTimingsInTurnTrace()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 914, "Camden");

            await _service.SendTurnAsync(
                ownerUserId: 914,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var trace = await _context.AssistantMessages
                .Where(message => message.Role == AssistantMessageRole.Assistant)
                .OrderByDescending(message => message.Id)
                .Select(message => message.TurnTraceJson)
                .FirstAsync();
            Assert.NotNull(trace);
            using var document = JsonDocument.Parse(trace!);
            var timings = document.RootElement.GetProperty("timings");
            Assert.True(timings.GetProperty("totalMs").GetInt64() >= 0);
            Assert.Equal(JsonValueKind.Array, timings.GetProperty("modelCalls").ValueKind);
            var toolCalls = timings.GetProperty("toolCalls");
            Assert.True(toolCalls.GetArrayLength() > 0);
            Assert.Equal(
                "read_feedback_summary",
                toolCalls[0].GetProperty("name").GetString()
            );
            Assert.Equal("success", toolCalls[0].GetProperty("status").GetString());
            Assert.DoesNotContain("Summarise recent feedback", trace!, StringComparison.Ordinal);
        }
    }
}
