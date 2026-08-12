using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Offers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Offer Details Claims / Redemptions / Campaigns list reads (tickets 40–41)
    /// and location-wide redemption log (ticket 42).
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
            _ = atUtc;

            var offerExists = await _context.CatalogOffers
                .AsNoTracking()
                .AnyAsync(o => o.Id == offerId, cancellationToken);

            if (!offerExists)
            {
                return null;
            }

            return await BuildRedemptionsListAsync(
                catalogOfferId: offerId,
                locationId: null,
                cancellationToken
            );
        }

        public async Task<OfferDetailsRedemptionsListDto> ListLocationRedemptionsAsync(
            int locationId,
            CancellationToken cancellationToken = default
        )
        {
            return await BuildRedemptionsListAsync(
                catalogOfferId: null,
                locationId: locationId,
                cancellationToken
            );
        }

        private async Task<OfferDetailsRedemptionsListDto> BuildRedemptionsListAsync(
            int? catalogOfferId,
            int? locationId,
            CancellationToken cancellationToken
        )
        {
            var redeemedQuery = _context.OfferIssues
                .AsNoTracking()
                .Where(i => i.RedeemedAtUtc != null);

            if (catalogOfferId is int offerIdFilter)
            {
                redeemedQuery = redeemedQuery.Where(
                    i => i.CatalogOfferId == offerIdFilter
                );
            }
            else if (locationId is int locationFilter)
            {
                redeemedQuery = redeemedQuery.Where(
                    i =>
                        i.CatalogOffer != null
                        && i.CatalogOffer.RestaurantLocationId == locationFilter
                );
            }

            var redeemedIssues = await redeemedQuery
                .Include(i => i.LocationGuest!)
                    .ThenInclude(g => g.RestaurantLocation)
                .Include(i => i.Campaign)
                .Include(i => i.CatalogOffer)
                .ToListAsync(cancellationToken);

            var failedQuery = _context.OfferRedeemFailedAttempts.AsNoTracking();
            if (catalogOfferId is int failedOfferFilter)
            {
                failedQuery = failedQuery.Where(
                    a => a.CatalogOfferId == failedOfferFilter
                );
            }
            else if (locationId is int failedLocationFilter)
            {
                failedQuery = failedQuery.Where(
                    a => a.RestaurantLocationId == failedLocationFilter
                );
            }

            var failedAttempts = await failedQuery
                .Include(a => a.RestaurantLocation)
                .Include(a => a.CatalogOffer)
                .ToListAsync(cancellationToken);

            IQueryable<OfferIssue> issuesForMatchQuery =
                _context.OfferIssues.AsNoTracking();
            if (catalogOfferId is int matchOfferFilter)
            {
                issuesForMatchQuery = issuesForMatchQuery.Where(
                    i => i.CatalogOfferId == matchOfferFilter
                );
            }
            else if (locationId is int matchLocationFilter)
            {
                issuesForMatchQuery = issuesForMatchQuery.Where(
                    i =>
                        i.CatalogOffer != null
                        && i.CatalogOffer.RestaurantLocationId
                            == matchLocationFilter
                );
            }

            var issuesByCode = await issuesForMatchQuery
                .Include(i => i.LocationGuest!)
                    .ThenInclude(g => g.RestaurantLocation)
                .Include(i => i.Campaign)
                .Include(i => i.CatalogOffer)
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

        public async Task<OfferDetailsLinkedCampaignsListDto?> ListLinkedCampaignsAsync(
            int offerId,
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

            var campaigns = await _context.Campaigns
                .AsNoTracking()
                .Include(c => c.RestaurantLocation)
                .Where(c => c.OfferId == offerId)
                .OrderByDescending(c => c.UpdatedAt)
                .ThenByDescending(c => c.Id)
                .ToListAsync(cancellationToken);

            var campaignIds = campaigns.Select(c => c.Id).ToList();
            var issueStats = await _context.OfferIssues
                .AsNoTracking()
                .Where(
                    i =>
                        i.CatalogOfferId == offerId
                        && i.CampaignId != null
                        && campaignIds.Contains(i.CampaignId.Value)
                )
                .GroupBy(i => i.CampaignId!.Value)
                .Select(group => new
                {
                    CampaignId = group.Key,
                    PassesIssued = group.Count(),
                    Claims = group.Count(i => i.ClaimedAtUtc != null),
                    Redemptions = group.Count(
                        i =>
                            i.RedeemedAtUtc != null
                            && i.RedemptionVoidedAtUtc == null
                    ),
                })
                .ToDictionaryAsync(
                    row => row.CampaignId,
                    cancellationToken
                );

            var items = campaigns
                .Select(campaign =>
                {
                    issueStats.TryGetValue(campaign.Id, out var stats);
                    return new OfferDetailsLinkedCampaignListItemDto
                    {
                        Id = campaign.Id.ToString(CultureInfo.InvariantCulture),
                        CampaignName = campaign.Name.Trim(),
                        Status = campaign.Status,
                        StatusLabel = FormatCampaignStatusLabel(campaign.Status),
                        LocationName =
                            campaign.RestaurantLocation?.LocationName?.Trim()
                            ?? string.Empty,
                        ChannelLabel = FormatChannelLabel(campaign.Channel),
                        AudienceLabel = FormatAudienceLabel(campaign.AudienceKey),
                        OfferVersionLabel = FormatEnGbDayMonthYear(
                            campaign.CreatedAt
                        ),
                        PassesIssued = (stats?.PassesIssued ?? 0).ToString(
                            CultureInfo.InvariantCulture
                        ),
                        Claims = (stats?.Claims ?? 0).ToString(
                            CultureInfo.InvariantCulture
                        ),
                        Redemptions = (stats?.Redemptions ?? 0).ToString(
                            CultureInfo.InvariantCulture
                        ),
                        SendDateUtc = campaign.ScheduledAtUtc?.ToString("O"),
                        SendDateLabel = campaign.ScheduledAtUtc == null
                            ? "—"
                            : FormatEnGbDayMonthYear(
                                campaign.ScheduledAtUtc.Value
                            ),
                    };
                })
                .ToList();

            return new OfferDetailsLinkedCampaignsListDto { Items = items };
        }

        public async Task<OfferDetailsIssuanceSourcesListDto?> ListIssuanceSourcesAsync(
            int offerId,
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
                .Include(i => i.Campaign)
                .ToListAsync(cancellationToken);

            var items = issues
                .GroupBy(issue =>
                {
                    if (string.Equals(
                            issue.Source,
                            OfferIssueSources.Campaign,
                            StringComparison.Ordinal
                        )
                        && issue.CampaignId != null)
                    {
                        return $"campaign:{issue.CampaignId.Value}";
                    }

                    return issue.Source;
                })
                .Select(group =>
                {
                    var sample = group.First();
                    var lastIssued = group.Max(i => i.IssuedAtUtc);
                    return new OfferDetailsIssuanceSourceListItemDto
                    {
                        Id = group.Key,
                        SourceLabel = FormatIssuanceSourceCategory(sample),
                        PathLabel = FormatIssuancePathLabel(sample),
                        PassesIssued = group.Count().ToString(
                            CultureInfo.InvariantCulture
                        ),
                        LastIssuedAtUtc = lastIssued,
                        LastIssuedLabel = FormatEnGbDayMonthYear(lastIssued),
                    };
                })
                .OrderByDescending(row => row.LastIssuedAtUtc)
                .ThenBy(row => row.Id, StringComparer.Ordinal)
                .ToList();

            return new OfferDetailsIssuanceSourcesListDto { Items = items };
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
                OfferVersionLabel = FormatEnGbDayMonthYear(issue.IssuedAtUtc),
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
            var offerTitle =
                matchedIssue?.Title?.Trim()
                ?? attempt.CatalogOffer?.Title?.Trim()
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
                    : FormatEnGbDayMonthYear(matchedIssue.IssuedAtUtc),
                ExpiresAtUtc = matchedIssue?.ExpiryAtUtc,
                LinkedCampaignText = matchedIssue == null
                    ? null
                    : FormatLinkedCampaignText(matchedIssue),
                OfferTitle = offerTitle,
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

        /// <summary>
        /// Issuance Sources → Source column (channel category, not campaign name).
        /// </summary>
        private static string FormatIssuanceSourceCategory(OfferIssue issue)
        {
            if (string.Equals(
                    issue.Source,
                    OfferIssueSources.Campaign,
                    StringComparison.Ordinal
                ))
            {
                return "Campaign";
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

        /// <summary>
        /// Issuance Sources → Path column (campaign name or thank-you path).
        /// </summary>
        private static string FormatIssuancePathLabel(OfferIssue issue)
        {
            if (string.Equals(
                    issue.Source,
                    OfferIssueSources.Campaign,
                    StringComparison.Ordinal
                ))
            {
                var campaignName = issue.Campaign?.Name?.Trim();
                return string.IsNullOrEmpty(campaignName)
                    ? "—"
                    : campaignName;
            }

            if (string.Equals(
                    issue.Source,
                    OfferIssueSources.GuestFormThankYou,
                    StringComparison.Ordinal
                ))
            {
                return "Thank-you screen";
            }

            return "—";
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

        private static string FormatEnGbDayMonthYear(DateTime valueUtc)
        {
            return valueUtc.ToString(
                "d MMM yyyy",
                CultureInfo.GetCultureInfo("en-GB")
            );
        }

        private static string FormatCampaignStatusLabel(string status)
        {
            if (string.Equals(status, "draft", StringComparison.Ordinal))
            {
                return "Draft";
            }

            if (string.Equals(status, "partially-sent", StringComparison.Ordinal))
            {
                return "Partially sent";
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                return "—";
            }

            return char.ToUpperInvariant(status[0]) + status[1..];
        }

        private static string FormatChannelLabel(string? channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return "—";
            }

            return channel.Trim().ToUpperInvariant();
        }

        private static string FormatAudienceLabel(string? audienceKey)
        {
            if (string.IsNullOrWhiteSpace(audienceKey))
            {
                return "—";
            }

            var key = audienceKey.Trim();
            return key switch
            {
                "all-eligible-guests" => "All eligible guests",
                "new-guests" => "New guests",
                "positive-feedback" => "Positive feedback",
                "offer-not-redeemed" => "Offer not redeemed",
                "recent-redeemers" => "Recent redeemers",
                "no-recent-tummly-activity" => "No recent Tummly activity",
                "completed-recovery-follow-up" => "Completed recovery follow-up",
                "dormant-guests" => "Dormant guests",
                _ => HumanizeKey(key),
            };
        }

        private static string HumanizeKey(string key)
        {
            var parts = key.Split('-', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(
                " ",
                parts.Select(part =>
                    part.Length == 0
                        ? part
                        : char.ToUpperInvariant(part[0]) + part[1..]
                )
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
