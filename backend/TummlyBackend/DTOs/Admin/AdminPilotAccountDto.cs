namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminPilotAccountDto
    {
        public int RestaurantId { get; set; }

        public int OwnerUserId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public string OwnerEmail { get; set; } = string.Empty;

        public string BillingStatus { get; set; } = string.Empty;

        public DateTime? PilotStartedAtUtc { get; set; }

        public DateTime? PilotPeriodEnd { get; set; }

        public int ExtensionCount { get; set; }

        public int ExtensionDaysTotal { get; set; }

        public bool CanExtend { get; set; }

        public string? CannotExtendReason { get; set; }
    }
}
