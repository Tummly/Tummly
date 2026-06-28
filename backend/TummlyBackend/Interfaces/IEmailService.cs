using TummlyBackend.DTOs.Auth;

namespace TummlyBackend.Interfaces
{
    public interface IEmailService
    {
        /*
         =========================================
         OTP EMAIL
         =========================================
        */

        Task SendOtpEmailAsync(
            string toEmail,
            string otp
        );

        /*
         =========================================
         ACCOUNT SETUP INVITE EMAIL
         =========================================
        */

        Task SendAccountSetupEmailAsync(
            string toEmail,
            string fullName,
            string setupLink
        );

        Task SendAccountSetupReminderEmailAsync(
            string toEmail,
            string fullName,
            string setupLink,
            DateTime expiresAtUtc
        );
        /*
         =========================================
         SEND DECLINE EMAIL
         =========================================
        */

        Task SendDeclineEmailAsync(
            string toEmail,
            string fullName,
            string declineReason
        );

        /*
         =========================================
         SEND MORE INFO REQUEST EMAIL
         =========================================
        */

        Task SendMoreInfoEmailAsync(
            string toEmail,
            string fullName,
            string moreInfoMessage
        );

        Task SendResetPasswordEmailAsync(
            string toEmail,
            string resetLink
        );

        /*
         =========================================
         PASSWORD CHANGED CONFIRMATION
         =========================================
        */

        Task SendPasswordChangedEmailAsync(
            string toEmail,
            string firstName
        );

        /*
         =========================================
         NEW DEVICE SIGN-IN ALERT
         =========================================
        */

        Task SendNewDeviceSignInEmailAsync(
            string toEmail,
            NewDeviceSignInDetails details
        );
    }
}