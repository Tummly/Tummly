using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers.EmailTemplates;

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
         TRIAL REQUEST RECEIVED EMAIL
         =========================================
        */

        Task SendTrialRequestReceivedEmailAsync(
            string toEmail,
            string fullName,
            string businessName
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

        Task SendHelpCentreSupportReplyEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            string replyBody,
            string? myQueriesUrl
        );

        Task SendHelpCentreResolvedEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
            string? myQueriesUrl
        );

        Task SendHelpCentreEscalationEmailAsync(
            string toEmail,
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string threadSummary,
            string? escalationNote,
            string supportDashboardUrl
        );

        Task SendHelpCentreOperatorReplyEmailAsync(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string replyBody,
            string supportDashboardUrl
        );

        Task SendHelpCentreNewQueryEmailAsync(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string messagePreview,
            int attachmentCount,
            string supportDashboardUrl
        );

        /*
         =========================================
         GUEST RESPONSE EMAIL (venue-branded)
         =========================================
        */

        Task SendGuestResponseEmailAsync(
            string toEmail,
            string subject,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string message,
            string? brandLogoUrl = null,
            GuestResponseEmailOfferBlock? offer = null
        );

        Task SendTeamInvitationEmailAsync(
            string toEmail,
            string subject,
            string acceptUrl,
            string firstName,
            string inviterName,
            string workspaceName,
            string roleName,
            string locationScope,
            string? invitationMessage
        );

        Task SendBillingAccountNoticeEmailAsync(
            string toEmail,
            string firstName,
            string title,
            string body,
            string? ctaLabel,
            string? ctaHref
        );
    }
}
