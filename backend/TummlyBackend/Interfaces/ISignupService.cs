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

        /// <summary>
        /// One-time post-payment session issue (contract §6.5).
        /// Throws <see cref="SignupConflictException"/> for 409 cases.
        /// </summary>
        Task<object> SessionLoginAsync(Guid sessionToken);
    }

    public sealed class SignupConflictException : Exception
    {
        public string Code { get; }

        public SignupConflictException(string code, string message)
            : base(message)
        {
            Code = code;
        }
    }
}
