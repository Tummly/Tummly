namespace TummlyBackend.DTOs.Campaigns
{
    /// <summary>Concurrency token for list lifecycle actions (ticket 30).</summary>
    public sealed class CampaignLifecycleActionRequest
    {
        public byte[] RowVersion { get; init; } = [];
    }

    public sealed class CampaignLifecycleDto
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

    public sealed class CampaignDuplicateDto
    {
        public int Id { get; init; }

        public int LocationId { get; init; }

        public string Status { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string? GoalId { get; init; }

        public string? AudienceKey { get; init; }

        public string? Channel { get; init; }

        public string? OfferStance { get; init; }

        public int? OfferId { get; init; }

        public string? MessageSubject { get; init; }

        public string? MessageBody { get; init; }

        public string? TemplateId { get; init; }

        public int? TemplateVersion { get; init; }

        public byte[] RowVersion { get; init; } = [];

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    public abstract class CampaignLifecycleResult
    {
        private CampaignLifecycleResult()
        {
        }

        public sealed class Ok : CampaignLifecycleResult
        {
            public required CampaignLifecycleDto Campaign { get; init; }
        }

        public sealed class Duplicated : CampaignLifecycleResult
        {
            public required CampaignDuplicateDto Campaign { get; init; }
        }

        public sealed class NotFound : CampaignLifecycleResult
        {
        }

        public sealed class Conflict : CampaignLifecycleResult
        {
        }

        public sealed class InvalidStatus : CampaignLifecycleResult
        {
            public required string Message { get; init; }
        }

        public sealed class BillingReserveUnavailable : CampaignLifecycleResult
        {
        }

        public sealed class ReserveFailed : CampaignLifecycleResult
        {
            public required string Message { get; init; }
        }

        public sealed class ReleaseFailed : CampaignLifecycleResult
        {
            public required string Message { get; init; }
        }

        public sealed class ZeroEligible : CampaignLifecycleResult
        {
        }
    }
}
