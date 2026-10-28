namespace TummlyBackend.DTOs.Locations
{
    /// <summary>
    /// Warning on a row that was created as Draft because its premises match
    /// another row. Matched id and name are set only for a same-Workspace
    /// match; other-Workspace matches stay generic so no tenant data leaks.
    /// </summary>
    public sealed record PremisesDuplicateWarning(
        string Code,
        string Scope,
        int? MatchedLocationId = null,
        string? MatchedLocationName = null,
        int? MatchedRowIndex = null
    );

    public static class PossibleDuplicatePremises
    {
        public const string Code = "possible_duplicate_premises";

        /// <summary>Matches an earlier row in the same import file.</summary>
        public const string ScopeSameFile = "same_file";

        /// <summary>Matches an existing Location in the same Workspace.</summary>
        public const string ScopeSameWorkspace = "same_workspace";

        /// <summary>Matches a Location in a different Workspace.</summary>
        public const string ScopeOtherWorkspace = "other_workspace";
    }
}
