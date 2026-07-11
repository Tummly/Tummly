namespace TummlyBackend.DTOs.Trial
{
    public sealed record TrialVerifyOtpResult(
        bool Verified,
        bool ConfirmationEmailSent
    );
}
