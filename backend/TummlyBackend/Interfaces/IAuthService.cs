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

        /*
         =========================================
         PASSWORD RECOVERY
         =========================================
        */
        Task ForgotPasswordAsync(ForgotPasswordDto dto);

        Task ResetPasswordAsync(ResetPasswordDto dto);

        /*
         =========================================
         TRIAL / ACCOUNT SETUP (ONBOARDING)
         =========================================
        */
        Task<bool> CompleteAccountSetupAsync(CompleteSetupDto dto);
    }
}