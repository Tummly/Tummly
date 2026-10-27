using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace TummlyBackend.Tests.Helpers
{
    /// <summary>
    /// Wiring for tests that use the real purpose-bound OTP service.
    /// </summary>
    public static class OtpTestHelpers
    {
        public static OtpChallengeService CreateChallengeService(
            ApplicationDbContext context,
            TimeProvider? timeProvider = null
        ) =>
            new(
                context,
                new OtpCodeHasher(OtpCodeHasher.TestingFallbackKey),
                timeProvider ?? TimeProvider.System
            );

        /// <summary>
        /// Adds a live sign-in email code with a known value. The row is written the
        /// same way <see cref="OtpChallengeService"/> writes it: hash only, purpose
        /// <c>sign_in</c>, subject = user id, generation 0.
        /// </summary>
        public static async Task SeedSignInEmailCodeAsync(
            ApplicationDbContext context,
            string email,
            int userId,
            string code
        )
        {
            var subject = userId.ToString();
            var hasher = new OtpCodeHasher(OtpCodeHasher.TestingFallbackKey);

            context.OtpVerifications.Add(
                new OtpVerification
                {
                    UserId = userId,
                    Email = email,
                    OtpCode = string.Empty,
                    Channel = OtpVerification.ChannelEmail,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                    Purpose = OtpPurposes.SignIn,
                    SubjectId = subject,
                    Generation = 0,
                    CodeHash = hasher.HashCode(OtpPurposes.SignIn, subject, 0, code),
                    FailedAttempts = 0,
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
