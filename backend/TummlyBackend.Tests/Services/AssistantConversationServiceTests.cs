using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.DTOs.Assistant;
using TummlyBackend.DTOs.Campaigns;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.DTOs.OperatorHome;
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
        private readonly ControllableFeedbackRetrieve _retrieve;
        private readonly ControllableOffersRetrieve _offersRetrieve;
        private readonly ControllableCampaignsRetrieve _campaignsRetrieve;
        private readonly ControllableCaptureRetrieve _captureRetrieve;
        private readonly ControllableHomeKpiRetrieve _homeRetrieve;
        private readonly ControllableGuestsRetrieve _guestsRetrieve;
        private readonly RecordingAssistantProgressPublisher _progress;
        private readonly FakeCampaignMessageDraftProvider _messageDrafts;
        private readonly FakeFeedbackRecoveryDraftProvider _recoveryDrafts;
        private readonly ControllableHomeRecommendation _homeRecommendation;
        private readonly ControllableWeeklyBriefGenerate _weeklyBriefGenerate;
        private readonly AssistantConversationService _service;

        public AssistantConversationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _fake = new FakeAssistantLiveAnswerProvider();
            _retrieve = new ControllableFeedbackRetrieve(
                new AssistantFeedbackRetrieve(_context)
            );
            _offersRetrieve = new ControllableOffersRetrieve(
                new AssistantOffersRetrieve(_context, new OffersMetricsService(_context))
            );
            _campaignsRetrieve = new ControllableCampaignsRetrieve(
                new AssistantCampaignsRetrieve(
                    _context,
                    new CampaignsSummaryService(_context),
                    new CampaignEligibilityService(_context)
                )
            );
            _captureRetrieve = new ControllableCaptureRetrieve(
                new AssistantCaptureRetrieve(
                    _context,
                    new CaptureWindowedEngagementAggregate(_context)
                )
            );
            _homeRetrieve = new ControllableHomeKpiRetrieve(
                new AssistantHomeKpiRetrieve(_context)
            );
            _guestsRetrieve = new ControllableGuestsRetrieve(
                new AssistantGuestsRetrieve(_context)
            );
            _progress = new RecordingAssistantProgressPublisher();
            _messageDrafts = new FakeCampaignMessageDraftProvider();
            _recoveryDrafts = new FakeFeedbackRecoveryDraftProvider();
            _homeRecommendation = new ControllableHomeRecommendation();
            _weeklyBriefGenerate = new ControllableWeeklyBriefGenerate();
            _service = CreateConversationService();
        }

        private AssistantConversationService CreateConversationService(
            ICampaignDraftService? campaignDrafts = null,
            ICampaignEligibilityService? eligibility = null,
            IOffersCatalogService? offersCatalog = null,
            TimeProvider? timeProvider = null
        )
        {
            var catalog = offersCatalog ?? new OffersCatalogService(_context);
            return new(
                _context,
                new OwnedLocationService(_context),
                _fake,
                _retrieve,
                _offersRetrieve,
                _campaignsRetrieve,
                _captureRetrieve,
                _homeRetrieve,
                _guestsRetrieve,
                _progress,
                campaignDrafts
                    ?? new CampaignDraftService(
                        _context,
                        new CampaignTemplateCatalogueService(),
                        catalog
                    ),
                eligibility ?? new CampaignEligibilityService(_context),
                new CampaignMessageDraftService(_messageDrafts),
                catalog,
                new FeedbackRecoveryDraftsService(_context, _recoveryDrafts),
                new AssistantAttentionRetrieve(
                    _context,
                    new FeedbackInboxListService(_context),
                    new CampaignsListService(_context),
                    catalog,
                    new EmptyOfferVoidRequestService(),
                    _homeRecommendation,
                    _weeklyBriefGenerate
                ),
                timeProvider
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
            Assert.Contains("Camden", ok.Conversation.Messages[1].Title);
            Assert.Contains("the last 7 days", ok.Conversation.Messages[1].Body);
            Assert.Contains("nothing to summarise", ok.Conversation.Messages[1].Body);
            Assert.Contains("Change Scope", ok.Conversation.Messages[1].Body);
            Assert.Empty(ok.Conversation.Messages[1].Actions);
            Assert.NotNull(_fake.LastInput);
            Assert.Equal("Summarise recent feedback", _fake.LastInput!.UserMessage);
            Assert.Equal("Camden", _fake.LastInput.OwnedLocationName);

            Assert.Equal(1, await _context.AssistantConversations.CountAsync());
            Assert.Equal(2, await _context.AssistantMessages.CountAsync());
        }

        [Fact]
        public async Task SendTurn_PublishesFullPipelineProgress_InOrder()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(
                ["checking", "retrieving", "preparing"],
                _progress.Events.Select(item => item.Step)
            );
            Assert.All(_progress.Events, item => Assert.Equal(7, item.UserId));
            Assert.Single(_progress.Events.Select(item => item.ConversationId).Distinct());
        }

        [Fact]
        public async Task SendTurn_DraftOnly_DoesNotPublishRetrieving()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Create a campaign draft")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.DoesNotContain(
                "retrieving",
                _progress.Events.Select(item => item.Step)
            );
            Assert.Contains(
                "checking",
                _progress.Events.Select(item => item.Step)
            );
        }

        [Fact]
        public async Task SendTurn_ProgressPublishFailure_DoesNotFailTurn()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _progress.ThrowOnPublish = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", Assert.IsType<AssistantTurnOutcome.Ok>(outcome)
                .Conversation.Messages[^1].Class);
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
        public async Task ApplyScope_AllOwnedLocations_PersistsSentinelAndNullId()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(
                ownerUserId: 7,
                "Shoreditch",
                captureStatus: CaptureLocationStatus.Paused
            );
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            var lastActivity = ok.Conversation.LastActivityAt;

            var applied = await _service.ApplyScopeAsync(
                ownerUserId: 7,
                ok.Conversation.Id,
                AllOwnedLocationsScopeRequest()
            );

            var after = Assert.IsType<AssistantTurnOutcome.Ok>(applied);
            Assert.Equal("all", after.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(after.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal("All Locations", after.Conversation.AnalysisScope.OwnedLocationName);
            Assert.Equal(lastActivity, after.Conversation.LastActivityAt);

            var stored = await _context.AssistantConversations
                .AsNoTracking()
                .SingleAsync(row => row.Id == ok.Conversation.Id);
            Assert.Equal("all", stored.ScopeKind);
            Assert.Null(stored.OwnedLocationId);

            var got = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, ok.Conversation.Id)
            );
            Assert.Equal("all", got.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(got.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task ApplyScope_AllOwnedLocations_WithZeroId_DoesNotStoreSentinelInteger()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;

            var applied = await _service.ApplyScopeAsync(
                ownerUserId: 7,
                conversationId,
                new ApplyAssistantScopeRequest
                {
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        ScopeKind = "all",
                        OwnedLocationId = 0,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "last7",
                        },
                    },
                }
            );

            var after = Assert.IsType<AssistantTurnOutcome.Ok>(applied);
            Assert.Equal("all", after.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(after.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_SnapshotsNullIdAndDoesNotReadUnowned()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(
                ownerUserId: 7,
                "Shoreditch",
                captureStatus: CaptureLocationStatus.Paused
            );
            var other = await SeedLocationAsync(ownerUserId: 99, "Soho");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;

            await _service.ApplyScopeAsync(
                ownerUserId: 7,
                conversationId,
                AllOwnedLocationsScopeRequest()
            );

            _retrieve.Calls.Clear();
            _offersRetrieve.Calls.Clear();
            _campaignsRetrieve.Calls.Clear();
            _captureRetrieve.Calls.Clear();
            _homeRetrieve.Calls.Clear();
            _guestsRetrieve.Calls.Clear();

            var continued = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = conversationId,
                    Message = "Summarise recent feedback",
                    AnalysisScope = AllOwnedLocationsScope(),
                }
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(continued);
            var lastUser = ok.Conversation.Messages
                .Last(message => message.Role == "user");
            Assert.Equal("all", lastUser.AnalysisScope?.ScopeKind);
            Assert.Null(lastUser.AnalysisScope?.OwnedLocationId);
            Assert.Equal("All Locations", lastUser.AnalysisScope?.OwnedLocationName);
            Assert.Equal("All Locations", ok.Conversation.AnalysisScope.OwnedLocationName);
            Assert.DoesNotContain(
                _retrieve.Calls,
                call => call.OwnedLocationId == other
            );

            var denied = await _service.SendTurnAsync(
                ownerUserId: 99,
                new SendAssistantTurnRequest
                {
                    ConversationId = conversationId,
                    Message = "Summarise recent feedback",
                    AnalysisScope = AllOwnedLocationsScope(),
                }
            );
            Assert.IsType<AssistantTurnOutcome.NotFound>(denied);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_NamedOwnedVenue_IsNotMentionCaveat()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;
            await _service.ApplyScopeAsync(
                ownerUserId: 7,
                conversationId,
                AllOwnedLocationsScopeRequest()
            );

            var continued = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = conversationId,
                    Message = "How is Shoreditch doing?",
                    AnalysisScope = AllOwnedLocationsScope(),
                }
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(continued);
            Assert.DoesNotContain(
                "not a Compare turn",
                ok.Conversation.Messages[^1].Body
            );
            Assert.Equal("All Locations", ok.Conversation.AnalysisScope.OwnedLocationName);
        }

        [Fact]
        public async Task SendTurn_UnnamedCreateWhenAllSaved_IsLocationGapWithoutPersist()
        {
            await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(
                    "Draft an Email Campaign to bring back all currently Email-eligible guests"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Equal(
                "Which Owned location should this Campaign Draft use? Name one.",
                answer.Body
            );
            Assert.DoesNotContain("Soho", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal("all", ok.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task SendTurn_EverywhereCreateWhenAllSaved_IsLocationGap()
        {
            await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest("Create a campaign everywhere")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Name one", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_UniqueNamedCreateWhenAllSaved_PersistsAtVenueAndKeepsAll()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedLinkedGuestAsync(
                camden,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Equal("all", ok.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(ok.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal("All Locations", ok.Conversation.AnalysisScope.OwnedLocationName);
            Assert.Equal(1, await _context.Campaigns.CountAsync());
            Assert.Equal(camden, _context.Campaigns.Single().RestaurantLocationId);
        }

        [Fact]
        public async Task SendTurn_LocationGapAnswerWhenAllSaved_DoesNotUpdateScope()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedLinkedGuestAsync(
                camden,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    AllSendRequest(
                        "Draft an Email Campaign to bring back all currently Email-eligible guests"
                    )
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var completed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    AllSendRequest("Camden", started.Conversation.Id)
                )
            );

            Assert.Equal("grounded", completed.Conversation.Messages[^1].Class);
            Assert.Contains("Camden", completed.Conversation.Messages[^1].Body);
            Assert.Equal("all", completed.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(completed.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal(camden, _context.Campaigns.Single().RestaurantLocationId);
        }

        [Fact]
        public async Task ApplyScope_DuringLocationGapWhenAllSaved_DoesNotPersist()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    AllSendRequest(
                        "Draft an Email Campaign to bring back all currently Email-eligible guests"
                    )
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var applied = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.ApplyScopeAsync(
                    ownerUserId: 7,
                    started.Conversation.Id,
                    new ApplyAssistantScopeRequest
                    {
                        AnalysisScope = new AssistantAnalysisScopeDto
                        {
                            OwnedLocationId = camden,
                            OwnedLocationName = "Camden",
                            ReportingPeriod = new AssistantReportingPeriodDto
                            {
                                Kind = "preset",
                                PresetId = "last7",
                            },
                        },
                    }
                )
            );

            Assert.Equal(camden, applied.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal("gap", applied.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_RecoveryWhenAllSaved_TwoVenues_IsFeedbackGapWithVenueLabels()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var soho = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedFeedbackAsync(
                camden,
                DateTime.UtcNow.AddHours(-5),
                guestName: "Pat Guest"
            );
            await SeedFeedbackAsync(
                soho,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Alex Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest("Draft a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Pat Guest", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Alex Guest", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Soho", answer.Body, StringComparison.Ordinal);
            Assert.Contains(" · ", answer.Body, StringComparison.Ordinal);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Equal("all", ok.Conversation.AnalysisScope.ScopeKind);
        }

        [Fact]
        public async Task SendTurn_LatestNegativeOnThisLocationWhenAllSaved_BindsNewestAcrossVenues()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var soho = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedFeedbackAsync(
                camden,
                DateTime.UtcNow.AddHours(-5),
                guestName: "Pat Guest"
            );
            await SeedFeedbackAsync(
                soho,
                DateTime.UtcNow.AddHours(-1),
                guestName: "Alex Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(
                    "Draft a recovery response for the last negative feedback on this location"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.NotNull(ok.Conversation.PendingRecoveryDraft);
            var alex = await _context.Feedbacks.SingleAsync(
                row => row.GuestName == "Alex Guest"
            );
            Assert.Equal(alex.Id, ok.Conversation.PendingRecoveryDraft!.FeedbackId);
            Assert.Equal(soho, ok.Conversation.PendingRecoveryDraft.LocationId);
            Assert.Equal("all", ok.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task SendTurn_RecoveryOfferWhenAllSaved_BindsOfferAtBoundVenue()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            var guestId = await SeedLocationGuestAsync(
                camden,
                DateTime.UtcNow.AddHours(-2)
            );
            await SeedFeedbackAsync(
                camden,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest",
                locationGuestId: guestId
            );
            var offerId = await SeedCatalogOfferAsync(camden, "Weekend brunch");
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(
                    "Prepare a recovery response with a recovery offer. Weekend brunch"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            Assert.Equal(offerId, ok.Conversation.PendingRecoveryDraft!.OfferId);
            Assert.Equal(camden, ok.Conversation.PendingRecoveryDraft.LocationId);
        }

        [Fact]
        public async Task SendTurn_RecoveryUnionReadFailWhenAllSaved_IsFailureNotSubset()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedFeedbackAsync(
                camden,
                DateTime.UtcNow.AddHours(-1),
                guestName: "Pat Guest"
            );
            _retrieve.FailNext = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest("Draft a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[^1].Class);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
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
        public async Task List_ReturnsOnlyOwnerRecentThreads_NotFilteredByScope()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var shoreditch = await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var otherLocation = await SeedLocationAsync(ownerUserId: 99, "Soho");

            await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Camden ask")
            );
            await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(shoreditch, "Shoreditch ask")
            );
            await _service.SendTurnAsync(
                ownerUserId: 99,
                FirstSendRequest(otherLocation, "Other operator ask")
            );

            var recent = Assert.IsType<AssistantListOutcome.Ok>(
                await _service.ListAsync(ownerUserId: 7, archived: false)
            );
            Assert.Equal(2, recent.Conversations.Count);
            Assert.Equal(
                new[] { "Shoreditch ask", "Camden ask" },
                recent.Conversations.Select(row => row.Title).ToArray()
            );
            Assert.DoesNotContain(
                recent.Conversations,
                row => row.Title == "Other operator ask"
            );

            var otherRecent = Assert.IsType<AssistantListOutcome.Ok>(
                await _service.ListAsync(ownerUserId: 99, archived: false)
            );
            Assert.Single(otherRecent.Conversations);
            Assert.Equal("Other operator ask", otherRecent.Conversations[0].Title);
        }

        [Fact]
        public async Task Archive_HidesFromRecent_WithoutChangingLastActivity()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversation = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation;
            var lastActivity = conversation.LastActivityAt;

            await Task.Delay(20);
            var archived = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SetArchivedAsync(7, conversation.Id, archived: true)
            );
            Assert.True(archived.Conversation.IsArchived);
            Assert.Equal(lastActivity, archived.Conversation.LastActivityAt);

            var recent = Assert.IsType<AssistantListOutcome.Ok>(
                await _service.ListAsync(7, archived: false)
            );
            var archive = Assert.IsType<AssistantListOutcome.Ok>(
                await _service.ListAsync(7, archived: true)
            );
            Assert.Empty(recent.Conversations);
            Assert.Single(archive.Conversations);
            Assert.Equal(conversation.Id, archive.Conversations[0].Id);

            var unarchived = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SetArchivedAsync(7, conversation.Id, archived: false)
            );
            Assert.False(unarchived.Conversation.IsArchived);
            Assert.Equal(lastActivity, unarchived.Conversation.LastActivityAt);
        }

        [Fact]
        public async Task Delete_HardDeletesConversationAndMessages_LeavesLinkedRecords()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;

            var campaign = new Campaign
            {
                Name = "Keep me",
                Status = "draft",
                RestaurantLocationId = locationId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            var campaignId = campaign.Id;

            var deleted = await _service.DeleteAsync(7, conversationId);
            Assert.IsType<AssistantDeleteOutcome.Ok>(deleted);

            Assert.Equal(0, await _context.AssistantConversations.CountAsync());
            Assert.Equal(0, await _context.AssistantMessages.CountAsync());
            Assert.Equal(1, await _context.Campaigns.CountAsync(row => row.Id == campaignId));
            Assert.True(await _context.RestaurantLocations.AnyAsync(row => row.Id == locationId));
        }

        [Fact]
        public async Task Get_RestoresSavedAnalysisScope()
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

            var loaded = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(7, conversationId)
            );
            Assert.Equal(second, loaded.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal("Shoreditch", loaded.Conversation.AnalysisScope.OwnedLocationName);
            Assert.Equal("thisMonth", loaded.Conversation.AnalysisScope.ReportingPeriod.PresetId);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_ForAnotherOperator()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;

            var outcome = await _service.DeleteAsync(99, conversationId);

            Assert.IsType<AssistantDeleteOutcome.NotFound>(outcome);
            Assert.Equal(1, await _context.AssistantConversations.CountAsync());
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

        [Fact]
        public async Task SendTurn_GroundsOnFeedback_AndOffersFeedbackActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Negative,
                "[\"WaitTime\"]",
                FeedbackWorkflowStatus.New
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Camden", answer.Title);
            Assert.Contains("the last 7 days", answer.Body);
            Assert.Contains("1 feedback item", answer.Body);
            Assert.DoesNotContain("this week", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(answer.Actions, action => action.Type == "view-feedback-set");
            Assert.Contains(answer.Actions, action => action.Type == "prepare-recovery");
            Assert.Equal("View 1 feedback item", answer.Actions[0].Label);
            Assert.NotNull(_fake.LastInput);
            Assert.Equal(1, _fake.LastInput!.Evidence.Feedback.TotalCount);
        }

        [Fact]
        public async Task SendTurn_EmptyRetrieve_IsGroundedEmpty_WithNoActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Camden", answer.Title);
            Assert.Contains("the last 7 days", answer.Body);
            Assert.Contains("nothing to summarise", answer.Body);
            Assert.Contains("Change Scope", answer.Body);
            Assert.Empty(answer.Actions);
        }

        [Fact]
        public async Task SendTurn_MutateAsk_IsRefusal_WithNoActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Send an email to these guests")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("refusal", answer.Class);
            Assert.Null(answer.Title);
            Assert.Contains("cannot create, send, or change records", answer.Body);
            Assert.Empty(answer.Actions);
        }

        [Theory]
        [InlineData("Schedule a campaign")]
        [InlineData("Issue an offer")]
        [InlineData("Change the status")]
        [InlineData("Create a report")]
        [InlineData("How do I use the dashboard?")]
        [InlineData("Delete the guest record")]
        public async Task SendTurn_OtherWritesReportsAndHelp_StayRefused(string message)
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, message)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.Null(answer.Title);
            Assert.Empty(answer.Actions);
            Assert.False(ok.Conversation.DraftInterviewActive);
        }

        [Fact]
        public async Task SendTurn_WhatCanYouDo_IsCapabilitiesRetrieve_SkipsGets_AndDebits()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What can you do")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(AssistantProductExpertCopy.CapabilitiesTitle, answer.Title);
            Assert.Equal(AssistantProductExpertCopy.CapabilitiesBody, answer.Body);
            Assert.Contains("## Read", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Create Campaign with Offer", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(
                AssistantProductExpertCopy.CapabilitiesConversationTitle,
                ok.Conversation.Title
            );
            AssertNoRetrieveGets();
            Assert.NotNull(_fake.LastInput);
            Assert.DoesNotContain("retrieving", _progress.Events.Select(item => item.Step));
        }

        [Fact]
        public async Task SendTurn_WhatCanYouDraft_StaysCreateTargetGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "what can you draft")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Campaign", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Offer", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Feedback recovery", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain(
                AssistantProductExpertCopy.CapabilitiesTitle,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(answer.Actions);
            AssertNoRetrieveGets();
        }

        [Theory]
        [InlineData("support")]
        [InlineData("contact support")]
        [InlineData("raise a ticket")]
        [InlineData("How do I create a campaign?")]
        public async Task SendTurn_HelpCentreAndSupport_RefuseBeforeProductExpert(
            string message
        )
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, message)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.Equal(AssistantLiveAnswerCopy.HelpCentreRefusalBody, answer.Body);
            Assert.Empty(answer.Actions);
            Assert.DoesNotContain(
                "Create Campaign with Offer",
                answer.Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_HelpCentrePlusCapabilities_StillRefuses()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "help centre — what can you do")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.Equal(AssistantLiveAnswerCopy.HelpCentreRefusalBody, answer.Body);
        }

        [Fact]
        public async Task SendTurn_MultiTopicProduct_ConcatenatesInOrder_SkipsGets()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "campaign vs offer, campaign status, analysis scope, "
                    + "draft vs send, what can you do"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(AssistantProductExpertCopy.MultiTopicTitle, answer.Title);
            Assert.Equal("Product facts", ok.Conversation.Title);
            Assert.Empty(answer.Actions);
            Assert.Equal(
                AssistantProductExpertCopy.AnalysisScopeBody
                    + "\n\n"
                    + AssistantProductExpertCopy.CampaignVsOfferBody
                    + "\n\n"
                    + AssistantProductExpertCopy.StatusesBody
                    + "\n\n"
                    + AssistantProductExpertCopy.DraftVsSendBody
                    + "\n\n"
                    + AssistantProductExpertCopy.CapabilitiesBody,
                answer.Body
            );
            AssertNoRetrieveGets();
        }

        [Fact]
        public async Task SendTurn_MixedRetrieveAndProduct_AppendsCannedKeepsRestaurantActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Negative,
                "[\"WaitTime\"]",
                FeedbackWorkflowStatus.New
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback and campaign vs offer"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Camden", answer.Title);
            Assert.NotEqual(
                AssistantProductExpertCopy.CampaignVsOfferTitle,
                answer.Title
            );
            Assert.Contains("1 feedback item", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                "\n\n" + AssistantProductExpertCopy.CampaignVsOfferBody,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(answer.Actions, action => action.Type == "view-feedback-set");
            Assert.NotEmpty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_CanYouSchedule_WithoutGuard_StaysSendScheduleRefuse()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "can you schedule")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.DoesNotContain(
                AssistantProductExpertCopy.DraftVsSendTitle,
                answer.Title ?? string.Empty,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                AssistantProductExpertCopy.DraftVsSendBody,
                answer.Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_DoesTheAssistantSend_IsDraftVsSendProductExpert()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "does the assistant send")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(AssistantProductExpertCopy.DraftVsSendTitle, answer.Title);
            Assert.Equal(AssistantProductExpertCopy.DraftVsSendBody, answer.Body);
            Assert.Empty(answer.Actions);
            Assert.Equal("Draft vs send", ok.Conversation.Title);
            AssertNoRetrieveGets();
        }

        [Theory]
        [InlineData("campaign vs offer", "Campaign vs Offer")]
        [InlineData("campaign versus offer", "Campaign vs Offer")]
        [InlineData("difference between campaign and offer", "Campaign vs Offer")]
        [InlineData("all owned locations", "Analysis scope")]
        [InlineData("draft vs send", "Draft vs send")]
        public async Task SendTurn_ProductExpertNeedles_OnMultiLocation_SkipCompareClarify(
            string message,
            string expectedTitle
        )
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, message)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.NotEqual("clarify", answer.Class);
            Assert.Equal(expectedTitle, answer.Title);
            Assert.Empty(answer.Actions);
            AssertNoRetrieveGets();
        }

        [Fact]
        public async Task SendTurn_MixedRetrieveAndProduct_OnMultiLocation_DoesNotClarify()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedFeedbackAsync(
                camden,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Negative,
                "[\"WaitTime\"]",
                FeedbackWorkflowStatus.New
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    camden,
                    "Summarise recent feedback and campaign vs offer"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.NotEqual("clarify", answer.Class);
            Assert.Contains("1 feedback item", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                "\n\n" + AssistantProductExpertCopy.CampaignVsOfferBody,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(answer.Actions, action => action.Type == "view-feedback-set");
        }

        [Fact]
        public async Task SendTurn_HelpCentrePlusProduct_OnMultiLocation_StillRefuses()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "help centre — campaign vs offer")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.Equal(AssistantLiveAnswerCopy.HelpCentreRefusalBody, answer.Body);
        }

        [Fact]
        public async Task SendTurn_ForcedCreateTask_OnWhatCanYouDo_StaysProductExpertRetrieve()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Campaign Draft",
                "Create Campaign Draft.",
                AssistantTask.CreateCampaignDraft
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What can you do")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(AssistantProductExpertCopy.CapabilitiesTitle, answer.Title);
            Assert.Equal(AssistantProductExpertCopy.CapabilitiesBody, answer.Body);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        private const string CanonicalCamdenEmailWinBackAsk =
            "Draft an Email Campaign to bring back all currently Email-eligible guests at Camden";

        private const string CanonicalCamdenCampaignWithOfferAsk =
            "Create a campaign with 10% off valid 30 days after issue at Camden";

        private const string CanonicalGeneratedConversationTitle =
            "Bring back Email-eligible guests";

        [Fact]
        public async Task SendTurn_CanonicalCamdenEmailWinBack_PersistsDraftAndReviewAction()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, ok.Conversation.Messages.Count);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.DoesNotContain("What should this Campaign be called", answer.Body);
            Assert.DoesNotContain("Campaign goal catalogue", answer.Body);
            Assert.DoesNotContain("Audience catalogue", answer.Body);
            Assert.Contains("Camden", answer.Body);
            Assert.Contains("Email", answer.Body);
            Assert.Contains("All eligible guests", answer.Body);
            Assert.Contains("1 Email-eligible", answer.Body);
            Assert.Contains("No Offer", answer.Body);
            Assert.Contains("Bring back Email-eligible guests at Camden", answer.Body);
            Assert.Contains("Draft", answer.Body);
            Assert.Contains("Nothing was sent or scheduled", answer.Body);
            Assert.Null(ok.Conversation.PendingCampaignDraft);
            Assert.False(ok.Conversation.DraftInterviewActive);

            Assert.Equal(
                new[] { "review-campaign", "change-audience", "add-offer" },
                answer.Actions.Select(action => action.Type)
            );
            Assert.Equal("Review campaign draft", answer.Actions[0].Label);
            Assert.Equal("Change audience", answer.Actions[1].Label);
            Assert.Equal("Add Offer", answer.Actions[2].Label);
            Assert.All(answer.Actions, action => Assert.NotNull(action.CampaignId));
            var action = answer.Actions[0];

            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("draft", campaign.Status);
            Assert.Equal(locationId, campaign.RestaurantLocationId);
            Assert.Equal("email", campaign.Channel);
            Assert.Equal("all-eligible-guests", campaign.AudienceKey);
            Assert.Equal("re-engage-inactive", campaign.GoalId);
            Assert.Equal("no-offer", campaign.OfferStance);
            Assert.Null(campaign.OfferId);
            Assert.Empty(_context.CatalogOffers);
            Assert.Equal("Bring back Email-eligible guests at Camden", campaign.Name);
            Assert.Equal(action.CampaignId, campaign.Id);
            Assert.Equal(campaign.Id, _context.AssistantConversations.Single().CreatedCampaignId);

            Assert.NotNull(_fake.LastInput);
            Assert.Equal(CanonicalCamdenEmailWinBackAsk, _fake.LastInput!.UserMessage);
            Assert.Equal(
                AssistantTask.CreateCampaignDraft,
                AssistantTaskClassification.Classify(
                    CanonicalCamdenEmailWinBackAsk
                )
            );
            Assert.Equal(
                AssistantTask.CreateCampaignDraft,
                AssistantTaskClassification.Classify("create a campaign")
            );
            Assert.Equal(
                AssistantTask.Refuse,
                AssistantTaskClassification.Classify(
                    "How do I create a campaign?"
                )
            );

            var resumed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, ok.Conversation.Id)
            );
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "add-offer" },
                resumed.Conversation.Messages[^1].Actions.Select(item => item.Type)
            );
            var resumeAction = resumed.Conversation.Messages[^1].Actions[0];
            Assert.Equal("Review campaign draft", resumeAction.Label);
            Assert.Equal(campaign.Id, resumeAction.CampaignId);
            Assert.Equal("draft", campaign.Status);
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_PersistsAttachAndCombinedAnswer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal("Campaign Draft saved with Offer", answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                "I saved a Campaign Draft with an attached Offer for Camden.",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Email", answer.Body, StringComparison.Ordinal);
            Assert.Contains("1 Email-eligible", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Percentage discount", answer.Body, StringComparison.Ordinal);
            Assert.Contains("10%", answer.Body, StringComparison.Ordinal);
            Assert.Contains("30 days after issue", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                "Active (attached to this Campaign Draft)",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("**Status:** Draft", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Nothing was sent or scheduled", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("create-new-offer", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("existing-offer", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("offerStance", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("What should this Campaign be called", answer.Body);

            Assert.Equal(
                new[] { "review-campaign", "change-audience", "review-offer" },
                answer.Actions.Select(action => action.Type)
            );
            Assert.Equal("Review campaign draft", answer.Actions[0].Label);
            Assert.Equal("Change audience", answer.Actions[1].Label);
            Assert.Equal("Review offer draft", answer.Actions[2].Label);
            Assert.DoesNotContain(answer.Actions, action => action.Type == "add-offer");

            var campaign = Assert.Single(_context.Campaigns);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal("draft", campaign.Status);
            Assert.Equal("create-new-offer", campaign.OfferStance);
            Assert.Equal(offer.Id, campaign.OfferId);
            Assert.Equal(CatalogOfferStatus.Active, offer.Status);
            Assert.Equal(campaign.Id, answer.Actions[0].CampaignId);
            Assert.Equal(offer.Id, answer.Actions[2].OfferId);
            Assert.Equal(campaign.Id, _context.AssistantConversations.Single().CreatedCampaignId);
            Assert.Equal(offer.Id, _context.AssistantConversations.Single().CreatedOfferId);
            Assert.Equal(
                AssistantTask.CreateCampaignWithOffer,
                AssistantTaskClassification.Classify(CanonicalCamdenCampaignWithOfferAsk)
            );
            Assert.Equal(
                CanonicalCamdenCampaignWithOfferAsk,
                _fake.LastInput!.UserMessage
            );

            var resumed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, ok.Conversation.Id)
            );
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "review-offer" },
                resumed.Conversation.Messages[^1].Actions.Select(item => item.Type)
            );
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_UniqueAttachableDraft_AttachesExisting()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var offerId = await SeedCatalogOfferAsync(
                locationId,
                "10% off",
                status: CatalogOfferStatus.Draft,
                discountPercentage: 10m
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(offerId, offer.Id);
            Assert.Equal("existing-offer", campaign.OfferStance);
            Assert.Equal(offerId, campaign.OfferId);
            Assert.Equal(CatalogOfferStatus.Active, offer.Status);
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "review-offer" },
                ok.Conversation.Messages[^1].Actions.Select(action => action.Type)
            );
            Assert.DoesNotContain(
                "create-new-offer",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_OfferCreateFails_PersistsNeither()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var failing = CreateConversationService(
                offersCatalog: new ThrowingOffersCatalogService()
            );

            var outcome = await failing.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Campaign Draft not saved", answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Offer create", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.Null(_context.AssistantConversations.Single().CreatedCampaignId);
            Assert.Null(_context.AssistantConversations.Single().CreatedOfferId);
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_CampaignCreateFails_KeepsUnattachedOfferDraft()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var failing = CreateConversationService(new ThrowingCampaignDraftService());

            var outcome = await failing.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Campaign Draft not saved", answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                "Campaign was not saved. The Campaign create step failed.",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "I could not save this Campaign with Offer",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Draft (not Active)", answer.Body, StringComparison.Ordinal);
            Assert.Contains("not attached", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Campaign create", answer.Body, StringComparison.Ordinal);
            var action = Assert.Single(answer.Actions);
            Assert.Equal("review-offer", action.Type);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(offer.Id, action.OfferId);
            Assert.Null(_context.AssistantConversations.Single().CreatedCampaignId);
            Assert.Equal(offer.Id, _context.AssistantConversations.Single().CreatedOfferId);
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_ExistingDraft_CampaignCreateFails_KeepsUnattachedOfferDraft()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var offerId = await SeedCatalogOfferAsync(
                locationId,
                "10% off",
                status: CatalogOfferStatus.Draft,
                discountPercentage: 10m
            );
            var failing = CreateConversationService(new ThrowingCampaignDraftService());

            var outcome = await failing.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Campaign Draft not saved", answer.Title);
            Assert.Contains(
                "Campaign was not saved. The Campaign create step failed.",
                answer.Body,
                StringComparison.Ordinal
            );
            var action = Assert.Single(answer.Actions);
            Assert.Equal("review-offer", action.Type);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(offerId, offer.Id);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(offerId, action.OfferId);
            Assert.Null(_context.AssistantConversations.Single().CreatedCampaignId);
            Assert.Equal(offerId, _context.AssistantConversations.Single().CreatedOfferId);
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_ZeroEligible_PersistsAndStatesZero()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_context.Campaigns);
            Assert.Single(_context.CatalogOffers);
            Assert.Contains(
                "0 Email-eligible",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "review-offer" },
                ok.Conversation.Messages[^1].Actions.Select(action => action.Type)
            );
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_MissingValidity_TermsGapThenPersists()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Create a campaign with 25% off at Camden"
                    )
                )
            );
            var gap = started.Conversation.Messages[^1];
            Assert.Equal("gap", gap.Class);
            Assert.Contains("validity", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "30 days after issue",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Single(_context.Campaigns);
            Assert.Single(_context.CatalogOffers);
            Assert.Equal(
                "Campaign Draft saved with Offer",
                answered.Conversation.Messages[^1].Title
            );
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_TwoMatchingOffers_IsGapTurn()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            await SeedCatalogOfferAsync(locationId, "Lunch treat");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a campaign with Weekend brunch and Lunch treat at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("gap", ok.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(2, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_UniqueExistingDraft_AttachUpdatesOnly()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );
            var campaignId = await SeedCampaignAsync(
                locationId,
                "Summer win-back",
                "draft",
                audienceKey: "all-eligible-guests"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a campaign with 10% off valid 30 days after issue and attach to Summer win-back campaign at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Campaign Draft saved with Offer", answer.Title);
            Assert.DoesNotContain("updated", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("replaced", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, await _context.Campaigns.CountAsync());
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            Assert.NotNull(campaign);
            Assert.Equal(campaignId, campaign!.Id);
            Assert.Equal("create-new-offer", campaign.OfferStance);
            Assert.NotNull(campaign.OfferId);
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_ScheduledNamedCampaign_PersistsNeither()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCampaignAsync(
                locationId,
                "Summer win-back",
                CampaignsListService.ScheduledStatus
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a campaign with 10% off valid 30 days after issue and attach to Summer win-back campaign at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Campaign Draft not saved", answer.Title);
            Assert.Contains("Attach from chat is not allowed", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Campaigns UI", answer.Body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_ZeroDraftMatch_CreatesNamedCampaign()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a campaign with 10% off valid 30 days after issue and attach to New launch campaign at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Contains("New launch", campaign.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("Campaign Draft saved with Offer", ok.Conversation.Messages[^1].Title);
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_TwoDraftTitleMatches_IsCampaignTitleGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCampaignAsync(locationId, "Summer win-back", "draft");
            await SeedCampaignAsync(locationId, "Summer win-back special", "draft");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a campaign with 10% off valid 30 days after issue and attach to Summer win-back campaign at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("gap", ok.Conversation.Messages[^1].Class);
            Assert.Contains(
                "Which Campaign Draft should this attach to",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.Equal(2, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_CombinedCreate_LocationAndTermsOpen_LocationGapFirst()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    camden,
                    "Create a campaign with 25% off for all locations"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Name one", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("validity", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_CanonicalCampaignWithOffer_RetrieveTask_DoesNotPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Feedback",
                "Retrieved facts.",
                AssistantTask.Retrieve
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Theory]
        [InlineData("Create a campaign")]
        [InlineData("Draft an offer")]
        [InlineData("Prepare a recovery response")]
        public async Task SendTurn_FormerDraftInterviewAsks_DoNotStartDraftInterview(
            string message
        )
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, message)
                )
            );

            var answer = outcome.Conversation.Messages[^1];
            Assert.DoesNotContain("Campaign goal catalogue", answer.Body);
            Assert.DoesNotContain("Offer type catalogue", answer.Body);
            Assert.DoesNotContain("Recovery intent catalogue", answer.Body);
            Assert.DoesNotContain(
                answer.Actions,
                action => action.Type is "draft-campaign" or "draft-offer"
            );
            Assert.False(outcome.Conversation.DraftInterviewActive);
            Assert.Null(outcome.Conversation.PendingCampaignDraft);
            Assert.Null(outcome.Conversation.PendingOfferDraft);
        }

        [Fact]
        public async Task GetAsync_LeftoverCampaignInterviewJson_DoesNotResumeInterview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var conversationId = await SeedConversationWithInterviewJsonAsync(
                ownerUserId: 7,
                locationId,
                "Camden",
                AssistantCampaignDraftInterview.Serialize(
                    new AssistantCampaignDraftState
                    {
                        Name = "Win back",
                        GoalId = "re-engage-inactive",
                        AudienceKey = "all-eligible-guests",
                        Channel = "email",
                        OfferStance = "no-offer",
                        UsefulOptionalsSkipped = true,
                    }
                )
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, conversationId)
            );

            Assert.False(outcome.Conversation.DraftInterviewActive);
            Assert.Null(outcome.Conversation.PendingCampaignDraft);
            Assert.Empty(outcome.Conversation.Messages);
        }

        [Fact]
        public async Task SendTurn_LeftoverInterviewJson_FollowsRetrieveNotInterview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            var conversationId = await SeedConversationWithInterviewJsonAsync(
                ownerUserId: 7,
                locationId,
                "Camden",
                AssistantRecoveryDraftInterview.Serialize(
                    new AssistantRecoveryDraftState()
                )
            );

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Show me Campaign drafts",
                        conversationId
                    )
                )
            );

            var answer = outcome.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.DoesNotContain("Recovery intent catalogue", answer.Body);
            Assert.DoesNotContain("Campaign goal catalogue", answer.Body);
            Assert.DoesNotContain(
                answer.Actions,
                action => action.Type is "draft-campaign" or "draft-offer"
            );
            Assert.False(outcome.Conversation.DraftInterviewActive);
            Assert.Null(outcome.Conversation.PendingCampaignDraft);
            Assert.Empty(_context.Campaigns);
        }

        [Fact]
        public async Task SendTurn_ModelUnavailable_DoesNotStartDraftInterview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.Fail();

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create a campaign")
                )
            );

            var answer = outcome.Conversation.Messages[^1];
            Assert.Equal("failure", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.False(outcome.Conversation.DraftInterviewActive);
            Assert.Empty(_context.Campaigns);
        }

        private const string CanonicalCamdenOfferPathAsk =
            "Create a 25% Offer valid 30 days after issue";

        [Fact]
        public async Task SendTurn_CanonicalOfferPath_PersistsStoredDraftAndReviewAction()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenOfferPathAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, ok.Conversation.Messages.Count);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Draft", answer.Body, StringComparison.Ordinal);
            Assert.Contains("not Active", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Contains("25%", answer.Body, StringComparison.Ordinal);
            Assert.Contains("30 days after issue", answer.Body, StringComparison.Ordinal);
            Assert.Contains("25% off", answer.Body, StringComparison.Ordinal);
            Assert.Contains("not attached", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Nothing was issued", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Nothing was sent", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Offer type catalogue", answer.Body, StringComparison.Ordinal);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Null(ok.Conversation.PendingOfferDraft);

            var action = Assert.Single(answer.Actions);
            Assert.Equal("review-offer", action.Type);
            Assert.Equal("Review offer draft", action.Label);
            Assert.NotNull(action.OfferId);

            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(locationId, offer.RestaurantLocationId);
            Assert.Equal(CatalogOfferType.PercentageDiscount, offer.OfferType);
            Assert.Equal(25m, offer.DiscountPercentage);
            Assert.Equal(CatalogOfferValidity.Days30AfterIssue, offer.Validity);
            Assert.Equal("25% off", offer.Title);
            Assert.Equal("Save 25%.", offer.Description);
            Assert.Null(offer.StaffInstructions);
            Assert.Null(offer.AdditionalExclusions);
            Assert.Equal(action.OfferId, offer.Id);
            Assert.Equal(offer.Id, _context.AssistantConversations.Single().CreatedOfferId);
            Assert.Empty(_context.Campaigns);

            var attachable = await new OffersCatalogService(_context).IsAttachableForLocationAsync(
                offer.Id,
                locationId
            );
            Assert.False(attachable);

            Assert.Equal(
                AssistantTask.OfferPath,
                AssistantTaskClassification.Classify(CanonicalCamdenOfferPathAsk)
            );

            var resumed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, ok.Conversation.Id)
            );
            var resumeAction = Assert.Single(resumed.Conversation.Messages[^1].Actions);
            Assert.Equal("review-offer", resumeAction.Type);
            Assert.Equal("Review offer draft", resumeAction.Label);
            Assert.Equal(offer.Id, resumeAction.OfferId);
        }

        private const string PackAi018OfferPathAsk =
            "Create a 25% Offer for these guests and make it valid everywhere until the end of the year";

        [Fact]
        public async Task SendTurn_PackAi018OfferPath_AsksLocationThenPersistsYearEnd()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, PackAi018OfferPathAsk)
                )
            );
            var gap = started.Conversation.Messages[^1];
            Assert.Equal("gap", gap.Class);
            Assert.Contains("Name one", gap.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Soho", gap.Body, StringComparison.Ordinal);
            Assert.Empty(gap.Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.Equal(0, await _context.Campaigns.CountAsync());

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, "Camden", started.Conversation.Id)
                )
            );
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(camden, offer.RestaurantLocationId);
            Assert.Equal(CatalogOfferValidity.ChooseExpiryDate, offer.Validity);
            Assert.Equal(new DateOnly(2026, 12, 31), offer.CustomExpiryDate);
            Assert.Equal("review-offer", Assert.Single(answer.Actions).Type);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_OfferPathMissingValidity_TermsGapThenPersists()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create a 25% Offer")
                )
            );
            var gap = started.Conversation.Messages[^1];
            Assert.Equal("gap", gap.Class);
            Assert.Contains("validity", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Offer type catalogue", gap.Body, StringComparison.Ordinal);
            Assert.Empty(gap.Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "30 days after issue", started.Conversation.Id)
                )
            );
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(25m, offer.DiscountPercentage);
            Assert.Equal(CatalogOfferValidity.Days30AfterIssue, offer.Validity);
            Assert.Equal("review-offer", Assert.Single(answered.Conversation.Messages[^1].Actions).Type);
        }

        [Fact]
        public async Task SendTurn_OfferPathYouChoose_DoesNotPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Create a standard offer")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.Contains("will not invent", answer.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_OfferPathConflictingBenefits_AsksWhichBenefit()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Create a 25% Offer and a free dessert")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("authorised benefit", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Which should I create", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Offer type catalogue", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_OfferPathConflictingBenefits_ThenNamesOne_PersistsDraft()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Create a 25% Offer and a free dessert valid 30 days after issue"
                    )
                )
            );
            var gap = started.Conversation.Messages[^1];
            Assert.Equal("gap", gap.Class);
            Assert.Contains("authorised benefit", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "25%", started.Conversation.Id)
                )
            );

            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Equal(CatalogOfferType.PercentageDiscount, offer.OfferType);
            Assert.Equal(25m, offer.DiscountPercentage);
            Assert.Null(offer.FreeItemText);
            Assert.Equal(CatalogOfferValidity.Days30AfterIssue, offer.Validity);
            Assert.Equal("review-offer", Assert.Single(answered.Conversation.Messages[^1].Actions).Type);
            Assert.DoesNotContain(
                "Offer create",
                answered.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_ForcedOfferPathIncompleteTerms_IsTermsGapNotPersistFailure()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Offers catalog Draft",
                "Offer path.",
                AssistantTask.OfferPath,
                "Create Offer Draft"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Give diners 25% off")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("validity", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Offer create", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_OfferPathCreateAndActivate_PersistsDraftAndRefusesActivate()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    CanonicalCamdenOfferPathAsk + " and activate it"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            var answer = ok.Conversation.Messages[^1];
            Assert.Contains("Draft", answer.Body, StringComparison.Ordinal);
            Assert.Contains("did not activate", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Draft only", answer.Body, StringComparison.Ordinal);
            Assert.Equal("review-offer", Assert.Single(answer.Actions).Type);
        }

        [Fact]
        public async Task SendTurn_SimilarActiveOffer_DoesNotBlockNewStoredDraft()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCatalogOfferAsync(locationId, "25% off your next visit");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenOfferPathAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, await _context.CatalogOffers.CountAsync());
            Assert.Equal(1, await _context.CatalogOffers.CountAsync(row => row.Status == CatalogOfferStatus.Draft));
            Assert.DoesNotContain(
                "did you mean",
                ok.Conversation.Messages[^1].Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.Equal(
                "review-offer",
                Assert.Single(ok.Conversation.Messages[^1].Actions).Type
            );
        }

        [Fact]
        public async Task SendTurn_OfferPathAudienceWording_DoesNotCreateCampaign()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a 25% Offer for these guests valid 30 days after issue"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            Assert.Single(_context.CatalogOffers);
            Assert.Empty(_context.Campaigns);
            Assert.Contains("not attached", ok.Conversation.Messages[^1].Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Nothing was issued", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Contains("Nothing was sent", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_WhatOffersAreActive_RetrievesAndDoesNotPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What Offers are Active?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.DoesNotContain(
                ok.Conversation.Messages[^1].Actions,
                action => action.Type == "review-offer"
            );
            Assert.Null(_context.AssistantConversations.Single().CreatedOfferId);
        }

        [Fact]
        public async Task SendTurn_OfferPersistFailure_NamesFailedStepAndDoesNotInventId()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Offers catalog Draft",
                "Offer path.",
                AssistantTask.OfferPath,
                "Create Offer Draft"
            );
            var failing = CreateConversationService(offersCatalog: new ThrowingOffersCatalogService());

            var outcome = await failing.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenOfferPathAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal(2, ok.Conversation.Messages.Count);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.DoesNotContain("review-offer", answer.Actions.Select(action => action.Type));
            Assert.Contains("Offer create", answer.Body);
            Assert.Contains("Change Scope", answer.Body);
            Assert.Null(ok.Conversation.PendingOfferDraft);
            Assert.Null(_context.AssistantConversations.Single().CreatedOfferId);
            Assert.Equal("Create Offer Draft", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_ForcedRetrieveOnOfferLookingAsk_DoesNotUpgradeToPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Offers at Camden",
                "Retrieved only.",
                AssistantTask.Retrieve
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenOfferPathAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
            Assert.DoesNotContain(
                ok.Conversation.Messages[^1].Actions,
                action => action.Type == "review-offer"
            );
        }

        [Fact]
        public async Task SendTurn_NamedSmsChannel_PersistsSmsAndSmsEligibleCount()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: null,
                mobile: "+447700900123"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an SMS Campaign to bring back eligible guests at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("sms", campaign.Channel);
            Assert.Null(campaign.MessageSubject);
            Assert.NotNull(campaign.MessageBody);
            Assert.Contains("SMS", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Contains("1 SMS-eligible", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Last 7 days", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_NewGuestsAsk_PersistsNewGuestsAudience()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign to all eligible new guests at Camden"
                )
            );

            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("new-guests", campaign.AudienceKey);
            Assert.Contains(
                "New guests",
                Assert.IsType<AssistantTurnOutcome.Ok>(outcome).Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_UnevaluableAudience_PersistsNothing()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign to guests with no recent Tummly activity at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Empty(answer.Actions);
            Assert.Contains("cannot be evaluated yet", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("catalogue", answer.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_TwoNamedAudiences_IsGapTurnAndPersistsNothing()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign to new guests and dormant guests at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.Contains("New guests", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Dormant guests", answer.Body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_EmailAndSms_IsChannelGapTurn()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email and SMS Campaign to bring back eligible guests at Camden"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("gap", ok.Conversation.Messages[^1].Class);
            Assert.Contains("Email", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Contains("SMS", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_UniqueNamedOffer_AttachesAndOmitsAddOffer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );
            var offerId = await SeedCatalogOfferAsync(locationId, "Weekend brunch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    CanonicalCamdenEmailWinBackAsk + " with Weekend brunch"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("existing-offer", campaign.OfferStance);
            Assert.Equal(offerId, campaign.OfferId);
            Assert.Contains("Weekend brunch", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Equal(
                new[] { "review-campaign", "change-audience" },
                ok.Conversation.Messages[^1].Actions.Select(action => action.Type)
            );
        }

        [Fact]
        public async Task SendTurn_TwoMatchingOffers_IsGapTurnThenUniqueTitlePersistsAttach()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );
            var attachedId = await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            var lunchId = await SeedCatalogOfferAsync(locationId, "Lunch treat");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        CanonicalCamdenEmailWinBackAsk + " with Weekend brunch and Lunch treat"
                    )
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Lunch treat",
                        started.Conversation.Id
                    )
                )
            );
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("existing-offer", campaign.OfferStance);
            Assert.Equal(lunchId, campaign.OfferId);
            Assert.NotEqual(attachedId, campaign.OfferId);
            Assert.Equal("grounded", answered.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_ZeroEligible_PersistsAndStatesZero()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_context.Campaigns);
            Assert.Contains("0 Email-eligible", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_EligibilityFailure_PersistsWithUnavailableCount()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var failing = CreateConversationService(
                eligibility: new ThrowingCampaignEligibilityService()
            );

            var outcome = await failing.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Single(_context.Campaigns);
            Assert.Contains(
                "eligible count unavailable",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain("Last 7 days", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_CopyGenerateFailure_PersistsEmptyMessageFields()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _messageDrafts.Fail();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Null(campaign.MessageSubject);
            Assert.Null(campaign.MessageBody);
        }

        [Fact]
        public async Task SendTurn_NamedDraftOffer_PersistsNoOfferAndExplains()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );
            await SeedCatalogOfferAsync(
                locationId,
                "Weekend brunch",
                status: CatalogOfferStatus.Draft
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    CanonicalCamdenEmailWinBackAsk + " with Weekend brunch"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("no-offer", campaign.OfferStance);
            Assert.Null(campaign.OfferId);
            Assert.Contains("No Offer", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Contains("Weekend brunch", ok.Conversation.Messages[^1].Body, StringComparison.Ordinal);
            Assert.Contains("not attachable", ok.Conversation.Messages[^1].Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_NamedOfferAtOtherOwnedLocation_PersistsNoOfferAndExplains()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var sohoId = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );
            await SeedCatalogOfferAsync(sohoId, "Weekend brunch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    CanonicalCamdenEmailWinBackAsk + " with Weekend brunch"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("no-offer", campaign.OfferStance);
            Assert.Null(campaign.OfferId);
            Assert.Contains(
                "not attachable",
                ok.Conversation.Messages[^1].Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SendTurn_ShowMeCampaignDrafts_RetrievesAndDoesNotPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show me Campaign drafts")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.DoesNotContain(answer.Actions, action => action.Type == "review-campaign");
            Assert.Null(ok.Conversation.PendingCampaignDraft);
            Assert.False(ok.Conversation.DraftInterviewActive);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_EligibleGuestMessage_StoresWorkAndReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Prepare a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.DoesNotContain("###", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Intent catalogue", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Purpose catalogue", answer.Body, StringComparison.Ordinal);
            var action = Assert.Single(answer.Actions);
            Assert.Equal("open-recovery", action.Type);
            Assert.Equal("Review recovery", action.Label);
            Assert.Equal("respond-to-guest", action.Intent);
            Assert.Contains(
                "**Intent:** Respond to the guest",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.NotNull(ok.Conversation.PendingRecoveryDraft);
            Assert.Equal(action.FeedbackId, ok.Conversation.PendingRecoveryDraft!.FeedbackId);
            Assert.Equal("respond-to-guest", ok.Conversation.PendingRecoveryDraft.Intent);
            Assert.Equal("email", ok.Conversation.PendingRecoveryDraft.Channel);
            Assert.Equal(
                "apologise_and_confirm_follow_up",
                ok.Conversation.PendingRecoveryDraft.Purpose
            );
            Assert.Equal("warm_and_apologetic", ok.Conversation.PendingRecoveryDraft.Tone);
            Assert.Equal("", ok.Conversation.PendingRecoveryDraft.IncludeNotes);
            Assert.Equal(
                "Regarding your recent visit",
                ok.Conversation.PendingRecoveryDraft.Subject
            );
            Assert.Equal(
                "Thank you for your feedback. We are looking into this.",
                ok.Conversation.PendingRecoveryDraft.Message
            );
            Assert.Equal(
                FeedbackWorkflowStatus.New,
                (await _context.Feedbacks.SingleAsync()).WorkflowStatus
            );
            Assert.Empty(_context.FeedbackRecoveryOffers);
            Assert.NotNull(_recoveryDrafts.LastInput);
            Assert.Equal("email", _recoveryDrafts.LastInput!.Channel);
            Assert.Equal(
                "apologise_and_confirm_follow_up",
                _recoveryDrafts.LastInput.Purpose
            );
            Assert.Equal("warm_and_apologetic", _recoveryDrafts.LastInput.Tone);
            Assert.Equal("prepare", _recoveryDrafts.LastInput.Mode);
            Assert.Null(_recoveryDrafts.LastInput.IncludeNotes);
            Assert.False(
                ok.Conversation.PendingRecoveryDraft.UseConfirmedActionForGuestResponse
            );

            var resumed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, ok.Conversation.Id)
            );
            Assert.Equal("open-recovery", resumed.Conversation.Messages[^1].Actions[0].Type);
            Assert.NotNull(resumed.Conversation.PendingRecoveryDraft);
            Assert.Equal(
                ok.Conversation.PendingRecoveryDraft.FeedbackId,
                resumed.Conversation.PendingRecoveryDraft!.FeedbackId
            );
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_Resolved_RefusesWithNoReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                workflow: FeedbackWorkflowStatus.Resolved,
                guestName: "Pat Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Prepare a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Contains("resolved", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("reopen", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Null(_recoveryDrafts.LastInput);
            Assert.Equal(
                FeedbackWorkflowStatus.Resolved,
                (await _context.Feedbacks.SingleAsync()).WorkflowStatus
            );
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_NoContact_NamesInternalAlternative()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest",
                guestContact: "",
                contactType: ContactType.Unknown
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Prepare a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains(
                "Record an internal action only",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_CopyPrepareFail_HasNoReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.Fail();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Prepare a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains("copy prepare", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Change Scope", answer.Body, StringComparison.Ordinal);
            Assert.Equal(
                FeedbackWorkflowStatus.New,
                (await _context.Feedbacks.SingleAsync()).WorkflowStatus
            );
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_InternalUnbound_PersistsNothing()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Record an internal action only"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains(
                "category",
                answer.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.DoesNotContain("other_action", answer.Body, StringComparison.Ordinal);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_InternalBound_StoresWorkAndReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Record an internal action only. Team briefed. Note: kitchen delay"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            var action = Assert.Single(answer.Actions);
            Assert.Equal("open-recovery", action.Type);
            Assert.Equal("record-internal-action-only", action.Intent);
            Assert.Contains(
                "**Intent:** Record an internal action only",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.NotNull(ok.Conversation.PendingRecoveryDraft);
            Assert.Equal(
                "record-internal-action-only",
                ok.Conversation.PendingRecoveryDraft!.Intent
            );
            Assert.Equal("team_briefed", ok.Conversation.PendingRecoveryDraft.Category);
            Assert.Equal("kitchen delay", ok.Conversation.PendingRecoveryDraft.Note);
            Assert.Null(ok.Conversation.PendingRecoveryDraft.Message);
            Assert.Equal(
                FeedbackWorkflowStatus.New,
                (await _context.Feedbacks.SingleAsync()).WorkflowStatus
            );
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_NamedIncludeNotes_PassThroughToPrepare()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Prepare a recovery response. Include-notes: kitchen delay"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(
                "kitchen delay",
                ok.Conversation.PendingRecoveryDraft!.IncludeNotes
            );
            Assert.Equal("kitchen delay", _recoveryDrafts.LastInput!.IncludeNotes);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_RespondAndRecordUnbound_PersistsNothing()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Respond and record")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains(
                "category",
                answer.Body,
                StringComparison.OrdinalIgnoreCase
            );
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_RespondAndRecordBound_StoresGuestCopyAndAction()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Respond and record. Team briefed. Note: kitchen delay"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var draft = ok.Conversation.PendingRecoveryDraft;
            Assert.NotNull(draft);
            Assert.Equal("respond-and-record-internal-action", draft!.Intent);
            Assert.Equal("team_briefed", draft.Category);
            Assert.Equal("kitchen delay", draft.Note);
            Assert.True(draft.UseConfirmedActionForGuestResponse);
            Assert.Contains(
                "**Intent:** Respond and record an internal action",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Equal(
                "Thank you for your feedback. We are looking into this.",
                draft.Message
            );
            Assert.Equal("team_briefed", _recoveryDrafts.LastInput!.ConfirmedInternalActionCategory);
            Assert.Equal("kitchen delay", _recoveryDrafts.LastInput.ConfirmedInternalActionNote);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_OfferUnbound_PersistsNothing()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var guestId = await SeedLocationGuestAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2)
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest",
                locationGuestId: guestId
            );
            await SeedCatalogOfferAsync(locationId, "Weekend brunch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Prepare a recovery response with a recovery offer"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains("Offer", answer.Body, StringComparison.Ordinal);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_OfferBound_StoresOfferIdAndReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var guestId = await SeedLocationGuestAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2)
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest",
                locationGuestId: guestId
            );
            var offerId = await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Prepare a recovery response with a recovery offer. Weekend brunch"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var action = Assert.Single(ok.Conversation.Messages[^1].Actions);
            Assert.Equal("open-recovery", action.Type);
            Assert.Equal("respond-with-recovery-offer", action.Intent);
            Assert.Equal(
                "respond-with-recovery-offer",
                ok.Conversation.PendingRecoveryDraft!.Intent
            );
            Assert.Equal(offerId, ok.Conversation.PendingRecoveryDraft.OfferId);
            Assert.Equal("email", ok.Conversation.PendingRecoveryDraft.Channel);
            Assert.Equal(
                "include_a_recovery_offer",
                ok.Conversation.PendingRecoveryDraft.Purpose
            );
            Assert.Contains(
                "**Intent:** Respond with a recovery offer",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.NotNull(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_NamedGuestMiss_ExplainsWithoutDump()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-3),
                guestName: "Alex Guest",
                guestContact: "alex@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Prepare a recovery response for Mehmet"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.DoesNotContain("Pat Guest", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Alex Guest", answer.Body, StringComparison.Ordinal);
            Assert.Contains("could not match", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_NamedFirstName_BindsThatGuest()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-3),
                guestName: "Alex Guest",
                guestContact: "alex@example.com"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Prepare a recovery response for Pat"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            Assert.Equal(
                "Pat Guest",
                (await _context.Feedbacks.SingleAsync(
                    row => row.Id == ok.Conversation.PendingRecoveryDraft!.FeedbackId
                )).GuestName
            );
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_TwoMatches_IsFeedbackGap()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-3),
                guestName: "Alex Guest",
                guestContact: "alex@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Prepare a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains("Pat Guest", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Alex Guest", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Intent catalogue", answer.Body, StringComparison.Ordinal);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_PrepareRecoveryResponse_ZeroMatches_ExplainsWithoutDump()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Prepare a recovery response")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Contains("last 7 days", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Change Scope", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("could not match", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("###", answer.Body, StringComparison.Ordinal);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_LastNegativeRecoveryAsk_WithNoNegative_NamesLocationAndPeriod()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                sentiment: FeedbackSentiment.Positive,
                guestName: "Pat Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a recovery offer for the last negative feedback we recieved on this location"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains("negative", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Contains("last 7 days", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Change Scope", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("could not match", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Pat Guest", answer.Body, StringComparison.Ordinal);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_LastNegativeRecoveryAsk_WhenOnlyResolvedNegative_NamesResolvedGuest()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                workflow: FeedbackWorkflowStatus.Resolved,
                guestName: "Pat Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Create a recovery offer for the last negative feedback we recieved on this location"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingRecoveryDraft);
            Assert.Contains("resolved", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("reopen", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Pat Guest", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("could not match", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Null(_recoveryDrafts.LastInput);
        }

        [Fact]
        public async Task SendTurn_MixedRetrieveAndCanonicalCreate_PersistsOneAnswerWithoutRetrieveActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback and "
                    + CanonicalCamdenEmailWinBackAsk
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, ok.Conversation.Messages.Count);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(1, await _context.Campaigns.CountAsync());
            Assert.DoesNotContain(answer.Actions, action => action.Type == "draft-campaign");
            Assert.DoesNotContain(answer.Actions, action => action.Type == "view-feedback-set");
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "add-offer" },
                answer.Actions.Select(action => action.Type)
            );
        }

        [Fact]
        public async Task SendTurn_ForcedRetrieveOnCreateLookingAsk_DoesNotUpgradeToPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Feedback at Camden",
                "Retrieved only.",
                AssistantTask.Retrieve
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.DoesNotContain(
                ok.Conversation.Messages[^1].Actions,
                action => action.Type == "review-campaign"
            );
        }

        [Fact]
        public async Task SendTurn_HowDoICreateACampaign_RefusesAndDoesNotPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How do I create a campaign?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Empty(answer.Actions);
            Assert.Null(ok.Conversation.PendingCampaignDraft);
        }

        [Fact]
        public async Task SendTurn_CanonicalAskPlusSendItNow_PersistsDraftAndDoesNotSend()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    CanonicalCamdenEmailWinBackAsk + " and send it now"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("draft", campaign.Status);
            Assert.Contains("Nothing was sent or scheduled", ok.Conversation.Messages[^1].Body);
            Assert.Null(ok.Conversation.SendScheduleRoute);
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "add-offer" },
                ok.Conversation.Messages[^1].Actions.Select(action => action.Type)
            );
        }

        [Fact]
        public async Task SendTurn_LaterSendItNow_RoutesStoredCampaignDraftAndDoesNotSend()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
                )
            );
            var campaign = Assert.Single(_context.Campaigns);

            var later = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "send it now", started.Conversation.Id)
                )
            );

            Assert.Equal("draft", campaign.Status);
            Assert.Equal(1, await _context.Campaigns.CountAsync());
            Assert.Null(campaign.ScheduledAtUtc);
            var answer = later.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Nothing was sent", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            var route = later.Conversation.SendScheduleRoute;
            Assert.NotNull(route);
            Assert.Equal("campaign", route!.Kind);
            Assert.Equal(campaign.Id, route.CampaignId);
            Assert.Equal("review", route.Step);
            Assert.Equal("send-now", route.ScheduleMode);

            var resumed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(ownerUserId: 7, started.Conversation.Id)
            );
            Assert.Null(resumed.Conversation.SendScheduleRoute);
            Assert.Equal(
                campaign.Id,
                resumed.Conversation.Messages
                    .SelectMany(message => message.Actions)
                    .First(action => action.Type == "review-campaign")
                    .CampaignId
            );
        }

        [Fact]
        public async Task SendTurn_SendItNowWithoutStoredId_DoesNotRouteOrPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "send it now")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Null(ok.Conversation.SendScheduleRoute);
            Assert.DoesNotContain(
                ok.Conversation.Messages[^1].Actions,
                action => action.Type == "review-campaign"
            );
        }

        [Fact]
        public async Task SendTurn_ActivateOffer_IsRefusedAndDoesNotRoute()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, CanonicalCamdenOfferPathAsk)
                )
            );

            var later = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "activate this offer", started.Conversation.Id)
                )
            );

            var offer = Assert.Single(_context.CatalogOffers);
            Assert.Equal(CatalogOfferStatus.Draft, offer.Status);
            Assert.Null(later.Conversation.SendScheduleRoute);
            Assert.Equal("refusal", later.Conversation.Messages[^1].Class);
            Assert.Contains(
                "cannot activate",
                later.Conversation.Messages[^1].Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SendTurn_LaterSendItNowOnRecovery_RoutesReviewAndDoesNotSend()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Prepare a recovery response")
                )
            );

            var later = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "send it now", started.Conversation.Id)
                )
            );

            Assert.Equal(
                FeedbackWorkflowStatus.New,
                (await _context.Feedbacks.SingleAsync()).WorkflowStatus
            );
            Assert.Empty(_context.FeedbackRecoveryOffers);
            var route = later.Conversation.SendScheduleRoute;
            Assert.NotNull(route);
            Assert.Equal("recovery", route!.Kind);
            Assert.Equal(
                started.Conversation.PendingRecoveryDraft!.FeedbackId,
                route.FeedbackId
            );
            Assert.Equal("respond-to-guest", route.Intent);
            Assert.Contains("Nothing was sent", later.Conversation.Messages[^1].Body);
        }

        [Fact]
        public async Task SendTurn_RecoveryTimedSchedule_StaysInAssistant()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Prepare a recovery response")
                )
            );

            var later = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "schedule it for Friday",
                        started.Conversation.Id
                    )
                )
            );

            Assert.Null(later.Conversation.SendScheduleRoute);
            Assert.Equal("refusal", later.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_LaterSendItNowOnResolvedRecovery_StaysAndDoesNotClaimOpen()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );
            _recoveryDrafts.SucceedWith(
                "Thank you for your feedback. We are looking into this.",
                "Regarding your recent visit",
                "email"
            );
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Prepare a recovery response")
                )
            );

            var feedback = await _context.Feedbacks.SingleAsync();
            feedback.WorkflowStatus = FeedbackWorkflowStatus.Resolved;
            await _context.SaveChangesAsync();

            var later = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "send it now", started.Conversation.Id)
                )
            );

            Assert.Null(later.Conversation.SendScheduleRoute);
            Assert.Equal("refusal", later.Conversation.Messages[^1].Class);
            Assert.DoesNotContain(
                "Opening Feedback recovery Review",
                later.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SendTurn_MixedRetrieveAndSendItNow_RoutesWithoutRetrieveAnswer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
                )
            );

            var later = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "Show me Campaign drafts and send it now",
                        started.Conversation.Id
                    )
                )
            );

            Assert.NotNull(later.Conversation.SendScheduleRoute);
            Assert.Equal("campaign", later.Conversation.SendScheduleRoute!.Kind);
            Assert.DoesNotContain(
                "No facts",
                later.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Contains("Nothing was sent", later.Conversation.Messages[^1].Body);
        }


        [Fact]
        public async Task SendTurn_CampaignPersistFailure_NamesFailedStepAndDoesNotInventId()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Campaign Draft",
                "Create Campaign Draft.",
                AssistantTask.CreateCampaignDraft,
                CanonicalGeneratedConversationTitle
            );
            var failing = CreateConversationService(new ThrowingCampaignDraftService());

            var outcome = await failing.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal(2, ok.Conversation.Messages.Count);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.DoesNotContain("review-campaign", answer.Actions.Select(action => action.Type));
            Assert.DoesNotContain("change-audience", answer.Actions.Select(action => action.Type));
            Assert.DoesNotContain("add-offer", answer.Actions.Select(action => action.Type));
            Assert.DoesNotContain("Bring back Email-eligible guests at Camden", answer.Body);
            Assert.Contains("Campaign create", answer.Body);
            Assert.Contains("Change Scope", answer.Body);
            Assert.Null(ok.Conversation.PendingCampaignDraft);
            Assert.Null(_context.AssistantConversations.Single().CreatedCampaignId);
            Assert.Equal(CanonicalGeneratedConversationTitle, ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_GeneratedTitle_ReplacesFallbackOnFirstSuccessfulComplete()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            StubRetrieveConversationTitle("Summarise feedback");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("Summarise feedback", ok.Conversation.Title);
            Assert.Equal(
                "No facts at Camden for the last 7 days",
                ok.Conversation.Messages[^1].Title
            );
        }

        [Fact]
        public async Task SendTurn_FailureComplete_KeepsFirstUserMessageFallback()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.Fail();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Title);
        }

        [Fact]
        public async Task RetryTurn_FirstSendFailure_AppliesGeneratedTitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.Fail();
            var failed = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(failed).Conversation.Id;
            StubRetrieveConversationTitle("Summarise feedback");

            var retried = await _service.RetryTurnAsync(ownerUserId: 7, conversationId);

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(retried);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Summarise feedback", ok.Conversation.Title);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("**Bold title**")]
        [InlineData("guest@example.com")]
        [InlineData("07700900000")]
        public async Task SendTurn_RejectedGeneratedTitle_KeepsFirstUserMessageFallback(
            string proposed
        )
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            StubRetrieveConversationTitle(proposed);

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_GeneratedTitleMatchingLiveAnswerTitle_KeepsFallback()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            const string liveAnswerTitle = "No facts at Camden for the last 7 days";
            StubRetrieveConversationTitle(liveAnswerTitle, liveAnswerTitle);

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Title);
            Assert.Equal(liveAnswerTitle, ok.Conversation.Messages[^1].Title);
        }

        [Fact]
        public async Task SendTurn_GeneratedTitleMatchingOverLengthLiveAnswerTitle_KeepsFallback()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            const string liveAnswerTitle =
                "No facts at Camden for the last 7 days and extra words after sixty";
            StubRetrieveConversationTitle(liveAnswerTitle, liveAnswerTitle);

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_OverLengthGeneratedTitle_CutsAtLastSpaceAndDoesNotFallBack()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            StubRetrieveConversationTitle(
                "Bring back Email-eligible guests during the quiet lunch period this week"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(
                "Bring back Email-eligible guests during the quiet lunch",
                ok.Conversation.Title
            );
        }

        [Fact]
        public async Task SendTurn_OverLengthGeneratedTitleWithoutSpace_HardCuts()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            StubRetrieveConversationTitle(
                "BringBackEmailEligibleGuestsWithoutAnySpacesInTheFirstSixtyX more"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(
                "BringBackEmailEligibleGuestsWithoutAnySpacesInTheFirstSixtyX",
                ok.Conversation.Title
            );
        }

        [Fact]
        public async Task SendTurn_MultilineGeneratedTitle_UsesFirstLine()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            StubRetrieveConversationTitle("Summarise feedback\nIgnore this second line");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("Summarise feedback", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_LaterSend_DoesNotRetitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            StubRetrieveConversationTitle("Summarise feedback");
            var first = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Summarise recent feedback")
                )
            );
            StubRetrieveConversationTitle("Later generated title");

            var later = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Show me Campaign drafts",
                    first.Conversation.Id
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(later);
            Assert.Equal("Summarise feedback", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_CanonicalCamdenEmailWinBack_UsesTaskTitleNotRawAsk()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Campaign Draft",
                "Create Campaign Draft.",
                AssistantTask.CreateCampaignDraft,
                CanonicalGeneratedConversationTitle
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(CanonicalGeneratedConversationTitle, ok.Conversation.Title);
            Assert.NotEqual(CanonicalCamdenEmailWinBackAsk, ok.Conversation.Title);
            Assert.True(ok.Conversation.Title.Length <= 60);
        }

        [Fact]
        public async Task SendTurn_Refuse_AppliesGeneratedTitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Refusal,
                null,
                AssistantLiveAnswerCopy.HelpCentreRefusalBody,
                AssistantTask.Refuse,
                "Help Centre question"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How do I create a campaign?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("refusal", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Help Centre question", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_GapTurn_AppliesGeneratedTitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Campaign Draft",
                "Create Campaign Draft.",
                AssistantTask.CreateCampaignDraft,
                "Create Campaign and recovery"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign and draft a recovery response"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("gap", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Create Campaign and recovery", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_CompareClarify_AppliesGeneratedTitle()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            StubRetrieveConversationTitle("Compare locations");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare all locations")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("clarify", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Compare locations", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_LocationGapRefusal_AppliesGeneratedTitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Campaign Draft",
                "Create Campaign Draft.",
                AssistantTask.CreateCampaignDraft,
                "Create Campaign Draft"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign to bring back guests at Paris"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("refusal", ok.Conversation.Messages[^1].Class);
            Assert.Equal("Create Campaign Draft", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_OfferPathInterview_AppliesGeneratedTitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Offer Draft",
                "Create an offer draft.",
                AssistantTask.OfferPath,
                "Create Offer Draft"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Create an offer draft")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("gap", ok.Conversation.Messages[^1].Class);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Equal("Create Offer Draft", ok.Conversation.Title);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_RecoveryPathInterview_AppliesGeneratedTitle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "Feedback recovery",
                "Prepare a recovery reply.",
                AssistantTask.RecoveryPath,
                "Prepare recovery reply"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Respond to these guests")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Equal("Prepare recovery reply", ok.Conversation.Title);
        }

        [Fact]
        public async Task SendTurn_TwoCreateTargets_IsGapAndPersistsNeither()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign and draft a recovery response"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Null(answer.Title);
            Assert.Empty(answer.Actions);
            Assert.Contains("Campaign", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Feedback recovery", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Offer", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("###", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Campaign goal catalogue", answer.Body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.False(ok.Conversation.DraftInterviewActive);
        }

        [Fact]
        public async Task SendTurn_UnnamedCreateTargets_ListsCampaignOfferAndRecovery()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "help me draft something")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Campaign", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Offer", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Feedback recovery", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_MixedRetrieveAndTwoCreateTargets_AsksTargetFirstWithoutRetrieveActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback, draft an offer, and draft a recovery response"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Offer", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Feedback recovery", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("1 feedback item", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Empty(_retrieve.Calls);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_TwoCreateTargets_CampaignAnswerPersistsWhenLocationUnique()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        CanonicalCamdenEmailWinBackAsk + " and draft a recovery response"
                    )
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Campaign", started.Conversation.Id)
                )
            );
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "add-offer" },
                answer.Actions.Select(action => action.Type)
            );
            Assert.Equal(1, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_HelpCentreDuringCreateTargetGap_RefusesAndPersistsNothing()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "help me draft something")
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var refused = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        locationId,
                        "How do I create a campaign?",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Equal("refusal", refused.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Empty(refused.Conversation.Messages[^1].Actions);
        }

        [Fact]
        public async Task SendTurn_UnnamedCreateWithSeveralOwnedLocations_PersistsAtAnalysisScope()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            await SeedLinkedGuestAsync(
                camden,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    camden,
                    "Draft an Email Campaign to bring back all currently Email-eligible guests"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            Assert.Equal(camden, campaign.RestaurantLocationId);
        }

        [Fact]
        public async Task SendTurn_LocationConflict_IsGapAndDoesNotPersist()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Soho", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.False(ok.Conversation.DraftInterviewActive);
        }

        [Fact]
        public async Task SendTurn_UniqueLocationAnswer_PersistsAndSetsAnalysisScope()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            var camden = await SeedSecondLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                camden,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "Camden", started.Conversation.Id)
                )
            );
            var campaign = Assert.Single(_context.Campaigns);
            Assert.Equal(camden, campaign.RestaurantLocationId);
            Assert.Equal(camden, answered.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal("Camden", answered.Conversation.AnalysisScope.OwnedLocationName);
            Assert.Equal("grounded", answered.Conversation.Messages[^1].Class);
            Assert.Equal(
                new[] { "review-campaign", "change-audience", "add-offer" },
                answered.Conversation.Messages[^1].Actions.Select(action => action.Type)
            );
        }

        [Fact]
        public async Task SendTurn_UnknownLocationName_RefusesAndDoesNotPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Draft an Email Campaign to bring back guests at Paris"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.Contains("Paris", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Camden", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_AllLocationsCreate_AsksToNameOneWithoutListingEveryLocation()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    camden,
                    "Draft an Email Campaign for all locations"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Name one", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Soho", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_CreateWinsOverCompareClarify()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    camden,
                    "Compare all locations and create a campaign draft"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.NotEqual("clarify", answer.Class);
            Assert.NotEqual("gap", answer.Class);
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(1, await _context.Campaigns.CountAsync());
            Assert.Equal(camden, _context.Campaigns.Single().RestaurantLocationId);
        }

        [Fact]
        public async Task SendTurn_RetrieveDuringLocationGap_ReplacesGapAndPersistsNothing()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var replaced = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "Show me Camden", started.Conversation.Id)
                )
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.NotEqual("gap", replaced.Conversation.Messages[^1].Class);
            Assert.DoesNotContain(
                replaced.Conversation.Messages[^1].Actions,
                action => action.Type == "review-campaign"
            );
        }

        [Fact]
        public async Task SendTurn_CompareDuringLocationGap_ReplacesGapWithClarify()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );

            var compared = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "Compare all locations", started.Conversation.Id)
                )
            );
            Assert.Equal("clarify", compared.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task ApplyScope_DuringLocationGap_DoesNotPersist()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            var camden = await SeedSecondLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                camden,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var applied = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.ApplyScopeAsync(
                    ownerUserId: 7,
                    started.Conversation.Id,
                    new ApplyAssistantScopeRequest
                    {
                        AnalysisScope = new AssistantAnalysisScopeDto
                        {
                            OwnedLocationId = camden,
                            ReportingPeriod = new AssistantReportingPeriodDto
                            {
                                Kind = "preset",
                                PresetId = "last7",
                            },
                        },
                    }
                )
            );
            Assert.Equal(camden, applied.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal("gap", applied.Conversation.Messages[^1].Class);

            var continued = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, "ok", started.Conversation.Id)
                )
            );
            Assert.Equal(1, await _context.Campaigns.CountAsync());
            Assert.Equal("grounded", continued.Conversation.Messages[^1].Class);
            Assert.Equal(camden, _context.Campaigns.Single().RestaurantLocationId);
        }

        [Fact]
        public async Task SendTurn_HelpCentreDuringLocationGap_RefusesAndPersistsNothing()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var refused = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        soho,
                        "How do I create a campaign?",
                        started.Conversation.Id
                    )
                )
            );
            Assert.Equal("refusal", refused.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_UnnamedAfterScopeChange_DoesNotPersist()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            var camden = await SeedSecondLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                camden,
                "Eligible Guest",
                email: "eligible@example.com"
            );

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.ApplyScopeAsync(
                    ownerUserId: 7,
                    started.Conversation.Id,
                    new ApplyAssistantScopeRequest
                    {
                        AnalysisScope = new AssistantAnalysisScopeDto
                        {
                            OwnedLocationId = camden,
                            ReportingPeriod = new AssistantReportingPeriodDto
                            {
                                Kind = "preset",
                                PresetId = "last7",
                            },
                        },
                    }
                )
            );

            var unnamed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, "hello", started.Conversation.Id)
                )
            );
            Assert.Equal("gap", unnamed.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
        }

        [Fact]
        public async Task SendTurn_SendItNowDuringLocationGap_DoesNotPersist()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );

            var refused = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "send it now", started.Conversation.Id)
                )
            );
            Assert.NotEqual("gap", refused.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.DoesNotContain(
                refused.Conversation.Messages[^1].Actions,
                action => action.Type == "review-campaign"
            );
        }

        [Fact]
        public async Task SendTurn_ClearCancel_ClearsLocationGapAndPersistsNothing()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );

            var cancelled = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "cancel the draft", started.Conversation.Id)
                )
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.False(cancelled.Conversation.DraftInterviewActive);
            Assert.NotEqual("gap", cancelled.Conversation.Messages[^1].Class);
        }

        [Fact]
        public async Task SendTurn_NonUniqueLocationAnswer_AsksAgain()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );

            var again = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "either", started.Conversation.Id)
                )
            );
            Assert.Equal("gap", again.Conversation.Messages[^1].Class);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Contains("Camden", again.Conversation.Messages[^1].Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_TwoCreateTargetsDuringLocationGap_ReplacesGapAndPersistsNeither()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );
            Assert.Equal("gap", started.Conversation.Messages[^1].Class);

            var replaced = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(
                        soho,
                        "Draft an Email Campaign and draft a recovery response",
                        started.Conversation.Id
                    )
                )
            );
            var answer = replaced.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Campaign", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Feedback recovery", answer.Body, StringComparison.Ordinal);
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.False(replaced.Conversation.DraftInterviewActive);
        }

        [Fact]
        public async Task SendTurn_DifferentTask_ReplacesLocationGap()
        {
            var soho = await SeedLocationAsync(ownerUserId: 7, "Soho");
            await SeedSecondLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, CanonicalCamdenEmailWinBackAsk)
                )
            );

            var replaced = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(soho, "Create an offer draft", started.Conversation.Id)
                )
            );
            Assert.Equal(0, await _context.Campaigns.CountAsync());
            Assert.Equal("gap", replaced.Conversation.Messages[^1].Class);
            Assert.DoesNotContain(
                "Offer type catalogue",
                replaced.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Theory]
        [InlineData("Make an offer")]
        public async Task SendTurn_GenericOfferAsk_IsTermsGapNotInterview(string message)
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, message)
                )
            );

            Assert.Equal("gap", outcome.Conversation.Messages[^1].Class);
            Assert.False(outcome.Conversation.DraftInterviewActive);
            Assert.DoesNotContain(
                "Offer type catalogue",
                outcome.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.Empty(outcome.Conversation.Messages[^1].Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_GenericResponseRequest_CompletesWithReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Respond to these guests")
                )
            );

            Assert.False(outcome.Conversation.DraftInterviewActive);
            Assert.Equal("open-recovery", Assert.Single(outcome.Conversation.Messages[^1].Actions).Type);
            Assert.NotNull(outcome.Conversation.PendingRecoveryDraft);
        }

        [Fact]
        public async Task SendTurn_OfferDraftInterview_StartsThenCompletesWithOneDraftAction()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create an offer draft")
                )
            );
            var gap = started.Conversation.Messages[^1];
            Assert.Equal("gap", gap.Class);
            Assert.Contains("type", gap.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Offer type catalogue", gap.Body, StringComparison.Ordinal);
            Assert.Empty(gap.Actions);
            Assert.Null(started.Conversation.PendingOfferDraft);
            Assert.False(started.Conversation.DraftInterviewActive);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_DraftInterview_TargetSwitch_ReplacesIncompleteState()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var offer = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create an offer draft")
                )
            );

            Assert.Equal("gap", offer.Conversation.Messages[^1].Class);

            var recovery = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    new SendAssistantTurnRequest
                    {
                        ConversationId = offer.Conversation.Id,
                        Message = "Draft a recovery response",
                        AnalysisScope = FirstSendRequest(locationId, "x").AnalysisScope,
                    }
                )
            );
            Assert.Contains("Feedback", recovery.Conversation.Messages[^1].Body);
            Assert.Null(recovery.Conversation.PendingOfferDraft);
            Assert.False(recovery.Conversation.DraftInterviewActive);

            var offerAgain = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    new SendAssistantTurnRequest
                    {
                        ConversationId = offer.Conversation.Id,
                        Message = "Create an offer draft",
                        AnalysisScope = FirstSendRequest(locationId, "x").AnalysisScope,
                    }
                )
            );
            Assert.Equal("gap", offerAgain.Conversation.Messages[^1].Class);
            Assert.Null(offerAgain.Conversation.PendingRecoveryDraft);
            Assert.DoesNotContain(
                "Offer type catalogue",
                offerAgain.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
        }

        [Fact]
            public async Task SendTurn_RecoveryPathAsk_CompletesWithReviewRecovery()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                guestName: "Pat Guest"
            );

            var completed = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Draft a recovery response")
                )
            );

            var answer = completed.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.False(completed.Conversation.DraftInterviewActive);
            var action = Assert.Single(answer.Actions);
            Assert.Equal("open-recovery", action.Type);
            Assert.Equal("Review recovery", action.Label);
            Assert.Equal("respond-to-guest", action.Intent);
            Assert.NotNull(action.FeedbackId);
            Assert.NotNull(completed.Conversation.PendingRecoveryDraft);
            Assert.Equal(
                action.FeedbackId,
                completed.Conversation.PendingRecoveryDraft!.FeedbackId
            );
            Assert.Equal(
                "respond-to-guest",
                completed.Conversation.PendingRecoveryDraft.Intent
            );
            Assert.Equal("email", completed.Conversation.PendingRecoveryDraft.Channel);
            Assert.Equal(
                "Thank you for your feedback. We are looking into this.",
                completed.Conversation.PendingRecoveryDraft.Message
            );
            Assert.Null(completed.Conversation.PendingCampaignDraft);
        }

        [Fact]
        public async Task SendTurn_UnsupportedMixedAsk_StaysAndDoesNotSearch()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback and schedule a campaign")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("refusal", answer.Class);
            Assert.Null(ok.Conversation.SendScheduleRoute);
            Assert.DoesNotContain("1 feedback item", answer.Body);
            Assert.Contains(
                "cannot send or schedule",
                answer.Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SendTurn_MixedRetrieveAndOfferDraft_GroundsThenStartsOneInterview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback and create an offer draft"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.DoesNotContain("Offer type catalogue", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
            public async Task SendTurn_MixedRetrieveAndRecoveryPath_CreateWinsWithReview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback and draft a recovery response"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Equal("open-recovery", Assert.Single(answer.Actions).Type);
            Assert.NotNull(ok.Conversation.PendingRecoveryDraft);
        }

        [Fact]
        public async Task SendTurn_TwoDraftTargets_PersistsChoiceAndStartsSelectedInterview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback, draft an offer, and draft a recovery response"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("gap", answer.Class);
            Assert.Contains("Offer", answer.Body);
            Assert.Contains("Feedback recovery", answer.Body);
            Assert.DoesNotContain("###", answer.Body);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Empty(answer.Actions);

            var selected = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    new SendAssistantTurnRequest
                    {
                        ConversationId = ok.Conversation.Id,
                        Message = "Offer",
                        AnalysisScope = FirstSendRequest(locationId, "x").AnalysisScope,
                    }
                )
            );
            var selectedAnswer = selected.Conversation.Messages[^1];
            Assert.Equal("gap", selectedAnswer.Class);
            Assert.False(selected.Conversation.DraftInterviewActive);
            Assert.DoesNotContain("Offer type catalogue", selectedAnswer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("feedback item", selectedAnswer.Body);
            Assert.Equal(0, await _context.CatalogOffers.CountAsync());
        }

        [Fact]
        public async Task SendTurn_CompareAllLocations_StillClarify()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare all locations")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("clarify", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.Empty(_retrieve.Calls);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Null(ok.Conversation.PendingCampaignDraft);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_DomainRetrieve_IsCompareAllOfOwnedSet()
        {
            var seeded = await SeedAllOwnedConversationAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.False(ok.Conversation.RetryEligible);
            AssertNamedLocationCalls(
                _retrieve.Calls,
                seeded.Brixton,
                seeded.Camden,
                seeded.Shoreditch,
                seeded.Soho
            );
            Assert.Equal(
                [seeded.Brixton, seeded.Camden, seeded.Shoreditch, seeded.Soho],
                _retrieve.Calls.Select(call => call.OwnedLocationId).ToList()
            );
            Assert.True(_fake.LastInput!.CompareAll);
            Assert.Equal(4, _fake.LastInput.CompareLocations!.Count);
            var stored = await _context.AssistantConversations
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.ConversationId);
            Assert.Null(stored.LastCompareLocationIdsJson);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_AllPhrases_ProceedsWithoutClarify()
        {
            var seeded = await SeedAllOwnedConversationAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "compare all locations")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.NotEqual("clarify", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.True(_fake.LastInput!.CompareAll);
            Assert.Equal(4, _fake.LastInput.CompareLocations!.Count);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_NamedSubset_KeepsCapThreeAndHundredOfN()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            for (var index = 0; index < 101; index++)
            {
                await SeedCatalogOfferAsync(seeded.Soho, $"Soho offer {index}");
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Compare Soho and Shoreditch")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Empty(answer.Actions);
            Assert.False(_fake.LastInput!.CompareAll);
            AssertNamedLocationCalls(_retrieve.Calls, seeded.Soho, seeded.Shoreditch);
            Assert.DoesNotContain(
                _retrieve.Calls,
                call => call.OwnedLocationId == seeded.Camden
                    || call.OwnedLocationId == seeded.Brixton
            );
            var sohoRow = Assert.Single(
                _fake.LastInput.CompareLocations!,
                row => row.OwnedLocationId == seeded.Soho
            );
            Assert.Equal(101, sohoRow.Evidence.Offers.CatalogTotalCount);
            Assert.Equal(100, sohoRow.Evidence.Offers.CatalogSampleCount);
            Assert.Equal(100, sohoRow.Evidence.Offers.Catalog.Count);
            var stored = await _context.AssistantConversations
                .AsNoTracking()
                .SingleAsync(row => row.Id == seeded.ConversationId);
            Assert.Null(stored.LastCompareLocationIdsJson);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_NamedFour_ClarifiesCapThree()
        {
            var seeded = await SeedAllOwnedConversationAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(
                    seeded.ConversationId,
                    "Compare Camden, Soho, Shoreditch and Brixton"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("clarify", answer.Class);
            Assert.Contains("up to 3", answer.Body);
            Assert.Empty(answer.Actions);
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_CompareAll_ThinsPayloadAndDisclosesExcerpts()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            for (var index = 0; index < 6; index++)
            {
                await SeedFeedbackAsync(
                    seeded.Camden,
                    DateTime.UtcNow.AddHours(-(index + 1)),
                    comment: $"Camden excerpt {index}"
                );
            }

            await SeedFeedbackAsync(
                seeded.Soho,
                DateTime.UtcNow.AddHours(-1),
                comment: "Soho excerpt"
            );
            for (var index = 0; index < 101; index++)
            {
                await SeedCatalogOfferAsync(seeded.Soho, $"Soho offer {index}");
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            var camdenRow = Assert.Single(
                _fake.LastInput!.CompareLocations!,
                row => row.OwnedLocationId == seeded.Camden
            );
            var sohoRow = Assert.Single(
                _fake.LastInput.CompareLocations!,
                row => row.OwnedLocationId == seeded.Soho
            );
            Assert.Equal(6, camdenRow.Evidence.Feedback.TotalCount);
            Assert.Equal(5, camdenRow.Evidence.Feedback.SampleCount);
            Assert.Equal(5, camdenRow.Evidence.Feedback.Rows.Count);
            Assert.Equal(101, sohoRow.Evidence.Offers.CatalogTotalCount);
            Assert.Empty(sohoRow.Evidence.Offers.Catalog);
            Assert.Contains("Comment samples are 5 of 6 at Camden", answer.Body);
            Assert.Contains("Theme totals are for the full Reporting period", answer.Body);
            Assert.DoesNotContain("These themes come from", answer.Body);
            Assert.DoesNotContain("No offers at Camden", answer.Body);
            Assert.Equal(3, CountQuotedExcerpts(answer.Body));
            Assert.Contains("Camden excerpt 0", answer.Body);
            Assert.DoesNotContain("Soho excerpt", answer.Body);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_BudgetMiss_NamesUnretrievedVenues()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var soho = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            var shoreditch = await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var brixton = await SeedSecondLocationAsync(ownerUserId: 7, "Brixton");
            var clock = new ManualTimeProvider(DateTimeOffset.UtcNow);
            var service = CreateConversationService(timeProvider: clock);
            var created = Assert.IsType<AssistantTurnOutcome.Ok>(
                await service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, "Summarise recent feedback")
                )
            );
            await service.ApplyScopeAsync(
                ownerUserId: 7,
                created.Conversation.Id,
                AllOwnedLocationsScopeRequest()
            );
            ClearRetrieveCalls();
            _retrieve.AfterRetrieve = _ => clock.Advance(TimeSpan.FromSeconds(21));
            await SeedFeedbackAsync(brixton, DateTime.UtcNow.AddHours(-1), comment: "Brixton note");

            var outcome = await service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(created.Conversation.Id, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.False(ok.Conversation.RetryEligible);
            AssertNamedLocationCalls(_retrieve.Calls, brixton);
            Assert.DoesNotContain(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(
                "Not retrieved this turn: Camden, Shoreditch, Soho.",
                answer.Body
            );
            Assert.Contains("This ranking is partial.", answer.Body);
            Assert.Contains("Retry this send, or name up to 3 locations.", answer.Body);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_ZeroVenuesLanded_IsFailureWithRetry()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            _retrieve.FailAll = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[^1].Class);
            Assert.True(ok.Conversation.RetryEligible);
            Assert.NotEqual(
                "Summarise recent feedback",
                _fake.LastInput?.UserMessage
            );
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_FailedVenue_IsGroundedNamedGap()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            await SeedFeedbackAsync(
                seeded.Soho,
                DateTime.UtcNow.AddHours(-1),
                comment: "Soho note"
            );
            _retrieve.FailLocationIds.Add(seeded.Camden);

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.False(ok.Conversation.RetryEligible);
            Assert.Contains("Could not load data for Camden.", answer.Body);
            Assert.Contains("This ranking is partial.", answer.Body);
            Assert.DoesNotContain(
                _fake.LastInput!.CompareLocations!,
                row => row.OwnedLocationId == seeded.Camden
            );
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_AzureFailAfterLanded_IsFailureWithRetry()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            await SeedFeedbackAsync(seeded.Camden, DateTime.UtcNow.AddHours(-1));
            _fake.Fail();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[^1].Class);
            Assert.True(ok.Conversation.RetryEligible);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_EmptyVenue_UsesNamedEmptyCopy()
        {
            var seeded = await SeedAllOwnedConversationAsync();
            await SeedFeedbackAsync(
                seeded.Camden,
                DateTime.UtcNow.AddHours(-1),
                comment: "Camden note"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains(
                "No feedback at Soho for the last 7 days.",
                ok.Conversation.Messages[^1].Body
            );
            Assert.DoesNotContain(
                "No offers at Camden",
                ok.Conversation.Messages[^1].Body
            );
            Assert.Contains("This ranking is partial.", ok.Conversation.Messages[^1].Body);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_ProductExpert_DoesNotCompareAll()
        {
            var seeded = await SeedAllOwnedConversationAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "campaign vs offer")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            AssertNoRetrieveGets();
            Assert.False(_fake.LastInput!.CompareAll);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_AttentionRetrieve_DoesNotCompareAll()
        {
            var seeded = await SeedAllOwnedConversationAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(seeded.ConversationId, "What needs attention?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Pick one location", answer.Title);
            Assert.Contains("Change Scope", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            AssertNoRetrieveGets();
            Assert.Equal(0, _homeRecommendation.CallCount);
            Assert.NotEqual("What needs attention?", _fake.LastInput?.UserMessage);
        }

        [Fact]
        public async Task SendTurn_CancelWithRetrieve_DropsInterviewAndRunsNormalRetrieve()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            var started = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(locationId, "Create an offer draft")
                )
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = started.Conversation.Id,
                    Message = "Never mind the draft. Summarise recent feedback.",
                    AnalysisScope = FirstSendRequest(locationId, "x").AnalysisScope,
                }
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("1 feedback item", answer.Body);
            Assert.DoesNotContain("offer type", answer.Body);
            Assert.False(ok.Conversation.DraftInterviewActive);
            Assert.Null(ok.Conversation.PendingOfferDraft);
        }

        [Fact]
        public async Task SendTurn_MixedUnsupportedWrite_GroundsAndAddsRefuseSentence()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback and delete the record")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("1 feedback item", answer.Body);
            Assert.Contains("cannot create, send, or change records", answer.Body);
        }

        [Fact]
        public async Task SendTurn_RetrieveFailure_IsFailureClass()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _retrieve.FailNext = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[1].Class);
            Assert.True(ok.Conversation.RetryEligible);
            Assert.Equal(
                ["checking", "retrieving"],
                _progress.Events.Select(item => item.Step)
            );
        }

        [Fact]
        public async Task RetryTurn_ReplacesFailure_WithoutSecondUserBubble()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            _fake.Fail();

            var failed = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(failed).Conversation.Id;
            _fake.ResetToCannedStub();

            var retried = await _service.RetryTurnAsync(ownerUserId: 7, conversationId);

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(retried);
            Assert.Equal(2, ok.Conversation.Messages.Count);
            Assert.Equal("user", ok.Conversation.Messages[0].Role);
            Assert.Equal("Summarise recent feedback", ok.Conversation.Messages[0].Body);
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Equal(1, await _context.AssistantMessages.CountAsync(row => row.Role == AssistantMessageRole.User));
        }

        [Fact]
        public async Task ApplyScope_HidesRetry_WhenSavedScopeNoLongerMatchesSend()
        {
            var first = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var second = await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            _fake.Fail();
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(first, "Summarise recent feedback")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            Assert.True(ok.Conversation.RetryEligible);

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
            Assert.False(after.Conversation.RetryEligible);
        }

        [Fact]
        public async Task SendTurn_Discloses100OfN_WhenListIsLargerThanSample()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            for (var index = 0; index < 101; index++)
            {
                await SeedFeedbackAsync(
                    locationId,
                    DateTime.UtcNow.AddMinutes(-index - 1),
                    FeedbackSentiment.Negative,
                    "[\"WaitTime\"]"
                );
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains("100 of 101", ok.Conversation.Messages[1].Body);
            Assert.Equal(101, _fake.LastInput!.Evidence.Feedback.TotalCount);
            Assert.Equal(100, _fake.LastInput.Evidence.Feedback.SampleCount);
            Assert.Equal(
                101,
                ok.Conversation.Messages[1].Actions
                    .Single(action => action.Type == "view-feedback-set")
                    .Count
            );
        }

        [Fact]
        public async Task Get_ReturnsOk_WhenLastActivityIs400DaysOld_AndWhenArchived()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created)
                .Conversation.Id;

            var stale = DateTime.UtcNow.AddDays(-400);
            var row = await _context.AssistantConversations.SingleAsync();
            row.CreatedAt = stale;
            row.LastActivityAt = stale;
            await _context.SaveChangesAsync();

            Assert.Null(typeof(AssistantConversation).GetProperty("ExpiresAt"));
            Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(7, conversationId)
            );

            row.IsArchived = true;
            await _context.SaveChangesAsync();

            var archived = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(7, conversationId)
            );
            Assert.True(archived.Conversation.IsArchived);
            Assert.Equal(
                1,
                await _context.AssistantConversations.CountAsync(c => c.Id == conversationId)
            );
        }

        [Fact]
        public async Task Retention_DoesNotApply_WhenNoFirstSend()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "   ")
            );

            Assert.IsType<AssistantTurnOutcome.Invalid>(outcome);
            Assert.Equal(0, await _context.AssistantConversations.CountAsync());
            Assert.Equal(0, await _context.AssistantMessages.CountAsync());
        }

        [Fact]
        public async Task DeleteAllForOwner_HardDeletesConversationsAndMessages_LeavesLinkedRecords()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var otherLocation = await SeedLocationAsync(ownerUserId: 99, "Soho");
            await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            await _service.SendTurnAsync(
                ownerUserId: 99,
                FirstSendRequest(otherLocation, "Other operator ask")
            );

            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Name = "Keep me",
                Status = "draft",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = "active",
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = "Keep offer",
                Description = "Linked offer stays",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();
            var campaignId = campaign.Id;
            var offerId = offer.Id;
            var feedbackId = await _context.Feedbacks
                .Where(row => row.RestaurantLocationId == locationId)
                .Select(row => row.Id)
                .SingleAsync();

            await _service.DeleteAllForOwnerAsync(7);

            Assert.Equal(
                0,
                await _context.AssistantConversations.CountAsync(row => row.OwnerUserId == 7)
            );
            Assert.Equal(
                0,
                await _context.AssistantMessages.CountAsync(
                    row => row.Conversation.OwnerUserId == 7
                )
            );
            Assert.Equal(
                1,
                await _context.AssistantConversations.CountAsync(row => row.OwnerUserId == 99)
            );
            Assert.True(await _context.Feedbacks.AnyAsync(row => row.Id == feedbackId));
            Assert.True(await _context.Campaigns.AnyAsync(row => row.Id == campaignId));
            Assert.True(await _context.CatalogOffers.AnyAsync(row => row.Id == offerId));
        }

        [Fact]
        public async Task Get_ReturnsOk_WhenActivationExpired_AndAfterExtend()
        {
            var user = await SeedUserAsync("expired-op@example.com");
            var locationId = await SeedLocationAsync(user.Id, "Camden");
            var created = await _service.SendTurnAsync(
                user.Id,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created)
                .Conversation.Id;
            await _service.SetArchivedAsync(user.Id, conversationId, archived: true);

            user.ActivationExpiresAt = DateTime.UtcNow.AddDays(-10);
            await _context.SaveChangesAsync();

            Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(user.Id, conversationId)
            );

            var admin = CreateAdminService();
            await admin.ExtendActivationAsync(user.Id, new ExtendActivationDto());

            var after = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.GetAsync(user.Id, conversationId)
            );
            Assert.True(after.Conversation.IsArchived);
            Assert.Equal(
                1,
                await _context.AssistantConversations.CountAsync(row => row.Id == conversationId)
            );
        }

        [Fact]
        public async Task SendTurn_DropsInventedActionTypes()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                "A title for Camden",
                "Body over the last 7 days."
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Empty(ok.Conversation.Messages[1].Actions);
        }

        [Fact]
        public async Task Compare_FourValidNames_Clarifies_AndDoesNotRetrieve()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            await SeedSecondLocationAsync(7, "Soho");
            await SeedSecondLocationAsync(7, "Shoreditch");
            await SeedSecondLocationAsync(7, "Brixton");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Camden, Soho, Shoreditch and Brixton")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("clarify", answer.Class);
            Assert.Null(answer.Title);
            Assert.Empty(answer.Actions);
            Assert.False(ok.Conversation.RetryEligible);
            Assert.Contains("up to 3", answer.Body);
            Assert.Empty(_retrieve.Calls);
            Assert.Empty(_guestsRetrieve.Calls);
            Assert.Empty(_offersRetrieve.Calls);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Equal(
                ["checking"],
                _progress.Events.Select(item => item.Step)
            );
        }

        [Fact]
        public async Task CompareTo_IncludesSavedBaseline_TwoRetrieves()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedSecondLocationAsync(7, "Shoreditch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare to Soho")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Equal(2, _retrieve.Calls.Count);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == soho);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
            Assert.Contains("Camden", ok.Conversation.Messages[1].Body);
            Assert.Contains("Soho", ok.Conversation.Messages[1].Body);
        }

        [Fact]
        public async Task Compare_HereAndThisLocation_IncludesSavedBaseline()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");

            var here = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare here and Soho")
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(here);
            Assert.Equal(2, _retrieve.Calls.Count);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == soho);

            _retrieve.Calls.Clear();
            var thisLocation = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = Assert.IsType<AssistantTurnOutcome.Ok>(here).Conversation.Id,
                    Message = "this location vs Soho",
                    AnalysisScope = FirstSendRequest(camden, "x").AnalysisScope,
                }
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(thisLocation);
            Assert.Equal(2, _retrieve.Calls.Count);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == soho);
        }

        [Fact]
        public async Task Compare_ExplicitSet_DoesNotAddSaved()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            var shoreditch = await SeedSecondLocationAsync(7, "Shoreditch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Soho and Shoreditch")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(3, _retrieve.Calls.Count);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == soho);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == shoreditch);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task Compare_UnnamedAndAll_ClarifyIncludingCapturePaused_NoRetrieve()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            await SeedSecondLocationAsync(7, "Soho");
            await SeedSecondLocationAsync(
                7,
                "Shoreditch",
                captureStatus: CaptureLocationStatus.Paused
            );

            foreach (var message in new[]
            {
                "Compare my locations",
                "compare all locations",
                "every location",
            })
            {
                _retrieve.Calls.Clear();
                var outcome = await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, message)
                );
                var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
                var answer = ok.Conversation.Messages[^1];
                Assert.Equal("clarify", answer.Class);
                Assert.Null(answer.Title);
                Assert.Empty(answer.Actions);
                Assert.False(ok.Conversation.RetryEligible);
                Assert.Contains("Camden", answer.Body);
                Assert.Contains("Soho", answer.Body);
                Assert.Contains("Shoreditch (Capture-Paused)", answer.Body);
                Assert.Empty(_retrieve.Calls);
                Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
            }
        }

        [Fact]
        public async Task Compare_AmbiguousName_Clarifies_NoGuess()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            await SeedSecondLocationAsync(7, "Soho Kitchen");
            await SeedSecondLocationAsync(7, "Soho Bar");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Soho and Camden")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("clarify", ok.Conversation.Messages[1].Class);
            Assert.Contains("Soho Kitchen", ok.Conversation.Messages[1].Body);
            Assert.Contains("Soho Bar", ok.Conversation.Messages[1].Body);
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task Compare_UnknownName_Dropped_ContinuesWithBaseline()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedSecondLocationAsync(7, "Shoreditch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Soho and Atlantis")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Contains("Atlantis", ok.Conversation.Messages[1].Body);
            Assert.Equal(2, _retrieve.Calls.Count);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == soho);
        }

        [Fact]
        public async Task Compare_FewerThanTwoValid_Clarifies()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            await SeedSecondLocationAsync(7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Atlantis")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("clarify", ok.Conversation.Messages[1].Class);
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task Compare_SingleMode_GroundedCaveat_RetrievesSavedOnly()
        {
            var camden = await SeedLocationAsync(7, "Camden", accountType: "Single");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare to Soho")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Contains("no other Owned location", ok.Conversation.Messages[1].Body);
            Assert.Single(_retrieve.Calls);
            Assert.Equal(camden, _retrieve.Calls[0].OwnedLocationId);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task MentionWithoutCompare_RetrievesSavedOnly_GroundedCaveat()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "How is Soho doing?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Contains("not a Compare turn", ok.Conversation.Messages[1].Body);
            Assert.Single(_retrieve.Calls);
            Assert.Equal(camden, _retrieve.Calls[0].OwnedLocationId);
            Assert.DoesNotContain(_retrieve.Calls, call => call.OwnedLocationId == soho);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task Compare_FollowUpReusesLastSet_OtherQuestionReturnsToSaved()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            var shoreditch = await SeedSecondLocationAsync(7, "Shoreditch");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Soho and Shoreditch")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created).Conversation.Id;
            _retrieve.Calls.Clear();

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = conversationId,
                    Message = "which one had more complaints?",
                    AnalysisScope = FirstSendRequest(camden, "x").AnalysisScope,
                }
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            Assert.Equal(3, _retrieve.Calls.Count);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == camden);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == soho);
            Assert.Contains(_retrieve.Calls, call => call.OwnedLocationId == shoreditch);
            _retrieve.Calls.Clear();

            var next = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = conversationId,
                    Message = "Summarise recent feedback",
                    AnalysisScope = FirstSendRequest(camden, "x").AnalysisScope,
                }
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(next);
            Assert.Single(_retrieve.Calls);
            Assert.Equal(camden, _retrieve.Calls[0].OwnedLocationId);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task Compare_UsesSavedReportingPeriod_TwoPeriodAskStaysOnSaved()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedSecondLocationAsync(7, "Shoreditch");

            var compared = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare to Soho")
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(compared);
            Assert.Equal(2, _retrieve.Calls.Count);
            Assert.Equal(_retrieve.Calls[0].FromUtc, _retrieve.Calls[1].FromUtc);
            Assert.Equal(_retrieve.Calls[0].ToUtc, _retrieve.Calls[1].ToUtc);
            var window = AssistantReportingPeriodWindow.Resolve(
                FirstSendRequest(camden, "x").AnalysisScope.ReportingPeriod,
                DateTime.UtcNow
            );
            Assert.Equal(window.FromUtc, _retrieve.Calls[0].FromUtc);
            _retrieve.Calls.Clear();

            var twoPeriod = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = Assert.IsType<AssistantTurnOutcome.Ok>(compared).Conversation.Id,
                    Message = "Compare last week to last month",
                    AnalysisScope = FirstSendRequest(camden, "x").AnalysisScope,
                }
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(twoPeriod);
            Assert.Equal("grounded", ok.Conversation.Messages[^1].Class);
            Assert.Contains("one Reporting period", ok.Conversation.Messages[^1].Body);
            Assert.Single(_retrieve.Calls);
            Assert.Equal(camden, _retrieve.Calls[0].OwnedLocationId);
        }

        [Fact]
        public async Task Compare_ActionsUseSavedScopeEvidenceOnly()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedFeedbackAsync(camden, DateTime.UtcNow.AddHours(-1));
            await SeedFeedbackAsync(soho, DateTime.UtcNow.AddHours(-2));
            await SeedFeedbackAsync(soho, DateTime.UtcNow.AddHours(-3));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare to Soho")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var set = ok.Conversation.Messages[1].Actions
                .Single(action => action.Type == "view-feedback-set");
            Assert.Equal(1, set.Count);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
        }

        [Fact]
        public async Task Compare_RetrievesSameSixDomains_ForEachNamedLocation()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedCatalogOfferAsync(soho, "Soho brunch");
            await SeedCampaignAsync(
                soho,
                "Soho lunch push",
                CampaignsListService.ScheduledStatus
            );
            var qrId = await SeedQrCodeAsync(soho);
            await SeedQrScanAsync(soho, qrId, DateTime.UtcNow.AddHours(-1));
            await SeedLocationGuestAsync(soho, DateTime.UtcNow.AddHours(-2));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Capture at Camden and Soho")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            AssertNamedLocationCalls(_offersRetrieve.Calls, camden, soho);
            AssertNamedLocationCalls(_campaignsRetrieve.Calls, camden, soho);
            AssertNamedLocationCalls(_captureRetrieve.Calls, camden, soho);
            AssertNamedLocationCalls(_homeRetrieve.Calls, camden, soho);
            Assert.Equal(2, _guestsRetrieve.Calls.Count);
            Assert.Contains(camden, _guestsRetrieve.Calls);
            Assert.Contains(soho, _guestsRetrieve.Calls);
            AssertNamedLocationCalls(_retrieve.Calls, camden, soho);

            var compare = _fake.LastInput!.CompareLocations;
            Assert.NotNull(compare);
            Assert.Equal(2, compare.Count);
            var sohoRow = Assert.Single(
                compare,
                row => row.OwnedLocationId == soho
            );
            Assert.Equal(1, sohoRow.Evidence.Offers.CatalogTotalCount);
            Assert.Equal(1, sohoRow.Evidence.Campaigns.ListTotalCount);
            Assert.Equal(1, sohoRow.Evidence.Capture.QrScans);
            Assert.Equal(1, sohoRow.Evidence.Home.GuestsJoined);
            Assert.Equal(1, sohoRow.Evidence.Guests.TotalCount);
            var camdenRow = Assert.Single(
                compare,
                row => row.OwnedLocationId == camden
            );
            Assert.Equal(0, camdenRow.Evidence.Offers.CatalogTotalCount);
        }

        [Fact]
        public async Task Clarify_IsBodyOnly_NotRetryable()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            await SeedSecondLocationAsync(7, "Soho");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "compare all locations")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("clarify", answer.Class);
            Assert.Null(answer.Title);
            Assert.Empty(answer.Actions);
            Assert.False(ok.Conversation.RetryEligible);
            Assert.False(string.IsNullOrWhiteSpace(answer.Body));
        }

        [Fact]
        public async Task SendTurn_RedactsEmailAndMobile_FromRetrievePayloadAndAnswer()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var guestId = await SeedLinkedGuestAsync(
                locationId,
                "Pat Guest",
                email: "pat@example.com",
                mobile: "07700900999"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-1),
                locationGuestId: guestId,
                guestContact: "pat@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "List feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            AssertNoContact(answer.Title, answer.Body);
            Assert.Contains("Pat Guest", answer.Body);
            Assert.DoesNotContain(
                "pat@example.com",
                answer.Body,
                StringComparison.OrdinalIgnoreCase
            );

            Assert.NotNull(_fake.LastInput);
            foreach (var row in _fake.LastInput!.Evidence.Feedback.Rows)
            {
                AssertNoContact(null, row.GuestName);
                AssertNoContact(null, row.Excerpt);
                AssertNoContact(null, row.FeedbackReference);
            }

            var promptJson = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "test-deployment",
                _fake.LastInput,
                "1"
            );
            AssertNoContact(null, promptJson);
            Assert.DoesNotContain("\"guestContact\"", promptJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"locationGuestId\"", promptJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_ListAsk_CapsNamedRowsAtFive_ThenAndNMore()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            for (var index = 0; index < 7; index++)
            {
                await SeedFeedbackAsync(
                    locationId,
                    DateTime.UtcNow.AddMinutes(-(index + 1)),
                    guestName: $"Named Guest {index + 1}"
                );
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "List feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[1].Body;
            Assert.Contains("Named Guest 1", body);
            Assert.Contains("Named Guest 5", body);
            Assert.DoesNotContain("Named Guest 6", body);
            Assert.DoesNotContain("Named Guest 7", body);
            Assert.Contains("and 2 more", body);
            Assert.Contains(
                ok.Conversation.Messages[1].Actions,
                action => action.Type == "view-feedback-set"
            );
        }

        [Fact]
        public async Task SendTurn_SummariseOmitsNames_ListIncludesNames_SummariseCapsExcerpts()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var names = new[] { "Ava Guest", "Ben Guest", "Cara Guest", "Drew Guest" };
            for (var index = 0; index < names.Length; index++)
            {
                await SeedFeedbackAsync(
                    locationId,
                    DateTime.UtcNow.AddMinutes(-(index + 1)),
                    guestName: names[index],
                    comment: $"Comment excerpt {index + 1} about service"
                );
            }

            var summarised = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var summariseBody = Assert.IsType<AssistantTurnOutcome.Ok>(summarised)
                .Conversation.Messages[1].Body;
            foreach (var name in names)
            {
                Assert.DoesNotContain(name, summariseBody);
            }

            var excerptCount = System.Text.RegularExpressions.Regex
                .Matches(summariseBody, "Comment excerpt")
                .Count;
            Assert.InRange(excerptCount, 1, 3);

            var listed = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Show feedback",
                    Assert.IsType<AssistantTurnOutcome.Ok>(summarised).Conversation.Id
                )
            );
            var listBody = Assert.IsType<AssistantTurnOutcome.Ok>(listed)
                .Conversation.Messages.Last().Body;
            Assert.Contains("Ava Guest", listBody);
        }

        [Fact]
        public async Task SendTurn_Placeholder4_IntersectsNegativeInWindowWithCurrentEligible()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var includedId = await SeedLinkedGuestAsync(
                locationId,
                "Included Eligible",
                email: "included@example.com"
            );
            var optedOutId = await SeedLinkedGuestAsync(
                locationId,
                "Opted Out",
                email: "opted@example.com",
                offersOptOut: true
            );
            var outsideId = await SeedLinkedGuestAsync(
                locationId,
                "Outside Window",
                email: "outside@example.com"
            );
            var recoveryId = await SeedLinkedGuestAsync(
                locationId,
                "Needs Recovery Old",
                email: "recovery@example.com"
            );

            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                locationGuestId: includedId,
                guestName: "Included Eligible"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                locationGuestId: optedOutId,
                guestName: "Opted Out"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                FeedbackSentiment.Positive,
                locationGuestId: outsideId,
                guestName: "Outside Window"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddDays(-20),
                locationGuestId: outsideId,
                guestName: "Outside Window"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddDays(-20),
                locationGuestId: recoveryId,
                guestName: "Needs Recovery Old"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Show guests who gave poor feedback but opted in"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[1].Body;
            Assert.Contains("Included Eligible", body);
            Assert.DoesNotContain("Opted Out", body);
            Assert.DoesNotContain("Outside Window", body);
            Assert.DoesNotContain("Needs Recovery Old", body);
            Assert.DoesNotContain("consent", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("current state", body, StringComparison.OrdinalIgnoreCase);

            var guestsAction = Assert.Single(
                ok.Conversation.Messages[1].Actions,
                action => action.Type == "view-guests"
            );
            Assert.True(guestsAction.MarketingEligible);
            Assert.NotEqual("needs-recovery", guestsAction.SmartGroup);
            Assert.Null(guestsAction.SmartGroup);
            Assert.DoesNotContain(
                ok.Conversation.Messages[1].Actions,
                action => action.Type == "view-guest"
            );
        }

        [Fact]
        public async Task SendTurn_UnlinkedFeedback_AppearsOnFeedbackList_NotGuestList()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-1),
                guestName: "Unlinked Snapshot"
            );
            var linkedId = await SeedLinkedGuestAsync(
                locationId,
                "Linked Guest",
                email: "linked@example.com"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddMinutes(-30),
                locationGuestId: linkedId,
                guestName: "Linked Guest"
            );

            var feedbackList = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "List feedback")
            );
            var feedbackBody = Assert.IsType<AssistantTurnOutcome.Ok>(feedbackList)
                .Conversation.Messages[1].Body;
            Assert.Contains("Unlinked Snapshot", feedbackBody);
            Assert.Contains("Linked Guest", feedbackBody);

            var guestList = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Show guests",
                    Assert.IsType<AssistantTurnOutcome.Ok>(feedbackList).Conversation.Id
                )
            );
            var guestBody = Assert.IsType<AssistantTurnOutcome.Ok>(guestList)
                .Conversation.Messages.Last().Body;
            Assert.Contains("Linked Guest", guestBody);
            Assert.DoesNotContain("Unlinked Snapshot", guestBody);
        }

        [Fact]
        public async Task SendTurn_ListGuests_ReadsLocationGuests_NotOnlyFeedbackSample()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Only Guest",
                email: "only@example.com"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show guests")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[1].Body;
            Assert.Equal("grounded", ok.Conversation.Messages[1].Class);
            Assert.Contains("Only Guest", body);
            Assert.DoesNotContain("the last 7 days", body);
            Assert.Contains("current state", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(
                ok.Conversation.Messages[1].Actions,
                action => action.Type == "view-guest"
            );
        }

        [Fact]
        public async Task SendTurn_HidesGuestActions_OnSummarise_AndShowsByGuestCount()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var first = await SeedLinkedGuestAsync(
                locationId,
                "First Guest",
                email: "first@example.com"
            );
            var second = await SeedLinkedGuestAsync(
                locationId,
                "Second Guest",
                email: "second@example.com"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-2),
                locationGuestId: first,
                guestName: "First Guest"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-1),
                locationGuestId: second,
                guestName: "Second Guest"
            );

            var summarised = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var summariseActions = Assert.IsType<AssistantTurnOutcome.Ok>(summarised)
                .Conversation.Messages[1].Actions;
            Assert.DoesNotContain(
                summariseActions,
                action => action.Type is "view-guests" or "view-guest"
            );

            var many = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Show guests",
                    Assert.IsType<AssistantTurnOutcome.Ok>(summarised).Conversation.Id
                )
            );
            var manyActions = Assert.IsType<AssistantTurnOutcome.Ok>(many)
                .Conversation.Messages.Last().Actions;
            Assert.Contains(manyActions, action => action.Type == "view-guests");
            Assert.DoesNotContain(manyActions, action => action.Type == "view-guest");
        }

        [Fact]
        public async Task SendTurn_ListExactlyOneLocationGuest_OffersViewGuestOnly()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var guestId = await SeedLinkedGuestAsync(
                locationId,
                "Solo Guest",
                email: "solo@example.com"
            );
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-1),
                locationGuestId: guestId,
                guestName: "Solo Guest"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show guests")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains("Solo Guest", ok.Conversation.Messages[1].Body);
            var actions = ok.Conversation.Messages[1].Actions;
            Assert.Contains(actions, action => action.Type == "view-guest");
            Assert.DoesNotContain(actions, action => action.Type == "view-guests");
            Assert.Equal(
                guestId,
                actions.Single(action => action.Type == "view-guest").GuestId
            );
        }

        [Fact]
        public async Task SendTurn_GroundsOnOffersCatalogAndPerformance()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var offerId = await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            await SeedOfferIssueAsync(
                locationId,
                offerId,
                claimedAt: DateTime.UtcNow.AddHours(-2),
                redeemedAt: DateTime.UtcNow.AddHours(-1)
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise Offers Performance")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Weekend brunch", answer.Body);
            Assert.Contains("Offers Performance over the last 7 days", answer.Body);
            Assert.Contains("1 claims", answer.Body);
            Assert.Contains("1 redemptions", answer.Body);
            Assert.Contains(answer.Actions, action => action.Type == "view-offers");
            Assert.Equal(1, _fake.LastInput!.Evidence.Offers.CatalogTotalCount);
            Assert.Equal(1, _fake.LastInput.Evidence.Offers.Claims);
        }

        [Fact]
        public async Task SendTurn_Discloses100OfN_WhenOffersCatalogIsLargerThanSample()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            for (var index = 0; index < 101; index++)
            {
                await SeedCatalogOfferAsync(locationId, $"Offer {index}");
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "List the offers")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains("100 of 101", ok.Conversation.Messages[1].Body);
            Assert.Equal(101, _fake.LastInput!.Evidence.Offers.CatalogTotalCount);
            Assert.Equal(100, _fake.LastInput.Evidence.Offers.CatalogSampleCount);
        }

        [Fact]
        public async Task SendTurn_GroundsOnCampaignsListAndSummary_Discloses100OfN()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            for (var index = 0; index < 100; index++)
            {
                await SeedCampaignAsync(
                    locationId,
                    $"Campaign {index}",
                    CampaignsListService.DraftStatus
                );
            }
            await SeedCampaignAsync(
                locationId,
                "Lunch push",
                CampaignsListService.ScheduledStatus
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise Campaigns")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Lunch push", answer.Body);
            Assert.Contains("scheduled", answer.Body);
            Assert.Contains("100 of 101", answer.Body);
            Assert.Contains(answer.Actions, action => action.Type == "view-campaigns");
            Assert.Equal(101, _fake.LastInput!.Evidence.Campaigns.ListTotalCount);
            Assert.Equal(100, _fake.LastInput.Evidence.Campaigns.ListSampleCount);
        }

        [Fact]
        public async Task SendTurn_OmitsCampaignMessageCopy_WhenAskDoesNotNeedIt()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCampaignAsync(
                locationId,
                "Lunch push",
                CampaignsListService.ScheduledStatus,
                messageSubject: "This weekend only",
                messageBody: "Come back this weekend for 20% off."
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise Campaigns")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains(false, _campaignsRetrieve.IncludeMessageCopyCalls);
            Assert.DoesNotContain(true, _campaignsRetrieve.IncludeMessageCopyCalls);
            var details = Assert.Single(_fake.LastInput!.Evidence.Campaigns.Details);
            Assert.Equal("Lunch push", details.Name);
            Assert.Null(details.MessageSubject);
            Assert.Null(details.MessageBody);

            var promptJson = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "test-deployment",
                _fake.LastInput,
                "1"
            );
            Assert.DoesNotContain("Come back this weekend for 20% off.", promptJson);
            Assert.DoesNotContain("This weekend only", promptJson);
        }

        [Fact]
        public async Task SendTurn_LoadsCampaignMessageCopy_WhenAskNeedsIt()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCampaignAsync(
                locationId,
                "Lunch push",
                CampaignsListService.ScheduledStatus,
                messageSubject: "This weekend only",
                messageBody: "Come back this weekend for 20% off."
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "What does the Lunch push campaign message say?"
                )
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains(true, _campaignsRetrieve.IncludeMessageCopyCalls);
            var details = Assert.Single(_fake.LastInput!.Evidence.Campaigns.Details);
            Assert.Equal("This weekend only", details.MessageSubject);
            Assert.Equal("Come back this weekend for 20% off.", details.MessageBody);
        }

        [Fact]
        public async Task Compare_OmitsCampaignMessageCopy_ForExtraLocations()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedCampaignAsync(
                soho,
                "Soho lunch push",
                CampaignsListService.ScheduledStatus,
                messageSubject: "Soho subject",
                messageBody: "Soho secret body"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare Campaigns at Camden and Soho")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, _campaignsRetrieve.IncludeMessageCopyCalls.Count);
            Assert.All(_campaignsRetrieve.IncludeMessageCopyCalls, copy => Assert.False(copy));
            var sohoRow = Assert.Single(
                _fake.LastInput!.CompareLocations!,
                row => row.OwnedLocationId == soho
            );
            var details = Assert.Single(sohoRow.Evidence.Campaigns.Details);
            Assert.Null(details.MessageSubject);
            Assert.Null(details.MessageBody);
        }

        [Fact]
        public async Task Compare_LoadsCampaignMessageCopy_ForExtraLocationsWhenAskNeedsIt()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedCampaignAsync(
                soho,
                "Soho lunch push",
                CampaignsListService.ScheduledStatus,
                messageSubject: "Soho subject",
                messageBody: "Soho secret body"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    camden,
                    "Compare campaign messages at Camden and Soho"
                )
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(2, _campaignsRetrieve.IncludeMessageCopyCalls.Count);
            Assert.All(_campaignsRetrieve.IncludeMessageCopyCalls, copy => Assert.True(copy));
            var sohoRow = Assert.Single(
                _fake.LastInput!.CompareLocations!,
                row => row.OwnedLocationId == soho
            );
            var details = Assert.Single(sohoRow.Evidence.Campaigns.Details);
            Assert.Equal("Soho subject", details.MessageSubject);
            Assert.Equal("Soho secret body", details.MessageBody);
        }

        [Fact]
        public async Task Compare_Discloses100OfN_PerLocationPagedDomain()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            await SeedCatalogOfferAsync(camden, "Camden brunch");
            for (var index = 0; index < 101; index++)
            {
                await SeedCatalogOfferAsync(soho, $"Soho offer {index}");
            }
            for (var index = 0; index < 101; index++)
            {
                await SeedLinkedGuestAsync(
                    soho,
                    $"Soho guest {index + 1}",
                    email: $"soho-guest-{index + 1}@example.com"
                );
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare offers at Camden and Soho")
            );

            Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var compare = _fake.LastInput!.CompareLocations!;
            var camdenRow = Assert.Single(compare, row => row.OwnedLocationId == camden);
            var sohoRow = Assert.Single(compare, row => row.OwnedLocationId == soho);
            Assert.Equal(1, camdenRow.Evidence.Offers.CatalogTotalCount);
            Assert.False(camdenRow.Evidence.Offers.DisclosesSample);
            Assert.Equal(101, sohoRow.Evidence.Offers.CatalogTotalCount);
            Assert.Equal(100, sohoRow.Evidence.Offers.CatalogSampleCount);
            Assert.True(sohoRow.Evidence.Offers.DisclosesSample);
            Assert.Equal(101, sohoRow.Evidence.Guests.TotalCount);
            Assert.Equal(100, sohoRow.Evidence.Guests.SampleCount);
            Assert.True(sohoRow.Evidence.Guests.DisclosesSample);
        }

        [Fact]
        public async Task Compare_ActionsDoNotDeepLinkExtraLocationOffers()
        {
            var camden = await SeedLocationAsync(7, "Camden");
            var soho = await SeedSecondLocationAsync(7, "Soho");
            var sohoOfferId = await SeedCatalogOfferAsync(soho, "Soho brunch");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Compare offers at Camden and Soho")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
            Assert.DoesNotContain(
                ok.Conversation.Messages[1].Actions,
                action => action.Type == "view-offer" && action.OfferId == sohoOfferId
            );
            Assert.DoesNotContain(
                _fake.LastInput!.Evidence.Offers.Catalog,
                offer => offer.Id == sohoOfferId
            );
            Assert.Contains(
                _fake.LastInput.CompareLocations!,
                row => row.OwnedLocationId == soho
                    && row.Evidence.Offers.Catalog.Any(offer => offer.Id == sohoOfferId)
            );
            Assert.Empty(
                AssistantActionCatalog.Validate(
                    [new AssistantActionDto { Type = "view-offer", OfferId = sohoOfferId }],
                    AssistantMessageClass.Grounded,
                    _fake.LastInput.Evidence
                )
            );
        }

        [Fact]
        public async Task SendTurn_ListGuests_Discloses100OfN_AndKeepsEmailMobileOffPrompt()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            for (var index = 0; index < 101; index++)
            {
                await SeedLinkedGuestAsync(
                    locationId,
                    $"Guest {index + 1}",
                    email: $"guest-{index + 1}@example.com"
                );
            }

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show guests")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[1].Body;
            Assert.Contains("Guest 101", body);
            Assert.Contains("Guest 97", body);
            Assert.DoesNotContain("Guest 96", body);
            Assert.Contains("and 95 more", body);
            Assert.Contains("100 of 101", body);
            Assert.Equal(101, _fake.LastInput!.Evidence.Guests.TotalCount);
            Assert.Equal(100, _fake.LastInput.Evidence.Guests.SampleCount);

            var promptJson = AssistantLiveAnswerStructuredOutput.BuildRequestJson(
                "test-deployment",
                _fake.LastInput,
                "1"
            );
            AssertNoContact(null, promptJson);
            Assert.DoesNotContain("\"locationGuestId\"", promptJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("guest-101@example.com", promptJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_GroundsOnCaptureSnapshot_WithoutOfferClaims()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var qrId = await SeedQrCodeAsync(locationId);
            await SeedQrScanAsync(locationId, qrId, DateTime.UtcNow.AddHours(-2));
            await SeedQrScanAsync(locationId, qrId, DateTime.UtcNow.AddHours(-1));
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddHours(-1),
                qrCodeId: qrId
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How is Capture performing?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("2 QR scans", answer.Body);
            Assert.Contains("Previous window", answer.Body);
            Assert.Contains("SmartGuest", answer.Body);
            Assert.DoesNotContain("offerClaims", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(answer.Actions, action => action.Type == "view-capture");
            Assert.Equal(2, _fake.LastInput!.Evidence.Capture.QrScans);
            Assert.DoesNotContain(
                "offerClaims",
                answer.Body,
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public async Task SendTurn_GroundsOnHomeKpis_WithoutOfferRedemptions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));
            await SeedLocationGuestAsync(locationId, DateTime.UtcNow.AddHours(-2));
            var qrId = await SeedQrCodeAsync(locationId);
            await SeedQrScanAsync(locationId, qrId, DateTime.UtcNow.AddHours(-3));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What is the Performance overview?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("feedbackSubmitted", answer.Body);
            Assert.Contains("guestsJoined", answer.Body);
            Assert.Contains("qrScans", answer.Body);
            Assert.DoesNotContain("offer redemption", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(1, _fake.LastInput!.Evidence.Home.FeedbackSubmitted);
            Assert.Equal(1, _fake.LastInput.Evidence.Home.GuestsJoined);
            Assert.Equal(1, _fake.LastInput.Evidence.Home.QrScans);
        }

        [Fact]
        public async Task SendTurn_HomeOfferRedemptions_UsesOffersPerformance_NotStubZeros()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var offerId = await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            await SeedOfferIssueAsync(
                locationId,
                offerId,
                claimedAt: DateTime.UtcNow.AddHours(-2),
                redeemedAt: DateTime.UtcNow.AddHours(-1)
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "How many Home offer redemptions?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("Offers Performance", answer.Body);
            Assert.Contains("1 redemptions", answer.Body);
            Assert.DoesNotContain("Home offer redemptions: 0", answer.Body);
            Assert.DoesNotContain("offerClaims", answer.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SendTurn_CaptureOverviewAndTemplates_AreRefusal()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var overview = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show me the Campaign templates")
            );
            var overviewOk = Assert.IsType<AssistantTurnOutcome.Ok>(overview);
            Assert.Equal("refusal", overviewOk.Conversation.Messages[1].Class);
            Assert.Contains(
                "Campaign templates",
                overviewOk.Conversation.Messages[1].Body
            );

            var latest = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = overviewOk.Conversation.Id,
                    Message = "What is on Latest activity?",
                    AnalysisScope = FirstSendRequest(locationId, "x").AnalysisScope,
                }
            );
            var latestOk = Assert.IsType<AssistantTurnOutcome.Ok>(latest);
            Assert.Equal("refusal", latestOk.Conversation.Messages[^1].Class);
            Assert.Contains("Latest activity", latestOk.Conversation.Messages[^1].Body);
        }

        [Fact]
        public async Task SendTurn_MixedCaptureOverview_GroundsInScope_AndRefusesOverview()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var qrId = await SeedQrCodeAsync(locationId);
            await SeedQrScanAsync(locationId, qrId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "How is Capture performing and what is on Capture overview?"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("grounded", answer.Class);
            Assert.Contains("QR scans", answer.Body);
            Assert.Contains("Capture overview", answer.Body);
        }

        [Fact]
        public async Task SendTurn_CurrentStateCatalogAndInFlight_AreNotInsidePeriod()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            await SeedCampaignAsync(
                locationId,
                "Lunch push",
                CampaignsListService.ScheduledStatus,
                audienceKey: "all-eligible-guests"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "What catalog offers and in-flight Campaigns do we have?"
                )
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[1].Body;
            Assert.Contains("Weekend brunch", body);
            Assert.Contains("scheduled", body);
            Assert.DoesNotContain(
                "Weekend brunch over the last 7 days",
                body
            );
            Assert.DoesNotContain("scheduled over the last 7 days", body);
            Assert.DoesNotContain(
                "currently eligible over the last 7 days",
                body
            );
        }

        [Fact]
        public async Task SendTurn_ClaimLogs_OnlyIncludeReportingPeriod()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var offerId = await SeedCatalogOfferAsync(locationId, "Weekend brunch");
            await SeedOfferIssueAsync(
                locationId,
                offerId,
                claimedAt: DateTime.UtcNow.AddHours(-2),
                claimCode: "TUM-100001"
            );
            await SeedOfferIssueAsync(
                locationId,
                offerId,
                claimedAt: DateTime.UtcNow.AddDays(-20),
                claimCode: "TUM-200002"
            );

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Which offer claims happened recently?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var body = ok.Conversation.Messages[1].Body;
            Assert.Contains("TUM-100001", body);
            Assert.DoesNotContain("TUM-200002", body);
            Assert.Single(_fake.LastInput!.Evidence.Offers.ClaimLogs);
            Assert.Equal("TUM-100001", _fake.LastInput.Evidence.Offers.ClaimLogs[0].ClaimCode);
        }

        [Fact]
        public async Task SendTurn_OffersRetrieveFailure_IsFailureClass()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _offersRetrieve.FailNext = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise Offers Performance")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Equal("failure", ok.Conversation.Messages[1].Class);
            Assert.True(ok.Conversation.RetryEligible);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private async Task<User> SeedUserAsync(string email)
        {
            var user = new User
            {
                FullName = "Expired Operator",
                Email = email,
                PasswordHash = "hash",
                PhoneNumber = "+447123456789",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                ActivatedAt = DateTime.UtcNow.AddDays(-40),
                ActivationExpiresAt = DateTime.UtcNow.AddDays(20),
            };
            _context.Users.Add(user);
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Expired Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Expired Operator",
                    Email = email,
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = true,
                    AccountType = "Single",
                    Status = TrialRequestStatus.AccountCreated,
                }
            );
            await _context.SaveChangesAsync();
            return user;
        }

        private AdminService CreateAdminService()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.com",
                        ["JwtSettings:Secret"] =
                            "test-secret-key-that-is-long-enough-for-hmac-sha256",
                    }
                )
                .Build();

            return new AdminService(
                _context,
                new TrialReviewTransition(
                    _context,
                    new TrackingEmailService(),
                    configuration,
                    NullLogger<TrialReviewTransition>.Instance
                ),
                configuration,
                NullLogger<AdminService>.Instance,
                _service
            );
        }

        private sealed class ThrowingCampaignEligibilityService : ICampaignEligibilityService
        {
            public Task<CampaignEligibilityDto> EvaluateAsync(
                int locationId,
                string audienceKey,
                CancellationToken cancellationToken = default
            )
                => throw new InvalidOperationException("eligibility boom");

            public Task<IReadOnlyList<int>> ListChannelEligibleLocationGuestIdsAsync(
                int locationId,
                string audienceKey,
                string channel,
                CancellationToken cancellationToken = default
            )
                => throw new InvalidOperationException("eligibility boom");
        }

        private sealed class ThrowingCampaignDraftService : ICampaignDraftService
        {
            public Task<CampaignDraftDto> CreateAsync(
                CreateCampaignDraftRequest request,
                int createdByUserId,
                CancellationToken cancellationToken = default
            )
                => throw new InvalidOperationException("Campaign create failed.");

            public Task<CampaignDraftDto?> GetByIdAsync(
                int campaignId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<CampaignDraftDto?>(null);

            public Task<int?> GetLocationIdAsync(
                int campaignId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<int?>(null);

            public Task<CampaignDraftWriteResult> PatchAsync(
                int campaignId,
                PatchCampaignDraftRequest request,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();
        }

        private sealed class ThrowingOffersCatalogService : IOffersCatalogService
        {
            public Task<CatalogOfferDto> CreateActiveAsync(
                CreateCatalogOfferRequest request,
                int? createdByUserId = null,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CatalogOfferDto> CreateDraftAsync(
                CreateCatalogOfferRequest request,
                int? createdByUserId = null,
                CancellationToken cancellationToken = default
            )
                => throw new InvalidOperationException("Offer create failed.");

            public Task<CatalogOfferLifecycleResult> UpdateAsync(
                int offerId,
                CreateCatalogOfferRequest request,
                int utcOffsetMinutes = 0,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CatalogOfferDto?> GetByIdAsync(
                int offerId,
                int utcOffsetMinutes = 0,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<CatalogOfferDto?>(null);

            public Task<bool> IsAttachableForLocationAsync(
                int offerId,
                int locationId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult(false);

            public Task SyncInFlightStoredStatusAsync(
                int offerId,
                CancellationToken cancellationToken = default
            )
                => Task.CompletedTask;

            public Task SyncInFlightStoredStatusForAttachChangeAsync(
                int? previousOfferId,
                int? nextOfferId,
                CancellationToken cancellationToken = default
            )
                => Task.CompletedTask;

            public Task<CatalogOffersListResponse> ListAsync(
                CatalogOffersListQuery query,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CatalogOfferLifecycleResult> PauseAsync(
                int offerId,
                int utcOffsetMinutes = 0,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CatalogOfferLifecycleResult> ResumeAsync(
                int offerId,
                int utcOffsetMinutes = 0,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CatalogOfferLifecycleResult> ArchiveAsync(
                int offerId,
                int utcOffsetMinutes = 0,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();

            public Task<CatalogOfferLifecycleResult> DuplicateAsync(
                int offerId,
                int? createdByUserId = null,
                int utcOffsetMinutes = 0,
                CancellationToken cancellationToken = default
            )
                => throw new NotImplementedException();
        }

        private sealed class TrackingEmailService
            : TummlyBackend.Tests.Helpers.EmailServiceStubBase
        {
        }

        private void StubRetrieveConversationTitle(
            string conversationTitle,
            string? liveAnswerTitle = "No facts at Camden for the last 7 days"
        )
        {
            _fake.SucceedWith(
                AssistantMessageClass.Grounded,
                liveAnswerTitle,
                "There is nothing to summarise.",
                AssistantTask.Retrieve,
                conversationTitle
            );
        }

        private void AssertNoRetrieveGets()
        {
            Assert.Empty(_retrieve.Calls);
            Assert.Empty(_offersRetrieve.Calls);
            Assert.Empty(_campaignsRetrieve.Calls);
            Assert.Empty(_captureRetrieve.Calls);
            Assert.Empty(_homeRetrieve.Calls);
            Assert.Empty(_guestsRetrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_ShowWhatNeedsAttention_IsAttentionRetrieveNotFeedbackSummarise()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _retrieve.FailNext = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show what needs attention")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal("Nothing needs attention at Camden", answer.Title);
            Assert.Contains(
                AssistantAttentionCopy.NeedsAttentionEmpty,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("now-queue", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Empty(_retrieve.Calls);
            Assert.Equal(0, _homeRecommendation.CallCount);
            Assert.Equal(0, _weeklyBriefGenerate.CallCount);
        }

        [Fact]
        public async Task SendTurn_NeedsAttention_ListsHomeItemsAndKindLevelActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddMinutes(-12));
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddMinutes(-20),
                comment: "Cold chips"
            );
            _context.Campaigns.Add(
                new Campaign
                {
                    RestaurantLocationId = locationId,
                    Status = "failed",
                    Name = "Weekend SMS blast",
                    GoalId = "thank-recent-guests",
                    Channel = "sms",
                    OfferStance = "no-offer",
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    UpdatedAt = DateTime.UtcNow.AddHours(-1),
                }
            );
            await _context.SaveChangesAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What needs attention?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("2 items need attention at Camden", answer.Title);
            Assert.Contains(
                "2 feedback items need attention",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantHomeNeedsAttention.FeedbackBody,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("Weekend SMS blast", answer.Body, StringComparison.Ordinal);
            Assert.Contains("This campaign failed.", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("View all", answer.Body, StringComparison.Ordinal);
            Assert.Equal(2, answer.Actions.Count);
            Assert.Equal("view-feedback-set", answer.Actions[0].Type);
            Assert.Equal("needs-attention", answer.Actions[0].Tab);
            Assert.Equal(2, answer.Actions[0].Count);
            Assert.Equal("view-campaigns", answer.Actions[1].Type);
            Assert.Equal("Open Campaigns", answer.Actions[1].Label);
            Assert.DoesNotContain(answer.Actions, action => action.Type == "prepare-recovery");
            Assert.DoesNotContain(answer.Actions, action => action.Type == "review-campaign");
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_WhatShouldIDoToday_UsesReportingPeriodAndOmitsReviewCampaign()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "thank-recent-guests",
                Title = "Thank recent guests",
                Opportunity = "Guests joined this week.",
                WhyBullets = ["Recent joiners have not had a thank-you"],
                EligibleAudience = "12 Email-eligible guests",
                SuggestedChannel = "email",
                EstimatedUsage = "12 Email credits",
            };

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What should I do today?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Thank recent guests", answer.Title);
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Reporting period", answer.Body, StringComparison.Ordinal);
            Assert.Contains("the last 7 days", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Thank recent guests", answer.Body, StringComparison.Ordinal);
            Assert.Contains("12 Email-eligible guests", answer.Body, StringComparison.Ordinal);
            Assert.Contains("email", answer.Body, StringComparison.Ordinal);
            Assert.Contains("12 Email credits", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(1, _homeRecommendation.CallCount);
            Assert.NotNull(_homeRecommendation.LastRequest);
            Assert.Equal(locationId, _homeRecommendation.LastRequest!.LocationId);
            Assert.Equal("last7", _homeRecommendation.LastRequest.OverviewDatePreset);
            Assert.NotNull(_homeRecommendation.LastRequest.From);
            Assert.NotNull(_homeRecommendation.LastRequest.To);
            Assert.Equal(0, _weeklyBriefGenerate.CallCount);
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_WhatShouldIDoToday_None_HasNoActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "none",
            };

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What should I do next")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Contains(
                AssistantAttentionCopy.RecommendationNone,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("- **Type:** none", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
        }

        [Fact]
        public async Task SendTurn_WhatShouldIDoToday_LoadError_UsesHomeErrorString()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _homeRecommendation.FailNext = true;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What should I do today?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Contains(
                AssistantAttentionCopy.RecommendationLoadError,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantAttentionCopy.RetryThisSend,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(answer.Actions);
        }

        [Fact]
        public async Task SendTurn_ReviewOpenFeedbackToday_MapsViewFeedbackSet()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "review-open-feedback",
                Title = "Review open feedback",
                Opportunity = "Guests left feedback that still needs a response.",
                Action = new HomeRecommendationDomainActionDto
                {
                    Kind = "open-feedback",
                },
            };

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What should I do today at Camden?")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var action = Assert.Single(ok.Conversation.Messages[^1].Actions);
            Assert.Equal("view-feedback-set", action.Type);
            Assert.NotEqual("needs-attention", action.Tab);
        }

        [Fact]
        public async Task SendTurn_WeeklyBrief_PresentsStoredBodyWithoutActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var closedWeek = WeeklyBriefWeekKey.ForClosedPriorWeek(
                WeeklyBriefWeekKey.DefaultLocationTimeZoneId,
                DateTime.UtcNow
            );
            var body = new WeeklyBriefBody(
                Headline: "Quiet week at Camden",
                Capture: new WeeklyBriefSection(true, "12 guests joined.", null),
                Feedback: new WeeklyBriefSection(true, "Feedback was mixed.", null),
                Offers: new WeeklyBriefSection(false, "No offer movement.", null),
                Campaigns: new WeeklyBriefSection(false, "No campaigns sent.", null),
                WatchNext: ["Watch lunch covers", "Watch Friday SMS"]
            );
            _context.WeeklyBriefs.Add(
                new WeeklyBrief
                {
                    LocationId = locationId,
                    WeekKey = closedWeek.WeekKey,
                    Status = WeeklyBriefStatus.Succeeded,
                    GeneratedAtUtc = DateTime.UtcNow,
                    BodyJson = JsonSerializer.Serialize(body, WeeklyBriefStoreJson.Options),
                    MetricsJson = "{}",
                }
            );
            await _context.SaveChangesAsync();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "weekly brief")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Quiet week at Camden", answer.Title);
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Watch lunch covers", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, _weeklyBriefGenerate.CallCount);
            Assert.Equal(0, _homeRecommendation.CallCount);
        }

        [Fact]
        public async Task SendTurn_WeeklyBriefMissing_UsesHomeEmptyCopy()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _weeklyBriefGenerate.Mode = WeeklyBriefGenerateMode.Empty;

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "watch next")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Contains(
                AssistantAttentionCopy.WeeklyBriefEmptyTitle,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantAttentionCopy.WeeklyBriefEmptyHelper,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(answer.Actions);
            Assert.Equal(1, _weeklyBriefGenerate.CallCount);
        }

        [Fact]
        public async Task SendTurn_MixFocusToday_NamesEmptyRecommendation()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddMinutes(-5));
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "none",
            };

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "what should I focus on")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Contains(
                "1 feedback item needs attention",
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantAttentionCopy.RecommendationNone,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Equal("view-feedback-set", Assert.Single(answer.Actions).Type);
            Assert.Equal(1, _homeRecommendation.CallCount);
        }

        [Fact]
        public async Task SendTurn_SummariseRecentFeedback_IsNotAttentionRetrieve()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            Assert.Contains(
                "nothing to summarise",
                ok.Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );
            Assert.NotEmpty(_retrieve.Calls);
            Assert.Equal(0, _homeRecommendation.CallCount);
        }

        [Theory]
        [InlineData("What needs attention?")]
        [InlineData("What should I do today?")]
        [InlineData("weekly brief")]
        [InlineData("what should I focus on")]
        public async Task SendTurn_AllOwnedLocations_AttentionAsks_PickOneLocationAndSkipHome(
            string message
        )
        {
            await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "thank-recent-guests",
                Title = "Thank recent guests",
            };

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest(message)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Pick one location", answer.Title);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Data", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Change Scope", answer.Body, StringComparison.Ordinal);
            Assert.Contains("Owned location", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("Compare", answer.Body, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(answer.Actions);
            Assert.Equal("all", ok.Conversation.AnalysisScope.ScopeKind);
            Assert.Null(_fake.LastInput);
            AssertNoRetrieveGets();
            Assert.Equal(0, _homeRecommendation.CallCount);
            Assert.Equal(0, _weeklyBriefGenerate.CallCount);
        }

        [Fact]
        public async Task SendTurn_AllOwnedLocations_NeedsAttention_DoesNotPresentHomeQueue()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            await SeedFeedbackAsync(camden, DateTime.UtcNow.AddMinutes(-8));

            var allAsk = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest("What needs attention?")
            );

            var allOk = Assert.IsType<AssistantTurnOutcome.Ok>(allAsk);
            var allAnswer = allOk.Conversation.Messages[^1];
            Assert.Equal("Pick one location", allAnswer.Title);
            Assert.DoesNotContain(
                "Slow service at dinner",
                allAnswer.Body,
                StringComparison.Ordinal
            );
            Assert.DoesNotContain(
                "need attention at Camden",
                allAnswer.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(allAnswer.Actions);
            Assert.Equal(0, _homeRecommendation.CallCount);

            var applied = await _service.ApplyScopeAsync(
                ownerUserId: 7,
                allOk.Conversation.Id,
                new ApplyAssistantScopeRequest
                {
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        OwnedLocationId = camden,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "last7",
                        },
                    },
                }
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(applied);

            var oneAsk = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "What needs attention?", allOk.Conversation.Id)
            );

            var oneOk = Assert.IsType<AssistantTurnOutcome.Ok>(oneAsk);
            var oneAnswer = oneOk.Conversation.Messages[^1];
            Assert.Equal("1 item needs attention at Camden", oneAnswer.Title);
            Assert.Contains(
                "1 feedback item needs attention",
                oneAnswer.Body,
                StringComparison.Ordinal
            );
            Assert.Equal("view-feedback-set", Assert.Single(oneAnswer.Actions).Type);
        }

        [Fact]
        public async Task SendTurn_AfterApplyAllOwnedLocations_AttentionAsk_PicksOneLocation()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(camden, "Summarise recent feedback")
            );
            var conversationId = Assert.IsType<AssistantTurnOutcome.Ok>(created)
                .Conversation.Id;
            await _service.ApplyScopeAsync(
                ownerUserId: 7,
                conversationId,
                AllOwnedLocationsScopeRequest()
            );
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "thank-recent-guests",
                Title = "Thank recent guests",
            };
            _retrieve.Calls.Clear();

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                AllSendRequest("What needs attention?", conversationId)
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[^1];
            Assert.Equal("Pick one location", answer.Title);
            Assert.Contains("Change Scope", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Empty(answer.Actions);
            Assert.Equal(0, _homeRecommendation.CallCount);
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public void AttentionRetrieve_DoesNotDebitAiCredits()
        {
            var ctor = typeof(AssistantAttentionRetrieve).GetConstructors().Single();
            Assert.DoesNotContain(
                ctor.GetParameters(),
                parameter =>
                    parameter.ParameterType.Name.Contains(
                        "Billing",
                        StringComparison.Ordinal
                    )
                    || parameter.ParameterType.Name.Contains(
                        "Credit",
                        StringComparison.Ordinal
                    )
            );
            var conversationCtor = typeof(AssistantConversationService)
                .GetConstructors()
                .Single();
            Assert.DoesNotContain(
                conversationCtor.GetParameters(),
                parameter =>
                    parameter.ParameterType.Name.Contains(
                        "Billing",
                        StringComparison.Ordinal
                    )
                    || parameter.ParameterType.Name.Contains(
                        "Credit",
                        StringComparison.Ordinal
                    )
            );
        }

        [Fact]
        public async Task SendTurn_ExplainWhatNeedsAttention_ReusesSnapshot_AddsInterpretation_NoNewActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddMinutes(-12));
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What needs attention?")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            var prior = ok.Conversation.Messages[^1];
            var priorActions = prior.Actions.Select(item => item.Type).ToList();
            await SeedFeedbackAsync(
                locationId,
                DateTime.UtcNow.AddMinutes(-6),
                comment: "New later item"
            );

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Why does this need attention?",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(prior.Title, answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantExplainWhyCopy.NeedsAttentionInterpretation,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("New later item", answer.Body, StringComparison.Ordinal);
            Assert.Equal(priorActions, answer.Actions.Select(item => item.Type).ToList());
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_WhyAreYouRecommending_AfterRecommendedNextStep_ReusesSnapshot_KeepsRecommendation()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "thank-recent-guests",
                Title = "Thank recent guests",
                Opportunity = "Guests joined this week.",
                WhyBullets = ["Recent joiners have not had a thank-you"],
                EligibleAudience = "12 Email-eligible guests",
                SuggestedChannel = "email",
                EstimatedUsage = "12 Email credits",
            };
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What should I do today?")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            Assert.Equal(1, _homeRecommendation.CallCount);
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "none",
            };

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Why are you recommending this?",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("12 Email-eligible guests", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain(
                AssistantAttentionCopy.RecommendationNone,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(answer.Actions);
            Assert.Equal(1, _homeRecommendation.CallCount);
            Assert.Empty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_ExplainTheseResults_AfterProductExpert_AddsInterpretation_SkipsGets()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What can you do")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Explain these results",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal(AssistantProductExpertCopy.CapabilitiesTitle, answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains(
                AssistantExplainWhyCopy.CapabilitiesInterpretation,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Contains(
                AssistantProductExpertCopy.CapabilitiesBody,
                answer.Body,
                StringComparison.Ordinal
            );
            Assert.Empty(answer.Actions);
            AssertNoRetrieveGets();
        }

        [Fact]
        public async Task SendTurn_ExplainTheseResults_AfterCombinedCreate_KeepsThreeActions_NoSecondPersist()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedLinkedGuestAsync(
                locationId,
                "Eligible Guest",
                email: "eligible@example.com",
                offersOptOut: false
            );
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, CanonicalCamdenCampaignWithOfferAsk)
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            Assert.Single(_context.Campaigns);
            Assert.Single(_context.CatalogOffers);

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Explain these results",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal("Campaign Draft saved with Offer", answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Data", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
            Assert.Equal(3, answer.Actions.Count);
            Assert.Equal("review-campaign", answer.Actions[0].Type);
            Assert.Equal("change-audience", answer.Actions[1].Type);
            Assert.Equal("review-offer", answer.Actions[2].Type);
            Assert.Single(_context.Campaigns);
            Assert.Single(_context.CatalogOffers);
        }

        [Fact]
        public async Task SendTurn_ExplainWhy_AfterPeriodChange_RefetchesRecommendedNextStep()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "thank-recent-guests",
                Title = "Thank recent guests",
                EligibleAudience = "12 Email-eligible guests",
            };
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What should I do today?")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            await _service.ApplyScopeAsync(
                ownerUserId: 7,
                ok.Conversation.Id,
                new ApplyAssistantScopeRequest
                {
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        OwnedLocationId = locationId,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "last30",
                        },
                    },
                }
            );
            _homeRecommendation.Recommendation = new HomeRecommendationDto
            {
                Type = "thank-recent-guests",
                Title = "Thank recent guests",
                EligibleAudience = "40 Email-eligible guests",
            };

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                new SendAssistantTurnRequest
                {
                    ConversationId = ok.Conversation.Id,
                    Message = "Why are you recommending this?",
                    AnalysisScope = new AssistantAnalysisScopeDto
                    {
                        OwnedLocationId = locationId,
                        ReportingPeriod = new AssistantReportingPeriodDto
                        {
                            Kind = "preset",
                            PresetId = "last30",
                        },
                    },
                }
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Contains("40 Email-eligible guests", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("12 Email-eligible guests", answer.Body, StringComparison.Ordinal);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.Equal(2, _homeRecommendation.CallCount);
            Assert.Equal("last30", _homeRecommendation.LastRequest!.OverviewDatePreset);
        }

        [Fact]
        public async Task SendTurn_ExplainTheseResults_AfterDomainRetrieve_SkipsGets_UntilNewRetrieve()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            var firstGets = _retrieve.Calls.Count;
            Assert.True(firstGets > 0);
            _retrieve.Calls.Clear();

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Explain these results",
                    ok.Conversation.Id
                )
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            Assert.Empty(_retrieve.Calls);
            Assert.Contains(
                "## Interpretation",
                Assert.IsType<AssistantTurnOutcome.Ok>(followUp).Conversation.Messages[^1].Body,
                StringComparison.Ordinal
            );

            var next = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Summarise recent feedback",
                    ok.Conversation.Id
                )
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(next);
            Assert.NotEmpty(_retrieve.Calls);
        }

        [Fact]
        public async Task SendTurn_HelpCentre_StillRefusesExplainWhyNeedle()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What needs attention?")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "How do I explain these results?",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal("refusal", answer.Class);
            Assert.DoesNotContain("## Interpretation", answer.Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_ExplainWhatNeedsAttention_AfterHelpCentreRefuse_ReusesGroundedSnapshot()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "What needs attention?")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);
            var grounded = ok.Conversation.Messages[^1];
            Assert.Equal("grounded", grounded.Class);

            var refused = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "How do I explain these results?",
                    ok.Conversation.Id
                )
            );
            Assert.IsType<AssistantTurnOutcome.Ok>(refused);

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Explain what needs attention",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Equal("grounded", answer.Class);
            Assert.Equal(grounded.Title, answer.Title);
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SendTurn_WhyAreYouRecommending_AfterNeedsAttention_StillOmitsRecommendation()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var created = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Show what needs attention")
            );
            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(created);

            var followUp = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(
                    locationId,
                    "Why are you recommending this?",
                    ok.Conversation.Id
                )
            );

            var answered = Assert.IsType<AssistantTurnOutcome.Ok>(followUp);
            var answer = answered.Conversation.Messages[^1];
            Assert.Contains("## Interpretation", answer.Body, StringComparison.Ordinal);
            Assert.DoesNotContain("## Recommendation", answer.Body, StringComparison.Ordinal);
        }

        private static SendAssistantTurnRequest AllSendRequest(
            int conversationId,
            string message
        )
            => new()
            {
                ConversationId = conversationId,
                Message = message,
                AnalysisScope = AllOwnedLocationsScope(),
            };

        private async Task<SeededAllOwnedConversation> SeedAllOwnedConversationAsync()
        {
            var camden = await SeedLocationAsync(ownerUserId: 7, "Camden");
            var soho = await SeedSecondLocationAsync(ownerUserId: 7, "Soho");
            var shoreditch = await SeedSecondLocationAsync(ownerUserId: 7, "Shoreditch");
            var brixton = await SeedSecondLocationAsync(ownerUserId: 7, "Brixton");
            var created = Assert.IsType<AssistantTurnOutcome.Ok>(
                await _service.SendTurnAsync(
                    ownerUserId: 7,
                    FirstSendRequest(camden, "Camden ask")
                )
            );
            await _service.ApplyScopeAsync(
                ownerUserId: 7,
                created.Conversation.Id,
                AllOwnedLocationsScopeRequest()
            );
            ClearRetrieveCalls();
            return new SeededAllOwnedConversation(
                created.Conversation.Id,
                camden,
                soho,
                shoreditch,
                brixton
            );
        }

        private void ClearRetrieveCalls()
        {
            _retrieve.Calls.Clear();
            _offersRetrieve.Calls.Clear();
            _campaignsRetrieve.Calls.Clear();
            _captureRetrieve.Calls.Clear();
            _homeRetrieve.Calls.Clear();
            _guestsRetrieve.Calls.Clear();
        }

        private static int CountQuotedExcerpts(string body)
            => System.Text.RegularExpressions.Regex.Matches(body, "\"[^\"]+\"").Count;

        private sealed record SeededAllOwnedConversation(
            int ConversationId,
            int Camden,
            int Soho,
            int Shoreditch,
            int Brixton
        );

        private sealed class ManualTimeProvider : TimeProvider
        {
            private DateTimeOffset _utcNow;

            public ManualTimeProvider(DateTimeOffset utcNow)
            {
                _utcNow = utcNow;
            }

            public void Advance(TimeSpan delta) => _utcNow += delta;

            public override DateTimeOffset GetUtcNow() => _utcNow;
        }

        private static SendAssistantTurnRequest FirstSendRequest(
            int locationId,
            string message,
            int? conversationId = null
        )
            => new()
            {
                ConversationId = conversationId,
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

        private static SendAssistantTurnRequest AllSendRequest(
            string message,
            int? conversationId = null
        )
            => new()
            {
                ConversationId = conversationId,
                Message = message,
                AnalysisScope = AllOwnedLocationsScope(),
            };

        private static AssistantAnalysisScopeDto AllOwnedLocationsScope()
            => new()
            {
                ScopeKind = "all",
                OwnedLocationName = "All Locations",
                ReportingPeriod = new AssistantReportingPeriodDto
                {
                    Kind = "preset",
                    PresetId = "last7",
                },
            };

        private static ApplyAssistantScopeRequest AllOwnedLocationsScopeRequest()
            => new()
            {
                AnalysisScope = AllOwnedLocationsScope(),
            };

        private static void AssertNoContact(string? title, string body)
        {
            Assert.DoesNotContain("pat@example.com", title ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pat@example.com", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("07700900999", title ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("07700900999", body, StringComparison.Ordinal);
        }

        private static void AssertNamedLocationCalls(
            List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> calls,
            params int[] locationIds
        )
        {
            Assert.Equal(locationIds.Length, calls.Count);
            foreach (var locationId in locationIds)
            {
                Assert.Contains(calls, call => call.OwnedLocationId == locationId);
            }
        }

        private async Task<int> SeedConversationWithInterviewJsonAsync(
            int ownerUserId,
            int locationId,
            string locationName,
            string interviewJson
        )
        {
            var conversation = new AssistantConversation
            {
                OwnerUserId = ownerUserId,
                Title = "Leftover interview",
                OwnedLocationId = locationId,
                OwnedLocationName = locationName,
                ReportingPeriodKind = "preset",
                ReportingPeriodPresetId = "last7",
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                DraftInterviewJson = interviewJson,
            };
            _context.AssistantConversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation.Id;
        }

        private async Task<int> SeedLocationAsync(
            int ownerUserId,
            string locationName,
            string accountType = "Multi",
            string? address = null,
            CaptureLocationStatus captureStatus = CaptureLocationStatus.Active
        )
        {
            var restaurant = new Restaurant
            {
                Name = "Test Restaurant",
                AccountType = accountType,
                OwnerUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = address ?? $"{locationName} address",
                CaptureLocationStatus = captureStatus,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedSecondLocationAsync(
            int ownerUserId,
            string locationName,
            string? address = null,
            CaptureLocationStatus captureStatus = CaptureLocationStatus.Active
        )
        {
            var restaurant = await _context.Restaurants
                .SingleAsync(row => row.OwnerUserId == ownerUserId);
            var location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = locationName,
                Address = address ?? $"{locationName} address",
                CaptureLocationStatus = captureStatus,
                CreatedAt = DateTime.UtcNow,
            };
            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
            return location.Id;
        }

        private async Task<int> SeedLinkedGuestAsync(
            int locationId,
            string name,
            string? email,
            string? mobile = null,
            bool offersOptOut = false
        )
        {
            var restaurantId = await _context.RestaurantLocations
                .Where(location => location.Id == locationId)
                .Select(location => location.RestaurantId)
                .SingleAsync();
            var master = new MasterGuest
            {
                RestaurantId = restaurantId,
                Email = email,
                NormalizedEmail = email?.Trim().ToLowerInvariant(),
                Mobile = mobile,
                CreatedAt = DateTime.UtcNow,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                MasterGuestId = master.Id,
                RestaurantLocationId = locationId,
                Name = name,
                MarketingPreference = LocationGuestMarketingPreferenceExtensions.FromFeedbackOffersOptOut(offersOptOut),

                CreatedAt = DateTime.UtcNow,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();
            return guest.Id;
        }

        private async Task SeedFeedbackAsync(
            int locationId,
            DateTime createdAt,
            FeedbackSentiment sentiment = FeedbackSentiment.Negative,
            string? detectedTagsJson = "[\"Service\"]",
            FeedbackWorkflowStatus workflow = FeedbackWorkflowStatus.New,
            int? locationGuestId = null,
            string guestName = "Pat Guest",
            string guestContact = "pat@example.com",
            string comment = "Slow service at dinner",
            int qrCodeId = 0,
            ContactType contactType = ContactType.Email
        )
        {
            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = locationId,
                    LocationGuestId = locationGuestId,
                    QrCodeId = qrCodeId,
                    GuestName = guestName,
                    GuestContact = guestContact,
                    ContactType = contactType,
                    Comment = comment,
                    OffersOptOut = false,
                    CreatedAt = createdAt,
                    ClassificationStatus = ClassificationStatus.Succeeded,
                    Sentiment = sentiment,
                    DetectedTagsJson = detectedTagsJson,
                    WorkflowStatus = workflow,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<int> SeedCatalogOfferAsync(
            int locationId,
            string title,
            string status = CatalogOfferStatus.Active,
            decimal? discountPercentage = 10m
        )
        {
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = status,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = title,
                Description = "Seeded catalog offer",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = discountPercentage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.CatalogOffers.Add(offer);
            await _context.SaveChangesAsync();
            return offer.Id;
        }

        private async Task SeedOfferIssueAsync(
            int locationId,
            int offerId,
            DateTime claimedAt,
            DateTime? redeemedAt = null,
            string claimCode = "TUM-000001"
        )
        {
            var guestId = await SeedLocationGuestAsync(locationId, claimedAt);
            _context.OfferIssues.Add(
                new OfferIssue
                {
                    CatalogOfferId = offerId,
                    LocationGuestId = guestId,
                    ClaimCode = claimCode,
                    IssuedAtUtc = claimedAt,
                    ClaimedAtUtc = claimedAt,
                    RedeemedAtUtc = redeemedAt,
                    Source = OfferIssueSources.Campaign,
                    ExpiryAtUtc = claimedAt.AddDays(14),
                    OfferType = CatalogOfferType.PercentageDiscount,
                    Title = "Seeded issue",
                    Description = "Seeded",
                    Validity = CatalogOfferValidity.Days14AfterIssue,
                    DiscountPercentage = 10m,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<int> SeedCampaignAsync(
            int locationId,
            string name,
            string status,
            string? audienceKey = null,
            string? messageSubject = null,
            string? messageBody = null
        )
        {
            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Name = name,
                Status = status,
                AudienceKey = audienceKey,
                MessageSubject = messageSubject,
                MessageBody = messageBody
                    ?? (audienceKey is null ? null : "Come back this weekend."),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
            return campaign.Id;
        }

        private async Task<int> SeedQrCodeAsync(int locationId)
        {
            var qr = new QrCode
            {
                RestaurantLocationId = locationId,
                QrType = QrType.SmartGuest,
                Token = Guid.NewGuid().ToString("N")[..16],
                Status = QrCodeStatus.Active,
                CreatedAt = DateTime.UtcNow,
            };
            _context.QrCodes.Add(qr);
            await _context.SaveChangesAsync();
            return qr.Id;
        }

        private async Task SeedQrScanAsync(int locationId, int qrCodeId, DateTime createdAt)
        {
            _context.QrScanEvents.Add(
                new QrScanEvent
                {
                    RestaurantLocationId = locationId,
                    QrCodeId = qrCodeId,
                    CreatedAt = createdAt,
                }
            );
            await _context.SaveChangesAsync();
        }

        private async Task<int> SeedLocationGuestAsync(int locationId, DateTime createdAt)
        {
            var location = await _context.RestaurantLocations.FindAsync(locationId);
            Assert.NotNull(location);
            var master = new MasterGuest
            {
                RestaurantId = location!.RestaurantId,
                Email = $"guest-{Guid.NewGuid():N}@example.com",
                CreatedAt = createdAt,
            };
            _context.MasterGuests.Add(master);
            await _context.SaveChangesAsync();

            var guest = new LocationGuest
            {
                RestaurantLocationId = locationId,
                MasterGuestId = master.Id,
                Name = "Pat Guest",
                CreatedAt = createdAt,
            };
            _context.LocationGuests.Add(guest);
            await _context.SaveChangesAsync();
            return guest.Id;
        }

        private enum WeeklyBriefGenerateMode
        {
            Fail,
            Empty,
            Succeed,
        }

        private sealed class ControllableHomeRecommendation : IHomeRecommendationService
        {
            public HomeRecommendationDto Recommendation { get; set; } = new()
            {
                Type = "none",
            };

            public bool FailNext { get; set; }

            public int CallCount { get; private set; }

            public HomeRecommendationRequest? LastRequest { get; private set; }

            public Task<HomeRecommendationServiceResult> RecommendAsync(
                int operatorUserId,
                HomeRecommendationRequest request,
                CancellationToken cancellationToken = default
            )
            {
                CallCount++;
                LastRequest = request;
                if (FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<HomeRecommendationServiceResult>(
                        new HomeRecommendationServiceResult.Failed(
                            AssistantAttentionCopy.RecommendationLoadError,
                            true
                        )
                    );
                }

                return Task.FromResult<HomeRecommendationServiceResult>(
                    new HomeRecommendationServiceResult.Ok(Recommendation)
                );
            }
        }

        private sealed class ControllableWeeklyBriefGenerate : IWeeklyBriefGenerateService
        {
            public WeeklyBriefGenerateMode Mode { get; set; } =
                WeeklyBriefGenerateMode.Fail;

            public int CallCount { get; private set; }

            public Task<WeeklyBriefGenerateResult> GenerateAsync(
                int locationId,
                WeeklyBriefClosedWeek closedWeek,
                CancellationToken cancellationToken = default
            )
            {
                CallCount++;
                if (Mode == WeeklyBriefGenerateMode.Empty)
                {
                    return Task.FromResult<WeeklyBriefGenerateResult>(
                        new WeeklyBriefGenerateResult.Succeeded(
                            new WeeklyBrief
                            {
                                LocationId = locationId,
                                WeekKey = closedWeek.WeekKey,
                                Status = WeeklyBriefStatus.Succeeded,
                                GeneratedAtUtc = DateTime.UtcNow,
                                BodyJson = "",
                                MetricsJson = "{}",
                            },
                            Created: false
                        )
                    );
                }

                return Task.FromResult<WeeklyBriefGenerateResult>(
                    new WeeklyBriefGenerateResult.Failed(
                        AssistantAttentionCopy.WeeklyBriefLoadError,
                        true
                    )
                );
            }
        }

        private sealed class EmptyOfferVoidRequestService : IOfferVoidRequestService
        {
            public Task<IReadOnlyList<OpenVoidAttentionOfferDto>> ListOpenAttentionAsync(
                int locationId,
                CancellationToken cancellationToken = default
            )
                => Task.FromResult<IReadOnlyList<OpenVoidAttentionOfferDto>>([]);

            public Task<OfferVoidCreateResult> CreateAsync(
                int userId,
                CreateOfferVoidRequestBody body,
                DateTime atUtc,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();

            public Task<OfferVoidOutcomeResult> ApproveAsync(
                int userId,
                int requestId,
                DateTime atUtc,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();

            public Task<OfferVoidOutcomeResult> RejectAsync(
                int userId,
                int requestId,
                DateTime atUtc,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();

            public Task<OfferVoidRequestDetailDto?> GetDetailAsync(
                int requestId,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();

            public Task<OfferDetailsVoidRequestsListDto?> ListForOfferAsync(
                int offerId,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();

            public Task NotifyApproversAsync(
                int requestId,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();

            public Task NotifySubmitterAsync(
                int requestId,
                string outcome,
                CancellationToken cancellationToken = default
            )
                => throw new NotSupportedException();
        }

        private sealed class ControllableFeedbackRetrieve : IAssistantFeedbackRetrieve
        {
            private readonly IAssistantFeedbackRetrieve _inner;

            public ControllableFeedbackRetrieve(IAssistantFeedbackRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public bool FailAll { get; set; }

            public HashSet<int> FailLocationIds { get; } = [];

            public Action<int>? AfterRetrieve { get; set; }

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                return RetrieveCoreAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    () => _inner.RetrieveAsync(
                        ownedLocationId,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    )
                );
            }

            public Task<AssistantFeedbackRetrieveResult> RetrieveIdentityAsync(
                int ownedLocationId,
                string locationName,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                return RetrieveCoreAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    () => _inner.RetrieveIdentityAsync(
                        ownedLocationId,
                        locationName,
                        fromUtc,
                        toUtc,
                        cancellationToken
                    )
                );
            }

            private Task<AssistantFeedbackRetrieveResult> RetrieveCoreAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                Func<Task<AssistantFeedbackRetrieveResult>> inner
            )
            {
                Calls.Add((ownedLocationId, fromUtc, toUtc));
                AfterRetrieve?.Invoke(ownedLocationId);
                if (FailAll
                    || FailLocationIds.Contains(ownedLocationId)
                    || FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<AssistantFeedbackRetrieveResult>(
                        new AssistantFeedbackRetrieveResult.Failed()
                    );
                }

                return inner();
            }
        }

        private sealed class ControllableOffersRetrieve : IAssistantOffersRetrieve
        {
            private readonly IAssistantOffersRetrieve _inner;

            public ControllableOffersRetrieve(IAssistantOffersRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public Task<AssistantOffersRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((ownedLocationId, fromUtc, toUtc));
                if (FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<AssistantOffersRetrieveResult>(
                        new AssistantOffersRetrieveResult.Failed()
                    );
                }

                return _inner.RetrieveAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
            }
        }

        private sealed class ControllableCampaignsRetrieve : IAssistantCampaignsRetrieve
        {
            private readonly IAssistantCampaignsRetrieve _inner;

            public ControllableCampaignsRetrieve(IAssistantCampaignsRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public List<bool> IncludeMessageCopyCalls { get; } = [];

            public Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                bool includeMessageCopy = false,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((ownedLocationId, fromUtc, toUtc));
                IncludeMessageCopyCalls.Add(includeMessageCopy);
                if (FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<AssistantCampaignsRetrieveResult>(
                        new AssistantCampaignsRetrieveResult.Failed()
                    );
                }

                return _inner.RetrieveAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    includeMessageCopy,
                    cancellationToken
                );
            }
        }

        private sealed class ControllableCaptureRetrieve : IAssistantCaptureRetrieve
        {
            private readonly IAssistantCaptureRetrieve _inner;

            public ControllableCaptureRetrieve(IAssistantCaptureRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public Task<AssistantCaptureRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((ownedLocationId, fromUtc, toUtc));
                if (FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<AssistantCaptureRetrieveResult>(
                        new AssistantCaptureRetrieveResult.Failed()
                    );
                }

                return _inner.RetrieveAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
            }
        }

        private sealed class ControllableHomeKpiRetrieve : IAssistantHomeKpiRetrieve
        {
            private readonly IAssistantHomeKpiRetrieve _inner;

            public ControllableHomeKpiRetrieve(IAssistantHomeKpiRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add((ownedLocationId, fromUtc, toUtc));
                if (FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<AssistantHomeKpiRetrieveResult>(
                        new AssistantHomeKpiRetrieveResult.Failed()
                    );
                }

                return _inner.RetrieveAsync(
                    ownedLocationId,
                    fromUtc,
                    toUtc,
                    cancellationToken
                );
            }
        }

        private sealed class ControllableGuestsRetrieve : IAssistantGuestsRetrieve
        {
            private readonly IAssistantGuestsRetrieve _inner;

            public ControllableGuestsRetrieve(IAssistantGuestsRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public List<int> Calls { get; } = [];

            public Task<AssistantGuestsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                CancellationToken cancellationToken = default
            )
            {
                Calls.Add(ownedLocationId);
                if (FailNext)
                {
                    FailNext = false;
                    return Task.FromResult<AssistantGuestsRetrieveResult>(
                        new AssistantGuestsRetrieveResult.Failed()
                    );
                }

                return _inner.RetrieveAsync(ownedLocationId, cancellationToken);
            }
        }

        private sealed class RecordingAssistantProgressPublisher
            : IAssistantProgressPublisher
        {
            public List<(int UserId, int ConversationId, string Step)> Events { get; } =
                [];

            public bool ThrowOnPublish { get; set; }

            public Task PublishAsync(
                int userId,
                int conversationId,
                string step,
                CancellationToken cancellationToken = default
            )
            {
                if (ThrowOnPublish)
                {
                    throw new InvalidOperationException("hub unavailable");
                }

                Events.Add((userId, conversationId, step));
                return Task.CompletedTask;
            }
        }
    }
}