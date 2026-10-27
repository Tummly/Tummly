using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Purpose-bound, hashed, single-use one-time codes. A code is stored only as
    /// an HMAC and is never verifiable under another purpose or subject.
    /// </summary>
    public sealed class OtpChallengeService : IOtpChallengeService
    {
        public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

        public const int MaxWrongAttempts = 5;

        private readonly ApplicationDbContext _context;
        private readonly IOtpCodeHasher _hasher;
        private readonly TimeProvider _timeProvider;

        public OtpChallengeService(
            ApplicationDbContext context,
            IOtpCodeHasher hasher,
            TimeProvider timeProvider
        )
        {
            _context = context;
            _hasher = hasher;
            _timeProvider = timeProvider;
        }

        public async Task<OtpIssueResult> IssueAsync(
            OtpIssueRequest request,
            CancellationToken ct = default
        )
        {
            ArgumentNullException.ThrowIfNull(request);

            var email = NormalizeEmail(request.Email);
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var code = GenerateOtp.CreateOtp();

            // Only unused codes for the same purpose are replaced. Other purposes keep theirs.
            var stale = await _context.OtpVerifications
                .Where(row =>
                    row.Email == email
                    && row.Purpose == request.Purpose
                    && !row.IsUsed
                )
                .ToListAsync(ct);
            _context.OtpVerifications.RemoveRange(stale);

            var row = new OtpVerification
            {
                UserId = request.UserId,
                Email = email,
                OtpCode = string.Empty,
                Channel = request.Channel,
                IsUsed = false,
                ExpiresAt = now.Add(CodeLifetime),
                CreatedAt = now,
                Purpose = request.Purpose,
                SubjectId = request.SubjectId,
                Generation = request.Generation,
                CodeHash = _hasher.HashCode(
                    request.Purpose,
                    request.SubjectId,
                    request.Generation,
                    code
                ),
                FailedAttempts = 0,
            };

            _context.OtpVerifications.Add(row);
            await _context.SaveChangesAsync(ct);

            return new OtpIssueResult(row.Id, code);
        }

        public async Task<OtpVerifyResult> VerifyAsync(
            OtpVerifyRequest request,
            CancellationToken ct = default
        )
        {
            ArgumentNullException.ThrowIfNull(request);

            var email = NormalizeEmail(request.Email);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var row = await _context.OtpVerifications
                .Where(candidate =>
                    candidate.Email == email
                    && candidate.Purpose == request.Purpose
                    && candidate.SubjectId == request.SubjectId
                    && !candidate.IsUsed
                    && candidate.FailedAttempts < MaxWrongAttempts
                )
                .OrderByDescending(candidate => candidate.CreatedAt)
                .ThenByDescending(candidate => candidate.Id)
                .FirstOrDefaultAsync(ct);

            if (row is null || row.CodeHash is null)
            {
                return Invalid();
            }

            if (row.Generation != request.Generation)
            {
                return Invalid();
            }

            if (row.ExpiresAt <= now)
            {
                return new OtpVerifyResult(OtpVerifyOutcome.Expired, null);
            }

            // Reserve one attempt before any comparison. A burst of parallel guesses
            // can then compare at most MaxWrongAttempts times for this code.
            if (!await ReserveAttemptAsync(row.Id, ct))
            {
                return Invalid();
            }

            var submitted = request.Code?.Trim() ?? string.Empty;
            var expected = _hasher.HashCode(
                request.Purpose,
                request.SubjectId,
                request.Generation,
                submitted
            );

            if (
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(row.CodeHash)
                )
            )
            {
                var locked = await LockIfBudgetSpentAsync(row.Id, ct);
                return locked
                    ? new OtpVerifyResult(OtpVerifyOutcome.AttemptsExceeded, null)
                    : Invalid();
            }

            var claimed = await MarkUsedAsync(row.Id, ct);
            if (claimed == 0)
            {
                return Invalid();
            }

            return new OtpVerifyResult(OtpVerifyOutcome.Verified, row.Id);
        }

        public async Task InvalidateAsync(int otpVerificationId, CancellationToken ct = default)
        {
            await MarkUsedAsync(otpVerificationId, ct);
        }

        /// <summary>
        /// Counts one guess against the budget before it is compared. Succeeds only
        /// while the row is unused and under <see cref="MaxWrongAttempts"/>. On a
        /// relational provider the check and the increment are one conditional UPDATE.
        /// </summary>
        private async Task<bool> ReserveAttemptAsync(int otpVerificationId, CancellationToken ct)
        {
            if (_context.Database.IsRelational())
            {
                var reserved = await _context.OtpVerifications
                    .Where(candidate =>
                        candidate.Id == otpVerificationId
                        && !candidate.IsUsed
                        && candidate.FailedAttempts < MaxWrongAttempts
                    )
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            candidate => candidate.FailedAttempts,
                            candidate => candidate.FailedAttempts + 1
                        ),
                        ct
                    );
                return reserved == 1;
            }

            var row = await _context.OtpVerifications.FirstOrDefaultAsync(
                candidate =>
                    candidate.Id == otpVerificationId
                    && !candidate.IsUsed
                    && candidate.FailedAttempts < MaxWrongAttempts,
                ct
            );
            if (row is null)
            {
                return false;
            }

            row.FailedAttempts += 1;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        /// <summary>
        /// Called after a wrong guess that reserved an attempt. Locks the row when the
        /// budget is spent and returns true in that case.
        /// </summary>
        private async Task<bool> LockIfBudgetSpentAsync(int otpVerificationId, CancellationToken ct)
        {
            var attempts = await _context.OtpVerifications
                .AsNoTracking()
                .Where(candidate => candidate.Id == otpVerificationId)
                .Select(candidate => candidate.FailedAttempts)
                .FirstOrDefaultAsync(ct);

            if (attempts < MaxWrongAttempts)
            {
                return false;
            }

            await MarkUsedAsync(otpVerificationId, ct);
            return true;
        }

        /// <summary>
        /// Sets <c>IsUsed</c> only while it is still false. Returns the rows changed.
        /// On a relational provider this is one conditional UPDATE, so only one
        /// caller can claim a code.
        /// </summary>
        private async Task<int> MarkUsedAsync(int otpVerificationId, CancellationToken ct)
        {
            if (_context.Database.IsRelational())
            {
                return await _context.OtpVerifications
                    .Where(candidate => candidate.Id == otpVerificationId && !candidate.IsUsed)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(candidate => candidate.IsUsed, true),
                        ct
                    );
            }

            var row = await _context.OtpVerifications.FirstOrDefaultAsync(
                candidate => candidate.Id == otpVerificationId && !candidate.IsUsed,
                ct
            );
            if (row is null)
            {
                return 0;
            }

            row.IsUsed = true;
            await _context.SaveChangesAsync(ct);
            return 1;
        }

        private static OtpVerifyResult Invalid() =>
            new(OtpVerifyOutcome.Invalid, null);

        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();
    }
}
