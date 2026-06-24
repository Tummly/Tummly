using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
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

            _service = new AdminService(
                _context,
                _emailService,
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
                Status = "APPROVED",
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
            Assert.Single(_emailService.SetupInvitationEmails);

            var updated = await _context.TrialRequests.SingleAsync();
            Assert.Equal("INVITE_SENT", updated.Status);
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
                    Status = "Account Created",
                    InviteSentAt = DateTime.UtcNow.AddDays(-20),
                }
            );
            await _context.SaveChangesAsync();

            var sentCount =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, sentCount);
            Assert.Empty(_emailService.SetupInvitationEmails);
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
                    Status = "APPROVED",
                    InviteSentAt = DateTime.UtcNow.AddDays(-7),
                }
            );
            await _context.SaveChangesAsync();

            var sentCount =
                await _service
                    .ProcessOperatorSetupInvitationRemindersAsync();

            Assert.Equal(0, sentCount);
            Assert.Empty(_emailService.SetupInvitationEmails);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : IEmailService
        {
            public List<(string Email, string FullName, string SetupLink)>
                SetupInvitationEmails { get; } = [];

            public Task SendOtpEmailAsync(string toEmail, string otp) =>
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

            public Task SendDeclineEmailAsync(
                string toEmail,
                string fullName
            ) =>
                Task.CompletedTask;

            public Task SendMoreInfoEmailAsync(
                string toEmail,
                string fullName
            ) =>
                Task.CompletedTask;

            public Task SendResetPasswordEmailAsync(
                string toEmail,
                string resetLink
            ) =>
                Task.CompletedTask;
        }
    }
}
