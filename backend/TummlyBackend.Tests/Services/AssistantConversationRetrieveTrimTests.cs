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

        [Fact]
        public async Task SendTurn_FeedbackToday_UsesThatUtcDay_NotLast7()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var today = UtcDayStart(DateTime.UtcNow);
            await SeedFeedbackAsync(locationId, today.AddHours(2), comment: "Today one");
            await SeedFeedbackAsync(locationId, today.AddHours(4), comment: "Today two");
            await SeedFeedbackAsync(locationId, today.AddDays(-5).AddHours(3), comment: "Older");
            ClearRetrieveCalls();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How many feedbacks received today")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[^1].Body;
            var call = Assert.Single(_retrieve.Calls);
            Assert.Equal(today, call.FromUtc);
            Assert.Equal(today.AddDays(1), call.ToUtc);
            Assert.Contains("2 feedback items", body, StringComparison.Ordinal);
            Assert.Contains(
                today.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("the last 7 days", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("3 feedback", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_QrScansTodayForNamedLocation_UsesThatDayAndLocation()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var soho = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            var camdenQr = await SeedQrCodeAsync(camden);
            var sohoQr = await SeedQrCodeAsync(soho);
            var today = UtcDayStart(DateTime.UtcNow);
            await SeedQrScanAsync(soho, sohoQr, today.AddHours(3));
            await SeedQrScanAsync(soho, sohoQr, today.AddDays(-5).AddHours(3));
            await SeedQrScanAsync(camden, camdenQr, today.AddHours(2));
            ClearRetrieveCalls();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "How many QR scans today for Soho")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[^1].Body;
            var call = Assert.Single(_captureRetrieve.Calls);
            Assert.Equal(soho, call.OwnedLocationId);
            Assert.Equal(today, call.FromUtc);
            Assert.Equal(today.AddDays(1), call.ToUtc);
            Assert.Contains("Soho had 1 QR scans", body, StringComparison.Ordinal);
            Assert.Contains(
                today.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("the last 7 days", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("2 QR scans", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_PerformanceOnNamedDay_UsesHomeKpisForThatDay()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var qrId = await SeedQrCodeAsync(locationId);
            var today = UtcDayStart(DateTime.UtcNow);
            var named = today.AddDays(-3);
            await SeedFeedbackAsync(locationId, named.AddHours(2), comment: "Named day");
            await SeedFeedbackAsync(locationId, today.AddHours(2), comment: "Today");
            await SeedQrScanAsync(locationId, qrId, named.AddHours(4));
            await SeedQrScanAsync(locationId, qrId, today.AddHours(1));
            ClearRetrieveCalls();
            var label = named.ToString(
                "d MMMM yyyy",
                System.Globalization.CultureInfo.InvariantCulture
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, $"How is performance on {label}")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[^1].Body;
            var call = Assert.Single(_homeRetrieve.Calls);
            Assert.Equal(named, call.FromUtc);
            Assert.Equal(named.AddDays(1), call.ToUtc);
            Assert.Contains("1 Feedback submitted", body, StringComparison.Ordinal);
            Assert.Contains("1 QR scans", body, StringComparison.Ordinal);
            Assert.Contains(
                named.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
                body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("the last 7 days", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("2 Feedback submitted", body, StringComparison.Ordinal);
        }

        private static DateTime UtcDayStart(DateTime utcNow)
            => new(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}
