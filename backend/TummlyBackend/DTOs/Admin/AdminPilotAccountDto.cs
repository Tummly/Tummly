namespace TummlyBackend.DTOs.Admin
{
    public sealed class AdminPilotAccountDto
    {
        public int RestaurantId { get; set; }

        public int OwnerUserId { get; set; }

        public string RestaurantName { get; set; } = string.Empty;

        public string OwnerEmail { get; set; } = string.Empty;

        public string BillingStatus { get; set; } = string.Empty;

        /// <summary>Original start of the Pilot term.</summary>
        public DateTime? PilotStartedAtUtc { get; set; }

        /// <summary>Original end of the Pilot term. Never changes after start.</summary>
        public DateTime? PilotOriginalEndUtc { get; set; }

        /// <summary>Effective expiry. Moves with each extension.</summary>
        public DateTime? PilotPeriodEnd { get; set; }

        /// <summary>Whole days from start to the earlier of now and the effective expiry.</summary>
        public int DaysUsed { get; set; }

        /// <summary>True once the account has been on a paid plan.</summary>
        public bool HasPaidHistory { get; set; }

        public int ExtensionCount { get; set; }

        public int ExtensionDaysTotal { get; set; }

        /// <summary>Successful extensions, oldest first. Taken from admin audit events.</summary>
        public IReadOnlyList<AdminPilotExtensionHistoryItemDto> ExtensionHistory { get; set; } = [];

        /// <summary>Pilot allowance per channel. Null when the balance cannot be read.</summary>
        public IReadOnlyList<AdminPilotAllowanceDto>? Allowances { get; set; }

        public bool CanExtend { get; set; }

        public string? CannotExtendReason { get; set; }
    }

    public sealed class AdminPilotExtensionHistoryItemDto
    {
        public DateTime OccurredAtUtc { get; set; }

        public int Days { get; set; }

        public string ActorIdentity { get; set; } = string.Empty;

        /// <summary>Null for extensions recorded before reasons were required.</summary>
        public string? Reason { get; set; }
    }

    public sealed class AdminPilotAllowanceDto
    {
        /// <summary>Credit channel: <c>ai</c>, <c>email</c> or <c>sms</c>.</summary>
        public string Channel { get; set; } = string.Empty;

        /// <summary>Pilot allocation granted once at start.</summary>
        public int Granted { get; set; }

        public int Used { get; set; }

        public int Remaining { get; set; }
    }
}
