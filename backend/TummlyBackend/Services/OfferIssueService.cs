using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Catalog Offer issue + MVP Claim pipeline (tickets 01 / 04 / 28).
    /// </summary>
    public class OfferIssueService : IOfferIssueService
    {
        public const int MaxCodeAttempts = 8;
        public const string ActiveStatus = "active";

        private readonly ApplicationDbContext _context;

        public OfferIssueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OfferIssue?> IssueOnCampaignAcceptedAsync(
            int campaignId,
            int locationGuestId,
            int catalogOfferId,
            string channel,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            _ = channel;

            var alreadyIssued = await _context.OfferIssues
                .AsNoTracking()
                .AnyAsync(
                    row =>
                        row.CampaignId == campaignId
                        && row.LocationGuestId == locationGuestId,
                    cancellationToken
                );
            if (alreadyIssued)
            {
                return null;
            }

            if (await IsOptedOutAsync(locationGuestId, cancellationToken))
            {
                return null;
            }

            var catalog = await LoadActiveCatalogOfferAsync(
                catalogOfferId,
                cancellationToken
            );
            if (catalog == null)
            {
                return null;
            }

            // MVP Claim proxy (ticket 04): Accepted ≈ IssuedAt + ClaimedAt.
            return await CreateIssueWithUniqueCodeAsync(
                catalog,
                locationGuestId,
                atUtc,
                source: OfferIssueSources.Campaign,
                campaignId: campaignId,
                feedbackId: null,
                claimedAtUtc: atUtc,
                cancellationToken
            );
        }

        public async Task<OfferIssue?> IssueOnThankYouSubmitAsync(
            int locationId,
            int locationGuestId,
            int? feedbackId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var catalogOfferId = await ResolveLiveThankYouCatalogOfferIdAsync(
                locationId,
                cancellationToken
            );
            if (catalogOfferId is not int offerId)
            {
                return null;
            }

            if (await IsOptedOutAsync(locationGuestId, cancellationToken))
            {
                return null;
            }

            var catalog = await LoadActiveCatalogOfferAsync(
                offerId,
                cancellationToken
            );
            if (catalog == null)
            {
                return null;
            }

            // MVP Claim proxy: no thank-you paint endpoint yet — ClaimedAt at
            // issue (Accepted-style). Do not invent open-tracking.
            return await CreateIssueWithUniqueCodeAsync(
                catalog,
                locationGuestId,
                atUtc,
                source: OfferIssueSources.GuestFormThankYou,
                campaignId: null,
                feedbackId: feedbackId,
                claimedAtUtc: atUtc,
                cancellationToken
            );
        }

        /// <summary>
        /// Live thank-you catalog attach is not shipped yet (no
        /// RestaurantLocation / GuestLoopSetup CatalogOfferId column). Always
        /// null until that product feature exists — callers stay no-op safe.
        /// </summary>
        protected virtual Task<int?> ResolveLiveThankYouCatalogOfferIdAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            _ = locationId;
            _ = cancellationToken;
            return Task.FromResult<int?>(null);
        }

        /// <summary>
        /// Overridable in tests to force unique-index collisions.
        /// </summary>
        protected virtual string GenerateCandidateCode()
        {
            return FeedbackRecoveryOfferMapping.GenerateRedemptionCode();
        }

        private async Task<bool> IsOptedOutAsync(
            int locationGuestId,
            CancellationToken cancellationToken
        )
        {
            return await _context.LocationGuests
                .AsNoTracking()
                .Where(g => g.Id == locationGuestId)
                .Select(g => g.OffersOptOut)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<CatalogOffer?> LoadActiveCatalogOfferAsync(
            int catalogOfferId,
            CancellationToken cancellationToken
        )
        {
            var catalog = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == catalogOfferId, cancellationToken);

            if (catalog == null)
            {
                return null;
            }

            if (!string.Equals(
                    catalog.Status,
                    ActiveStatus,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            return catalog;
        }

        private async Task<OfferIssue?> CreateIssueWithUniqueCodeAsync(
            CatalogOffer catalog,
            int locationGuestId,
            DateTime atUtc,
            string source,
            int? campaignId,
            int? feedbackId,
            DateTime? claimedAtUtc,
            CancellationToken cancellationToken
        )
        {
            var expiryAt = CatalogOfferMapping.ComputeExpiryAt(
                catalog.Validity,
                atUtc,
                catalog.CustomExpiryDate
            );

            for (var attempt = 1; ; attempt++)
            {
                var claimCode = GenerateCandidateCode();

                var codeExists = await _context.OfferIssues
                    .AsNoTracking()
                    .AnyAsync(o => o.ClaimCode == claimCode, cancellationToken);

                if (codeExists)
                {
                    if (attempt >= MaxCodeAttempts)
                    {
                        throw new OfferIssueCodeAllocationException();
                    }

                    continue;
                }

                var issue = BuildIssue(
                    catalog,
                    locationGuestId,
                    atUtc,
                    expiryAt,
                    claimCode,
                    source,
                    campaignId,
                    feedbackId,
                    claimedAtUtc
                );

                _context.OfferIssues.Add(issue);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    return issue;
                }
                catch (DbUpdateException)
                {
                    DetachIfTracked(issue);

                    if (attempt >= MaxCodeAttempts)
                    {
                        throw new OfferIssueCodeAllocationException();
                    }
                }
            }
        }

        private static OfferIssue BuildIssue(
            CatalogOffer catalog,
            int locationGuestId,
            DateTime atUtc,
            DateTime expiryAt,
            string claimCode,
            string source,
            int? campaignId,
            int? feedbackId,
            DateTime? claimedAtUtc
        )
        {
            return new OfferIssue
            {
                CatalogOfferId = catalog.Id,
                LocationGuestId = locationGuestId,
                ClaimCode = claimCode,
                IssuedAtUtc = atUtc,
                ClaimedAtUtc = claimedAtUtc,
                Source = source,
                CampaignId = campaignId,
                FeedbackId = feedbackId,
                ExpiryAtUtc = expiryAt,
                OfferType = catalog.OfferType,
                Title = catalog.Title,
                Description = catalog.Description,
                Validity = catalog.Validity,
                CustomExpiryDate = catalog.CustomExpiryDate,
                DiscountPercentage = catalog.DiscountPercentage,
                DiscountAmount = catalog.DiscountAmount,
                FreeItemText = catalog.FreeItemText,
                PurchaseRequirement = catalog.PurchaseRequirement,
                MinimumSpend = catalog.MinimumSpend,
                AdditionalExclusions = catalog.AdditionalExclusions,
                ReplacementItemText = catalog.ReplacementItemText,
                StaffInstructions = catalog.StaffInstructions,
            };
        }

        private void DetachIfTracked(object entity)
        {
            var entry = _context.Entry(entity);
            if (entry.State != EntityState.Detached)
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
