namespace TummlyBackend.Models
{
    /// <summary>
    /// Model-authored observation / interpretation / recommendation for one
    /// server candidate (lock 03).
    /// </summary>
    public sealed record WeeklyBriefInsightNarrative(
        string CandidateId,
        string Observation,
        string Interpretation,
        string? Recommendation = null
    );
}
