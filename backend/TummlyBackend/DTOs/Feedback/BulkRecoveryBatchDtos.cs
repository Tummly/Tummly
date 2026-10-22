namespace TummlyBackend.DTOs.Feedback
{
    public sealed class CreateBulkRecoveryBatchRequest
    {
        public int LocationId { get; set; }

        public List<int> FeedbackIds { get; set; } = [];

        public string Channel { get; set; } = "email";

        public string Intent { get; set; } = "respond_to_guest";

        public string? Subject { get; set; }

        public string BodyTemplate { get; set; } = string.Empty;

        public string? Purpose { get; set; }

        public string? Tone { get; set; }

        public string? IncludeNotes { get; set; }
    }

    public sealed class UpdateBulkRecoveryBatchDraftRequest
    {
        public string? Subject { get; set; }

        public string? BodyTemplate { get; set; }

        public string? Purpose { get; set; }

        public string? Tone { get; set; }

        public string? IncludeNotes { get; set; }
    }

    public sealed class BulkRecoveryBatchMemberDto
    {
        public int Id { get; init; }

        public int FeedbackId { get; init; }

        public string GuestName { get; init; } = string.Empty;

        public string MaskedDestination { get; init; } = string.Empty;

        public string? FeedbackComment { get; init; }

        public string Status { get; init; } = "pending";

        public int? FeedbackGuestResponseId { get; init; }

        public string? FailureReason { get; init; }
    }

    public sealed class BulkRecoveryBatchDto
    {
        public int Id { get; init; }

        public int RestaurantId { get; init; }

        public int LocationId { get; init; }

        public string Channel { get; init; } = "email";

        public string Intent { get; init; } = "respond_to_guest";

        public string? Subject { get; init; }

        public string BodyTemplate { get; init; } = string.Empty;

        public string? Purpose { get; init; }

        public string? Tone { get; init; }

        public string? IncludeNotes { get; init; }

        public string Status { get; init; } = "draft";

        public int MemberCount { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? CompletedAt { get; init; }

        public IReadOnlyList<BulkRecoveryBatchMemberDto> Members { get; init; }
            = [];
    }

    public sealed class SendBulkRecoveryBatchResultDto
    {
        public int BatchId { get; init; }

        public string Status { get; init; } = string.Empty;

        public int Sent { get; init; }

        public int Skipped { get; init; }

        public int Failed { get; init; }

        public int Total { get; init; }
    }
}
