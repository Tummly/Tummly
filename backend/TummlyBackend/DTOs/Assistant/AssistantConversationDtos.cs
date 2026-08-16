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
