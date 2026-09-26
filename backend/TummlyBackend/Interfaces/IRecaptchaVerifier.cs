namespace TummlyBackend.Interfaces
{
    public enum RecaptchaVerifyStatus
    {
        Skipped,
        Passed,
        MissingToken,
        Failed,
    }

    public readonly record struct RecaptchaVerifyResult(
        RecaptchaVerifyStatus Status,
        string? Message
    );

    /// <summary>
    /// Google reCAPTCHA v3 siteverify for public Guest Form submit.
    /// </summary>
    public interface IRecaptchaVerifier
    {
        /// <summary>
        /// When SecretKey is unset, returns Skipped. When set, requires a token
        /// that passes siteverify with expected action and min score.
        /// </summary>
        Task<RecaptchaVerifyResult> VerifyGuestFeedbackAsync(
            string? token,
            string? remoteIp,
            CancellationToken cancellationToken = default
        );
    }
}
