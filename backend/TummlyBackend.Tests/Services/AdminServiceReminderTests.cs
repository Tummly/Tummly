using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

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
                NullLogger<AdminService>.Instance
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

            var sentCount =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(1, sentCount);
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

            var sentCount =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, sentCount);
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

            var sentCount =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, sentCount);
            Assert.Empty(_emailService.SetupReminderEmails);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : IEmailService
        {
            public List<(string Email, string FullName, string SetupLink)>
                SetupInvitationEmails { get; } = [];

            public List<(
                string Email,
                string FullName,
                string SetupLink,
                DateTime ExpiresAtUtc
            )> SetupReminderEmails { get; } = [];

            public Task SendOtpEmailAsync(string toEmail, string otp) =>
                Task.CompletedTask;

            public Task SendTrialRequestReceivedEmailAsync(
                string toEmail,
                string fullName,
                string businessName
            ) =>
                Task.CompletedTask;

            public Task SendAccountSetupEmailAsync(
                string toEmail,
                string fullName,
                string setupLink
            )
            {
                SetupInvitationEmails.Add((toEmail, fullName, setupLink));
                return Task.CompletedTask;
            }

            public Task SendAccountSetupReminderEmailAsync(
                string toEmail,
                string fullName,
                string setupLink,
                DateTime expiresAtUtc
            )
            {
                SetupReminderEmails.Add(
                    (toEmail, fullName, setupLink, expiresAtUtc)
                );
                return Task.CompletedTask;
            }

            public Task SendDeclineEmailAsync(
                string toEmail,
                string fullName,
                string declineReason
            ) =>
                Task.CompletedTask;

            public Task SendMoreInfoEmailAsync(
                string toEmail,
                string fullName,
                string moreInfoMessage
            ) =>
                Task.CompletedTask;

            public Task SendResetPasswordEmailAsync(
                string toEmail,
                string resetLink
            ) =>
                Task.CompletedTask;

            public Task SendPasswordChangedEmailAsync(
                string toEmail,
                string firstName
            ) =>
                Task.CompletedTask;

            public Task SendNewDeviceSignInEmailAsync(
                string toEmail,
                NewDeviceSignInDetails details
            ) =>
                Task.CompletedTask;

            public Task SendHelpCentreSupportReplyEmailAsync(
                string toEmail,
                string submitterName,
                string topicLabel,
                string replyBody,
                string? myQueriesUrl
            ) => Task.CompletedTask;

            public Task SendHelpCentreEscalationEmailAsync(
                string toEmail,
                string topicLabel,
                string submitterName,
                string submitterEmail,
                string businessName,
                string? locationLabel,
                string threadSummary,
                string? escalationNote,
                string supportDashboardUrl
            ) => Task.CompletedTask;

            public Task SendHelpCentreOperatorReplyEmailAsync(
                string topicLabel,
                string submitterName,
                string submitterEmail,
                string businessName,
                string replyBody,
                string supportDashboardUrl
            ) => Task.CompletedTask;
        }
    }
}
