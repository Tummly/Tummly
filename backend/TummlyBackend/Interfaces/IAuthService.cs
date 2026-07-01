using TummlyBackend.DTOs.Auth;

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
        Task<object> VerifyOtpAsync(
            VerifyOtpDto dto,
            SignInContext? signInContext = null
        );

        Task<SendOtpResultDto> SendAuthOtpAsync(string email, string purpose);

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
         ACCOUNT ACTIVATION
         =========================================
         */
        Task<SessionRoutingFields> GetCurrentUserRoutingAsync(int userId);

        Task<SessionRoutingFields> ActivateAccountAsync(
            int userId,
            string activationCode
        );
    }
}