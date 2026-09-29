using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AdminServiceReminderTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminService _service;

        public AdminServiceReminderTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Frontend:BaseUrl"] = "https://app.tummly.com",
                    }
                )
                .Build();

            var trialReviewTransition = new TrialReviewTransition(
                _context,
                new EmailServiceStubBase(),
                configuration,
                NullLogger<TrialReviewTransition>.Instance,
                new AdminAuditService(_context, TimeProvider.System)
            );

            _service = new AdminService(
                _context,
                trialReviewTransition,
                configuration,
                NullLogger<AdminService>.Instance,
                new AssistantConversationService(
                    _context,
                    new OwnedLocationService(_context),
                    new FakeAssistantLiveAnswerProvider(),
                    new AssistantFeedbackRetrieve(_context),
                    new AssistantOffersRetrieve(_context, new OffersMetricsService(_context)),
                    new AssistantCampaignsRetrieve(
                        _context,
                        new CampaignsSummaryService(_context),
                        new CampaignEligibilityService(_context)
                    ),
                    new AssistantCaptureRetrieve(
                        _context,
                        new CaptureWindowedEngagementAggregate(_context)
                    ),
                    new AssistantHomeKpiRetrieve(_context),
                    new AssistantGuestsRetrieve(_context),
                    new NullAssistantProgressPublisher(),
                    new CampaignDraftService(
                        _context,
                        new CampaignTemplateCatalogueService(),
                        new OffersCatalogService(_context)
                    ),
                    new CampaignEligibilityService(_context),
                    new CampaignMessageDraftService(new FakeCampaignMessageDraftProvider()),
                    new OffersCatalogService(_context),
                    new FeedbackRecoveryDraftsService(
                        _context,
                        new FakeFeedbackRecoveryDraftProvider()
                    ),
                    new CaptureThankYouOfferService(
                        _context,
                        new OffersCatalogService(_context)
                    ),
                    new RestaurantPermissionHelper(_context),
                    new AssistantAiBillingService(
                        _context,
                        new CreditLedgerService(
                            _context,
                            TimeProvider.System,
                            TestPricebookPaths.LoadV3(),
                            new AdminAuditService(_context, TimeProvider.System)
                        ),
                        new CreditBalanceSnapshotService(_context, TimeProvider.System),
                        TimeProvider.System
                    )
                ),
                new NoOpBillingAccountLifecycle(),
                new AdminAuditService(_context, TimeProvider.System)
            );
        }

        [Fact]
        public async Task ProcessOperatorSetupInvitationRemindersAsync_RotatesInvite_WhenInviteIsOlderThan14Days()
        {
            var originalToken = "original-token";
            var inviteSentAt = DateTime.UtcNow.AddDays(-15);

            var trialRequest = new TrialRequest
            {
                BusinessName = "Test Cafe",
                BusinessCategory = "Cafe / coffee shop",
                Locations = "1",
                FullName = "Jane Operator",
                Email = "jane@example.com",
                Mobile = "07123456789",
                Role = "Owner",
                Goal = "Grow repeat guests",
                TermsAccepted = true,
                IsApproved = true,
                IsAccountCreated = false,
                AccountType = "Single",
                Status = TrialRequestStatus.Approved,
                ApprovalToken = originalToken,
                InviteSentAt = inviteSentAt,
                InviteExpiresAt = inviteSentAt.AddDays(14),
            };

            _context.TrialRequests.Add(trialRequest);
            await _context.SaveChangesAsync();

            var batch =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(1, batch.Sent);
            Assert.Equal(0, batch.Failed);

            var updated = await _context.TrialRequests.SingleAsync();
            Assert.Equal(TrialRequestStatus.InviteSent, updated.Status);
            Assert.NotEqual(originalToken, updated.ApprovalToken);
            Assert.True(updated.InviteSentAt > inviteSentAt);
            Assert.True(updated.InviteExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task ProcessOperatorSetupInvitationRemindersAsync_Skips_WhenAccountAlreadyCreated()
        {
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Test Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Jane Operator",
                    Email = "jane@example.com",
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = true,
                    AccountType = "Single",
                    Status = TrialRequestStatus.AccountCreated,
                    InviteSentAt = DateTime.UtcNow.AddDays(-20),
                }
            );
            await _context.SaveChangesAsync();

            var batch =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, batch.Sent);
            Assert.Equal(0, batch.Failed);
        }

        [Fact]
        public async Task ProcessOperatorSetupInvitationRemindersAsync_Skips_WhenInviteIsStillWithin14Days()
        {
            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Test Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Jane Operator",
                    Email = "jane@example.com",
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = false,
                    AccountType = "Single",
                    Status = TrialRequestStatus.Approved,
                    InviteSentAt = DateTime.UtcNow.AddDays(-7),
                }
            );
            await _context.SaveChangesAsync();

            var batch =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, batch.Sent);
            Assert.Equal(0, batch.Failed);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
