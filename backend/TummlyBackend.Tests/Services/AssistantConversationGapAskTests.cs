using Microsoft.EntityFrameworkCore;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public partial class AssistantConversationServiceTests
    {
        private const string HappyPathCampaignWithOfferAsk =
            "Draft a campaign with 50% off offer and select all eligible guests with Email channels";

        [Fact]
        public async Task SendTurn_HappyPath_KeepsNamedFactsAndAsksWhenOfferShouldEnd()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Campaign Draft with Offer",
                "Create Campaign with Offer.",
                AssistantTask.CreateCampaignWithOffer,
                null,
                new AssistantOfferPathTermsState
                {
                    OfferType = "percentage_discount",
                }
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var gap = started.Conversation.Messages[^1];
            Assert.Equal("gap", gap.Class);
            Assert.Equal(AssistantGapAsk.EndDateAsk, gap.Body);
            Assert.DoesNotContain("validity", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("value", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("required usage", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name the offer", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Offer name", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(gap.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());

            var gapState = await StoredGapStateAsync(started.Conversation.Id);
            Assert.Equal(AssistantGapTurn.KindOfferTerms, gapState.Kind);
            var storedTerms = AssistantOfferPathTerms.FromJson(gapState.OfferTermsJson);
            Assert.NotNull(storedTerms);
            Assert.Equal(50m, storedTerms!.DiscountPercentage);
        }

        [Fact]
        public async Task SendTurn_HappyPath_ThirtyDaysAfterTheyGetIt_PersistsDraft()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "30 days after they get it",
                        started.Conversation.Id
                    )
                )
            );

            var campaign = Assert.Single(_context.Campaigns);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal("draft", campaign.Status);
            Assert.Equal("email", campaign.Channel);
            Assert.Equal("all-eligible-guests", campaign.AudienceKey);
            Assert.Equal(offer.Id, campaign.OfferId);
            Assert.Equal(50m, offer.DiscountPercentage);
            Assert.Equal(CatalogOfferValidity.Days30AfterIssue, offer.Validity);
            Assert.Equal("grounded", answered.Conversation.Messages[^1].Class);
            Assert.Contains(
                answered.Conversation.Messages[^1].Actions.Select(action => action.Type),
                type => type == "review-campaign"
            );
            Assert.Contains(
                answered.Conversation.Messages[^1].Actions.Select(action => action.Type),
                type => type == "review-offer"
            );
            Assert.DoesNotContain(
                answered.Conversation.Messages[^1].Actions,
                action => action.Type is "send-campaign" or "schedule-campaign"
            );
            Assert.Null(await StoredGapStateOrNullAsync(started.Conversation.Id));
        }

        [Fact]
        public async Task SendTurn_HappyPath_NamedDate_PersistsFixedEndDate()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "it should end on 30 September 2026",
                        started.Conversation.Id
                    )
                )
            );

            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferValidity.ChooseExpiryDate, offer.Validity);
            Assert.Equal(new DateOnly(2026, 9, 30), offer.CustomExpiryDate);
            Assert.Equal("grounded", answered.Conversation.Messages[^1].Class);
            Assert.Single(_context.Campaigns);
        }

        [Fact]
        public async Task SendTurn_ConfusedWhatDoesThatMean_ExplainsEndDateWithoutLiveAnswer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );
            var afterGap = _fake.CompleteCount;
            var lastAsk = started.Conversation.Messages[^1].Body;

            var explained = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "what does that mean?",
                        started.Conversation.Id
                    )
                )
            );

            var body = explained.Conversation.Messages[^1].Body;
            Assert.Equal("gap", explained.Conversation.Messages[^1].Class);
            Assert.NotEqual(lastAsk, body);
            Assert.Contains("end", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(afterGap, _fake.CompleteCount);
            Assert.NotNull(await StoredGapStateOrNullAsync(started.Conversation.Id));
        }

        [Fact]
        public async Task SendTurn_ConfusedIDontKnow_DoesNotInventEndDate()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var explained = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "I don't know", started.Conversation.Id)
                )
            );

            Assert.Equal("gap", explained.Conversation.Messages[^1].Class);
            Assert.Contains(
                "will not pick an end date",
                explained.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            var terms = AssistantOfferPathTerms.FromJson(
                (await StoredGapStateAsync(started.Conversation.Id)).OfferTermsJson
            );
            Assert.Null(terms!.Validity);
        }

        [Fact]
        public async Task SendTurn_QuestionNamingLastGapAsk_IsConfused()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var explained = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "When should the offer end?",
                        started.Conversation.Id
                    )
                )
            );

            Assert.Equal("gap", explained.Conversation.Messages[^1].Class);
            Assert.NotEqual(
                AssistantGapAsk.EndDateAsk,
                explained.Conversation.Messages[^1].Body
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_RetrieveDuringEndDateGap_KeepsGapThenFillPersists()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Guests",
                "You had 42 guests last week.",
                AssistantTask.Retrieve
            );

            var retrieved = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "How many guests came last week?",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Contains("42 guests", retrieved.Conversation.Messages[^1].Body);
            Assert.Equal(
                AssistantGapTurn.KindOfferTerms,
                (await StoredGapStateAsync(started.Conversation.Id)).Kind
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "30 days after they get it",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Single(_context.Campaigns);
            Assert.Single(_context.CatalogOffers);
            Assert.Contains(
                answered.Conversation.Messages[^1].Actions.Select(action => action.Type),
                type => type == "review-campaign"
            );
        }

        [Fact]
        public async Task SendTurn_RefuseHowToSend_KeepsEndDateGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var refused = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "How do I send a campaign?",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Equal("refusal", refused.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(
                AssistantGapTurn.KindOfferTerms,
                (await StoredGapStateAsync(started.Conversation.Id)).Kind
            );
        }

        [Fact]
        public async Task SendTurn_NewCreateDuringEndDateGap_DropsAndRunsRecovery()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var replaced = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Draft a recovery",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Contains(
                AssistantGapAsk.PreviousDraftDropped,
                replaced.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.NotEqual(
                AssistantGapTurn.KindOfferTerms,
                (await StoredGapStateOrNullAsync(started.Conversation.Id))?.Kind
            );
        }

        [Fact]
        public async Task SendTurn_CancelDuringEndDateGap_DropsWithoutCreate()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, HappyPathCampaignWithOfferAsk)
                )
            );

            var cancelled = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "cancel the draft", started.Conversation.Id)
                )
            );
            Assert.Equal("grounded", cancelled.Conversation.Messages[^1].Class);
            Assert.Null(await StoredGapStateOrNullAsync(started.Conversation.Id));
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_OfferDraft_AsksWhatGuestsGet()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create an offer draft")
                )
            );
            Assert.Equal(AssistantGapAsk.TypeAsk, outcome.Conversation.Messages[^1].Body);
            Assert.DoesNotContain(
                "validity",
                outcome.Conversation.Messages[^1].Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SendTurn_PercentOffWithoutNumber_AsksHowMuchOff()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create a percent off offer")
                )
            );
            Assert.Equal(AssistantGapAsk.PercentValueAsk, outcome.Conversation.Messages[^1].Body);
            Assert.DoesNotContain(
                "value",
                outcome.Conversation.Messages[^1].Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SendTurn_FreeItemOmitsPurchase_AsksWhetherGuestsMustBuy()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create a free coffee offer")
                )
            );
            Assert.Equal(
                AssistantGapAsk.RequiredUsageAsk,
                outcome.Conversation.Messages[^1].Body
            );
        }

        [Fact]
        public async Task SendTurn_AttachWithoutPlacement_AsksThankYouPage()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Create a 25% Offer valid 14 days and attach it"
                    )
                )
            );
            Assert.Equal(AssistantGapAsk.PlacementAsk, outcome.Conversation.Messages[^1].Body);
        }

        [Fact]
        public async Task SendTurn_ConflictingBenefits_AsksWhichToKeep()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Create a 25% Offer and a free dessert"
                    )
                )
            );
            var body = outcome.Conversation.Messages[^1].Body;
            Assert.StartsWith(AssistantGapAsk.ConflictAskPrefix, body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_EmailAndSms_EmailFillsChannelGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Draft an Email and SMS Campaign to bring back eligible guests at Camden"
                    )
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);
            Assert.Equal(AssistantGapAsk.ChannelAsk, started.Conversation.Messages[^1].Body);
            Assert.DoesNotContain(
                "Reply with one exact label",
                started.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );

            var filled = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "email", started.Conversation.Id)
                )
            );
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("email", campaign.Channel);
            Assert.Equal("grounded", filled.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_AllLocationsUnnamed_AsksWhichVenue()
        {
            await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    AllSendRequest(
                        "Draft an Email Campaign to bring back all currently Email-eligible guests"
                    )
                )
            );
            Assert.Equal(
                AssistantGapAsk.ForLocation("Campaign Draft"),
                outcome.Conversation.Messages[^1].Body
            );
            Assert.DoesNotContain("Soho", outcome.Conversation.Messages[^1].Body);
        }

        [Fact]
        public async Task SendTurn_LocationGap_TheSohoSiteFillsUniqueVenue()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var soho = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    AllSendRequest(
                        "Draft an Email Campaign to bring back all currently Email-eligible guests"
                    )
                )
            );

            var filled = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    AllSendRequest("the Soho site", started.Conversation.Id)
                )
            );
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal(soho, campaign.RestaurantLocationId);
            Assert.Equal("grounded", filled.Conversation.Messages[^1].Class);
            Assert.NotEqual(camden, campaign.RestaurantLocationId);
        }

        [Fact]
        public async Task SendTurn_TwoMatchingOffers_ExplainsAndDoesNotDemandExactLabel()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            await SeedCatalogOfferAsync(locationId, "Lunch treat");

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        CanonicalCamdenEmailWinBackAsk + " with Weekend brunch and Lunch treat"
                    )
                )
            );
            var body = outcome.Conversation.Messages[^1].Body;
            Assert.Equal("gap", outcome.Conversation.Messages[^1].Class);
            Assert.Contains("Weekend brunch", body, StringComparison.Ordinal);
            Assert.Contains("Lunch treat", body, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Reply with one exact label",
                body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_CreateTargetGap_UsesCampaignOfferOrRecovery()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "help me draft something")
                )
            );
            Assert.Contains(
                AssistantGapAsk.CreateTargetAskPrefix,
                outcome.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Contains("Campaign", outcome.Conversation.Messages[^1].Body);
            Assert.Contains("Offer", outcome.Conversation.Messages[^1].Body);
            Assert.Contains("Feedback recovery", outcome.Conversation.Messages[^1].Body);
        }

        [Fact]
        public async Task SendTurn_TwoFeedbacks_AsksWhichFeedbackToRecover()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Alex Guest"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-1),
                guestName: "Sam Guest"
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Draft a recovery")
                )
            );
            Assert.Equal("gap", outcome.Conversation.Messages[^1].Class);
            Assert.Contains(
                AssistantGapAsk.FeedbackAskPrefix,
                outcome.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "Reply with one exact label",
                outcome.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_AdvisoryHealth_ShortHistory_PersistsAdvisoryGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 41, "Advisory Venue");
            var snapshot = new RestaurantContextSnapshot(
                "2026-09-05",
                new SingleLocation(locationId.ToString()),
                new PeriodWindow(new DateOnly(2026, 8, 7), new DateOnly(2026, 9, 5)),
                new PeriodWindow(new DateOnly(2026, 7, 8), new DateOnly(2026, 8, 6)),
                new AccountSection(
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    []
                ),
                new CampaignsSection([], [], []),
                new OffersSection([], [], []),
                new FeedbackSection(
                    new MetricPoint(0m, null, null),
                    [],
                    [],
                    0,
                    []
                ),
                new GuestsSection(
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    []
                ),
                new CaptureSection(
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    new MetricPoint(0m, null, null),
                    null
                ),
                new RecentActionsSection([]),
                new SnapshotMeta(IsNewAccount: true, TotalDaysOfHistory: 5, [])
            );
            var service = CreateConversationService(
                restaurantContextSnapshot: new FixedRestaurantContextSnapshot(snapshot)
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 41,
                    FirstSendRequest(locationId, "How are we doing this month?")
                )
            );

            Assert.Equal("gap", outcome.Conversation.Messages[^1].Class);
            var gapState = await StoredGapStateOrNullAsync(outcome.Conversation.Id);
            Assert.NotNull(gapState);
            Assert.Equal(AssistantGapTurn.GapKindAdvisory, gapState!.GapKind);
            Assert.Equal(AssistantGapTurn.KindAdvisoryData, gapState.Kind);
            Assert.Equal(
                nameof(AdvisoryGapReason.InsufficientData),
                gapState.AdvisoryReason
            );
        }

        private sealed class FixedRestaurantContextSnapshot : IRestaurantContextSnapshotService
        {
            private readonly RestaurantContextSnapshot _snapshot;

            public FixedRestaurantContextSnapshot(RestaurantContextSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public Task<RestaurantContextSnapshot> BuildAsync(
                int ownerUserId,
                LocationScope scope,
                PeriodWindow? currentOverride,
                PeriodWindow? comparisonOverride,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(_snapshot);
        }

        private async Task<AssistantGapState?> StoredGapStateOrNullAsync(int conversationId)
        {
            var row = await _context.AssistantConversations.SingleAsync(
                conversation => conversation.Id == conversationId
            );
            return AssistantGapTurn.Parse(row.DraftInterviewJson);
        }
    }
}
