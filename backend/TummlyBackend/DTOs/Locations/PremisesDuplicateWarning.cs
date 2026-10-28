namespace TummlyBackend.DTOs.Locations
{
    /// <summary>
    /// Warning on a Location that was saved as Draft for premises review.
    /// Every match looks the same to the operator: no scope, id, name or
    /// other-Workspace signal. <see cref="MatchedRowIndex"/> is set only when
    /// the match is an earlier row of the caller's own import file.
    /// </summary>
    public sealed record PremisesDuplicateWarning(
        string Code,
        string Message,
        int? MatchedRowIndex = null
    );

    public static class PossibleDuplicatePremises
    {
        public const string Code = "possible_duplicate_premises";

        public const string Message =
            "Possible duplicate premises. Saved as Draft for review.";

        public static PremisesDuplicateWarning Warning(int? matchedRowIndex = null) =>
            new(Code, Message, matchedRowIndex);
    }
}
