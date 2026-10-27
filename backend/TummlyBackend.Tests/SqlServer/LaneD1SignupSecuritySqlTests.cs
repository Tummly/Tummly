using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TummlyBackend.Configurations;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Signup;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Migrations;
using TummlyBackend.Models;
using TummlyBackend.Services;
using TummlyBackend.Tests.Helpers;

namespace TummlyBackend.Tests.SqlServer
{
    [Collection(SqlServerCollection.Name)]
    public sealed class LaneD1SignupSecuritySqlTests
    {
        private const int CodeModulus = 1_000_000;

        private readonly SqlServerFixture _fixture;

        public LaneD1SignupSecuritySqlTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        [SkippableFact]
        public async Task Migration_LegacyVerifiedRow_TokenRotated_IssuedNull()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var now = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
            var oldToken = Guid.NewGuid();

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            var row = SignupRow(PendingSignupStatuses.Verified, oldToken, now);
            context.PendingSignups.Add(row);
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                LaneD1SignupSecurity.LegacyTokenInvalidationSql
            );

            context.ChangeTracker.Clear();
            var after = await context
                .PendingSignups.AsNoTracking()
                .SingleAsync(r => r.Id == row.Id);

            Assert.NotEqual(oldToken, after.SessionToken);
            Assert.False(await context.PendingSignups.AnyAsync(r => r.SessionToken == oldToken));
            Assert.Null(after.SessionTokenIssuedAtUtc);

            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Migration_CompleteRow_Unchanged()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var now = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
            var token = Guid.NewGuid();

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            var row = SignupRow(PendingSignupStatuses.Complete, token, now);
            context.PendingSignups.Add(row);
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                LaneD1SignupSecurity.LegacyTokenInvalidationSql
            );

            context.ChangeTracker.Clear();
            var after = await context
                .PendingSignups.AsNoTracking()
                .SingleAsync(r => r.Id == row.Id);

            Assert.Equal(token, after.SessionToken);
            Assert.Equal(now, after.SessionTokenIssuedAtUtc);

            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task Migration_OpenCodes_MarkedUsed()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var now = new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);
            var email = $"lane-d1-open-{Guid.NewGuid():N}@example.com";

            await using var context = _fixture.CreateContext();
            await using var tx = await context.Database.BeginTransactionAsync();

            var open = OtpRow(email, isUsed: false, now);
            var used = OtpRow(email, isUsed: true, now);
            context.OtpVerifications.AddRange(open, used);
            await context.SaveChangesAsync();

            await context.Database.ExecuteSqlRawAsync(
                LaneD1SignupSecurity.LegacyTokenInvalidationSql
            );

            context.ChangeTracker.Clear();
            var rows = await context
                .OtpVerifications.AsNoTracking()
                .Where(r => r.Email == email)
                .ToDictionaryAsync(r => r.Id);

            Assert.True(rows[open.Id].IsUsed);
            Assert.True(rows[used.Id].IsUsed);

            await tx.RollbackAsync();
        }

        [SkippableFact]
        public async Task ConcurrentVerify_OneSuccess()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var email = $"lane-d1-race-{Guid.NewGuid():N}@example.com";
            var subject = Guid.NewGuid().ToString("N");

            string code;
            await using (var issuer = _fixture.CreateContext())
            {
                var service = OtpTestHelpers.CreateChallengeService(issuer);
                var issued = await service.IssueAsync(
                    new OtpIssueRequest(email, OtpPurposes.Signup, subject, 1)
                );
                code = issued.Code;
            }

            var results = await Task.WhenAll(
                Enumerable
                    .Range(0, 2)
                    .Select(_ => VerifyOnFreshContextAsync(email, subject, 1, code))
            );

            Assert.Equal(
                1,
                results.Count(r => r.Outcome == OtpVerifyOutcome.Verified)
            );
        }

        [SkippableFact]
        public async Task ParallelGuessBurst_AtMostFiveComparisons_NeverVerifiedAfterBudget()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var email = $"lane-d1-burst-{Guid.NewGuid():N}@example.com";
            var subject = Guid.NewGuid().ToString("N");

            string code;
            await using (var issuer = _fixture.CreateContext())
            {
                var service = OtpTestHelpers.CreateChallengeService(issuer);
                var issued = await service.IssueAsync(
                    new OtpIssueRequest(email, OtpPurposes.Signup, subject, 0)
                );
                code = issued.Code;
            }

            // Nineteen wrong guesses and the right code at position 10. Each wrong
            // guess is a distinct value because the offsets are 1..20 (never 0 mod 10^6).
            var guesses = Enumerable
                .Range(0, 20)
                .Select(i => i == 10 ? code : WrongGuess(code, i + 1))
                .ToArray();

            var results = await Task.WhenAll(
                guesses.Select(guess => VerifyOnFreshContextAsync(email, subject, 0, guess))
            );

            await using var check = _fixture.CreateContext();
            var row = await check
                .OtpVerifications.AsNoTracking()
                .SingleAsync(r =>
                    r.Email == email && r.Purpose == OtpPurposes.Signup && r.SubjectId == subject
                );

            var verified = results.Count(r => r.Outcome == OtpVerifyOutcome.Verified);

            // Every comparison holds one reserved attempt, so the counter bounds them all.
            Assert.True(row.FailedAttempts <= OtpChallengeService.MaxWrongAttempts);
            Assert.True(verified <= 1);

            // A Verified outcome is only possible for a call that held one of the reserved
            // attempts. Such a call also set IsUsed, so the row cannot be verified twice.
            if (verified == 1)
            {
                Assert.True(row.IsUsed);
            }
        }

        [SkippableFact]
        public async Task ParallelStartResend_SendsAtMostOneCodePerCooldown()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var email = $"lane-d1-send-{Guid.NewGuid():N}@example.com";
            var now = DateTime.UtcNow;
            Guid pendingId;
            await using (var seed = _fixture.CreateContext())
            {
                // The cooldown of the first code has passed: exactly one new send may win.
                var row = SignupRow(PendingSignupStatuses.EmailPending, Guid.NewGuid(), now);
                row.Email = email;
                row.TermsAccepted = true;
                row.CodeGeneration = 1;
                row.CodesSentInWindow = 1;
                row.AbuseWindowStartUtc = now.AddMinutes(-5);
                row.LastOtpSentAt = now.AddMinutes(-2);
                seed.PendingSignups.Add(row);
                await seed.SaveChangesAsync();
                pendingId = row.Id;
            }

            var mail = new ConcurrentMail();
            await Task.WhenAll(
                Enumerable
                    .Range(0, 20)
                    .Select(i => RunOnFreshServiceAsync(
                        mail,
                        service => i % 2 == 0
                            ? service.StartAsync(new StartSignupDto { Email = email, TermsAccepted = true })
                            : service.ResendOtpAsync(email)
                    ))
            );

            Assert.Single(mail.Sent);

            await using var check = _fixture.CreateContext();
            var after = await check.PendingSignups.AsNoTracking().SingleAsync(r => r.Id == pendingId);
            Assert.Equal(2, after.CodesSentInWindow);
            Assert.Equal(2, after.CodeGeneration);

            // One live code of the current generation, never several with the same generation.
            var live = await check.OtpVerifications
                .AsNoTracking()
                .Where(r => r.Email == email && r.Purpose == OtpPurposes.Signup && !r.IsUsed)
                .ToListAsync();
            var current = Assert.Single(live);
            Assert.Equal(2, current.Generation);
        }

        [SkippableFact]
        public async Task ParallelFirstStarts_CreateOneRow_SendOneCode()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var email = $"lane-d1-first-{Guid.NewGuid():N}@example.com";
            var mail = new ConcurrentMail();

            await Task.WhenAll(
                Enumerable
                    .Range(0, 10)
                    .Select(_ => RunOnFreshServiceAsync(
                        mail,
                        service => service.StartAsync(new StartSignupDto { Email = email, TermsAccepted = true })
                    ))
            );

            Assert.Single(mail.Sent);
            await using var check = _fixture.CreateContext();
            var row = await check.PendingSignups.AsNoTracking().SingleAsync(r => r.Email == email);
            Assert.Equal(1, row.CodesSentInWindow);
            Assert.Equal(1, row.CodeGeneration);
        }

        [SkippableFact]
        public async Task ParallelWrongVerifies_CountAll()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var (email, pendingId, code) = await StartedSignupAsync();

            var outcomes = await Task.WhenAll(
                Enumerable
                    .Range(0, 12)
                    .Select(i => VerifyThroughServiceAsync(email, WrongGuess(code, i + 1)))
            );

            // No increment is lost: every wrong guess moved the window counter by one.
            await using var check = _fixture.CreateContext();
            var row = await check.PendingSignups.AsNoTracking().SingleAsync(r => r.Id == pendingId);
            Assert.Equal(12, row.FailedVerifiesInWindow);
            Assert.DoesNotContain("challenge_locked", outcomes);
            Assert.DoesNotContain("verified", outcomes);
        }

        [SkippableFact]
        public async Task ParallelWrongVerifies_StopAtWindowLimit()
        {
            Skip.IfNot(_fixture.IsAvailable, _fixture.SkipReason);

            var (email, pendingId, code) = await StartedSignupAsync();

            var outcomes = await Task.WhenAll(
                Enumerable
                    .Range(0, 20)
                    .Select(i => VerifyThroughServiceAsync(email, WrongGuess(code, i + 1)))
            );

            await using var check = _fixture.CreateContext();
            var row = await check.PendingSignups.AsNoTracking().SingleAsync(r => r.Id == pendingId);
            Assert.Equal(SignupAbuseLimits.MaxFailedVerifiesPerWindow, row.FailedVerifiesInWindow);
            Assert.Equal(5, outcomes.Count(o => o == "challenge_locked"));
        }

        private async Task<(string Email, Guid PendingId, string Code)> StartedSignupAsync()
        {
            var email = $"lane-d1-wrong-{Guid.NewGuid():N}@example.com";
            var mail = new ConcurrentMail();
            await RunOnFreshServiceAsync(
                mail,
                service => service.StartAsync(new StartSignupDto { Email = email, TermsAccepted = true })
            );
            var sent = Assert.Single(mail.Sent);

            await using var context = _fixture.CreateContext();
            var id = (await context.PendingSignups.AsNoTracking().SingleAsync(r => r.Email == email)).Id;
            return (email, id, sent.Otp);
        }

        private async Task<string> VerifyThroughServiceAsync(string email, string code)
        {
            await using var context = _fixture.CreateContext();
            var service = CreateSignupService(context, new ConcurrentMail());
            try
            {
                await service.VerifyOtpAsync(
                    new VerifySignupOtpDto { Email = email, OtpCode = code }
                );
                return "verified";
            }
            catch (SignupFlowException ex)
            {
                return ex.Code;
            }
        }

        private async Task RunOnFreshServiceAsync(
            ConcurrentMail mail,
            Func<SignupService, Task> action
        )
        {
            await using var context = _fixture.CreateContext();
            var service = CreateSignupService(context, mail);
            try
            {
                await action(service);
            }
            catch (SignupFlowException)
            {
                // A refused call is an outcome the assertions look at, not a test error.
            }
        }

        private static SignupService CreateSignupService(
            ApplicationDbContext context,
            ConcurrentMail mail
        ) =>
            new(
                context,
                mail,
                new NoOpProvisioningService(),
                new CustomPlanQuoteService(context, Options.Create(new TummlySellerVatSettings())),
                new StubJwtService(),
                OtpTestHelpers.CreateChallengeService(context),
                TimeProvider.System,
                NullLogger<SignupService>.Instance
            );

        private sealed class ConcurrentMail : EmailServiceStubBase
        {
            public ConcurrentBag<(string Email, string Otp)> Sent { get; } = [];

            public override Task SendOtpEmailAsync(
                string toEmail,
                string otp,
                string? restaurantName = null
            )
            {
                Sent.Add((toEmail, otp));
                return Task.CompletedTask;
            }
        }

        private async Task<OtpVerifyResult> VerifyOnFreshContextAsync(
            string email,
            string subject,
            int generation,
            string code
        )
        {
            await using var context = _fixture.CreateContext();
            var service = OtpTestHelpers.CreateChallengeService(context);
            return await service.VerifyAsync(
                new OtpVerifyRequest(email, OtpPurposes.Signup, subject, generation, code)
            );
        }

        private static string WrongGuess(string code, int offset)
        {
            var value = (int.Parse(code) + offset) % CodeModulus;
            return value.ToString("D6");
        }

        private static PendingSignup SignupRow(string status, Guid token, DateTime now)
        {
            return new PendingSignup
            {
                SessionToken = token,
                Email = $"lane-d1-{Guid.NewGuid():N}@example.com",
                Status = status,
                SessionTokenIssuedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
        }

        private static OtpVerification OtpRow(string email, bool isUsed, DateTime now)
        {
            return new OtpVerification
            {
                Email = email,
                OtpCode = string.Empty,
                Channel = OtpVerification.ChannelEmail,
                IsUsed = isUsed,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                Purpose = OtpPurposes.Signup,
                SubjectId = Guid.NewGuid().ToString("N"),
                Generation = 0,
                CodeHash = new string('a', 64),
                FailedAttempts = 0,
            };
        }
    }
}
