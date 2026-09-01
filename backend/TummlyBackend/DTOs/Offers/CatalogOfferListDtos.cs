using TummlyBackend.DTOs.BillingCredits;

namespace TummlyBackend.DTOs.Offers
{
    public sealed class CatalogOffersListItemDto
    {
        public int Id { get; init; }

        public int LocationId { get; init; }

        public string Title { get; init; } = string.Empty;

        /// <summary>Effective / badge status.</summary>
        public string Status { get; init; } = "active";

        public string OfferType { get; init; } = string.Empty;

        public string Validity { get; init; } = string.Empty;

        public string? ExpiryDate { get; init; }

        public IReadOnlyList<string> AttachKinds { get; init; }
            = Array.Empty<string>();

        public string? Description { get; init; }

        public int LifetimeClaims { get; init; }

        public int LifetimeRedeemed { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime UpdatedAt { get; init; }
    }

    public sealed class CatalogOffersTabCountsDto
    {
        public int All { get; init; }

        public int NeedsAttention { get; init; }

        public int Drafts { get; init; }

        public int InFlight { get; init; }

        public int Sent { get; init; }
    }

    public sealed class CatalogOffersListResponse
    {
        public IReadOnlyList<CatalogOffersListItemDto> Items { get; init; }
            = Array.Empty<CatalogOffersListItemDto>();

        public int TotalCount { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public CatalogOffersTabCountsDto TabCounts { get; init; } = new();

        public PlanEntitlementsAccountSnapshotDto Entitlements { get; init; } = new();
    }

    public sealed class CatalogOffersListQuery
    {
        public int LocationId { get; init; }

        public string? View { get; init; }

        public string? Q { get; init; }

        public string Sort { get; init; } = "recent-activity";

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public IReadOnlyList<string> Status { get; init; } = Array.Empty<string>();

        public IReadOnlyList<string> AttachSource { get; init; }
            = Array.Empty<string>();

        public int UtcOffsetMinutes { get; init; }

        /// <summary>
        /// Optional Needs attention warning scope: expiry (7-day rule) or void
        /// (open Void request). Ignored unless view is needs-attention.
        /// </summary>
        public string? AttentionWarningType { get; init; }
    }

    public abstract class CatalogOfferLifecycleResult
    {
        private CatalogOfferLifecycleResult()
        {
        }

        public sealed class Ok : CatalogOfferLifecycleResult
        {
            public required CatalogOfferDto Offer { get; init; }
        }

        public sealed class Duplicated : CatalogOfferLifecycleResult
        {
            public required CatalogOfferDto Offer { get; init; }
        }

        public sealed class NotFound : CatalogOfferLifecycleResult
        {
        }

        public sealed class InvalidStatus : CatalogOfferLifecycleResult
        {
            public required string Message { get; init; }
        }

        public sealed class CapReached : CatalogOfferLifecycleResult
        {
            public required int Cap { get; init; }

            public required int Current { get; init; }
        }

        public sealed class FailClosed : CatalogOfferLifecycleResult
        {
        }
    }
}
