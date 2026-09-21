using TummlyBackend.DTOs.Signup;

namespace TummlyBackend.Interfaces
{
    public interface ISignupService
    {
        Task<SignupSessionResponse> StartAsync(StartSignupDto dto);

        Task<SignupSessionResponse> VerifyOtpAsync(VerifySignupOtpDto dto);

        Task ResendOtpAsync(string email);

        Task<SignupSessionResponse> SaveOnboardingAsync(
            Guid sessionToken,
            SaveSignupOnboardingDto dto
        );

        Task<SignupResumeResponse> GetBySessionAsync(Guid sessionToken);

        /// <summary>
        /// Re-runs Pilot provision for stalled onboarding / mid-provision sessions.
        /// </summary>
        Task RetryProvisionAsync(Guid sessionToken);

        Task<SignupProvisioningStatusResponse> GetProvisioningStatusAsync(
            Guid sessionToken
        );
    }
}
