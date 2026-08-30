namespace TummlyBackend.DTOs.Admin
{
    public sealed class StaffManualCreditAdjustmentRequestDto
    {
        public int RestaurantId { get; set; }

        public string Channel { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;

        public Guid? AllocationId { get; set; }

        public int? HelpCentreQueryId { get; set; }
    }

    public sealed class StaffManualCreditAdjustmentResponseDto
    {
        public bool Success { get; set; } = true;

        public int RestaurantId { get; set; }

        public string Channel { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Direction { get; set; } = string.Empty;

        public int CombinedRemaining { get; set; }
    }

    public sealed class StaffCreditReversalRequestDto
    {
        public Guid ReversedEntryId { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int? HelpCentreQueryId { get; set; }
    }
}
