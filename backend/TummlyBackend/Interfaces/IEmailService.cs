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
            string otp,
            string? restaurantName = null
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

        Task SendContactEnquiryReceivedEmailAsync(
            string toEmail,
            string topicLabel,
            string reference
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
            GuestResponseEmailOfferBlock? offer = null,
            string? unsubscribeHref = null,
            string? ticketSubject = null
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

        Task SendPilotStartedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string pilotEndDateLabel
        );

        Task SendPilotEndingSoonEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            int daysRemaining,
            string pilotEndDateLabel,
            string plansUrl
        );

        Task SendPilotEndedEmailAsync(
            string toEmail,
            string firstName,
            string restaurantName,
            string plansUrl
        );

        Task SendPaymentActionRequiredEmailAsync(
            string toEmail,
            string firstName,
            string orderDescription,
            string billingUrl,
            string ctaLabel
        );

        Task SendUsageWarningEmailAsync(
            string toEmail,
            string firstName,
            string allowanceKind,
            string percentUsed,
            string usedAmount,
            string remainingAmount,
            string resetDate,
            string usageUrl
        );

        Task SendUsageExhaustedEmailAsync(
            string toEmail,
            string firstName,
            string allowanceKind,
            string ctaUrl,
            string ctaLabel
        );

        /*
         =========================================
         WEEKLY BRIEF READY (operator)
         =========================================
        */

        Task SendWeeklyBriefEmailAsync(
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
        );

        /*
         =========================================
         PAYMENT CONFIRMED + VAT INVOICE PDF
         =========================================
        */

        Task SendTummlyVatInvoiceEmailAsync(
            string toEmail,
            string firstName,
            string documentNumber,
            string lineDescription,
            int grossPence,
            DateTime paymentSuccessUtc,
            string billingUrl,
            byte[] pdfContent,
            string pdfFileName
        );

        /*
         =========================================
         SHOP ORDER CONFIRMED / DISPATCHED
         =========================================
        */

        Task SendShopOrderConfirmedEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string orderNumber,
            string materialsLinesHtml,
            string deliveryAddressHtml,
            string orderUrl
        );

        Task SendShopOrderDispatchedEmailAsync(
            string toEmail,
            string firstName,
            string locationName,
            string orderNumber,
            string deliveryEstimate,
            string trackingDetails,
            string orderUrl
        );
    }
}
