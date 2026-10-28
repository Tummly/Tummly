using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TummlyBackend.Billing.CustomPlan;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.Services
{
    public class SignupServiceTests : IDisposable
    {
        private const string HashKey = "test-otp-hash-key";

        private static readonly DateTimeOffset T0 = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

        private static readonly TimeSpan ResendWait = TimeSpan.FromSeconds(61);

        private readonly ApplicationDbContext _db;
        private readonly TrackingEmailService _emailService;
        private readonly FakeTimeProvider _time;
        private readonly CapturingLogger<SignupService> _logger;
        private readonly SignupService _sut;

        public SignupServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _emailService = new TrackingEmailService();
            _time = new FakeTimeProvider(T0);
            _logger = new CapturingLogger<SignupService>();
            _sut = new SignupService(
                _db,
                _emailService,
                new NoOpProvisioningService(),
                new CustomPlanQuoteService(_db, Options.Create(new TummlySellerVatSettings())),
                new StubJwtService(),
                new OtpChallengeService(_db, new OtpCodeHasher(HashKey), _time),
                _time,
                _logger
            );
        }

        [Fact]
        public async Task Start_NewEmail_SendsCode_NoToken()
        {
            var result = await StartFor("owner@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Equal("owner@example.com", result.Email);
            Assert.Null(typeof(SignupCodeSentResponse).GetProperty("SessionToken"));

            var sent = Assert.Single(_emailService.SentOtpEmails);
            Assert.Equal("owner@example.com", sent.Email);
            Assert.Matches("^[0-9]{6}$", sent.Otp);

            var pending = await Row("owner@example.com");
            Assert.Equal(PendingSignupStatuses.EmailPending, pending.Status);
            Assert.Equal(1, pending.CodeGeneration);
            Assert.Equal(1, pending.CodesSentInWindow);
            Assert.Equal(T0.UtcDateTime, pending.AbuseWindowStartUtc);
            Assert.Null(pending.SessionTokenIssuedAtUtc);

            var otp = await _db.OtpVerifications.SingleAsync();
            Assert.Equal(OtpPurposes.Signup, otp.Purpose);
            Assert.Equal(pending.Id.ToString("N"), otp.SubjectId);
            Assert.Equal(1, otp.Generation);
            Assert.Equal(string.Empty, otp.OtpCode);
            Assert.False(otp.IsUsed);
        }

        [Fact]
        public async Task Start_MidFlowVerified_SendsNewCode_NoToken_StatusUnchanged()
        {
            var sessionToken = Guid.NewGuid();
            var issuedAt = T0.UtcDateTime.AddMinutes(-5);
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    Email = "verified@example.com",
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = issuedAt,
                    SessionTokenIssuedAtUtc = issuedAt,
                    CreatedAtUtc = T0.UtcDateTime.AddHours(-1),
                    UpdatedAtUtc = issuedAt,
                }
            );
            await _db.SaveChangesAsync();

            var result = await StartFor("verified@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Single(_emailService.SentOtpEmails);
            var pending = await Row("verified@example.com");
            Assert.Equal(PendingSignupStatuses.Verified, pending.Status);
            Assert.Equal(sessionToken, pending.SessionToken);
            Assert.Equal(issuedAt, pending.SessionTokenIssuedAtUtc);
            Assert.Equal(1, pending.CodeGeneration);
        }

        [Fact]
        public async Task Start_ExistingUser_GenericResponse_NoEmail()
        {
            _db.Users.Add(
                new User
                {
                    Email = "taken@example.com",
                    FullName = "Taken User",
                    PasswordHash = "hash",
                }
            );
            await _db.SaveChangesAsync();

            var result = await StartFor("taken@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Equal("taken@example.com", result.Email);
            Assert.Empty(_emailService.SentOtpEmails);
            Assert.Empty(await _db.PendingSignups.ToListAsync());
            Assert.Empty(await _db.OtpVerifications.ToListAsync());
        }

        [Fact]
        public async Task Start_CompleteRow_GenericResponse_NoEmail()
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = "complete@example.com",
                    Status = PendingSignupStatuses.Complete,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = T0.UtcDateTime.AddDays(-1),
                }
            );
            await _db.SaveChangesAsync();

            var result = await StartFor("complete@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Empty(_emailService.SentOtpEmails);
            var pending = await Row("complete@example.com");
            Assert.Equal(PendingSignupStatuses.Complete, pending.Status);
            Assert.Equal(sessionToken, pending.SessionToken);
            Assert.Equal(1, await _db.PendingSignups.CountAsync());
        }

        [Fact]
        public async Task StartAsync_ResumesExistingEmailPending_NoDuplicate()
        {
            await StartFor("resume@example.com");
            var sessionToken = (await Row("resume@example.com")).SessionToken;

            _time.Advance(ResendWait);
            var second = await StartFor("Resume@example.com");

            Assert.Equal("code_sent", second.Status);
            Assert.Equal("resume@example.com", second.Email);
            var pending = await Row("resume@example.com");
            Assert.Equal(sessionToken, pending.SessionToken);
            Assert.Equal(1, await _db.PendingSignups.CountAsync());
            Assert.Equal(2, _emailService.SentOtpEmails.Count);
            Assert.Equal(1, pending.OtpResendCount);
        }

        [Fact]
        public async Task Start_WithinCooldown_ReturnsCodeSent_NothingSent_EarlierCodeValid()
        {
            await StartFor("start-cooldown@example.com");
            var firstCode = LastCode();

            var second = await StartFor("start-cooldown@example.com");

            // Ruling 13: the same answer as a sent code, so the response does not tell
            // whether the email has an account or is cooling down.
            Assert.Equal("code_sent", second.Status);
            Assert.Equal("start-cooldown@example.com", second.Email);
            Assert.Single(_emailService.SentOtpEmails);
            var pending = await Row("start-cooldown@example.com");
            Assert.Equal(0, pending.OtpResendCount);
            Assert.Equal(1, pending.CodesSentInWindow);
            Assert.Equal(1, pending.CodeGeneration);

            var session = await VerifyWith("start-cooldown@example.com", firstCode);
            Assert.Equal(PendingSignupStatuses.Verified, session.Status);
        }

        [Fact]
        public async Task Start_AtResendCap_ReturnsCodeSent_NothingSent_Abandoned()
        {
            await StartFor("start-limit@example.com");
            _time.Advance(ResendWait);
            var pending = await Row("start-limit@example.com");
            pending.OtpResendCount = 5;
            await _db.SaveChangesAsync();

            var result = await StartFor("start-limit@example.com");

            Assert.Equal("code_sent", result.Status);
            pending = await Row("start-limit@example.com");
            Assert.Equal(PendingSignupStatuses.Abandoned, pending.Status);
            Assert.Single(_emailService.SentOtpEmails);
        }

        [Fact]
        public async Task StartAsync_ResetsAbandoned()
        {
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = "abandoned@example.com",
                    Status = PendingSignupStatuses.Abandoned,
                    OtpResendCount = 5,
                    CodesSentInWindow = 4,
                    AbuseWindowStartUtc = T0.UtcDateTime.AddHours(-2),
                    TermsAccepted = true,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = T0.UtcDateTime.AddDays(-1),
                }
            );
            await _db.SaveChangesAsync();

            var result = await StartFor("abandoned@example.com");

            Assert.Equal("code_sent", result.Status);
            var row = await Row("abandoned@example.com");
            Assert.Equal(PendingSignupStatuses.EmailPending, row.Status);
            Assert.Equal(0, row.OtpResendCount);
            Assert.Equal(5, row.CodesSentInWindow);
            Assert.Single(_emailService.SentOtpEmails);
        }

        [Fact]
        public async Task Verify_RightCode_IssuesNewToken_OldTokenChanged()
        {
            await StartFor("verify@example.com");
            var oldToken = (await Row("verify@example.com")).SessionToken;

            var session = await VerifyWith("verify@example.com", LastCode());

            Assert.Equal(PendingSignupStatuses.Verified, session.Status);
            Assert.Equal("verify@example.com", session.Email);
            Assert.NotEqual(Guid.Empty, session.SessionToken);
            Assert.NotEqual(oldToken, session.SessionToken);
            Assert.False(await _db.PendingSignups.AnyAsync(x => x.SessionToken == oldToken));

            var pending = await Row("verify@example.com");
            Assert.Equal(session.SessionToken, pending.SessionToken);
            Assert.Equal(T0.UtcDateTime, pending.SessionTokenIssuedAtUtc);
            Assert.Equal(T0.UtcDateTime, pending.EmailVerifiedAt);
            Assert.True((await _db.OtpVerifications.SingleAsync()).IsUsed);
            Assert.Empty(await _db.TrialRequests.ToListAsync());
        }

        [Fact]
        public async Task Verify_MidFlowRow_KeepsStatus_NewToken()
        {
            var oldToken = Guid.NewGuid();
            var issuedAt = T0.UtcDateTime.AddHours(-1);
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = oldToken,
                    Email = "midflow@example.com",
                    Status = PendingSignupStatuses.AwaitingPayment,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = issuedAt,
                    SessionTokenIssuedAtUtc = issuedAt,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = issuedAt,
                }
            );
            await _db.SaveChangesAsync();
            await StartFor("midflow@example.com");

            var session = await VerifyWith("midflow@example.com", LastCode());

            Assert.Equal(PendingSignupStatuses.AwaitingPayment, session.Status);
            Assert.NotEqual(oldToken, session.SessionToken);
            var pending = await Row("midflow@example.com");
            Assert.Equal(PendingSignupStatuses.AwaitingPayment, pending.Status);
            Assert.Equal(session.SessionToken, pending.SessionToken);
            Assert.Equal(T0.UtcDateTime, pending.SessionTokenIssuedAtUtc);
        }

        [Fact]
        public async Task Verify_EmailCaseAndSpaces_Normalised()
        {
            await StartFor(" Owner@Example.com ");

            var session = await VerifyWith(" OWNER@example.com ", LastCode());

            Assert.Equal(PendingSignupStatuses.Verified, session.Status);
            Assert.Equal("owner@example.com", session.Email);
            Assert.Equal(PendingSignupStatuses.Verified, (await Row("owner@example.com")).Status);
        }

        [Fact]
        public async Task Verify_FiveWrong_AttemptsExceeded()
        {
            const string email = "five@example.com";
            await StartFor(email);
            var code = LastCode();

            for (var i = 0; i < 4; i++)
            {
                var miss = await Assert.ThrowsAsync<SignupFlowException>(() =>
                    VerifyWith(email, WrongCode(code))
                );
                Assert.Equal("otp_invalid", miss.Code);
                Assert.Equal(400, miss.StatusCode);
            }

            var spent = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, WrongCode(code))
            );
            Assert.Equal("otp_attempts_exceeded", spent.Code);
            Assert.Equal(400, spent.StatusCode);

            var afterSpent = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, code)
            );
            Assert.Equal("otp_invalid", afterSpent.Code);
            var pending = await Row(email);
            Assert.Equal(PendingSignupStatuses.EmailPending, pending.Status);
            Assert.Equal(6, pending.FailedVerifiesInWindow);
        }

        [Fact]
        public async Task Verify_TenthCodeSent_RightCodeStillVerifies()
        {
            const string email = "tenth@example.com";
            await StartFor(email);
            var code = LastCode();

            // Ten codes sent is the send limit. It must not lock a correct code.
            var pending = await Row(email);
            pending.CodesSentInWindow = SignupAbuseLimits.MaxCodesPerWindow;
            pending.FailedVerifiesInWindow = SignupAbuseLimits.MaxFailedVerifiesPerWindow - 1;
            await _db.SaveChangesAsync();

            var session = await VerifyWith(email, code);

            Assert.Equal(PendingSignupStatuses.Verified, session.Status);
            Assert.Equal(session.SessionToken, (await Row(email)).SessionToken);
        }

        [Fact]
        public async Task Verify_NoLiveRow_OtpInvalid()
        {
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = "no-live@example.com",
                    Status = PendingSignupStatuses.Complete,
                    TermsAccepted = true,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = T0.UtcDateTime.AddDays(-1),
                }
            );
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith("no-live@example.com", "123456")
            );

            Assert.Equal("otp_invalid", ex.Code);
            Assert.Equal(400, ex.StatusCode);
        }

        [Fact]
        public async Task Window_FifteenWrongAcrossResends_Locked_ResendDoesNotReset()
        {
            const string email = "fifteen@example.com";
            await StartFor(email);
            var first = LastCode();
            await SpendCodeAsync(email, first);

            _time.Advance(ResendWait);
            await _sut.ResendOtpAsync(email);
            var second = LastCode();
            await SpendCodeAsync(email, second);

            _time.Advance(ResendWait);
            await _sut.ResendOtpAsync(email);
            var third = LastCode();
            await SpendCodeAsync(email, third);

            var spentPending = await Row(email);
            Assert.Equal(15, spentPending.FailedVerifiesInWindow);
            Assert.Equal(3, spentPending.CodesSentInWindow);

            // Ruling 13: a locked send is silent. Only verify-otp answers challenge_locked.
            _time.Advance(ResendWait);
            var resend = await _sut.ResendOtpAsync(email);
            Assert.Equal("code_sent", resend.Status);

            var verify = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, third)
            );
            Assert.Equal("challenge_locked", verify.Code);

            var pending = await Row(email);
            Assert.Equal(15, pending.FailedVerifiesInWindow);
            Assert.Equal(3, pending.CodesSentInWindow);
            Assert.Equal(3, _emailService.SentOtpEmails.Count);
        }

        [Fact]
        public async Task Window_AbandonedRestart_DoesNotResetCounters()
        {
            const string email = "restart-window@example.com";
            await StartFor(email);
            for (var i = 0; i < 5; i++)
            {
                _time.Advance(ResendWait);
                await _sut.ResendOtpAsync(email);
            }

            _time.Advance(ResendWait);
            var cap = await _sut.ResendOtpAsync(email);
            Assert.Equal("code_sent", cap.Status);
            Assert.Equal(6, _emailService.SentOtpEmails.Count);

            var abandoned = await Row(email);
            Assert.Equal(PendingSignupStatuses.Abandoned, abandoned.Status);
            Assert.Equal(6, abandoned.CodesSentInWindow);
            var windowStart = abandoned.AbuseWindowStartUtc;

            _time.Advance(ResendWait);
            await StartFor(email);

            var restarted = await Row(email);
            Assert.Equal(PendingSignupStatuses.EmailPending, restarted.Status);
            Assert.Equal(0, restarted.OtpResendCount);
            Assert.Equal(7, restarted.CodesSentInWindow);
            Assert.Equal(windowStart, restarted.AbuseWindowStartUtc);
            Assert.Equal(7, _emailService.SentOtpEmails.Count);
        }

        [Fact]
        public async Task Window_EleventhCode_Locked_WithRetryAfter()
        {
            const string email = "eleventh@example.com";
            await StartFor(email);
            for (var i = 0; i < 5; i++)
            {
                _time.Advance(ResendWait);
                await _sut.ResendOtpAsync(email);
            }

            _time.Advance(ResendWait);
            await _sut.ResendOtpAsync(email);
            Assert.Equal(6, _emailService.SentOtpEmails.Count);

            await StartFor(email);
            for (var i = 0; i < 3; i++)
            {
                _time.Advance(ResendWait);
                await _sut.ResendOtpAsync(email);
            }
            Assert.Equal(10, _emailService.SentOtpEmails.Count);

            // The eleventh code is not sent, and the answer still says code_sent (Ruling 13).
            _time.Advance(ResendWait);
            var eleventh = await _sut.ResendOtpAsync(email);
            Assert.Equal("code_sent", eleventh.Status);
            var started = await StartFor(email);
            Assert.Equal("code_sent", started.Status);

            var pending = await Row(email);
            Assert.Equal(10, pending.CodesSentInWindow);
            Assert.Equal(10, _emailService.SentOtpEmails.Count);
        }

        [Fact]
        public async Task Verify_WindowLocked_ChallengeLocked_RetryAfterIsUtcKind()
        {
            const string email = "locked-verify@example.com";
            await StartFor(email);
            var pending = await Row(email);
            // SQL Server returns DateTime values with Kind=Unspecified.
            pending.AbuseWindowStartUtc = DateTime.SpecifyKind(
                T0.UtcDateTime.AddHours(-1),
                DateTimeKind.Unspecified
            );
            pending.FailedVerifiesInWindow = SignupAbuseLimits.MaxFailedVerifiesPerWindow;
            await _db.SaveChangesAsync();

            var locked = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, LastCode())
            );

            Assert.Equal("challenge_locked", locked.Code);
            Assert.Equal(429, locked.StatusCode);
            Assert.Equal("Too many attempts for this email. Try again later.", locked.Message);
            Assert.Equal(T0.UtcDateTime.AddHours(23), locked.RetryAfterUtc);
            Assert.Equal(DateTimeKind.Utc, locked.RetryAfterUtc!.Value.Kind);
            Assert.EndsWith(
                "Z",
                System.Text.Json.JsonSerializer.Serialize(locked.RetryAfterUtc!.Value).Trim('"')
            );
        }

        [Fact]
        public async Task Window_After24h_Resets()
        {
            const string email = "after-day@example.com";
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = email,
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    CodeGeneration = 10,
                    LastOtpSentAt = T0.UtcDateTime.AddHours(-1),
                    AbuseWindowStartUtc = T0.UtcDateTime,
                    CodesSentInWindow = 10,
                    FailedVerifiesInWindow = 15,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = T0.UtcDateTime.AddHours(-1),
                }
            );
            await _db.SaveChangesAsync();

            _time.Advance(TimeSpan.FromHours(23));
            var locked = await _sut.ResendOtpAsync(email);
            Assert.Equal("code_sent", locked.Status);
            Assert.Empty(_emailService.SentOtpEmails);

            _time.Advance(TimeSpan.FromHours(1));
            await _sut.ResendOtpAsync(email);

            var pending = await Row(email);
            Assert.Equal(1, pending.CodesSentInWindow);
            Assert.Equal(0, pending.FailedVerifiesInWindow);
            Assert.Equal(T0.UtcDateTime.AddHours(24), pending.AbuseWindowStartUtc);
            Assert.Single(_emailService.SentOtpEmails);
        }

        [Fact]
        public async Task Send_EmailFails_CodeUnusable_NotCounted_503()
        {
            const string email = "delivery@example.com";
            _emailService.FailNextSend = true;

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() => StartFor(email));

            Assert.Equal("code_delivery_failed", ex.Code);
            Assert.Equal(503, ex.StatusCode);
            Assert.Equal("We could not send the code. Try again in a minute.", ex.Message);
            Assert.Empty(_emailService.SentOtpEmails);
            Assert.True((await _db.OtpVerifications.SingleAsync()).IsUsed);
            Assert.Equal(0, (await Row(email)).CodesSentInWindow);

            _time.Advance(ResendWait);
            _emailService.FailNextSend = false;
            await StartFor(email);

            Assert.Single(_emailService.SentOtpEmails);
            Assert.Equal(1, (await Row(email)).CodesSentInWindow);
        }

        [Fact]
        public async Task TokenRefusal_LogsReason_NeverTheToken()
        {
            var token = await SeedVerifiedPendingAsync("refused@example.com");
            var row = await Row("refused@example.com");
            row.SessionTokenIssuedAtUtc = null;
            await _db.SaveChangesAsync();

            var legacy = await Assert.ThrowsAsync<SignupFlowException>(() =>
                _sut.GetBySessionAsync(token)
            );
            Assert.Equal("signup_session_expired", legacy.Code);
            Assert.Equal(401, legacy.StatusCode);
            Assert.Null(legacy.RetryAfterUtc);

            row.SessionTokenIssuedAtUtc = T0.UtcDateTime.AddHours(-25);
            await _db.SaveChangesAsync();
            await Assert.ThrowsAsync<SignupFlowException>(() => _sut.GetBySessionAsync(token));

            var logged = string.Join("\n", _logger.Entries);
            Assert.Contains("signup.token_rejected reason=legacy", logged, StringComparison.Ordinal);
            Assert.Contains("signup.token_rejected reason=expired", logged, StringComparison.Ordinal);
            Assert.DoesNotContain(token.ToString(), logged, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(token.ToString("N"), logged, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TokenRefusal_ClaimedToken_LogsClaimed_NotLegacy()
        {
            var token = await SeedVerifiedPendingAsync("claimed@example.com");
            var row = await Row("claimed@example.com");
            row.Status = PendingSignupStatuses.Complete;
            // The claim clears the issued time. A replay must still be logged as claimed.
            row.SessionLoginConsumedAtUtc = T0.UtcDateTime.AddMinutes(-1);
            row.SessionTokenIssuedAtUtc = null;
            await _db.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() => _sut.GetBySessionAsync(token));

            Assert.Equal("signup_session_expired", ex.Code);
            var logged = string.Join("\n", _logger.Entries);
            Assert.Contains("signup.token_rejected reason=claimed", logged, StringComparison.Ordinal);
            Assert.DoesNotContain("reason=legacy", logged, StringComparison.Ordinal);
            Assert.DoesNotContain(token.ToString(), logged, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Logs_NeverContainCodeOrToken()
        {
            const string email = "logs@example.com";
            await StartFor(email);
            var initialToken = (await Row(email)).SessionToken;
            var firstCode = LastCode();

            await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, WrongCode(firstCode))
            );
            var session = await VerifyWith(email, firstCode);

            _time.Advance(ResendWait);
            await _sut.ResendOtpAsync(email);
            var secondCode = LastCode();
            await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith("nobody-logs@example.com", "123456")
            );

            Assert.NotEmpty(_logger.Entries);
            var logged = string.Join("\n", _logger.Entries);
            foreach (
                var secret in new[]
                {
                    firstCode,
                    secondCode,
                    initialToken.ToString(),
                    initialToken.ToString("N"),
                    session.SessionToken!.Value.ToString(),
                    session.SessionToken!.Value.ToString("N"),
                }
            )
            {
                Assert.DoesNotContain(secret, logged, StringComparison.Ordinal);
            }
            Assert.DoesNotContain(email, logged, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(LogContactMask.Email(email), logged, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Logs_AuditEvents_CarryPendingIdPurposeGenerationMaskedEmail_NoSecrets()
        {
            const string email = "audit@example.com";
            await StartFor(email);
            var pending = await Row(email);
            var code = LastCode();

            await Assert.ThrowsAsync<SignupFlowException>(() => VerifyWith(email, WrongCode(code)));
            var session = await VerifyWith(email, code);

            string Event(string name) =>
                Assert.Single(_logger.Entries, entry => entry.StartsWith(name + " ", StringComparison.Ordinal));

            foreach (var name in new[] { "signup.code_sent", "signup.verify_failed", "signup.token_issued" })
            {
                var line = Event(name);
                Assert.Contains($"pending_id={pending.Id}", line, StringComparison.Ordinal);
                Assert.Contains("purpose=signup", line, StringComparison.Ordinal);
                Assert.Contains($"email={LogContactMask.Email(email)}", line, StringComparison.Ordinal);
                Assert.DoesNotContain(code, line, StringComparison.Ordinal);
                Assert.DoesNotContain(session.SessionToken!.Value.ToString(), line, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("ip", line.Split(' ').Select(part => part.Split('=')[0]));
            }

            Assert.Contains("generation=1", Event("signup.code_sent"), StringComparison.Ordinal);
            Assert.Contains("generation=1", Event("signup.verify_failed"), StringComparison.Ordinal);
            Assert.Contains("generation=1", Event("signup.token_issued"), StringComparison.Ordinal);
            Assert.Contains("reason=Invalid", Event("signup.verify_failed"), StringComparison.Ordinal);
            Assert.DoesNotContain(
                _logger.Entries,
                entry => entry.StartsWith("signup.otp_rejected", StringComparison.Ordinal)
                    || entry.StartsWith("signup.verified", StringComparison.Ordinal)
            );
        }

        [Fact]
        public async Task ResendOtpAsync_NoRow_GenericResponse_NoEmail()
        {
            var result = await _sut.ResendOtpAsync("nobody@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Equal("nobody@example.com", result.Email);
            Assert.Empty(_emailService.SentOtpEmails);
            Assert.Empty(await _db.PendingSignups.ToListAsync());
        }

        [Fact]
        public async Task ResendOtpAsync_WithinCooldown_ReturnsCodeSent_NothingSent()
        {
            await StartFor("cooldown@example.com");

            var result = await _sut.ResendOtpAsync("cooldown@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Single(_emailService.SentOtpEmails);
            var pending = await Row("cooldown@example.com");
            Assert.Equal(1, pending.CodesSentInWindow);
            Assert.Equal(1, pending.CodeGeneration);
            Assert.Equal(1, await _db.OtpVerifications.CountAsync(x => !x.IsUsed));
        }

        [Fact]
        public async Task ResendOtpAsync_MaxResends_ReturnsCodeSent_NothingSent_Abandoned()
        {
            await StartFor("limit@example.com");
            _time.Advance(ResendWait);
            var pending = await Row("limit@example.com");
            pending.OtpResendCount = 5;
            await _db.SaveChangesAsync();

            var result = await _sut.ResendOtpAsync("limit@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Single(_emailService.SentOtpEmails);
            pending = await Row("limit@example.com");
            Assert.Equal(PendingSignupStatuses.Abandoned, pending.Status);
        }

        [Fact]
        public async Task ResendOtpAsync_AbandonedRow_RestartsAndSends()
        {
            const string email = "resend-restart@example.com";
            await StartFor(email);
            for (var i = 0; i < 5; i++)
            {
                _time.Advance(ResendWait);
                await _sut.ResendOtpAsync(email);
            }

            // The sixth resend hits the cap: nothing is sent and the row is Abandoned.
            _time.Advance(ResendWait);
            await _sut.ResendOtpAsync(email);
            Assert.Equal(PendingSignupStatuses.Abandoned, (await Row(email)).Status);
            Assert.Equal(6, _emailService.SentOtpEmails.Count);

            // The seventh finds the Abandoned row and restarts it like Start does.
            var result = await _sut.ResendOtpAsync(email);

            Assert.Equal("code_sent", result.Status);
            Assert.Equal(7, _emailService.SentOtpEmails.Count);
            var restarted = await Row(email);
            Assert.Equal(PendingSignupStatuses.EmailPending, restarted.Status);
            Assert.Equal(0, restarted.OtpResendCount);
            Assert.Equal(7, restarted.CodesSentInWindow);
        }

        [Fact]
        public async Task ResendOtpAsync_AbandonedRow_WindowStillLimits()
        {
            const string email = "resend-restart-window@example.com";
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = email,
                    Status = PendingSignupStatuses.Abandoned,
                    TermsAccepted = true,
                    AbuseWindowStartUtc = T0.UtcDateTime.AddHours(-1),
                    CodesSentInWindow = SignupAbuseLimits.MaxCodesPerWindow,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = T0.UtcDateTime.AddDays(-1),
                }
            );
            await _db.SaveChangesAsync();

            var result = await _sut.ResendOtpAsync(email);

            Assert.Equal("code_sent", result.Status);
            Assert.Empty(_emailService.SentOtpEmails);
            Assert.Equal(SignupAbuseLimits.MaxCodesPerWindow, (await Row(email)).CodesSentInWindow);
        }

        [Fact]
        public async Task ResendOtpAsync_AbandonedRow_AccountExists_NothingSent()
        {
            const string email = "resend-restart-user@example.com";
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = Guid.NewGuid(),
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = email,
                    Status = PendingSignupStatuses.Abandoned,
                    TermsAccepted = true,
                    CreatedAtUtc = T0.UtcDateTime.AddDays(-1),
                    UpdatedAtUtc = T0.UtcDateTime.AddDays(-1),
                }
            );
            _db.Users.Add(new User { Email = email, FullName = "Owner", PasswordHash = "hash" });
            await _db.SaveChangesAsync();

            var result = await _sut.ResendOtpAsync(email);

            Assert.Equal("code_sent", result.Status);
            Assert.Empty(_emailService.SentOtpEmails);
            Assert.Equal(PendingSignupStatuses.Abandoned, (await Row(email)).Status);
        }

        [Fact]
        public async Task SuppressedAnswers_LookTheSame_ForAccountCooldownLockAndCap()
        {
            _db.Users.Add(new User { Email = "has-account@example.com", FullName = "A", PasswordHash = "h" });
            await _db.SaveChangesAsync();
            await StartFor("cooling@example.com");

            var account = await StartFor("has-account@example.com");
            var cooling = await StartFor("cooling@example.com");
            var fresh = await StartFor("fresh@example.com");

            Assert.Equal(account.Status, cooling.Status);
            Assert.Equal(account.Status, fresh.Status);
            Assert.Equal(
                System.Text.Json.JsonSerializer.Serialize(account).Replace("has-account", "x"),
                System.Text.Json.JsonSerializer.Serialize(cooling).Replace("cooling", "x")
            );
        }

        [Fact]
        public async Task Verify_SignInCodeForSameEmail_Refused()
        {
            const string email = "wrong-purpose@example.com";
            await StartFor(email);
            var pending = await Row(email);

            // A sign-in code for this email is not a signup code, even when the digits are right.
            var challenge = new OtpChallengeService(_db, new OtpCodeHasher(HashKey), _time);
            var signIn = await challenge.IssueAsync(
                new OtpIssueRequest(email, OtpPurposes.SignIn, "42", 0)
            );

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, signIn.Code)
            );

            Assert.Equal("otp_invalid", ex.Code);
            Assert.Equal(PendingSignupStatuses.EmailPending, (await Row(email)).Status);
            Assert.Equal(pending.Id, (await Row(email)).Id);
        }

        [Fact]
        public async Task ResendOtpAsync_AfterCooldown_SendsNewOtp()
        {
            await StartFor("resend@example.com");
            _time.Advance(ResendWait);

            var result = await _sut.ResendOtpAsync("resend@example.com");

            Assert.Equal("code_sent", result.Status);
            Assert.Equal(2, _emailService.SentOtpEmails.Count);
            var pending = await Row("resend@example.com");
            Assert.Equal(1, pending.OtpResendCount);
            Assert.Equal(2, pending.CodeGeneration);
            Assert.Equal(1, await _db.OtpVerifications.CountAsync(x => !x.IsUsed));
        }

        [Fact]
        public void PendingSignup_EmailHasUniqueIndex()
        {
            var entity = _db.Model.FindEntityType(typeof(PendingSignup));
            Assert.NotNull(entity);

            var emailIndex = entity!
                .GetIndexes()
                .Single(index =>
                    index.Properties.Select(p => p.Name).SequenceEqual(
                        new[] { "Email" }
                    )
                );
            Assert.True(emailIndex.IsUnique);
        }

        [Fact]
        public async Task SaveOnboarding_OneLocation_SetsSingle()
        {
            var sessionToken = await SeedVerifiedPendingAsync("single@example.com");

            var result = await _sut.SaveOnboardingAsync(
                sessionToken,
                new SaveSignupOnboardingDto
                {
                    Password = "Password1!",
                    ConfirmPassword = "Password1!",
                    FullName = "Single Owner",
                    GroupName = "Solo Kitchen",
                    BusinessCategory = "takeaway",
                    PrimaryPhone = "+447700900123",
                    ChosenPlan = BillingSubscriptionPlans.Pilot,
                    Locations =
                    [
                        new SaveSignupOnboardingDto.LocationItem
                        {
                            LocationName = "Main",
                            Address = "1 High St",
                            City = "London",
                            Postcode = "SW1A 1AA",
                        },
                    ],
                }
            );

            // Stub provisioner leaves status at Provisioning (real path reaches Complete).
            Assert.Equal(PendingSignupStatuses.Provisioning, result.Status);
            Assert.Equal(sessionToken, result.SessionToken);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Equal("Single", pending.AccountType);
            Assert.Equal(BillingSubscriptionPlans.Pilot, pending.ChosenPlan);
            Assert.Equal("monthly", pending.ChosenCadence);
            Assert.Equal(PendingSignupStatuses.Provisioning, pending.Status);
            Assert.Equal("Single Owner", pending.FullName);
            Assert.False(string.IsNullOrWhiteSpace(pending.PasswordHash));
            Assert.True(
                BCrypt.Net.BCrypt.Verify("Password1!", pending.PasswordHash)
            );
            Assert.False(string.IsNullOrWhiteSpace(pending.OnboardingJson));
            Assert.DoesNotContain(
                "Password1!",
                pending.OnboardingJson!,
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task SaveOnboarding_MissingOrFreePlan_ThrowsChoosePlan()
        {
            var sessionToken = await SeedVerifiedPendingAsync("noplan@example.com");

            var missing = await Assert.ThrowsAsync<SignupFlowException>(() =>
                _sut.SaveOnboardingAsync(
                    sessionToken,
                    new SaveSignupOnboardingDto
                    {
                        Password = "Password1!",
                        ConfirmPassword = "Password1!",
                        FullName = "No Plan",
                        GroupName = "Kitchen",
                        BusinessCategory = "takeaway",
                        Locations =
                        [
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "Main",
                                Address = "1 High St",
                            },
                        ],
                    }
                )
            );
            Assert.Equal(SignupService.ChoosePlanMessage, missing.Message);

            var free = await Assert.ThrowsAsync<SignupFlowException>(() =>
                _sut.SaveOnboardingAsync(
                    sessionToken,
                    new SaveSignupOnboardingDto
                    {
                        Password = "Password1!",
                        ConfirmPassword = "Password1!",
                        FullName = "Free Plan",
                        GroupName = "Kitchen",
                        BusinessCategory = "takeaway",
                        ChosenPlan = BillingSubscriptionPlans.Free,
                        Locations =
                        [
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "Main",
                                Address = "1 High St",
                            },
                        ],
                    }
                )
            );
            Assert.Equal(SignupService.ChoosePlanMessage, free.Message);
        }

        [Fact]
        public async Task SaveOnboarding_TwoLocations_Throws()
        {
            var sessionToken = await SeedVerifiedPendingAsync("multi@example.com");

            var ex = await Assert.ThrowsAsync<SignupFlowException>(() =>
                _sut.SaveOnboardingAsync(
                    sessionToken,
                    new SaveSignupOnboardingDto
                    {
                        Password = "Password1!",
                        ConfirmPassword = "Password1!",
                        FullName = "Multi Owner",
                        GroupName = "Group Kitchen",
                        BusinessCategory = "restaurant",
                        ChosenPlan = BillingSubscriptionPlans.Pilot,
                        Locations =
                        [
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "North",
                                Address = "1 North St",
                                City = "Manchester",
                                Postcode = "M1 1AE",
                            },
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "South",
                                Address = "2 South St",
                                City = "Bristol",
                                Postcode = "BS1 1AA",
                            },
                        ],
                    }
                )
            );

            Assert.Equal("Exactly one location is required.", ex.Message);

            var pending = await _db.PendingSignups.SingleAsync();
            Assert.Null(pending.AccountType);
            Assert.NotEqual("Multi", pending.AccountType);
            Assert.Equal(PendingSignupStatuses.Verified, pending.Status);
        }

        [Fact]
        public async Task SaveOnboarding_EmailPending_Throws()
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = "early@example.com",
                    Status = PendingSignupStatuses.EmailPending,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                }
            );
            await _db.SaveChangesAsync();

            await Assert.ThrowsAsync<SignupFlowException>(() =>
                _sut.SaveOnboardingAsync(
                    sessionToken,
                    new SaveSignupOnboardingDto
                    {
                        Password = "Password1!",
                        ConfirmPassword = "Password1!",
                        FullName = "Too Early",
                        GroupName = "Kitchen",
                        BusinessCategory = "takeaway",
                        Locations =
                        [
                            new SaveSignupOnboardingDto.LocationItem
                            {
                                LocationName = "Main",
                                Address = "1 High St",
                            },
                        ],
                    }
                )
            );
        }

        [Fact]
        public async Task GetBySession_ReturnsResumeWithoutPassword()
        {
            var sessionToken = await SeedVerifiedPendingAsync("resume-get@example.com");

            await _sut.SaveOnboardingAsync(
                sessionToken,
                new SaveSignupOnboardingDto
                {
                    Password = "Password1!",
                    ConfirmPassword = "Password1!",
                    FullName = "Resume Owner",
                    GroupName = "Resume Kitchen",
                    BusinessCategory = "cafe",
                    ChosenPlan = BillingSubscriptionPlans.Pilot,
                    Locations =
                    [
                        new SaveSignupOnboardingDto.LocationItem
                        {
                            LocationName = "Main",
                            Address = "9 Resume Rd",
                            City = "Leeds",
                            Postcode = "LS1 1AA",
                        },
                    ],
                }
            );

            var resume = await _sut.GetBySessionAsync(sessionToken);

            Assert.Null(typeof(SignupResumeResponse).GetProperty("SessionToken"));
            Assert.Equal("resume-get@example.com", resume.Email);
            Assert.Equal(PendingSignupStatuses.Provisioning, resume.Status);
            Assert.Equal("provisioning", resume.LastStepHint);
            Assert.Equal("Resume Owner", resume.FullName);
            Assert.Equal("Single", resume.AccountType);
            Assert.Equal("Resume Kitchen", resume.GroupName);
            Assert.Equal("cafe", resume.BusinessCategory);
            Assert.Null(resume.PasswordHash);
            Assert.DoesNotContain(
                "Password1!",
                resume.OnboardingJson ?? "",
                StringComparison.Ordinal
            );
        }

        [Fact]
        public async Task GetBySession_ReturnsSavedCustomPlanCadenceLocationsAndExtras()
        {
            var sessionToken = await SeedVerifiedPendingAsync("resume-custom@example.com");
            var row = await Row("resume-custom@example.com");
            row.ChosenPlan = BillingSubscriptionPlans.Custom;
            row.ChosenCadence = "annual";
            row.CustomPlanConfigJson = new CustomPlanConfig
            {
                Version = 2,
                Cadence = "annual",
                BasePlan = "growth",
                Locations = 3,
                Packs = new Dictionary<string, int> { ["qr_10"] = 2 },
                QuoteHash = "0123456789abcdef0123456789abcdef",
            }.ToJson();
            await _db.SaveChangesAsync();

            var resume = await _sut.GetBySessionAsync(sessionToken);

            Assert.Equal(BillingSubscriptionPlans.Custom, resume.ChosenPlan);
            Assert.Equal("annual", resume.ChosenCadence);
            Assert.NotNull(resume.PlanConfig);
            Assert.Equal("growth", resume.PlanConfig.BasePlan);
            Assert.Equal(3, resume.PlanConfig.Locations);
            Assert.Equal(2, resume.PlanConfig.Packs["qr_10"]);
            Assert.Equal("0123456789abcdef0123456789abcdef", resume.PlanConfig.QuoteHash);
        }

        [Fact]
        public async Task GetBySession_StandardPlan_ReturnsPlanAndCadence_NoPlanConfig()
        {
            var sessionToken = await SeedVerifiedPendingAsync("resume-starter@example.com");
            var row = await Row("resume-starter@example.com");
            row.ChosenPlan = BillingSubscriptionPlans.Starter;
            row.ChosenCadence = "annual";
            row.CustomPlanConfigJson = null;
            await _db.SaveChangesAsync();

            var resume = await _sut.GetBySessionAsync(sessionToken);

            Assert.Equal(BillingSubscriptionPlans.Starter, resume.ChosenPlan);
            Assert.Equal("annual", resume.ChosenCadence);
            Assert.Null(resume.PlanConfig);
        }

        [Fact]
        public async Task GetBySession_BeforeOnboarding_ReturnsNoSavedPlan()
        {
            var sessionToken = await SeedVerifiedPendingAsync("resume-none@example.com");

            var resume = await _sut.GetBySessionAsync(sessionToken);

            Assert.Null(resume.ChosenPlan);
            Assert.Null(resume.ChosenCadence);
            Assert.Null(resume.PlanConfig);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        private Task<SignupCodeSentResponse> StartFor(string email) =>
            _sut.StartAsync(new StartSignupDto { Email = email, TermsAccepted = true });

        private Task<SignupSessionResponse> VerifyWith(string email, string code) =>
            _sut.VerifyOtpAsync(new VerifySignupOtpDto { Email = email, OtpCode = code });

        private string LastCode() => _emailService.SentOtpEmails[^1].Otp;

        private Task<PendingSignup> Row(string email) =>
            _db.PendingSignups.SingleAsync(x => x.Email == email);

        private static string WrongCode(string code) => code == "000000" ? "111111" : "000000";

        private async Task SpendCodeAsync(string email, string code)
        {
            for (var i = 0; i < 4; i++)
            {
                var miss = await Assert.ThrowsAsync<SignupFlowException>(() =>
                    VerifyWith(email, WrongCode(code))
                );
                Assert.Equal("otp_invalid", miss.Code);
            }

            var spent = await Assert.ThrowsAsync<SignupFlowException>(() =>
                VerifyWith(email, WrongCode(code))
            );
            Assert.Equal("otp_attempts_exceeded", spent.Code);
        }

        private async Task<Guid> SeedVerifiedPendingAsync(string email)
        {
            var sessionToken = Guid.NewGuid();
            _db.PendingSignups.Add(
                new PendingSignup
                {
                    Id = Guid.NewGuid(),
                    SessionToken = sessionToken,
                    SessionTokenIssuedAtUtc = DateTime.UtcNow,
                    Email = email,
                    Status = PendingSignupStatuses.Verified,
                    OtpResendCount = 0,
                    TermsAccepted = true,
                    EmailVerifiedAt = DateTime.UtcNow.AddMinutes(-5),
                    CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
                    UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                }
            );
            await _db.SaveChangesAsync();
            return sessionToken;
        }

        private sealed class TrackingEmailService : EmailServiceStubBase
        {
            public List<(string Email, string Otp)> SentOtpEmails { get; } = [];

            public bool FailNextSend { get; set; }

            public override Task SendOtpEmailAsync(
                string toEmail,
                string otp,
                string? restaurantName = null
            )
            {
                if (FailNextSend)
                {
                    return Task.FromException(
                        new InvalidOperationException("Mail provider unavailable.")
                    );
                }

                SentOtpEmails.Add((toEmail, otp));
                return Task.CompletedTask;
            }
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private DateTimeOffset _now;

            public FakeTimeProvider(DateTimeOffset start)
            {
                _now = start;
            }

            public override DateTimeOffset GetUtcNow() => _now;

            public void Advance(TimeSpan by)
            {
                _now = _now.Add(by);
            }
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<string> Entries { get; } = [];

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
                Entries.Add(formatter(state, exception));
                if (state is IEnumerable<KeyValuePair<string, object?>> properties)
                {
                    foreach (var property in properties)
                    {
                        Entries.Add($"{property.Key}={property.Value}");
                    }
                }
                if (exception is not null)
                {
                    Entries.Add(exception.Message);
                }
            }
        }
    }
}
