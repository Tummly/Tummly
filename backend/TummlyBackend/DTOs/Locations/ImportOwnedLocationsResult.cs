namespace TummlyBackend.DTOs.Locations
{
    /// <summary>
    /// Bulk import result. Contract is partial-success: valid rows insert as
    /// Draft Owned locations until the location cap; remaining valid rows and
    /// invalid rows are reported in <see cref="Errors"/>. Fail-closed
    /// (missing billing / pricebook) is a separate whole-request outcome when
    /// nothing has been created yet.
    /// </summary>
    public abstract record ImportOwnedLocationsResult
    {
        public sealed record Completed(
            IReadOnlyList<ImportCreatedRow> Created,
            IReadOnlyList<ImportErrorRow> Errors
        ) : ImportOwnedLocationsResult;

        public sealed record InvalidRequest(string Message) : ImportOwnedLocationsResult;

        public sealed record FailClosed : ImportOwnedLocationsResult;
    }

    public sealed record ImportCreatedRow(int RowIndex, int LocationId);

    public sealed record ImportErrorRow(
        int RowIndex,
        string Message,
        string? Code = null,
        int? Cap = null,
        int? Current = null
    );
}
