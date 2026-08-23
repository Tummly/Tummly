using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AdminServiceReminderTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingEmailService _emailService;
        private readonly AdminService _service;

        public AdminServiceReminderTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingEmailService();

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
                _emailService,
                configuration,
                NullLogger<TrialReviewTransition>.Instance
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
                    new UnusedAssistantAttentionRetrieve(),
                    new CaptureThankYouOfferService(
                        _context,
                        new OffersCatalogService(_context)
                    )
                )
            );
        }

        [Fact]
        public async Task ProcessOperatorSetupInvitationRemindersAsync_SendsReminder_WhenInviteIsOlderThan14Days()
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
            Assert.Single(_emailService.SetupReminderEmails);

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
            Assert.Empty(_emailService.SetupReminderEmails);
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
            Assert.Empty(_emailService.SetupReminderEmails);
        }

        [Fact]
        public async Task ProcessOperatorSetupInvitationRemindersAsync_CountsFailed_WhenEmailDispatchFails()
        {
            var inviteSentAt = DateTime.UtcNow.AddDays(-15);

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
                    ApprovalToken = "original-token",
                    InviteSentAt = inviteSentAt,
                    InviteExpiresAt = inviteSentAt.AddDays(14),
                }
            );
            await _context.SaveChangesAsync();
            _emailService.ShouldThrowOnReminder = true;

            var batch =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, batch.Sent);
            Assert.Equal(1, batch.Failed);
            Assert.Empty(_emailService.SetupReminderEmails);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public List<(string Email, string FullName, string SetupLink)>
                SetupInvitationEmails { get; } = [];

            public List<(
                string Email,
                string FullName,
                string SetupLink,
                DateTime ExpiresAtUtc
            )> SetupReminderEmails { get; } = [];

            public bool ShouldThrowOnReminder { get; set; }

            public override Task SendAccountSetupEmailAsync(
                string toEmail,
                string fullName,
                string setupLink
            )
            {
                SetupInvitationEmails.Add((toEmail, fullName, setupLink));
                return Task.CompletedTask;
            }

            public override Task SendAccountSetupReminderEmailAsync(
                string toEmail,
                string fullName,
                string setupLink,
                DateTime expiresAtUtc
            )
            {
                if (ShouldThrowOnReminder)
                {
                    throw new InvalidOperationException("Resend failed");
                }

                SetupReminderEmails.Add(
                    (toEmail, fullName, setupLink, expiresAtUtc)
                );
                return Task.CompletedTask;
            }
        }
    }
}
