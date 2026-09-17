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

        Task<ChoosePlanResult> ChoosePlanAsync(
            Guid sessionToken,
            string planId,
            string cadence
        );

        Task<SignupProvisioningStatusResponse> GetProvisioningStatusAsync(
            Guid sessionToken
        );
    }
}
