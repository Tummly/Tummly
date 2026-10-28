using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class AdminServiceReminderTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminService _service;
        private readonly IConfiguration _configuration;

        public AdminServiceReminderTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _configuration = new ConfigurationBuilder()
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
                _configuration,
                NullLogger<TrialReviewTransition>.Instance,
                new AdminAuditService(_context, TimeProvider.System)
            );

            _service = CreateAdminService(
                trialReviewTransition,
                NullLogger<AdminService>.Instance
            );
        }

        private AdminService CreateAdminService(
            ITrialReviewTransition trialReviewTransition,
            ILogger<AdminService> logger
        ) =>
            new(
                _context,
                trialReviewTransition,
                _configuration,
                logger,
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
                new AdminAuditService(_context, TimeProvider.System),
                new CreditBalanceSnapshotService(_context, TimeProvider.System)
            );

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

        [Fact]
        public async Task ProcessOperatorSetupInvitationRemindersAsync_EmailDispatchFailure_LogOmitsExceptionObjectAndRawEmail()
        {
            const string rawEmail = "alice@example.com";
            var inviteSentAt = DateTime.UtcNow.AddDays(-15);

            _context.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Test Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Alice Operator",
                    Email = rawEmail,
                    Mobile = "07123456789",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsApproved = true,
                    IsAccountCreated = false,
                    AccountType = "Single",
                    Status = TrialRequestStatus.Approved,
                    ApprovalToken = "old-token",
                    InviteSentAt = inviteSentAt,
                    InviteExpiresAt = inviteSentAt.AddDays(14),
                }
            );
            await _context.SaveChangesAsync();

            var logger = new CapturingLogger<AdminService>();
            var service = CreateAdminService(
                new ThrowingEmailDispatchTrialReviewTransition(rawEmail),
                logger
            );

            var batch = await service.ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, batch.Sent);
            Assert.Equal(1, batch.Failed);

            var error = Assert.Single(
                logger.Entries,
                e => e.Level == LogLevel.Error
            );
            Assert.Null(error.Exception);
            Assert.DoesNotContain(rawEmail, error.Message);
            Assert.Contains("TrialReviewEmailDispatchException", error.Message);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class ThrowingEmailDispatchTrialReviewTransition
            : ITrialReviewTransition
        {
            private readonly string _rawEmail;

            public ThrowingEmailDispatchTrialReviewTransition(string rawEmail)
            {
                _rawEmail = rawEmail;
            }

            public Task<TrialReviewResult> ApplyTransitionAsync(
                int trialRequestId,
                TrialReviewDecision decision,
                TrialReviewContext context,
                CancellationToken cancellationToken = default
            )
            {
                var result = new TrialReviewResult(
                    TrialRequestStatus.InviteSent,
                    SetupLink: null,
                    InviteExpiresAt: DateTime.UtcNow.AddDays(14)
                );
                var providerFailure = new InvalidOperationException(
                    $"Resend rejected {_rawEmail}"
                );
                throw new TrialReviewEmailDispatchException(result, providerFailure);
            }
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<(
                LogLevel Level,
                Exception? Exception,
                string Message
            )> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            )
            {
                Entries.Add((logLevel, exception, formatter(state, exception)));
            }
        }
    }
}
