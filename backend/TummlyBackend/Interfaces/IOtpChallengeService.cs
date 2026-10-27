using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// HMAC-SHA256 hex hasher for one-time codes and IP addresses (lane D signup security).
    /// </summary>
    public interface IOtpCodeHasher
    {
        string HashCode(string purpose, string subjectId, int generation, string code);

        string HashIp(string ip);
    }

    public sealed record OtpIssueRequest(
        string Email,
        string Purpose,
        string SubjectId,
        int Generation,
        int? UserId = null,
        string Channel = OtpVerification.ChannelEmail
    );

    /// <summary>The plain code is returned in memory only. It is never stored or logged.</summary>
    public sealed record OtpIssueResult(int OtpVerificationId, string Code);

    public sealed record OtpVerifyRequest(
        string Email,
        string Purpose,
        string SubjectId,
        int Generation,
        string Code
    );

    public enum OtpVerifyOutcome
    {
        Verified,
        Invalid,
        Expired,
        AttemptsExceeded,
    }

    public sealed record OtpVerifyResult(OtpVerifyOutcome Outcome, int? OtpVerificationId);

    /// <summary>
    /// Purpose-bound, hashed, single-use one-time codes.
    /// </summary>
    public interface IOtpChallengeService
    {
        Task<OtpIssueResult> IssueAsync(OtpIssueRequest request, CancellationToken ct = default);

        Task<OtpVerifyResult> VerifyAsync(OtpVerifyRequest request, CancellationToken ct = default);

        Task InvalidateAsync(int otpVerificationId, CancellationToken ct = default);
    }
}
