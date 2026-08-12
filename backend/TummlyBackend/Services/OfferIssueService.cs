using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Catalog Offer issue + MVP Claim pipeline (tickets 01 / 04 / 28)
    /// and Staff Redeem Check / Mark as redeemed (ticket 38 / 05).
    /// </summary>
    public class OfferIssueService : IOfferIssueService
    {
        public const int MaxCodeAttempts = 8;
        public const string ActiveStatus = "active";
        public const string SingleUseLabel = "Single-use";

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
            CancellationToken cancellationToken = default,
            string? preallocatedClaimCode = null
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
                cancellationToken,
                preallocatedClaimCode
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
                cancellationToken,
                preallocatedClaimCode: null
            );
        }

        public async Task<OfferIssue?> IssueOnRecoverySendAsync(
            int catalogOfferId,
            int locationGuestId,
            int feedbackId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            return await CreateRecoveryIssueAsync(
                catalogOfferId,
                locationGuestId,
                feedbackId,
                atUtc,
                saveChanges: true,
                cancellationToken
            );
        }

        public async Task<OfferIssue?> StageIssueOnRecoverySendAsync(
            int catalogOfferId,
            int locationGuestId,
            int feedbackId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            return await CreateRecoveryIssueAsync(
                catalogOfferId,
                locationGuestId,
                feedbackId,
                atUtc,
                saveChanges: false,
                cancellationToken
            );
        }

        private async Task<OfferIssue?> CreateRecoveryIssueAsync(
            int catalogOfferId,
            int locationGuestId,
            int feedbackId,
            DateTime atUtc,
            bool saveChanges,
            CancellationToken cancellationToken
        )
        {
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

            // MVP Claim proxy: Recovery Send ≈ IssuedAt + ClaimedAt (Accepted-style).
            return await CreateIssueWithUniqueCodeAsync(
                catalog,
                locationGuestId,
                atUtc,
                source: OfferIssueSources.Recovery,
                campaignId: null,
                feedbackId: feedbackId,
                claimedAtUtc: atUtc,
                cancellationToken,
                preallocatedClaimCode: null,
                saveChanges: saveChanges
            );
        }

        public async Task<OfferRedeemCheckResult> CheckClaimCodeAsync(
            int locationId,
            string code,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var normalized = NormalizeClaimCode(code);
            if (normalized.Length == 0)
            {
                return new OfferRedeemCheckResult.Failed(
                    OfferRedeemFailureReasons.Invalid
                );
            }

            var issue = await FindIssueByClaimCodeAsync(
                normalized,
                cancellationToken
            );
            if (issue == null)
            {
                return new OfferRedeemCheckResult.Failed(
                    OfferRedeemFailureReasons.Invalid
                );
            }

            var gate = await EvaluateRedeemGateAsync(
                issue,
                locationId,
                atUtc,
                writeFailedAttempt: true,
                cancellationToken
            );
            if (gate != null)
            {
                return new OfferRedeemCheckResult.Failed(gate);
            }

            return new OfferRedeemCheckResult.Ok(BuildPreview(issue));
        }

        public async Task<OfferRedeemMarkResult> RedeemClaimCodeAsync(
            int locationId,
            string code,
            string issueId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var normalized = NormalizeClaimCode(code);
            if (normalized.Length == 0
                || !int.TryParse(
                    issueId.Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var parsedIssueId
                ))
            {
                return new OfferRedeemMarkResult.Failed(
                    OfferRedeemFailureReasons.Invalid
                );
            }

            var issue = await FindIssueByClaimCodeAsync(
                normalized,
                cancellationToken
            );
            if (issue == null || issue.Id != parsedIssueId)
            {
                return new OfferRedeemMarkResult.Failed(
                    OfferRedeemFailureReasons.Invalid
                );
            }

            var gate = await EvaluateRedeemGateAsync(
                issue,
                locationId,
                atUtc,
                writeFailedAttempt: false,
                cancellationToken
            );
            if (gate != null)
            {
                return new OfferRedeemMarkResult.Failed(gate);
            }

            issue.RedeemedAtUtc = atUtc;
            await _context.SaveChangesAsync(cancellationToken);
            return new OfferRedeemMarkResult.Ok();
        }

        /// <summary>
        /// Returns the persisted location thank-you catalog OfferId only when
        /// that offer is still attachable Active for the location; otherwise null.
        /// Does not clear the stored FK when non-Active (treat null for issue).
        /// </summary>
        protected virtual async Task<int?> ResolveLiveThankYouCatalogOfferIdAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            var offerId = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => row.ThankYouCatalogOfferId)
                .FirstOrDefaultAsync(cancellationToken);

            if (offerId is not int storedId)
            {
                return null;
            }

            var offer = await _context.CatalogOffers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row =>
                        row.Id == storedId
                        && row.RestaurantLocationId == locationId,
                    cancellationToken
                );

            if (offer == null)
            {
                return null;
            }

            var today = CatalogOfferStatus.VenueLocalToday(DateTime.UtcNow, 0);
            if (!CatalogOfferStatus.IsAttachableActive(
                    offer.Status,
                    offer.Validity,
                    offer.CustomExpiryDate,
                    today
                ))
            {
                return null;
            }

            return storedId;
        }

        private async Task<OfferIssue?> FindIssueByClaimCodeAsync(
            string normalizedCode,
            CancellationToken cancellationToken
        )
        {
            return await _context.OfferIssues
                .Include(i => i.LocationGuest)!
                    .ThenInclude(g => g!.RestaurantLocation)
                .FirstOrDefaultAsync(
                    i => i.ClaimCode == normalizedCode,
                    cancellationToken
                );
        }

        /// <summary>
        /// Returns failure reason when not redeemable; null when ok.
        /// Order: wrong_location → voided → already_used → expired.
        /// </summary>
        private async Task<string?> EvaluateRedeemGateAsync(
            OfferIssue issue,
            int locationId,
            DateTime atUtc,
            bool writeFailedAttempt,
            CancellationToken cancellationToken
        )
        {
            string? reason = null;

            var guestLocationId = issue.LocationGuest?.RestaurantLocationId;
            if (guestLocationId is not int issueLocationId
                || issueLocationId != locationId)
            {
                reason = OfferRedeemFailureReasons.WrongLocation;
            }
            else if (issue.CancelledAtUtc != null)
            {
                reason = OfferRedeemFailureReasons.Voided;
            }
            else if (issue.RedeemedAtUtc != null)
            {
                reason = OfferRedeemFailureReasons.AlreadyUsed;
            }
            else if (issue.ExpiryAtUtc <= atUtc)
            {
                reason = OfferRedeemFailureReasons.Expired;
            }

            if (reason == null)
            {
                return null;
            }

            if (writeFailedAttempt)
            {
                await WriteFailedAttemptAsync(
                    issue.CatalogOfferId,
                    locationId,
                    issue.ClaimCode,
                    reason,
                    atUtc,
                    cancellationToken
                );
            }

            return reason;
        }

        private async Task WriteFailedAttemptAsync(
            int catalogOfferId,
            int restaurantLocationId,
            string claimCode,
            string reason,
            DateTime atUtc,
            CancellationToken cancellationToken
        )
        {
            _context.OfferRedeemFailedAttempts.Add(
                new OfferRedeemFailedAttempt
                {
                    CatalogOfferId = catalogOfferId,
                    RestaurantLocationId = restaurantLocationId,
                    AttemptedAtUtc = atUtc,
                    ClaimCode = claimCode,
                    Reason = reason,
                }
            );
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static OfferRedeemConfirmPreviewDto BuildPreview(OfferIssue issue)
        {
            var guestName = issue.LocationGuest?.Name?.Trim() ?? string.Empty;
            var validAt =
                issue.LocationGuest?.RestaurantLocation?.LocationName?.Trim()
                ?? string.Empty;

            return new OfferRedeemConfirmPreviewDto
            {
                IssueId = issue.Id.ToString(CultureInfo.InvariantCulture),
                OfferTitle = issue.Title,
                GuestName = guestName,
                ValidAt = validAt,
                Expires = FormatExpiresLabel(issue.ExpiryAtUtc),
                Usage = SingleUseLabel,
                StaffInstruction = issue.StaffInstructions?.Trim() ?? string.Empty,
            };
        }

        private static string FormatExpiresLabel(DateTime expiryAtUtc)
        {
            return expiryAtUtc.ToString(
                "d MMM yyyy, h:mmtt",
                CultureInfo.GetCultureInfo("en-GB")
            ).Replace("AM", "am").Replace("PM", "pm");
        }

        private static string NormalizeClaimCode(string code)
        {
            return code.Trim().ToUpperInvariant();
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
            CancellationToken cancellationToken,
            string? preallocatedClaimCode = null,
            bool saveChanges = true
        )
        {
            var expiryAt = CatalogOfferMapping.ComputeExpiryAt(
                catalog.Validity,
                atUtc,
                catalog.CustomExpiryDate
            );

            var lockedClaimCode = string.IsNullOrWhiteSpace(preallocatedClaimCode)
                ? null
                : preallocatedClaimCode.Trim().ToUpperInvariant();

            for (var attempt = 1; ; attempt++)
            {
                var claimCode = lockedClaimCode ?? GenerateCandidateCode();

                var codeExists = await _context.OfferIssues
                    .AsNoTracking()
                    .AnyAsync(o => o.ClaimCode == claimCode, cancellationToken);

                if (codeExists)
                {
                    // Preallocated codes are already in the guest email — never swap.
                    if (lockedClaimCode != null || attempt >= MaxCodeAttempts)
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

                if (!saveChanges)
                {
                    // Caller commits atomically with related facts (Recovery Send).
                    return issue;
                }

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    return issue;
                }
                catch (DbUpdateException)
                {
                    DetachIfTracked(issue);

                    if (lockedClaimCode != null || attempt >= MaxCodeAttempts)
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
