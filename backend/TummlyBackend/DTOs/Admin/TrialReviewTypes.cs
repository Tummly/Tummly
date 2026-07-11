using System;
using TummlyBackend.Models;

namespace TummlyBackend.DTOs.Admin
{
    public enum TrialReviewDecision
    {
        Approve,
        Decline,
        RequestMoreInfo,
        ResendInvite
    }

    public sealed record TrialReviewContext(
        string AdminIdentity,
        string? Reason,
        string? AdminNotes
    );

    public sealed record TrialReviewResult(
        TrialRequestStatus NewStatus,
        string? SetupLink,
        DateTime? InviteExpiresAt
    );

    public sealed record OperatorSetupReminderBatchResult(
        int Sent,
        int Failed
    );

    public class IllegalTrialTransitionException : Exception
    {
        public IllegalTrialTransitionException(
            TrialRequestStatus currentStatus,
            TrialReviewDecision decision
        )
            : base(
                $"Cannot apply {decision} to a Trial Request in {currentStatus} state."
            )
        {
            CurrentStatus = currentStatus;
            Decision = decision;
        }

        public TrialRequestStatus CurrentStatus { get; }
        public TrialReviewDecision Decision { get; }
    }

    public class TrialReviewEmailDispatchException : Exception
    {
        public const string DefaultMessage =
            "The status was saved but the notification email could not be sent. "
            + "Use Resend invitation if the operator should receive a setup link.";

        public TrialReviewEmailDispatchException(
            TrialReviewResult result,
            Exception innerException
        )
            : base(DefaultMessage, innerException)
        {
            Result = result;
        }

        public TrialReviewResult Result { get; }
    }

    public class TrialReviewConcurrentModificationException : Exception
    {
        public TrialReviewConcurrentModificationException()
            : base(
                "This trial request was updated by another action. Refresh and try again."
            )
        {
        }
    }
}
