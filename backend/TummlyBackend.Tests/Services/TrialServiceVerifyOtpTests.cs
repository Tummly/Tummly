using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class TrialServiceVerifyOtpTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingEmailService _emailService;
        private readonly TrialService _service;
        private readonly IOtpChallengeService _otp;

        public TrialServiceVerifyOtpTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _emailService = new TrackingEmailService();
            _otp = OtpTestHelpers.CreateChallengeService(_context);
            _service = new TrialService(
                _context,
                _emailService,
                NullLogger<TrialService>.Instance,
                _otp
            );
        }

        [Fact]
        public async Task VerifyOtpAsync_SendsTrialRequestReceivedEmail_OnSuccess()
        {
            const string email = "jane@example.com";

            var otp = await SeedPendingTrialAsync(email);

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = email,
                    OtpCode = otp,
                }
            );

            Assert.True(result.Verified);
            Assert.True(result.ConfirmationEmailSent);
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

            var otp = await SeedPendingTrialAsync(email);
            _emailService.ShouldThrowOnTrialRequestReceivedEmail = true;

            var result = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = email,
                    OtpCode = otp,
                }
            );

            Assert.True(result.Verified);
            Assert.False(result.ConfirmationEmailSent);
            Assert.Single(await _context.TrialRequests.ToListAsync());
        }

        [Fact]
        public async Task Trial_SignupCodeRefused()
        {
            const string email = "jane@example.com";
            var trialCode = await SeedPendingTrialAsync(email);
            var signup = await _otp.IssueAsync(
                new OtpIssueRequest(email, OtpPurposes.Signup, Guid.NewGuid().ToString("N"), 0)
            );

            var refused = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = email,
                    OtpCode = signup.Code,
                }
            );

            Assert.False(refused.Verified);
            Assert.Empty(await _context.TrialRequests.ToListAsync());
            Assert.Single(await _context.PendingTrialRequests.ToListAsync());

            // The trial code is untouched by the refused signup code.
            var accepted = await _service.VerifyOtpAsync(
                new VerifyOtpDto
                {
                    Email = email,
                    OtpCode = trialCode,
                }
            );
            Assert.True(accepted.Verified);
        }

        [Fact]
        public void TrialRequestReceivedEmailTemplate_Generate_IncludesPersonalization()
        {
            var contentRoot = FindBackendContentRoot();
            var env = new StubWebHostEnvironment { ContentRootPath = contentRoot };

            var html = TrialRequestReceivedEmailTemplate.Generate(
                env,
                "Jane Operator",
                "Test Cafe",
                "https://app.tummly.test/help-center",
                "https://app.tummly.test/terms",
                "https://app.tummly.test/privacy",
                "https://app.tummly.test/cookie-policy",
                "https://app.tummly.test/email/tummly-logo-dark.png"
            );

            Assert.Contains("Hi Jane,", html);
            Assert.Contains("guided Tummly trial for Test Cafe", html);
            Assert.Contains("What happens next", html);
            Assert.Contains("We help prepare your QR guest", html);
            Assert.Contains("onboarding materials.", html);
            Assert.Equal(
                "We've received your Tummly trial request",
                TrialRequestReceivedEmailTemplate.Subject
            );
            Assert.DoesNotContain("{{", html);
        }

        private static string FindBackendContentRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(
                    dir.FullName,
                    "Assets",
                    "emails",
                    "templates",
                    "trial-request-received.html"
                );
                if (File.Exists(candidate))
                {
                    return dir.FullName;
                }

                var nested = Path.Combine(
                    dir.FullName,
                    "TummlyBackend",
                    "Assets",
                    "emails",
                    "templates",
                    "trial-request-received.html"
                );
                if (File.Exists(nested))
                {
                    return Path.Combine(dir.FullName, "TummlyBackend");
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate trial-request-received.html for tests."
            );
        }

        private sealed class StubWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
        {
            public string EnvironmentName { get; set; } = "Test";
            public string ApplicationName { get; set; } = "Tests";
            public string WebRootPath { get; set; } = ".";
            public string ContentRootPath { get; set; } = ".";
            public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
                new Microsoft.Extensions.FileProviders.NullFileProvider();
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
                new Microsoft.Extensions.FileProviders.NullFileProvider();
        }

        private async Task<string> SeedPendingTrialAsync(string email)
        {
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

            // The trial code is issued for the pending request id, as the service does on Create.
            var pending = await _context.PendingTrialRequests.SingleAsync(x => x.Email == email);
            var issued = await _otp.IssueAsync(
                new OtpIssueRequest(email, OtpPurposes.Trial, pending.Id.ToString(), 0)
            );
            return issued.Code;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public List<(string Email, string FullName, string BusinessName)>
                TrialRequestReceivedEmails { get; } = [];

            public bool ShouldThrowOnTrialRequestReceivedEmail { get; set; }

            public override Task SendTrialRequestReceivedEmailAsync(
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
        }
    }
}
