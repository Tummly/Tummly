namespace TummlyBackend.Models
{
    /// <summary>
    /// Server-owned insight candidate for Weekly brief generate (locks 02 / 06).
    /// </summary>
    public sealed record WeeklyBriefInsightEvidence(
        IReadOnlyList<string> MetricKeys,
        IReadOnlyDictionary<string, object> Snapshot,
        bool CausalEvidence = false
    );

    /// <summary>
    /// One deterministic insight candidate. Wire <see cref="Type"/> ids are kebab-case.
    /// </summary>
    public sealed record WeeklyBriefInsightCandidate(
        string Id,
        string Type,
        WeeklyBriefInsightEvidence Evidence,
        string? ActionKind = null,
        string? MetricKey = null,
        string? ChangeKind = null,
        int? DeltaPercent = null,
        string? Direction = null,
        string? ThemeLabel = null
    );

    /// <summary>
    /// Capped candidate bag for generate prompt / durable audit.
    /// </summary>
    public sealed record WeeklyBriefInsightCandidateBag(
        string ThresholdVersion,
        IReadOnlyList<WeeklyBriefInsightCandidate> Candidates
    );

    /// <summary>
    /// Control-signal input already resolved by recommended-action emitters.
    /// </summary>
    public sealed record WeeklyBriefControlSignalInput(
        string ActionKind,
        IReadOnlyDictionary<string, object> Snapshot
    );
}
