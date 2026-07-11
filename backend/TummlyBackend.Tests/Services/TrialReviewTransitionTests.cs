using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class TrialReviewTransitionTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TrackingEmailService _emailService;
        private readonly TrialReviewTransition _transition;

        public TrialReviewTransitionTests()
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

            _transition = new TrialReviewTransition(
                _context,
                _emailService,
                configuration,
                NullLogger<TrialReviewTransition>.Instance
            );
        }

        private TrialRequest Seed(
            TrialRequestStatus status,
            bool isApproved = false,
            bool isAccountCreated = false,
            string locations = "1",
            string? approvalToken = null
        )
        {
            var request = new TrialRequest
            {
                BusinessName = "Test Cafe",
                BusinessCategory = "Cafe / coffee shop",
                Locations = locations,
                FullName = "Jane Operator",
                Email = "jane@example.com",
                Mobile = "07123456789",
                MainLocation = "42 High Street",
                TownCity = "Manchester",
                Postcode = "M1 1AA",
                Role = "Owner",
                Goal = "Grow repeat guests",
                TermsAccepted = true,
                IsEmailVerified = true,
                IsApproved = isApproved,
                IsAccountCreated = isAccountCreated,
                AccountType = "Single",
                Status = status,
                ApprovalToken = approvalToken,
            };

            _context.TrialRequests.Add(request);
            _context.SaveChanges();

            return request;
        }

        private static TrialReviewContext Context(
            string admin = "admin@tummly.com",
            string? reason = null,
            string? notes = null
        ) => new(admin, reason, notes);

        // ---------- Legal transitions ----------

        [Theory]
        [InlineData(
            TrialRequestStatus.EmailVerified,
            TrialReviewDecision.Approve,
            TrialRequestStatus.Approved)]
        [InlineData(
            TrialRequestStatus.EmailVerified,
            TrialReviewDecision.Decline,
            TrialRequestStatus.Declined)]
        [InlineData(
            TrialRequestStatus.EmailVerified,
            TrialReviewDecision.RequestMoreInfo,
            TrialRequestStatus.MoreInfoRequested)]
        [InlineData(
            TrialRequestStatus.MoreInfoRequested,
            TrialReviewDecision.Approve,
            TrialRequestStatus.Approved)]
        [InlineData(
            TrialRequestStatus.MoreInfoRequested,
            TrialReviewDecision.Decline,
            TrialRequestStatus.Declined)]
        [InlineData(
            TrialRequestStatus.Approved,
            TrialReviewDecision.ResendInvite,
            TrialRequestStatus.InviteSent)]
        [InlineData(
            TrialRequestStatus.Approved,
            TrialReviewDecision.Decline,
            TrialRequestStatus.Declined)]
        [InlineData(
            TrialRequestStatus.InviteSent,
            TrialReviewDecision.ResendInvite,
            TrialRequestStatus.InviteSent)]
        [InlineData(
            TrialRequestStatus.InviteSent,
            TrialReviewDecision.RequestMoreInfo,
            TrialRequestStatus.MoreInfoRequested)]
        [InlineData(
            TrialRequestStatus.MoreInfoRequested,
            TrialReviewDecision.RequestMoreInfo,
            TrialRequestStatus.MoreInfoRequested)]
        public async Task ApplyTransitionAsync_LegalEdge_MovesToExpectedStatus(
            TrialRequestStatus from,
            TrialReviewDecision decision,
            TrialRequestStatus expectedTo
        )
        {
            var request = Seed(from, isApproved: from != TrialRequestStatus.EmailVerified);
            var reason = decision == TrialReviewDecision.Decline
                ? "Not a fit"
                : decision == TrialReviewDecision.RequestMoreInfo
                    ? "Need more info"
                    : null;

            var result = await _transition.ApplyTransitionAsync(
                request.Id,
                decision,
                Context(reason: reason)
            );

            Assert.Equal(expectedTo, result.NewStatus);
            Assert.Equal(expectedTo, _context.TrialRequests.Single().Status);
        }

        // ---------- Illegal transitions ----------

        [Theory]
        [InlineData(
            TrialRequestStatus.Declined,
            TrialReviewDecision.Approve)]
        [InlineData(
            TrialRequestStatus.Declined,
            TrialReviewDecision.ResendInvite)]
        [InlineData(
            TrialRequestStatus.AccountCreated,
            TrialReviewDecision.Approve)]
        [InlineData(
            TrialRequestStatus.AccountCreated,
            TrialReviewDecision.Decline)]
        [InlineData(
            TrialRequestStatus.EmailVerified,
            TrialReviewDecision.ResendInvite)]
        [InlineData(
            TrialRequestStatus.MoreInfoRequested,
            TrialReviewDecision.ResendInvite)]
        [InlineData(
            TrialRequestStatus.Approved,
            TrialReviewDecision.Approve)]
        [InlineData(
            TrialRequestStatus.InviteSent,
            TrialReviewDecision.Approve)]
        public async Task ApplyTransitionAsync_IllegalEdge_Throws(
            TrialRequestStatus from,
            TrialReviewDecision decision
        )
        {
            var request = Seed(
                from,
                isApproved: from == TrialRequestStatus.Approved
                    || from == TrialRequestStatus.InviteSent,
                isAccountCreated: from == TrialRequestStatus.AccountCreated
            );

            await Assert.ThrowsAsync<IllegalTrialTransitionException>(
                () => _transition.ApplyTransitionAsync(
                    request.Id,
                    decision,
                    Context()
                )
            );

            Assert.Equal(from, _context.TrialRequests.Single().Status);
        }

        // ---------- Validation ----------

        [Fact]
        public async Task ApplyTransitionAsync_DeclineWithoutReason_Throws()
        {
            var request = Seed(TrialRequestStatus.EmailVerified);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _transition.ApplyTransitionAsync(
                    request.Id,
                    TrialReviewDecision.Decline,
                    Context(reason: null)
                )
            );

            Assert.Contains("Decline reason is required", ex.Message);
            Assert.Empty(_emailService.DeclineEmails);
        }

        [Fact]
        public async Task ApplyTransitionAsync_MoreInfoWithoutMessage_Throws()
        {
            var request = Seed(TrialRequestStatus.EmailVerified);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _transition.ApplyTransitionAsync(
                    request.Id,
                    TrialReviewDecision.RequestMoreInfo,
                    Context(reason: "   ")
                )
            );

            Assert.Contains("More info message is required", ex.Message);
            Assert.Empty(_emailService.MoreInfoEmails);
        }

        [Fact]
        public async Task ApplyTransitionAsync_DeclineReasonOverMaxLength_Throws()
        {
            var request = Seed(TrialRequestStatus.EmailVerified);
            var longReason = new string('x', 2001);

            await Assert.ThrowsAsync<ArgumentException>(
                () => _transition.ApplyTransitionAsync(
                    request.Id,
                    TrialReviewDecision.Decline,
                    Context(reason: longReason)
                )
            );
        }

        // ---------- Field writes ----------

        [Fact]
        public async Task ApplyTransitionAsync_Decline_WritesAllReviewFields()
        {
            var request = Seed(TrialRequestStatus.EmailVerified);

            await _transition.ApplyTransitionAsync(
                request.Id,
                TrialReviewDecision.Decline,
                Context(reason: "Not a fit", notes: "internal note")
            );

            var updated = _context.TrialRequests.Single();
            Assert.Equal(TrialRequestStatus.Declined, updated.Status);
            Assert.NotNull(updated.DeclinedAt);
            Assert.Equal("Not a fit", updated.DeclineReason);
            Assert.NotNull(updated.ReviewedAt);
            Assert.Equal("admin@tummly.com", updated.ReviewedBy);
            Assert.Equal("internal note", updated.AdminNotes);
            Assert.Single(_emailService.DeclineEmails);
            Assert.Equal("Not a fit", _emailService.DeclineEmails[0].Reason);
        }

        [Fact]
        public async Task ApplyTransitionAsync_Approve_SetsApprovedFieldsAndRotatesToken()
        {
            var request = Seed(
                TrialRequestStatus.EmailVerified,
                locations: "3",
                approvalToken: "old-token"
            );

            var result = await _transition.ApplyTransitionAsync(
                request.Id,
                TrialReviewDecision.Approve,
                Context()
            );

            var updated = _context.TrialRequests.Single();
            Assert.True(updated.IsApproved);
            Assert.NotNull(updated.ApprovedAt);
            Assert.Equal("Multi", updated.AccountType);
            Assert.NotEqual("old-token", updated.ApprovalToken);
            Assert.NotNull(updated.InviteExpiresAt);
            Assert.NotNull(updated.InviteSentAt);
            Assert.Single(_emailService.SetupInvitationEmails);
            Assert.Empty(_emailService.SetupReminderEmails);
            Assert.NotNull(result.SetupLink);
        }

        [Fact]
        public async Task ApplyTransitionAsync_ResendInvite_RotatesTokenAndSendsReminder()
        {
            var request = Seed(
                TrialRequestStatus.Approved,
                isApproved: true,
                approvalToken: "old-token"
            );

            await _transition.ApplyTransitionAsync(
                request.Id,
                TrialReviewDecision.ResendInvite,
                Context()
            );

            var updated = _context.TrialRequests.Single();
            Assert.Equal(TrialRequestStatus.InviteSent, updated.Status);
            Assert.NotEqual("old-token", updated.ApprovalToken);
            Assert.Single(_emailService.SetupReminderEmails);
            Assert.Empty(_emailService.SetupInvitationEmails);
        }

        [Fact]
        public async Task ApplyTransitionAsync_ResendInvite_PreservesReviewedFields()
        {
            var reviewedAt = DateTime.UtcNow.AddDays(-3);
            var request = Seed(
                TrialRequestStatus.Approved,
                isApproved: true,
                approvalToken: "old-token"
            );
            request.ReviewedAt = reviewedAt;
            request.ReviewedBy = "original-admin@tummly.com";
            request.AdminNotes = "Keep this note";
            await _context.SaveChangesAsync();

            await _transition.ApplyTransitionAsync(
                request.Id,
                TrialReviewDecision.ResendInvite,
                Context(admin: "resender@tummly.com")
            );

            var updated = _context.TrialRequests.Single();
            Assert.Equal(TrialRequestStatus.InviteSent, updated.Status);
            Assert.Equal(reviewedAt, updated.ReviewedAt);
            Assert.Equal("original-admin@tummly.com", updated.ReviewedBy);
            Assert.Equal("Keep this note", updated.AdminNotes);
        }

        [Fact]
        public async Task ApplyTransitionAsync_Approve_DoesNotClearExistingAdminNotes()
        {
            var request = Seed(TrialRequestStatus.EmailVerified);
            request.AdminNotes = "Prior internal note";
            await _context.SaveChangesAsync();

            await _transition.ApplyTransitionAsync(
                request.Id,
                TrialReviewDecision.Approve,
                Context()
            );

            Assert.Equal("Prior internal note", _context.TrialRequests.Single().AdminNotes);
        }

        // ---------- After-commit atomicity (ADR-0005) ----------

        [Fact]
        public async Task ApplyTransitionAsync_EmailThrowsAfterSave_ThrowsEmailDispatchButStatePersisted()
        {
            await using var throwContext = new ApplicationDbContext(
                new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options
            );
            throwContext.TrialRequests.Add(
                new TrialRequest
                {
                    BusinessName = "Test Cafe",
                    BusinessCategory = "Cafe / coffee shop",
                    Locations = "1",
                    FullName = "Jane Operator",
                    Email = "jane@example.com",
                    Mobile = "07123456789",
                    MainLocation = "42 High Street",
                    TownCity = "Manchester",
                    Postcode = "M1 1AA",
                    Role = "Owner",
                    Goal = "Grow repeat guests",
                    TermsAccepted = true,
                    IsEmailVerified = true,
                    Status = TrialRequestStatus.EmailVerified,
                    AccountType = "Single",
                }
            );
            await throwContext.SaveChangesAsync();

            var throwingTransition = new TrialReviewTransition(
                throwContext,
                new ThrowingEmailService(),
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["Frontend:BaseUrl"] = "https://app.tummly.com",
                        }
                    )
                    .Build(),
                NullLogger<TrialReviewTransition>.Instance
            );

            var requestId = throwContext.TrialRequests.Single().Id;

            await Assert.ThrowsAsync<TrialReviewEmailDispatchException>(
                () => throwingTransition.ApplyTransitionAsync(
                    requestId,
                    TrialReviewDecision.Decline,
                    Context(reason: "Not a fit")
                )
            );

            var persisted = throwContext.TrialRequests.AsNoTracking().Single();
            Assert.Equal(TrialRequestStatus.Declined, persisted.Status);
            Assert.Equal("Not a fit", persisted.DeclineReason);
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

            public List<(string Email, string FullName, string Reason)>
                DeclineEmails { get; } = [];

            public List<(string Email, string FullName, string Message)>
                MoreInfoEmails { get; } = [];

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
                SetupReminderEmails.Add(
                    (toEmail, fullName, setupLink, expiresAtUtc)
                );
                return Task.CompletedTask;
            }

            public override Task SendDeclineEmailAsync(
                string toEmail,
                string fullName,
                string declineReason
            )
            {
                DeclineEmails.Add((toEmail, fullName, declineReason));
                return Task.CompletedTask;
            }

            public override Task SendMoreInfoEmailAsync(
                string toEmail,
                string fullName,
                string moreInfoMessage
            )
            {
                MoreInfoEmails.Add((toEmail, fullName, moreInfoMessage));
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingEmailService : EmailServiceStubBase
        {
            public override Task SendDeclineEmailAsync(
                string toEmail,
                string fullName,
                string declineReason
            ) =>
                throw new InvalidOperationException("email send failed");
        }
    }
}
