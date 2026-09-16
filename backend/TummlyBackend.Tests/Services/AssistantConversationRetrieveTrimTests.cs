using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        [Fact]
        public async Task SendTurn_CampaignsActiveAsk_SkipsUnusedDomainRetrieves()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            ClearRetrieveCalls();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Are there any active campaigns for this Location?"
                )
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_campaignsRetrieve.Calls);
            Assert.Empty(_retrieve.Calls);
            Assert.Empty(_offersRetrieve.Calls);
            Assert.Empty(_captureRetrieve.Calls);
            Assert.Empty(_homeRetrieve.Calls);
            Assert.Empty(_guestsRetrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_CaptureQrAsk_SkipsUnusedDomainRetrieves()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            ClearRetrieveCalls();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Have we had any QR scans today?")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_captureRetrieve.Calls);
            Assert.Empty(_retrieve.Calls);
            Assert.Empty(_offersRetrieve.Calls);
            Assert.Empty(_campaignsRetrieve.Calls);
            Assert.Empty(_homeRetrieve.Calls);
            Assert.Empty(_guestsRetrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_OffersRedemptionsAsk_SkipsUnusedDomainRetrieves()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            ClearRetrieveCalls();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Are there any Offer Redemptions today?")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_offersRetrieve.Calls);
            Assert.Empty(_retrieve.Calls);
            Assert.Empty(_campaignsRetrieve.Calls);
            Assert.Empty(_captureRetrieve.Calls);
            Assert.Empty(_homeRetrieve.Calls);
            Assert.Empty(_guestsRetrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_MixedSummaryAsk_RetrievesFullDomainPack()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            ClearRetrieveCalls();

            // Multi-domain ask → MixedSummary focus; must load every pack.
            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise feedback and campaigns")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_retrieve.Calls);
            Assert.Single(_offersRetrieve.Calls);
            Assert.Single(_campaignsRetrieve.Calls);
            Assert.Single(_captureRetrieve.Calls);
            Assert.Single(_homeRetrieve.Calls);
            Assert.Single(_guestsRetrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_CompareAll_RetrievesFullDomainPackEvenForNarrowAsk()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            ClearRetrieveCalls();

            // Narrow Campaigns ask — compare-all still loads every domain pack.
            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(
                    seeded.ConversationId,
                    "Are there any active campaigns for this Location?"
                )
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(4, _retrieve.Calls.Count);
            Assert.Equal(4, _offersRetrieve.Calls.Count);
            Assert.Equal(4, _campaignsRetrieve.Calls.Count);
            Assert.Equal(4, _captureRetrieve.Calls.Count);
            Assert.Equal(4, _homeRetrieve.Calls.Count);
            Assert.Equal(4, _guestsRetrieve.Calls.Count);
        }
    }
}
