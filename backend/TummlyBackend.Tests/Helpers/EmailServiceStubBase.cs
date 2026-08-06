using TummlyBackend.DTOs.Auth;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    public class EmailServiceStubBase : IEmailService
    {
        public virtual Task SendOtpEmailAsync(string toEmail, string otp) =>
            Task.CompletedTask;

        public virtual Task SendTrialRequestReceivedEmailAsync(
            string toEmail,
            string fullName,
            string businessName
        ) => Task.CompletedTask;

        public virtual Task SendAccountSetupEmailAsync(
            string toEmail,
            string fullName,
            string setupLink
        ) => Task.CompletedTask;

        public virtual Task SendAccountSetupReminderEmailAsync(
            string toEmail,
            string fullName,
            string setupLink,
            DateTime expiresAtUtc
        ) => Task.CompletedTask;

        public virtual Task SendDeclineEmailAsync(
            string toEmail,
            string fullName,
            string declineReason
        ) => Task.CompletedTask;

        public virtual Task SendMoreInfoEmailAsync(
            string toEmail,
            string fullName,
            string moreInfoMessage
        ) => Task.CompletedTask;

        public virtual Task SendResetPasswordEmailAsync(
            string toEmail,
            string resetLink
        ) => Task.CompletedTask;

        public virtual Task SendPasswordChangedEmailAsync(
            string toEmail,
            string firstName
        ) => Task.CompletedTask;

        public virtual Task SendNewDeviceSignInEmailAsync(
            string toEmail,
            NewDeviceSignInDetails details
        ) => Task.CompletedTask;

        public virtual Task SendHelpCentreSupportReplyEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            string replyBody,
            string? myQueriesUrl
        ) => Task.CompletedTask;

        public virtual Task SendHelpCentreResolvedEmailAsync(
            string toEmail,
            string submitterName,
            string topicLabel,
            IReadOnlyList<(string AuthorLabel, string Body)> excerptMessages,
            string? myQueriesUrl
        ) => Task.CompletedTask;

        public virtual Task SendHelpCentreEscalationEmailAsync(
            string toEmail,
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string threadSummary,
            string? escalationNote,
            string supportDashboardUrl
        ) => Task.CompletedTask;

        public virtual Task SendHelpCentreOperatorReplyEmailAsync(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string replyBody,
            string supportDashboardUrl
        ) => Task.CompletedTask;

        public virtual Task SendHelpCentreNewQueryEmailAsync(
            string topicLabel,
            string submitterName,
            string submitterEmail,
            string businessName,
            string? locationLabel,
            string messagePreview,
            int attachmentCount,
            string supportDashboardUrl
        ) => Task.CompletedTask;

        public virtual Task SendGuestResponseEmailAsync(
            string toEmail,
            string subject,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string message,
            string giveFeedbackUrl,
            string? brandLogoUrl = null
        ) => Task.CompletedTask;
    }
}
