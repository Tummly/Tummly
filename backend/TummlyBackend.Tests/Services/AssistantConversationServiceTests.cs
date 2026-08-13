using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
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
        private readonly ControllableFeedbackRetrieve _retrieve;
        private readonly ControllableOffersRetrieve _offersRetrieve;
        private readonly ControllableCampaignsRetrieve _campaignsRetrieve;
        private readonly ControllableCaptureRetrieve _captureRetrieve;
        private readonly ControllableHomeKpiRetrieve _homeRetrieve;
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
            _service = new AssistantConversationService(
                _context,
                new OwnedLocationService(_context),
                _fake,
                _retrieve,
                _offersRetrieve,
                _campaignsRetrieve,
                _captureRetrieve,
                _homeRetrieve
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
            Assert.Empty(ok.Conversation.Messages[1].Actions);
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
            Assert.Empty(answer.Actions);
        }

        [Fact]
        public async Task SendTurn_MutateAsk_IsRefusal_WithNoActions()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Create a campaign for these guests")
            );

            var ok = Assert.IsType<AssistantTurnOutcome.Ok>(outcome);
            var answer = ok.Conversation.Messages[1];
            Assert.Equal("refusal", answer.Class);
            Assert.Null(answer.Title);
            Assert.Contains("cannot create, send, or change records", answer.Body);
            Assert.Empty(answer.Actions);
        }

        [Fact]
        public async Task SendTurn_MixedAsk_GroundsInScope_AndAddsRefuseSentence()
        {
            var locationId = await SeedLocationAsync(ownerUserId: 7, "Camden");
            await SeedFeedbackAsync(locationId, DateTime.UtcNow.AddHours(-1));

            var outcome = await _service.SendTurnAsync(
                ownerUserId: 7,
                FirstSendRequest(locationId, "Summarise recent feedback and create a campaign")
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
            Assert.Equal(camden, ok.Conversation.AnalysisScope.OwnedLocationId);
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

        private sealed class TrackingEmailService
            : TummlyBackend.Tests.Helpers.EmailServiceStubBase
        {
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

        private static void AssertNoContact(string? title, string body)
        {
            Assert.DoesNotContain("pat@example.com", title ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pat@example.com", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("07700900999", title ?? string.Empty, StringComparison.Ordinal);
            Assert.DoesNotContain("07700900999", body, StringComparison.Ordinal);
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
                OffersOptOut = offersOptOut,
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
            int qrCodeId = 0
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
                    ContactType = ContactType.Email,
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

        private async Task<int> SeedCatalogOfferAsync(int locationId, string title)
        {
            var offer = new CatalogOffer
            {
                RestaurantLocationId = locationId,
                Status = CatalogOfferStatus.Active,
                OfferType = CatalogOfferType.PercentageDiscount,
                Title = title,
                Description = "Seeded catalog offer",
                Validity = CatalogOfferValidity.Days14AfterIssue,
                DiscountPercentage = 10m,
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
            string? audienceKey = null
        )
        {
            var campaign = new Campaign
            {
                RestaurantLocationId = locationId,
                Name = name,
                Status = status,
                AudienceKey = audienceKey,
                MessageBody = audienceKey is null ? null : "Come back this weekend.",
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

        private sealed class ControllableFeedbackRetrieve : IAssistantFeedbackRetrieve
        {
            private readonly IAssistantFeedbackRetrieve _inner;

            public ControllableFeedbackRetrieve(IAssistantFeedbackRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public List<(int OwnedLocationId, DateTime FromUtc, DateTime ToUtc)> Calls { get; }
                = [];

            public Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
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
                    return Task.FromResult<AssistantFeedbackRetrieveResult>(
                        new AssistantFeedbackRetrieveResult.Failed()
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

        private sealed class ControllableOffersRetrieve : IAssistantOffersRetrieve
        {
            private readonly IAssistantOffersRetrieve _inner;

            public ControllableOffersRetrieve(IAssistantOffersRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public Task<AssistantOffersRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
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

            public Task<AssistantCampaignsRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
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

            public Task<AssistantCaptureRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
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

            public Task<AssistantHomeKpiRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
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
    }
}