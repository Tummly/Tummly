using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    public class TrialServiceVerifyOtpTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingEmailService _emailService;
        private readonly TrialService _service;

        public TrialServiceVerifyOtpTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingEmailService();
            _service = new TrialService(
                _context,
                _emailService,
                NullLogger<TrialService>.Instance
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_SendsTrialRequestReceivedEmail_OnSuccess()
        {
            const string email = "jane@example.com";
            const string otp = "123456";

            await SeedPendingTrialAsync(email, otp);

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = email,
                    OtpCode = otp,
                }
            );

            Assert.True(result);
            Assert.Single(_emailService.TrialRequestReceivedEmails);
            Assert.Equal(
                (email, "Jane Operator", "Test Cafe"),
                _emailService.TrialRequestReceivedEmails[0]
            );
            Assert.Single(await _context.TrialRequests.ToListAsync());
            Assert.Empty(await _context.PendingTrialRequests.ToListAsync());
        }

        [Fact]
        public async Task VerifyOtpAsync_Succeeds_WhenTrialRequestReceivedEmailFails()
        {
            const string email = "jane@example.com";
            const string otp = "123456";

            await SeedPendingTrialAsync(email, otp);
            _emailService.ShouldThrowOnTrialRequestReceivedEmail = true;

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = email,
                    OtpCode = otp,
                }
            );

            Assert.True(result);
            Assert.Single(await _context.TrialRequests.ToListAsync());
        }

        [Fact]
        public void TrialRequestReceivedEmailTemplate_GenerateBody_IncludesPersonalization()
        {
            var body = TrialRequestReceivedEmailTemplate.GenerateBody(
                "Jane Operator",
                "Test Cafe"
            );

            Assert.Contains("Hi Jane,", body);
            Assert.Contains("guided Tummly trial for Test Cafe", body);
            Assert.Contains("What happens next", body);
            Assert.Contains("We help prepare your QR guest prompts", body);
            Assert.Equal(
                "We've received your Tummly trial request",
                TrialRequestReceivedEmailTemplate.Subject
            );
        }

        private async Task SeedPendingTrialAsync(string email, string otp)
        {
            _context.OtpVerifications.Add(
                new OtpVerification
                {
                    Email = email,
                    OtpCode = otp,
                    IsUsed = false,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    CreatedAt = DateTime.UtcNow,
                }
            );

            _context.PendingTrialRequests.Add(
                new PendingTrialRequest
                {
                    BusinessName = "Test Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Jane Operator",
                    Email = email,
                    Mobile = "07123456789",
                    MainLocation = "125 High Street",
                    TownCity = "Manchester",
                    Postcode = "M1 4AB",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                }
            );

            await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : IEmailService
        {
            public List<(string Email, string FullName, string BusinessName)>
                TrialRequestReceivedEmails { get; } = [];

            public bool ShouldThrowOnTrialRequestReceivedEmail { get; set; }

            public Task SendOtpEmailAsync(string toEmail, string otp) =>
                Task.CompletedTask;

            public Task SendTrialRequestReceivedEmailAsync(
                string toEmail,
                string fullName,
                string businessName
            )
            {
                if (ShouldThrowOnTrialRequestReceivedEmail)
                {
                    throw new InvalidOperationException("Email delivery failed.");
                }

                TrialRequestReceivedEmails.Add((toEmail, fullName, businessName));
                return Task.CompletedTask;
            }

            public Task SendAccountSetupEmailAsync(
                string toEmail,
                string fullName,
                string setupLink
            ) =>
                Task.CompletedTask;

            public Task SendAccountSetupReminderEmailAsync(
                string toEmail,
                string fullName,
                string setupLink,
                DateTime expiresAtUtc
            ) =>
                Task.CompletedTask;

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
