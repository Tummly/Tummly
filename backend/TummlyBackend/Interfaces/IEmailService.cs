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
        /*
         =========================================
         SEND DECLINE EMAIL
         =========================================
        */

        Task SendDeclineEmailAsync(
            string toEmail,
            string fullName
        );

        /*
         =========================================
         SEND MORE INFO REQUEST EMAIL
         =========================================
        */

        Task SendMoreInfoEmailAsync(
            string toEmail,
            string fullName
        );

    }
}