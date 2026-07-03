using System;

namespace TummlyBackend.Models
{
    public enum TrialRequestStatus
    {
        EmailVerified,
        MoreInfoRequested,
        Approved,
        InviteSent,
        Declined,
        AccountCreated
    }

    public static class TrialRequestStatusExtensions
    {
        public static string ToWireString(this TrialRequestStatus status) =>
            status switch
            {
                TrialRequestStatus.EmailVerified => "EMAIL_VERIFIED",
                TrialRequestStatus.MoreInfoRequested => "MORE_INFO_REQUESTED",
                TrialRequestStatus.Approved => "APPROVED",
                TrialRequestStatus.InviteSent => "INVITE_SENT",
                TrialRequestStatus.Declined => "DECLINED",
                TrialRequestStatus.AccountCreated => "ACCOUNT_CREATED",
                _ => status.ToString()
            };

        public static TrialRequestStatus FromWireString(string stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                return TrialRequestStatus.EmailVerified;
            }

            var normalized = stored.Trim().ToUpperInvariant().Replace(" ", "_");

            return normalized switch
            {
                "EMAIL_VERIFIED" => TrialRequestStatus.EmailVerified,
                "MORE_INFO_REQUESTED" => TrialRequestStatus.MoreInfoRequested,
                "APPROVED" => TrialRequestStatus.Approved,
                "INVITE_SENT" => TrialRequestStatus.InviteSent,
                "DECLINED" => TrialRequestStatus.Declined,
                "ACCOUNT_CREATED" => TrialRequestStatus.AccountCreated,
                _ => TrialRequestStatus.EmailVerified
            };
        }
    }
}
