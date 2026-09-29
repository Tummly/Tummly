namespace TummlyBackend.Models
{
    public sealed record AssistantToolCallRequest(
        string Id,
        string Name,
        string ArgumentsJson
    );

    public sealed record AssistantToolCallResult(
        string ToolCallId,
        string Name,
        string ContentJson
    );

    /// <summary>
    /// Server-owned scope for retrieve tool calls. The model cannot widen this.
    /// </summary>
    public sealed class AssistantRetrieveToolContext
    {
        public required int OwnerUserId { get; init; }

        public required int? OwnedLocationId { get; init; }

        public required string OwnedLocationName { get; init; }

        public required DateTime FromUtc { get; init; }

        public required DateTime ToUtc { get; init; }

        public required string PeriodPhrase { get; init; }

        public required IReadOnlyList<Helpers.AssistantOwnedLocationRef> OwnedLocations
        {
            get;
            init;
        }

        public required IReadOnlyList<int> CompareLocationIds { get; init; }

        public bool IncludeCampaignCopy { get; init; }

        /// <summary>
        /// When true, <c>compare_all_locations</c> thins packs and tracks
        /// failed / not-started names under the retrieve budget.
        /// </summary>
        public bool CompareAllMode { get; init; }

        /// <summary>
        /// Accumulated evidence from tool calls this turn (for post-resolve).
        /// </summary>
        public AssistantRetrievedEvidence AccumulatedEvidence { get; set; }
            = AssistantRetrievedEvidence.Empty;

        public IReadOnlyList<AssistantCompareLocationEvidence>? CompareLocations { get; set; }

        public IReadOnlyList<string> FailedLocationNames { get; set; } = [];

        public IReadOnlyList<string> NotStartedLocationNames { get; set; } = [];
    }

    public delegate Task<IReadOnlyList<AssistantToolCallResult>> AssistantRetrieveToolExecutor(
        IReadOnlyList<AssistantToolCallRequest> calls,
        CancellationToken cancellationToken
    );

    public delegate Task AssistantRetrieveProgressCallback(
        string step,
        CancellationToken cancellationToken
    );
}
