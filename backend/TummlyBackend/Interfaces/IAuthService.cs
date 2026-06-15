using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;

namespace TummlyBackend.Interfaces
{
    public interface IAuthService
    {
        /*
         =========================================
         ADMIN AUTH
         =========================================
        */
        Task<string> AdminLoginAsync(AdminLoginDto dto);

        /*
         =========================================
         USER AUTH
         =========================================
        */
        Task<string> UserLoginAsync(UserLoginDto dto);

        Task<object> UniversalLoginAsync(UserLoginDto dto);

        /*
         =========================================
         OTP
         =========================================
        */
        Task<object> VerifyOtpAsync(VerifyOtpDto dto);

        Task SendAuthOtpAsync(string email);

        Task<SendOtpResultDto> SendAuthOtpAsync(
            string email,
            string purpose
        );

        Task<SendOtpResultDto> SendAuthOtpSmsAsync(string email);

        /*
         =========================================
         PASSWORD RECOVERY
         =========================================
        */
        Task ForgotPasswordAsync(ForgotPasswordDto dto);

        Task ResetPasswordAsync(ResetPasswordDto dto);

        /*
         =========================================
         WORKSPACE SETUP (SIGN-IN A5)
         =========================================
        */
        Task<IReadOnlyList<WorkspaceLocationDto>> GetWorkspaceLocationsAsync(
            int userId
        );

        Task SelectWorkspaceAsync(
            int userId,
            SelectWorkspaceDto dto
        );

        /*
         =========================================
         TRIAL / ACCOUNT SETUP (ONBOARDING)
         =========================================
        */
        Task<bool> CompleteAccountSetupAsync(CompleteSetupDto dto);
    }
}