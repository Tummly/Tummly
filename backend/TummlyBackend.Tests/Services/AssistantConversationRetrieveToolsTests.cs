using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_RetrieveTools_FeedbackAsk_UsesToolsAndSkipsEagerOffers()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 711, "Camden");
            _offersRetrieve.Calls.Clear();
            _retrieve.Calls.Clear();
            _progress.Events.Clear();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 711,
                    FirstSendRequest(locationId, "Summarise recent feedback")
                )
            );

            Assert.NotNull(_fake.LastInput!.ExecuteRetrieveTools);
            // Fake tool path calls feedback only for Feedback focus — not offers.
            Assert.Single(_retrieve.Calls);
            Assert.Empty(_offersRetrieve.Calls);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            Assert.Contains(
                AssistantTurnProgressSteps.Retrieving,
                _progress.Events.Select(step => step.Step)
            );
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_CreatePath_UsesToolsAndSkipsEagerFullPack()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 712, "Camden");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 712,
                    FirstSendRequest(locationId, "Create a campaign draft")
                )
            );

            Assert.NotNull(_fake.LastInput!.ExecuteRetrieveTools);
            Assert.NotNull(_fake.LastInput.ExecuteRetrieveTools);
            // Create tools call campaigns + offers only — not full eager pack.
            Assert.NotEmpty(_campaignsRetrieve.Calls);
            Assert.NotEmpty(_offersRetrieve.Calls);
            Assert.Empty(_retrieve.Calls);
            Assert.Empty(_captureRetrieve.Calls);
            Assert.Empty(_homeRetrieve.Calls);
            Assert.Empty(_guestsRetrieve.Calls);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            Assert.Equal(
                AssistantTask.CreateCampaignDraft,
                AssistantTaskClassification.Classify("Create a campaign draft")
            );
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_NamedCompare_UsesCompareLocationsTool()
        {
            var camden = await SeedLocationAsync(ownerUserId: 714, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 714, "Soho");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 714,
                    FirstSendRequest(camden, "Compare Camden and Soho")
                )
            );

            Assert.NotNull(_fake.LastInput!.ExecuteRetrieveTools);
            Assert.True(_fake.LastInput.NamedCompare);
            Assert.False(_fake.LastInput.CompareAll);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            // compare_locations loads full packs for each authorised Location.
            Assert.Equal(2, _retrieve.Calls.Count);
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_CompareAll_UsesCompareAllTool()
        {
            var camden = await SeedLocationAsync(ownerUserId: 715, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 715, "Soho");
            var service = CreateConversationService();

            var created = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 715,
                    FirstSendRequest(camden, "Summarise recent feedback")
                )
            );
            await service.ApplyScopeAsync(
                ownerUserId: 715,
                created.Conversation.Id,
                AllOwnedLocationsScopeRequest()
            );
            ClearRetrieveCalls();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 715,
                    AllSendRequest(created.Conversation.Id, "Summarise recent feedback")
                )
            );

            Assert.NotNull(_fake.LastInput!.ExecuteRetrieveTools);
            Assert.True(_fake.LastInput.CompareAll);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            Assert.Equal(2, _retrieve.Calls.Count);
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_RecoveryPath_UsesTools()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 716, "Camden");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 716,
                    FirstSendRequest(locationId, "Draft a recovery response")
                )
            );

            Assert.NotNull(_fake.LastInput!.ExecuteRetrieveTools);
            Assert.NotNull(_fake.LastInput.ExecuteRetrieveTools);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
        }


        [Fact]
        public async Task SendTurn_RetrieveTools_Last30Ask_OverridesPeriodAndPersists()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 719, "Camden");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 719,
                    FirstSendRequest(
                        locationId,
                        "Summarise feedback from the last 30 days"
                    )
                )
            );

            Assert.Equal("last30", outcome.Conversation.AnalysisScope.ReportingPeriod.PresetId);
            Assert.Equal(
                "the last 30 days",
                _fake.LastInput!.PeriodPhrase
            );
            var answer = outcome.Conversation.Messages[^1];
            Assert.NotNull(answer.ScopeChange);
            Assert.Contains("period", answer.ScopeChange!.Kinds);
            Assert.Equal("Last 7 days", answer.ScopeChange.PreviousPeriodLabel);
            Assert.Equal("Last 30 days", answer.ScopeChange.NextPeriodLabel);
            Assert.Single(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_AcrossAllGuests_AutoPromotesAndCompareAll()
        {
            var camden = await SeedLocationAsync(ownerUserId: 720, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 720, "Soho");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 720,
                    FirstSendRequest(
                        camden,
                        "How many guests have we got across all our locations?"
                    )
                )
            );

            Assert.Equal("all", outcome.Conversation.AnalysisScope.ScopeKind);
            Assert.True(_fake.LastInput!.CompareAll);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            var answer = outcome.Conversation.Messages[^1];
            Assert.NotNull(answer.ScopeChange);
            Assert.Contains("locations", answer.ScopeChange!.Kinds);
            Assert.Equal(2, _retrieve.Calls.Count);
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_NoNlCue_LeavesScopeUnchanged()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 721, "Camden");
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 721,
                    FirstSendRequest(locationId, "Summarise recent feedback")
                )
            );

            Assert.Equal("last7", outcome.Conversation.AnalysisScope.ReportingPeriod.PresetId);
            Assert.Equal("single", outcome.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(outcome.Conversation.Messages[^1].ScopeChange);
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_AttentionAsk_UsesTools()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 722, "Camden");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 722,
                    FirstSendRequest(locationId, "What needs attention?")
                )
            );

            Assert.NotNull(_fake.LastInput!.ExecuteRetrieveTools);
            Assert.NotNull(_fake.LastInput.ExecuteRetrieveTools);
            Assert.Equal(0, _homeRecommendation.CallCount);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            Assert.Contains(
                AssistantTurnProgressSteps.Retrieving,
                _progress.Events.Select(step => step.Step)
            );
        }

        [Fact]
        public async Task SendTurn_RetrieveTools_AllLocations_AttentionAsk_StillPicksOne()
        {
            await SeedLocationAsync(ownerUserId: 723, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 723, "Soho");
            ClearRetrieveCalls();
            var service = CreateConversationService();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 723,
                    AllSendRequest("What needs attention?")
                )
            );

            Assert.Equal("Pick one location", outcome.Conversation.Messages[^1].Title);
            Assert.Contains(
                "Change Scope",
                outcome.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Empty(_retrieve.Calls);
            Assert.Null(_fake.LastInput);
            Assert.Equal(0, _homeRecommendation.CallCount);
        }
    }
}
