namespace TummlyBackend.Models
{
    public sealed record AssistantGuestsEvidence(
        int TotalCount,
        int SampleCount,
        IReadOnlyList<AssistantGuestEvidenceRow> Rows,
        IReadOnlyList<string> ContactRedactionTokens
    )
    {
        public static AssistantGuestsEvidence Empty { get; } =
            new(0, 0, [], []);

        public bool IsEmpty => TotalCount == 0;

        public bool DisclosesSample => TotalCount > SampleCount;
    }
}
