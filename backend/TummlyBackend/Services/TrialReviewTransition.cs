using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class TrialReviewTransition : ITrialReviewTransition
    {
        public const int MaxAdminFeedbackLength = 2000;

        private static readonly
            Dictionary<(TrialRequestStatus, TrialReviewDecision), TrialRequestStatus>
            Transitions = new()
            {
                [(TrialRequestStatus.EmailVerified, TrialReviewDecision.Approve)] =
                    TrialRequestStatus.Approved,
                [(TrialRequestStatus.EmailVerified, TrialReviewDecision.Decline)] =
                    TrialRequestStatus.Declined,
                [(TrialRequestStatus.EmailVerified, TrialReviewDecision.RequestMoreInfo)] =
                    TrialRequestStatus.MoreInfoRequested,

                [(TrialRequestStatus.MoreInfoRequested, TrialReviewDecision.Approve)] =
                    TrialRequestStatus.Approved,
                [(TrialRequestStatus.MoreInfoRequested, TrialReviewDecision.Decline)] =
                    TrialRequestStatus.Declined,
                [(TrialRequestStatus.MoreInfoRequested, TrialReviewDecision.RequestMoreInfo)] =
                    TrialRequestStatus.MoreInfoRequested,

                [(TrialRequestStatus.Approved, TrialReviewDecision.Decline)] =
                    TrialRequestStatus.Declined,
                [(TrialRequestStatus.Approved, TrialReviewDecision.RequestMoreInfo)] =
                    TrialRequestStatus.MoreInfoRequested,
                [(TrialRequestStatus.Approved, TrialReviewDecision.ResendInvite)] =
                    TrialRequestStatus.InviteSent,

                [(TrialRequestStatus.InviteSent, TrialReviewDecision.Decline)] =
                    TrialRequestStatus.Declined,
                [(TrialRequestStatus.InviteSent, TrialReviewDecision.RequestMoreInfo)] =
                    TrialRequestStatus.MoreInfoRequested,
                [(TrialRequestStatus.InviteSent, TrialReviewDecision.ResendInvite)] =
                    TrialRequestStatus.InviteSent,
            };

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TrialReviewTransition> _logger;

        public TrialReviewTransition(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<TrialReviewTransition> logger
        )
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TrialReviewResult> ApplyTransitionAsync(
            int trialRequestId,
            TrialReviewDecision decision,
            TrialReviewContext context,
            CancellationToken cancellationToken = default
        )
        {
            if (string.IsNullOrWhiteSpace(context.AdminIdentity))
            {
                throw new ArgumentException(
                    "AdminIdentity is required.",
                    nameof(context)
                );
            }

            var trialRequest = await _context
                .TrialRequests
                .FirstOrDefaultAsync(
                    x => x.Id == trialRequestId,
                    cancellationToken
                );

            if (trialRequest == null)
            {
                throw new ArgumentException(
                    "Trial request not found."
                );
            }

            var currentStatus = trialRequest.Status;

            if (
                !Transitions.TryGetValue(
                    (currentStatus, decision),
                    out var newStatus
                )
            )
            {
                throw new IllegalTrialTransitionException(
                    currentStatus,
                    decision
                );
            }

            ValidateReasonForDecision(decision, context.Reason);

            var now = DateTime.UtcNow;
            string? setupLink = null;
            DateTime? inviteExpiresAt = null;

            ApplyStatusFields(
                trialRequest,
                decision,
                newStatus,
                context,
                now
            );

            if (
                decision == TrialReviewDecision.Approve
                || decision == TrialReviewDecision.ResendInvite
            )
            {
                setupLink = RotateInviteToken(
                    trialRequest,
                    now
                );
                inviteExpiresAt = trialRequest.InviteExpiresAt;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrent modification detected for TrialRequest {TrialRequestId} decision {Decision}.",
                    trialRequestId,
                    decision
                );
                throw new TrialReviewConcurrentModificationException();
            }

            var result = new TrialReviewResult(
                newStatus,
                setupLink,
                inviteExpiresAt
            );

            try
            {
                await DispatchEmailAsync(
                    trialRequest,
                    decision,
                    setupLink,
                    context.Reason
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Post-commit email dispatch failed for TrialRequest {TrialRequestId} decision {Decision}. State is persisted.",
                    trialRequest.Id,
                    decision
                );
                throw new TrialReviewEmailDispatchException(result, ex);
            }

            return result;
        }

        private static void ValidateReasonForDecision(
            TrialReviewDecision decision,
            string? reason
        )
        {
            if (decision == TrialReviewDecision.Decline)
            {
                ValidateFeedback(
                    reason,
                    "Decline reason is required.",
                    "Decline reason must be 2000 characters or fewer."
                );
            }
            else if (decision == TrialReviewDecision.RequestMoreInfo)
            {
                ValidateFeedback(
                    reason,
                    "More info message is required.",
                    "More info message must be 2000 characters or fewer."
                );
            }
        }

        private static void ValidateFeedback(
            string? feedback,
            string requiredMessage,
            string maxLengthMessage
        )
        {
            if (string.IsNullOrWhiteSpace(feedback))
            {
                throw new ArgumentException(requiredMessage);
            }

            if (feedback.Length > MaxAdminFeedbackLength)
            {
                throw new ArgumentException(maxLengthMessage);
            }
        }

        private static void ApplyStatusFields(
            TrialRequest trialRequest,
            TrialReviewDecision decision,
            TrialRequestStatus newStatus,
            TrialReviewContext context,
            DateTime now
        )
        {
            trialRequest.Status = newStatus;

            if (
                decision == TrialReviewDecision.Approve
                || decision == TrialReviewDecision.Decline
                || decision == TrialReviewDecision.RequestMoreInfo
            )
            {
                trialRequest.ReviewedAt = now;
                trialRequest.ReviewedBy = context.AdminIdentity;
            }

            if (context.AdminNotes != null)
            {
                trialRequest.AdminNotes = context.AdminNotes;
            }

            if (decision == TrialReviewDecision.Approve)
            {
                trialRequest.IsApproved = true;
                trialRequest.ApprovedAt = now;
                trialRequest.AccountType =
                    trialRequest.Locations == "1" ? "Single" : "Multi";
            }

            if (decision == TrialReviewDecision.Decline)
            {
                trialRequest.DeclinedAt = now;
                trialRequest.DeclineReason = context.Reason!.Trim();
            }

            if (decision == TrialReviewDecision.RequestMoreInfo)
            {
                trialRequest.MoreInfoRequestedAt = now;
                trialRequest.MoreInfoMessage = context.Reason!.Trim();
            }
        }

        private string RotateInviteToken(
            TrialRequest trialRequest,
            DateTime now
        )
        {
            var newToken = Guid.NewGuid().ToString();

            trialRequest.ApprovalToken = newToken;
            trialRequest.InviteExpiresAt = now.AddDays(
                TrialReviewConstants.InviteValidityDays
            );
            trialRequest.InviteSentAt = now;

            return BuildSetupLink(
                GetFrontendBaseUrl(),
                trialRequest.AccountType,
                newToken
            );
        }

        private async Task DispatchEmailAsync(
            TrialRequest trialRequest,
            TrialReviewDecision decision,
            string? setupLink,
            string? reason
        )
        {
            if (decision == TrialReviewDecision.Approve)
            {
                await _emailService.SendAccountSetupEmailAsync(
                    trialRequest.Email,
                    trialRequest.FullName,
                    setupLink!
                );
            }
            else if (decision == TrialReviewDecision.ResendInvite)
            {
                await _emailService.SendAccountSetupReminderEmailAsync(
                    trialRequest.Email,
                    trialRequest.FullName,
                    setupLink!,
                    trialRequest.InviteExpiresAt!.Value
                );
            }
            else if (decision == TrialReviewDecision.Decline)
            {
                await _emailService.SendDeclineEmailAsync(
                    trialRequest.Email,
                    trialRequest.FullName,
                    reason!.Trim()
                );
            }
            else if (decision == TrialReviewDecision.RequestMoreInfo)
            {
                await _emailService.SendMoreInfoEmailAsync(
                    trialRequest.Email,
                    trialRequest.FullName,
                    reason!.Trim()
                );
            }
        }

        private string GetFrontendBaseUrl()
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                throw new Exception(
                    "Frontend:BaseUrl is not configured."
                );
            }

            if (
                !Uri.TryCreate(
                    frontendBaseUrl,
                    UriKind.Absolute,
                    out var uri
                ) ||
                (uri.Scheme != Uri.UriSchemeHttps &&
                    uri.Scheme != Uri.UriSchemeHttp)
            )
            {
                throw new Exception(
                    "Frontend:BaseUrl must be an absolute http(s) URL."
                );
            }

            return frontendBaseUrl;
        }

        private static string BuildSetupLink(
            string frontendBaseUrl,
            string accountType,
            string approvalToken
        )
        {
            var route =
                accountType == "Single"
                    ? "setup-account-single"
                    : "setup-account-multi";

            return
                $"{frontendBaseUrl}/{route}?token={approvalToken}";
        }
    }
}
