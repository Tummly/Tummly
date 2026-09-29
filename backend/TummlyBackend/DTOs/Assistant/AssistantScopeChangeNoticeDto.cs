namespace TummlyBackend.DTOs.Assistant
{
    /// <summary>
    /// Server applied an NL Analysis scope override on this assistant turn.
    /// Frontend highlights the change and syncs the header from conversation.analysisScope.
    /// </summary>
    public class AssistantScopeChangeNoticeDto
    {
        public string? PreviousPeriodLabel { get; set; }

        public string? NextPeriodLabel { get; set; }

        public string? PreviousLocationLabel { get; set; }

        public string? NextLocationLabel { get; set; }

        /// <summary>"period" and/or "locations".</summary>
        public IReadOnlyList<string> Kinds { get; set; } = Array.Empty<string>();
    }
}
