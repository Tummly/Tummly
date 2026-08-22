using System.Text.Json.Serialization;

namespace TummlyBackend.DTOs.Assistant
{
    public class AssistantAnalysisScopeDto
    {
        public string ScopeKind { get; set; } = "single";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? OwnedLocationId { get; set; }

        public string OwnedLocationName { get; set; } = string.Empty;

        public AssistantReportingPeriodDto ReportingPeriod { get; set; }
            = new();
    }

    public class AssistantReportingPeriodDto
    {
        public string Kind { get; set; } = "preset";

        public string? PresetId { get; set; }

        public string? StartDate { get; set; }

        public string? EndDate { get; set; }
    }
}
