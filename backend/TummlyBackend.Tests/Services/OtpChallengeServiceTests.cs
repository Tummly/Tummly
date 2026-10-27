using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Services
{
    /// <summary>
    /// Seam: <see cref="OtpChallengeService"/> — purpose-bound, hashed, single-use codes.
    /// </summary>
    public class OtpChallengeServiceTests : IDisposable
    {
        private const string Email = "owner@example.com";
        private const string Subject = "subject-a";
        private const string HashKey = "test-otp-hash-key";
        private const int MaxWrongAttempts = OtpChallengeService.MaxWrongAttempts;

        private readonly ApplicationDbContext _db;
        private readonly FakeTimeProvider _time;
        private readonly OtpCodeHasher _hasher;
        private readonly OtpChallengeService _sut;

        public OtpChallengeServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new ApplicationDbContext(options);
            _time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero));
            _hasher = new OtpCodeHasher(HashKey);
            _sut = new OtpChallengeService(_db, _hasher, _time);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        [Fact]
        public async Task Issue_CodeIsSixDigitsWithLeadingZeros()
        {
            for (var i = 0; i < 200; i++)
            {
                var result = await _sut.IssueAsync(
                    new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
                );

                Assert.Matches("^[0-9]{6}$", result.Code);
            }
        }

        [Fact]
        public async Task Issue_StoresHashNotCode()
        {
            var result = await _sut.IssueAsync(
                new OtpIssueRequest(" Owner@Example.com ", OtpPurposes.Signup, Subject, 3)
            );

            var row = await _db.OtpVerifications.SingleAsync();
            Assert.Equal(result.OtpVerificationId, row.Id);
            Assert.Equal(string.Empty, row.OtpCode);
            Assert.Equal(Email, row.Email);
            Assert.Equal(OtpPurposes.Signup, row.Purpose);
            Assert.Equal(Subject, row.SubjectId);
            Assert.Equal(3, row.Generation);
            Assert.Equal(0, row.FailedAttempts);
            Assert.False(row.IsUsed);
            Assert.Equal(
                ExpectedHmac(OtpPurposes.Signup, Subject, 3, result.Code),
                row.CodeHash
            );
            Assert.Equal(_time.GetUtcNow().UtcDateTime.AddMinutes(10), row.ExpiresAt);
        }

        [Fact]
        public async Task Verify_RightCode_Verified()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, issued.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Verified, result.Outcome);
            Assert.Equal(issued.OtpVerificationId, result.OtpVerificationId);
            Assert.True((await _db.OtpVerifications.SingleAsync()).IsUsed);
        }

        [Fact]
        public async Task Verify_LeadingZeroCode_Verified()
        {
            await SeedRowAsync(OtpPurposes.Signup, Subject, 1, "012345");

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, "012345")
            );

            Assert.Equal(OtpVerifyOutcome.Verified, result.Outcome);
        }

        [Fact]
        public async Task Verify_WrongPurpose_Invalid()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.SignIn, Subject, 1, issued.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public async Task Verify_WrongSubject_Invalid()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, "subject-b", 1, issued.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public async Task Verify_OlderGeneration_Invalid()
        {
            var first = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );
            await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 2)
            );

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, first.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public async Task Verify_Expired_Expired()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );
            _time.Advance(TimeSpan.FromMinutes(11));

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, issued.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Expired, result.Outcome);
        }

        [Fact]
        public async Task Verify_FiveWrong_AttemptsExceeded_ThenRightCodeInvalid()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );
            var wrong = WrongCodeFor(issued.Code);

            for (var i = 1; i <= 4; i++)
            {
                var miss = await _sut.VerifyAsync(
                    new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, wrong)
                );
                Assert.Equal(OtpVerifyOutcome.Invalid, miss.Outcome);
            }

            var fifth = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, wrong)
            );
            Assert.Equal(OtpVerifyOutcome.AttemptsExceeded, fifth.Outcome);

            var afterLock = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, issued.Code)
            );
            Assert.Equal(OtpVerifyOutcome.Invalid, afterLock.Outcome);
        }

        [Fact]
        public async Task Verify_FifthAttemptRight_Verified()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );
            var wrong = WrongCodeFor(issued.Code);

            for (var i = 1; i <= 4; i++)
            {
                await _sut.VerifyAsync(
                    new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, wrong)
                );
            }

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, issued.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Verified, result.Outcome);
        }

        [Fact]
        public async Task Verify_BudgetSpentButUnused_RightCodeInvalid()
        {
            await SeedRowAsync(OtpPurposes.Signup, Subject, 1, "123456", failedAttempts: MaxWrongAttempts);

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, "123456")
            );

            Assert.Equal(OtpVerifyOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public async Task Verify_SameCodeTwice_SecondInvalid()
        {
            var issued = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );
            var request = new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, issued.Code);

            var first = await _sut.VerifyAsync(request);
            var second = await _sut.VerifyAsync(request);

            Assert.Equal(OtpVerifyOutcome.Verified, first.Outcome);
            Assert.Equal(OtpVerifyOutcome.Invalid, second.Outcome);
        }

        [Fact]
        public async Task Issue_SignInDoesNotRemoveSignupCode()
        {
            var signup = await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.Signup, Subject, 1)
            );
            await _sut.IssueAsync(
                new OtpIssueRequest(Email, OtpPurposes.SignIn, Subject, 1)
            );

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, Subject, 1, signup.Code)
            );

            Assert.Equal(OtpVerifyOutcome.Verified, result.Outcome);
        }

        [Fact]
        public async Task Verify_NullPurposeLegacyRow_Invalid()
        {
            _db.OtpVerifications.Add(
                new OtpVerification
                {
                    Email = Email,
                    OtpCode = "123456",
                    IsUsed = false,
                    ExpiresAt = _time.GetUtcNow().UtcDateTime.AddMinutes(10),
                    CreatedAt = _time.GetUtcNow().UtcDateTime,
                    Purpose = null,
                    SubjectId = null,
                    Generation = 1,
                    CodeHash = ExpectedHmac(OtpPurposes.Signup, string.Empty, 1, "123456"),
                }
            );
            await _db.SaveChangesAsync();

            var result = await _sut.VerifyAsync(
                new OtpVerifyRequest(Email, OtpPurposes.Signup, string.Empty, 1, "123456")
            );

            Assert.Equal(OtpVerifyOutcome.Invalid, result.Outcome);
        }

        [Fact]
        public void Hasher_FromConfiguration_MissingKeyInProduction_Throws()
        {
            var config = new ConfigurationBuilder().Build();
            var env = new TestHostEnvironment("Production");

            var ex = Assert.Throws<InvalidOperationException>(() =>
                OtpCodeHasher.FromConfiguration(config, env)
            );
            Assert.Equal("Otp:HashKey is required.", ex.Message);
        }

        private async Task SeedRowAsync(
            string purpose,
            string subjectId,
            int generation,
            string code,
            int failedAttempts = 0
        )
        {
            _db.OtpVerifications.Add(
                new OtpVerification
                {
                    Email = Email,
                    OtpCode = string.Empty,
                    IsUsed = false,
                    ExpiresAt = _time.GetUtcNow().UtcDateTime.AddMinutes(10),
                    CreatedAt = _time.GetUtcNow().UtcDateTime,
                    Purpose = purpose,
                    SubjectId = subjectId,
                    Generation = generation,
                    CodeHash = ExpectedHmac(purpose, subjectId, generation, code),
                    FailedAttempts = failedAttempts,
                }
            );
            await _db.SaveChangesAsync();
        }

        private static string ExpectedHmac(string purpose, string subjectId, int generation, string code)
        {
            var key = Encoding.UTF8.GetBytes(HashKey);
            var data = Encoding.UTF8.GetBytes($"{purpose}|{subjectId}|{generation}|{code}");
            return Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();
        }

        private static string WrongCodeFor(string code) => code == "000000" ? "000001" : "000000";

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

        private sealed class TestHostEnvironment : IHostEnvironment
        {
            public TestHostEnvironment(string environmentName)
            {
                EnvironmentName = environmentName;
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "Tests";

            public string ContentRootPath { get; set; } = string.Empty;

            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
