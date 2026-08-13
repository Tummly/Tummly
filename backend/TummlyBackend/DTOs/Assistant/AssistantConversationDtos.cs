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

        public DateTime LastActivityAt { get; set; }

        public IReadOnlyList<AssistantMessageDto> Messages { get; set; }
            = Array.Empty<AssistantMessageDto>();

        public bool RetryEligible { get; set; }
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
