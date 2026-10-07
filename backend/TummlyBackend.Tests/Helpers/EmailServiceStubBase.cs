using TummlyBackend.DTOs.Auth;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    public class EmailServiceStubBase : IEmailService
    {
        public virtual Task SendOtpEmailAsync(
            string toEmail,
            string otp,
            string? restaurantName = null
        ) => Task.CompletedTask;

        public virtual Task SendTrialRequestReceivedEmailAsync(
            string toEmail,
            string fullName,
            string businessName
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

        public virtual Task SendContactEnquiryReceivedEmailAsync(
            string toEmail,
            string topicLabel,
            string reference
        ) => Task.CompletedTask;

        public virtual Task SendGuestResponseEmailAsync(
            string toEmail,
            string subject,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string message,
            string? brandLogoUrl = null,
            GuestResponseEmailOfferBlock? offer = null,
            string? unsubscribeHref = null,
            string? ticketSubject = null
        ) => Task.CompletedTask;

        public virtual Task<string?> SendCampaignGuestEmailAsync(
            string toEmail,
            string subject,
            string brandTitle,
            string? brandSubtitle,
            string? locationAddress,
            string message,
            string? brandLogoUrl = null,
            GuestResponseEmailOfferBlock? offer = null,
            string? unsubscribeHref = null,
            string? ticketSubject = null,
            IReadOnlyDictionary<string, string>? tags = null
        ) => Task.FromResult<string?>(null);

        public virtual Task SendTeamInvitationEmailAsync(
            string toEmail,
            string subject,
            string acceptUrl,
            string firstName,
            string inviterName,
            string workspaceName,
            string roleName,
            string locationScope,
            string? invitationMessage
        ) => Task.CompletedTask;

        public virtual Task SendBillingAccountNoticeEmailAsync(
            string toEmail,
            string firstName,
            string title,
            string body,
            string? ctaLabel,
            string? ctaHref
        ) => Task.CompletedTask;

        public virtual Task SendPilotStartedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string pilotEndDateLabel
        ) => Task.CompletedTask;

        public virtual Task SendPilotEndingSoonEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            int daysRemaining,
            string pilotEndDateLabel,
            string plansUrl
        ) => Task.CompletedTask;

        public virtual Task SendPilotEndedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string plansUrl
        ) => Task.CompletedTask;

        public virtual Task SendPaymentActionRequiredEmailAsync(
            string toEmail,
            string firstName,
            string orderDescription,
            string billingUrl,
            string ctaLabel
        ) => Task.CompletedTask;

        public virtual Task SendUsageWarningEmailAsync(
            string toEmail,
            string firstName,
            string allowanceKind,
            string percentUsed,
            string usedAmount,
            string remainingAmount,
            string resetDate,
            string usageUrl
        ) => Task.CompletedTask;

        public virtual Task SendUsageExhaustedEmailAsync(
            string toEmail,
            string firstName,
            string allowanceKind,
            string ctaUrl,
            string ctaLabel
        ) => Task.CompletedTask;

        public virtual Task SendWeeklyBriefEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string periodLabel,
            int qrScans,
            int feedbackReceived,
            int guestsCaptured,
            int offerClaimed,
            int redemptions,
            int campaignEngagement,
            string whatChanged,
            string recommendedNextStep,
            string weeklyBriefUrl
        ) => Task.CompletedTask;

        public virtual Task SendTummlyVatInvoiceEmailAsync(
            string toEmail,
            string firstName,
            string documentNumber,
            string lineDescription,
            int grossPence,
            DateTime paymentSuccessUtc,
            string billingUrl,
            byte[] pdfContent,
            string pdfFileName
        ) => Task.CompletedTask;

        public virtual Task SendShopOrderConfirmedEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string orderNumber,
            string materialsLinesHtml,
            string deliveryAddressHtml,
            string orderUrl
        ) => Task.CompletedTask;

        public virtual Task SendShopOrderDispatchedEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string orderNumber,
            string deliveryEstimate,
            string trackingDetails,
            string orderUrl
        ) => Task.CompletedTask;
    }
}
