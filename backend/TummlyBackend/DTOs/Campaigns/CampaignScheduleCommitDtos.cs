namespace TummlyBackend.DTOs.Campaigns
{
    public sealed class CommitCampaignScheduleRequest
    {
        /// <summary>Base64 SQL rowversion from the last Draft read.</summary>
        public byte[] RowVersion { get; init; } = [];

        /// <summary><c>send-now</c> or <c>schedule-later</c>.</summary>
        public string ScheduleMode { get; init; } = string.Empty;

        /// <summary>
        /// Required when schedule-later; must be strictly after commit UTC.
        /// Send-now may omit.
        /// </summary>
        public DateTime? ScheduledAtUtc { get; init; }

        /// <summary>Account / restaurant IANA timezone.</summary>
        public string ScheduleTimeZone { get; init; } = string.Empty;
    }

    public sealed class CampaignScheduleCommitDto
    {
        public int Id { get; init; }

        public int LocationId { get; init; }

        public string Status { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? ScheduleMode { get; init; }

        public DateTime? ScheduledAtUtc { get; init; }

        public string? ScheduleTimeZone { get; init; }

        public string? BillingReservationRef { get; init; }

        public int? ReservedEstimate { get; init; }

        public int FrozenRecipientCount { get; init; }

        public byte[] RowVersion { get; init; } = [];

        public DateTime UpdatedAt { get; init; }
    }

    public abstract class CampaignScheduleCommitResult
    {
        private CampaignScheduleCommitResult()
        {
        }

        public sealed class Ok : CampaignScheduleCommitResult
        {
            public required CampaignScheduleCommitDto Campaign { get; init; }
        }

        public sealed class NotFound : CampaignScheduleCommitResult
        {
        }

        public sealed class Conflict : CampaignScheduleCommitResult
        {
        }

        public sealed class NotDraft : CampaignScheduleCommitResult
        {
        }

        public sealed class BillingReserveUnavailable : CampaignScheduleCommitResult
        {
        }

        public sealed class ReserveFailed : CampaignScheduleCommitResult
        {
            public required string Message { get; init; }
        }

        public sealed class ZeroEligible : CampaignScheduleCommitResult
        {
        }

        public sealed class InvalidSchedule : CampaignScheduleCommitResult
        {
            public required string Message { get; init; }
        }

        public sealed class NotReviewReady : CampaignScheduleCommitResult
        {
            public required string Message { get; init; }
        }
    }
}
