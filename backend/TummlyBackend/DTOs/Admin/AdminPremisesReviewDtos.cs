namespace TummlyBackend.DTOs.Admin
{
    /// <summary>
    /// A Location waiting for the duplicate-premises review. Admin only: the
    /// matches may belong to other Workspaces.
    /// </summary>
    public sealed class AdminPremisesReviewItemDto
    {
        public int LocationId { get; init; }

        public int RestaurantId { get; init; }

        public string WorkspaceName { get; init; } = string.Empty;

        public string LocationName { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string? City { get; init; }

        public string? Postcode { get; init; }

        public string? FsaEstablishmentId { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public IReadOnlyList<AdminPremisesReviewMatchDto> Matches { get; init; } = [];
    }

    public sealed class AdminPremisesReviewMatchDto
    {
        public int LocationId { get; init; }

        public int RestaurantId { get; init; }

        public string WorkspaceName { get; init; } = string.Empty;

        public string? LocationName { get; init; }

        public string? Address { get; init; }

        public string? City { get; init; }

        public string? Postcode { get; init; }

        public string? LifecycleStatus { get; init; }

        public bool SameWorkspace { get; init; }

        public bool PremisesKeyMatch { get; init; }

        public bool FsaMatch { get; init; }

        public bool PriorPilot { get; init; }

        public bool PriorStarterKit { get; init; }
    }

    public sealed class AdminPremisesReviewResolveRequest
    {
        public string? Reason { get; set; }
    }

    public abstract record AdminPremisesReviewResolveResult
    {
        public sealed record Resolved : AdminPremisesReviewResolveResult;

        public sealed record NotFound : AdminPremisesReviewResolveResult;

        public sealed record ReasonRequired : AdminPremisesReviewResolveResult;

        public sealed record NotPending : AdminPremisesReviewResolveResult;
    }
}
