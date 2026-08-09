namespace TummlyBackend.DTOs.Campaigns
{
    public sealed class CampaignFireDto
    {
        public int Id { get; init; }

        public string Status { get; init; } = string.Empty;

        public int AcceptedCount { get; init; }

        public int SkippedIneligibleCount { get; init; }

        public int RemainingUnsentCount { get; init; }

        public string? BillingReservationRef { get; init; }

        public byte[] RowVersion { get; init; } = [];
    }

    public abstract class CampaignFireResult
    {
        private CampaignFireResult()
        {
        }

        public sealed class Ok : CampaignFireResult
        {
            public required CampaignFireDto Campaign { get; init; }
        }

        public sealed class NotFound : CampaignFireResult
        {
        }

        public sealed class NotFireable : CampaignFireResult
        {
            public required string Message { get; init; }
        }

        public sealed class NotDue : CampaignFireResult
        {
        }

        public sealed class Conflict : CampaignFireResult
        {
        }

        /// <summary>
        /// Cannot-start → Failed persisted; reservation released; no auto-retry.
        /// </summary>
        public sealed class CannotStart : CampaignFireResult
        {
            public required CampaignFireDto Campaign { get; init; }
        }
    }
}
