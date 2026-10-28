using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    /// <summary>What to check: the premises a Location is about to occupy.</summary>
    public sealed record PremisesCheckInput(
        int RestaurantId,
        string? Address,
        string? City,
        string? Postcode,
        string? FsaEstablishmentId,
        int? ExcludeLocationId = null
    );

    /// <summary>
    /// One server-side match. Never sent to operators: only the Admin review
    /// reads it, because the matched Location may belong to another Workspace.
    /// </summary>
    public sealed record PremisesMatch(
        int LocationId,
        int RestaurantId,
        bool SameWorkspace,
        bool PremisesKeyMatch,
        bool FsaMatch,
        bool PriorPilot,
        bool PriorStarterKit
    );

    public sealed record PremisesCheckResult(
        string? PremisesKey,
        string? FsaEstablishmentId,
        IReadOnlyList<PremisesMatch> Matches
    )
    {
        public bool Flagged => Matches.Count > 0;
    }

    /// <summary>
    /// The one duplicate-premises check. CSV import, single Add Location and
    /// signup all call it. A Location name never matches on its own.
    /// </summary>
    public interface IPremisesReviewService
    {
        Task<PremisesCheckResult> CheckAsync(
            PremisesCheckInput input,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Writes the persisted key and FSA id, and sets PendingReview when
        /// the check flagged or <paramref name="alsoFlagged"/> is set (a match
        /// the caller found itself, such as an earlier row in the same import
        /// file). Returns true when the Location needs review. Does not save.
        /// </summary>
        bool Apply(
            RestaurantLocation location,
            PremisesCheckResult result,
            bool alsoFlagged = false
        );

        /// <summary>
        /// Stages the system audit row that carries the match detail for
        /// Admin. Call after the Location has an id. Does not save.
        /// </summary>
        void RecordFlag(
            RestaurantLocation location,
            PremisesCheckResult result,
            int? matchedImportRowIndex = null
        );
    }
}
