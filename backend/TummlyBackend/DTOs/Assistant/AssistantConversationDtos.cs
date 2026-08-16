namespace TummlyBackend.DTOs.Assistant
{
    public class SendAssistantTurnRequest
    {
        public int? ConversationId { get; set; }

        public string Message { get; set; } = string.Empty;

        public AssistantAnalysisScopeDto AnalysisScope { get; set; } = new();
    }

    public class ApplyAssistantScopeRequest
    {
        public AssistantAnalysisScopeDto AnalysisScope { get; set; } = new();
    }

    public class AssistantConversationDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public AssistantAnalysisScopeDto AnalysisScope { get; set; } = new();

        public bool IsArchived { get; set; }

        public DateTime LastActivityAt { get; set; }

        public IReadOnlyList<AssistantMessageDto> Messages { get; set; }
            = Array.Empty<AssistantMessageDto>();

        public bool RetryEligible { get; set; }

        public AssistantCampaignDraftPayloadDto? PendingCampaignDraft { get; set; }

        public AssistantOfferDraftPayloadDto? PendingOfferDraft { get; set; }

        public AssistantRecoveryDraftPayloadDto? PendingRecoveryDraft { get; set; }

        public bool DraftInterviewActive { get; set; }
    }

    public class AssistantCampaignDraftPayloadDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? GoalId { get; set; }
        public string? TemplateId { get; set; }
        public int? TemplateVersion { get; set; }
        public string? AudienceKey { get; set; }
        public string? Channel { get; set; }
        public string? OfferStance { get; set; }
        public int? OfferId { get; set; }
        public string? MessageSubject { get; set; }
        public string? MessageBody { get; set; }
    }

    public class AssistantOfferDraftPayloadDto
    {
        public int LocationId { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Validity { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? FreeItemText { get; set; }
        public string? PurchaseRequirement { get; set; }
        public decimal? MinimumSpend { get; set; }
        public string? AdditionalExclusions { get; set; }
        public string? ReplacementItemText { get; set; }
        public string? StaffInstructions { get; set; }
    }

    public class AssistantRecoveryDraftPayloadDto
    {
        public int FeedbackId { get; set; }
        public string Intent { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public string? Purpose { get; set; }
        public string? Tone { get; set; }
        public string? IncludeNotes { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public string? Category { get; set; }
        public string? Note { get; set; }
        public int? OfferId { get; set; }
        public bool UseConfirmedActionForGuestResponse { get; set; }
    }

    public class AssistantConversationListItemDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string OwnedLocationName { get; set; } = string.Empty;

        public DateTime LastActivityAt { get; set; }

        public bool IsArchived { get; set; }
    }

    public class AssistantMessageDto
    {
        public int Id { get; set; }

        public string Role { get; set; } = string.Empty;

        public string? Class { get; set; }

        public string? Title { get; set; }

        public string Body { get; set; } = string.Empty;

                public AssistantAnalysisScopeDto? AnalysisScope { get; set; }

        public IReadOnlyList<AssistantActionDto> Actions { get; set; }
            = Array.Empty<AssistantActionDto>();
    }
}
