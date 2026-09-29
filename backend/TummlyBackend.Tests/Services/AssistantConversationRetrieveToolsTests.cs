using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_RetrieveToolsEnabled_FeedbackAsk_UsesToolsAndSkipsEagerOffers()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 711, "Camden");
            _offersRetrieve.Calls.Clear();
            _retrieve.Calls.Clear();
            _progress.Events.Clear();

            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = true,
                    }
                )
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 711,
                    FirstSendRequest(locationId, "Summarise recent feedback")
                )
            );

            Assert.True(_fake.LastInput!.UseRetrieveTools);
            Assert.NotNull(_fake.LastInput.ExecuteRetrieveTools);
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
        public async Task SendTurn_RetrieveToolsEnabled_CreatePath_UsesToolsAndSkipsEagerFullPack()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 712, "Camden");
            ClearRetrieveCalls();
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = true,
                    }
                )
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 712,
                    FirstSendRequest(locationId, "Create a campaign draft")
                )
            );

            Assert.True(_fake.LastInput!.UseRetrieveTools);
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
        public async Task SendTurn_RetrieveToolsEnabled_NamedCompare_UsesCompareLocationsTool()
        {
            var camden = await SeedLocationAsync(ownerUserId: 714, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 714, "Soho");
            ClearRetrieveCalls();

            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = true,
                    }
                )
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 714,
                    FirstSendRequest(camden, "Compare Camden and Soho")
                )
            );

            Assert.True(_fake.LastInput!.UseRetrieveTools);
            Assert.True(_fake.LastInput.NamedCompare);
            Assert.False(_fake.LastInput.CompareAll);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            // compare_locations loads full packs for each authorised Location.
            Assert.Equal(2, _retrieve.Calls.Count);
        }

        [Fact]
        public async Task SendTurn_RetrieveToolsEnabled_CompareAll_UsesCompareAllTool()
        {
            var camden = await SeedLocationAsync(ownerUserId: 715, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 715, "Soho");
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = true,
                    }
                )
            );

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

            Assert.True(_fake.LastInput!.UseRetrieveTools);
            Assert.True(_fake.LastInput.CompareAll);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
            Assert.Equal(2, _retrieve.Calls.Count);
        }

        [Fact]
        public async Task SendTurn_RetrieveToolsEnabled_RecoveryPath_UsesTools()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 716, "Camden");
            ClearRetrieveCalls();
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = true,
                    }
                )
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 716,
                    FirstSendRequest(locationId, "Draft a recovery response")
                )
            );

            Assert.True(_fake.LastInput!.UseRetrieveTools);
            Assert.NotNull(_fake.LastInput.ExecuteRetrieveTools);
            Assert.Equal("grounded", outcome.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_RetrieveToolsDisabled_DoesNotSetToolExecutor()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 713, "Camden");
            var service = CreateConversationService(
                liveAnswerOptions: Options.Create(
                    new FeedbackClassificationSettings
                    {
                        AssistantRetrieveToolsEnabled = false,
                    }
                )
            );

            await service.SendTurnAsync(
                ownerUserId: 713,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            Assert.False(_fake.LastInput!.UseRetrieveTools);
            Assert.Null(_fake.LastInput.ExecuteRetrieveTools);
        }
    }
}
