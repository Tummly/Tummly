using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offer Details Claims + Redemptions list reads (ticket 40).
    /// </summary>
    public sealed class OfferLifecycleService : IOfferLifecycleService
    {
        private readonly ApplicationDbContext _context;

        public OfferLifecycleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OfferDetailsClaimsListDto?> ListClaimsAsync(
            int offerId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var offerExists = await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(o => o.Id == offerId, cancellationToken);

            if (!offerExists)
            {
                return null;
            }

            var issues = await _context.OfferIssues
                .AsNoTracking()
                .Where(i => i.CatalogOfferId == offerId)
                .Include(i => i.LocationGuest!)
                    .ThenInclude(g => g.RestaurantLocation)
                .Include(i => i.Campaign)
                .OrderByDescending(i => i.IssuedAtUtc)
                .ToListAsync(cancellationToken);

            var items = issues
                .Select(issue => ToClaimListItem(issue, atUtc))
                .ToList();

            return new OfferDetailsClaimsListDto { Items = items };
        }

        public async Task<OfferDetailsRedemptionsListDto?> ListRedemptionsAsync(
            int offerId,
            DateTime atUtc,
            CancellationToken cancellationToken = default
        )
        {
            var offerExists = await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(o => o.Id == offerId, cancellationToken);

            if (!offerExists)
            {
                return null;
            }

            var redeemedIssues = await _context.OfferIssues
                .AsNoTracking()
                .Where(
                    i =>
                        i.CatalogOfferId == offerId
                        && i.RedeemedAtUtc != null
                )
                .Include(i => i.LocationGuest!)
                    .ThenInclude(g => g.RestaurantLocation)
                .Include(i => i.Campaign)
                .ToListAsync(cancellationToken);

            var failedAttempts = await _context.OfferRedeemFailedAttempts
                .AsNoTracking()
                .Where(a => a.CatalogOfferId == offerId)
                .Include(a => a.RestaurantLocation)
                .ToListAsync(cancellationToken);

            var issuesByCode = await _context.OfferIssues
                .AsNoTracking()
                .Where(i => i.CatalogOfferId == offerId)
                .Include(i => i.LocationGuest!)
                    .ThenInclude(g => g.RestaurantLocation)
                .Include(i => i.Campaign)
                .ToDictionaryAsync(
                    i => NormalizeClaimCode(i.ClaimCode),
                    cancellationToken
                );

            var rows = new List<OfferDetailsRedemptionListItemDto>();

            foreach (var issue in redeemedIssues)
            {
                rows.Add(ToRedeemedListItem(issue));
            }

            foreach (var attempt in failedAttempts)
            {
                rows.Add(
                    ToFailedAttemptListItem(attempt, issuesByCode)
                );
            }

            rows.Sort(
                (left, right) =>
                    right.DateTimeUtc.CompareTo(left.DateTimeUtc)
            );

            return new OfferDetailsRedemptionsListDto { Items = rows };
        }

        private static OfferDetailsClaimListItemDto ToClaimListItem(
            OfferIssue issue,
            DateTime atUtc
        )
        {
            var guestName = issue.LocationGuest?.Name?.Trim() ?? string.Empty;
            var locationName =
                issue.LocationGuest?.RestaurantLocation?.LocationName?.Trim()
                ?? string.Empty;
            var (status, statusLabel) = ResolveClaimStatus(issue, atUtc);
            var sourceLabel = FormatIssueSourceLabel(issue);

            return new OfferDetailsClaimListItemDto
            {
                Id = issue.Id.ToString(CultureInfo.InvariantCulture),
                GuestName = guestName,
                GuestId = issue.LocationGuestId,
                ClaimCode = issue.ClaimCode,
                ClaimedAtUtc = issue.ClaimedAtUtc,
                IssuedAtUtc = issue.IssuedAtUtc,
                Source = issue.Source,
                SourceLabel = sourceLabel,
                CampaignName = issue.Campaign?.Name?.Trim(),
                LocationName = locationName,
                ExpiryAtUtc = issue.ExpiryAtUtc,
                Status = status,
                StatusLabel = statusLabel,
                PassCodeMasked = MaskClaimCode(issue.ClaimCode),
                OfferTitle = issue.Title,
                LinkedCampaignText = FormatLinkedCampaignText(issue),
            };
        }

        private static OfferDetailsRedemptionListItemDto ToRedeemedListItem(
            OfferIssue issue
        )
        {
            var guestName = issue.LocationGuest?.Name?.Trim() ?? string.Empty;
            var locationName =
                issue.LocationGuest?.RestaurantLocation?.LocationName?.Trim()
                ?? string.Empty;
            var isVoided = issue.RedemptionVoidedAtUtc != null;

            return new OfferDetailsRedemptionListItemDto
            {
                Id = $"redeemed-{issue.Id}",
                Kind = "redeemed",
                DateTimeUtc = issue.RedeemedAtUtc!.Value,
                GuestName = guestName,
                GuestId = issue.LocationGuestId,
                PassReferenceText = issue.ClaimCode,
                PassId = issue.Id.ToString(CultureInfo.InvariantCulture),
                PassCodeMasked = MaskClaimCode(issue.ClaimCode),
                LocationName = locationName,
                StaffMemberText = null,
                Outcome = isVoided ? "voided" : "redeemed",
                OutcomeLabel = isVoided ? "Voided" : "Redeemed",
                Reason = null,
                ReasonLabel = null,
                OfferVersionLabel = FormatOfferVersionLabel(issue.IssuedAtUtc),
                ExpiresAtUtc = issue.ExpiryAtUtc,
                LinkedCampaignText = FormatLinkedCampaignText(issue),
                OfferTitle = issue.Title,
            };
        }

        private static OfferDetailsRedemptionListItemDto ToFailedAttemptListItem(
            OfferRedeemFailedAttempt attempt,
            IReadOnlyDictionary<string, OfferIssue> issuesByCode
        )
        {
            OfferIssue? matchedIssue = null;
            if (!string.IsNullOrWhiteSpace(attempt.ClaimCode))
            {
                issuesByCode.TryGetValue(
                    NormalizeClaimCode(attempt.ClaimCode),
                    out matchedIssue
                );
            }

            var guestName =
                matchedIssue?.LocationGuest?.Name?.Trim() ?? string.Empty;
            var locationName =
                attempt.RestaurantLocation?.LocationName?.Trim()
                ?? matchedIssue?.LocationGuest?.RestaurantLocation?.LocationName?.Trim()
                ?? string.Empty;
            var reason = attempt.Reason?.Trim() ?? OfferRedeemFailureReasons.Invalid;
            var passReference =
                attempt.ClaimCode?.Trim()
                ?? matchedIssue?.ClaimCode
                ?? string.Empty;

            return new OfferDetailsRedemptionListItemDto
            {
                Id = $"failed-{attempt.Id}",
                Kind = "failed",
                DateTimeUtc = attempt.AttemptedAtUtc,
                GuestName = guestName,
                GuestId = matchedIssue?.LocationGuestId,
                PassReferenceText = passReference,
                PassId = matchedIssue?.Id.ToString(CultureInfo.InvariantCulture)
                    ?? $"failed-{attempt.Id}",
                PassCodeMasked = MaskClaimCode(passReference),
                LocationName = locationName,
                StaffMemberText = null,
                Outcome = reason,
                OutcomeLabel = FormatFailureOutcomeLabel(reason),
                Reason = reason,
                ReasonLabel = FormatFailureReasonLabel(reason),
                OfferVersionLabel = matchedIssue == null
                    ? "—"
                    : FormatOfferVersionLabel(matchedIssue.IssuedAtUtc),
                ExpiresAtUtc = matchedIssue?.ExpiryAtUtc,
                LinkedCampaignText = matchedIssue == null
                    ? null
                    : FormatLinkedCampaignText(matchedIssue),
                OfferTitle = matchedIssue?.Title ?? string.Empty,
            };
        }

        private static (string Status, string StatusLabel) ResolveClaimStatus(
            OfferIssue issue,
            DateTime atUtc
        )
        {
            if (issue.CancelledAtUtc != null
                || issue.RedemptionVoidedAtUtc != null)
            {
                return ("voided", "Voided");
            }

            if (issue.RedeemedAtUtc != null)
            {
                return ("redeemed", "Redeemed");
            }

            if (issue.ExpiryAtUtc <= atUtc)
            {
                return ("expired", "Expired");
            }

            return ("open", "Open");
        }

        private static string FormatIssueSourceLabel(OfferIssue issue)
        {
            if (string.Equals(
                    issue.Source,
                    OfferIssueSources.Campaign,
                    StringComparison.Ordinal
                ))
            {
                var campaignName = issue.Campaign?.Name?.Trim();
                return string.IsNullOrEmpty(campaignName)
                    ? "Campaign"
                    : campaignName;
            }

            if (string.Equals(
                    issue.Source,
                    OfferIssueSources.GuestFormThankYou,
                    StringComparison.Ordinal
                ))
            {
                return "Guest form thank-you";
            }

            return issue.Source;
        }

        private static string? FormatLinkedCampaignText(OfferIssue issue)
        {
            if (issue.CampaignId == null)
            {
                return null;
            }

            var campaignName = issue.Campaign?.Name?.Trim();
            return string.IsNullOrEmpty(campaignName)
                ? "Campaign"
                : campaignName;
        }

        private static string FormatOfferVersionLabel(DateTime issuedAtUtc)
        {
            return issuedAtUtc.ToString(
                "d MMM yyyy",
                CultureInfo.GetCultureInfo("en-GB")
            );
        }

        private static string FormatFailureOutcomeLabel(string reason)
        {
            return reason switch
            {
                OfferRedeemFailureReasons.Expired => "Expired",
                OfferRedeemFailureReasons.AlreadyUsed => "Already used",
                OfferRedeemFailureReasons.Voided => "Voided",
                OfferRedeemFailureReasons.WrongLocation => "Wrong location",
                _ => "Failed",
            };
        }

        private static string FormatFailureReasonLabel(string reason)
        {
            return reason switch
            {
                OfferRedeemFailureReasons.Expired => "Pass expired",
                OfferRedeemFailureReasons.AlreadyUsed => "Already redeemed",
                OfferRedeemFailureReasons.Voided => "Pass voided",
                OfferRedeemFailureReasons.WrongLocation => "Wrong location",
                OfferRedeemFailureReasons.Invalid => "Invalid code",
                _ => "Check failed",
            };
        }

        private static string MaskClaimCode(string code)
        {
            var trimmed = code.Trim();
            if (trimmed.Length <= 4)
            {
                return trimmed;
            }

            var suffix = trimmed[^4..];
            return $"•••• {suffix}";
        }

        private static string NormalizeClaimCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }
    }
}
