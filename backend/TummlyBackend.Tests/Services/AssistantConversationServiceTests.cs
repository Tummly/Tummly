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
        private readonly ControllableFeedbackRetrieve _retrieve;
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
            _service = new AssistantConversationService(
                _context,
                new OwnedLocationService(_context),
                _fake,
                _retrieve
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
            Assert.Equal(1, _fake.LastInput!.Evidence.TotalCount);
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
            Assert.Equal(101, _fake.LastInput!.Evidence.TotalCount);
            Assert.Equal(100, _fake.LastInput.Evidence.SampleCount);
            Assert.Equal(
                101,
                ok.Conversation.Messages[1].Actions
                    .Single(action => action.Type == "view-feedback-set")
                    .Count
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

        private async Task SeedFeedbackAsync(
            int locationId,
            DateTime createdAt,
            FeedbackSentiment sentiment = FeedbackSentiment.Negative,
            string? detectedTagsJson = "[\"Service\"]",
            FeedbackWorkflowStatus workflow = FeedbackWorkflowStatus.New
        )
        {
            _context.Feedbacks.Add(
                new Feedback
                {
                    RestaurantLocationId = locationId,
                    GuestName = "Pat Guest",
                    GuestContact = "pat@example.com",
                    ContactType = ContactType.Email,
                    Comment = "Slow service at dinner",
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

        private sealed class ControllableFeedbackRetrieve : IAssistantFeedbackRetrieve
        {
            private readonly IAssistantFeedbackRetrieve _inner;

            public ControllableFeedbackRetrieve(IAssistantFeedbackRetrieve inner)
            {
                _inner = inner;
            }

            public bool FailNext { get; set; }

            public Task<AssistantFeedbackRetrieveResult> RetrieveAsync(
                int ownedLocationId,
                DateTime fromUtc,
                DateTime toUtc,
                CancellationToken cancellationToken = default
            )
            {
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
    }
}
