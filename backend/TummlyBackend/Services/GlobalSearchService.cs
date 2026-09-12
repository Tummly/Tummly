using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Search;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class GlobalSearchService : IGlobalSearchService
    {
        private const int CommentExcerptMaxLength = 120;

        private static readonly (string Key, string Label)[] FeedbackSearchTagLabels =
            Enum.GetValues<DetectedTag>()
                .Select(tag => (tag.ToString(), DetectedTagLabels.For(tag)))
                .ToArray();

        private readonly ApplicationDbContext _context;

        public GlobalSearchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GlobalSearchResponse> SearchAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var groups = new List<GlobalSearchGroupDto>();

            if (query.IncludeGuests)
            {
                groups.Add(
                    await SearchGuestsAsync(query, cancellationToken)
                );
            }

            if (query.IncludeFeedback)
            {
                groups.Add(
                    await SearchFeedbacksAsync(query, cancellationToken)
                );
            }

            if (query.IncludeCampaigns)
            {
                groups.Add(
                    await SearchCampaignsAsync(query, cancellationToken)
                );
            }

            if (query.IncludeOffers)
            {
                groups.Add(
                    await SearchOffersAsync(query, cancellationToken)
                );
            }

            if (query.IncludeQrCodes)
            {
                groups.Add(
                    await SearchQrCodesAsync(query, cancellationToken)
                );
            }

            return new GlobalSearchResponse
            {
                Success = true,
                Q = query.Q,
                LocationId = query.LocationId,
                Groups = groups,
            };
        }

        private async Task<GlobalSearchGroupDto> SearchGuestsAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken
        )
        {
            var trimmed = query.Q.Trim();
            if (trimmed.Length < 2 || query.LocationIds.Count == 0)
            {
                return EmptyGroup("guests");
            }

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests
                    .AsNoTracking()
                    .Include(lg => lg.MasterGuest),
                query.LocationIds
            );

            var matched = await GuestsListQueryComposer
                .ApplySearch(scoped, trimmed)
                .Select(lg => new GuestMatchRow(
                    lg.Id,
                    lg.Name,
                    lg.MasterGuest!.Email,
                    lg.MasterGuest.Mobile,
                    lg.MarketingPreference,
                    lg.CreatedAt,
                    lg.RestaurantLocationId
                ))
                .ToListAsync(cancellationToken);

            var term = trimmed.ToLowerInvariant();
            var hits = matched
                .OrderBy(row => RankName(row.Name, term))
                .ThenByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .Take(query.Limit)
                .Select(row => ToGuestHit(row, query.LocationNamesById))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "guests",
                Hits = hits,
            };
        }

        private async Task<GlobalSearchGroupDto> SearchFeedbacksAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken
        )
        {
            var trimmed = query.Q.Trim();
            var hasIdentity = TryParseFeedbackIdentity(trimmed, out var identityId);
            if (trimmed.Length < 2 && !hasIdentity)
            {
                return EmptyGroup("feedback");
            }

            if (query.LocationIds.Count == 0)
            {
                return EmptyGroup("feedback");
            }

            var locationIdSet = query.LocationIds.ToHashSet();
            var scoped = _context.Feedbacks
                .AsNoTracking()
                .Where(f => locationIdSet.Contains(f.RestaurantLocationId));

            var matched = await ApplyFeedbackSearch(scoped, trimmed)
                .Select(f => new FeedbackMatchRow(
                    f.Id,
                    f.GuestName,
                    f.Comment,
                    f.WorkflowStatus,
                    f.CreatedAt,
                    f.RestaurantLocationId
                ))
                .ToListAsync(cancellationToken);

            var term = trimmed.ToLowerInvariant();
            var hits = matched
                .OrderBy(row =>
                    RankFeedback(row, term, hasIdentity ? identityId : null)
                )
                .ThenByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .Take(query.Limit)
                .Select(row => ToFeedbackHit(row, query.LocationNamesById))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "feedback",
                Hits = hits,
            };
        }

        private async Task<GlobalSearchGroupDto> SearchCampaignsAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken
        )
        {
            var trimmed = query.Q.Trim();
            if (trimmed.Length < 2)
            {
                return EmptyGroup("campaigns");
            }

            if (query.LocationIds.Count == 0)
            {
                return EmptyGroup("campaigns");
            }

            var term = trimmed.ToLowerInvariant();
            var locationIdSet = query.LocationIds.ToHashSet();
            var matched = await _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    locationIdSet.Contains(campaign.RestaurantLocationId)
                    && campaign.Name.ToLower().Contains(term)
                )
                .Select(campaign => new CampaignMatchRow(
                    campaign.Id,
                    campaign.Name,
                    campaign.Status,
                    campaign.Channel,
                    campaign.UpdatedAt,
                    campaign.RestaurantLocationId
                ))
                .ToListAsync(cancellationToken);

            var hits = matched
                .OrderBy(row => RankName(row.Name, term))
                .ThenByDescending(row => row.UpdatedAt)
                .ThenByDescending(row => row.Id)
                .Take(query.Limit)
                .Select(row => ToCampaignHit(row, query.LocationNamesById))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "campaigns",
                Hits = hits,
            };
        }

        private async Task<GlobalSearchGroupDto> SearchOffersAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken
        )
        {
            var trimmed = query.Q.Trim();
            if (trimmed.Length < 2)
            {
                return EmptyGroup("offers");
            }

            if (query.LocationIds.Count == 0)
            {
                return EmptyGroup("offers");
            }

            var term = trimmed.ToLowerInvariant();
            var locationIdSet = query.LocationIds.ToHashSet();
            // Same match fields as Offers list: title or attached campaign name.
            var matched = await _context.CatalogOffers
                .AsNoTracking()
                .Where(offer =>
                    locationIdSet.Contains(offer.RestaurantLocationId)
                    && (
                        offer.Title.ToLower().Contains(term)
                        || _context.Campaigns.Any(campaign =>
                            campaign.OfferId == offer.Id
                            && campaign.Name.ToLower().Contains(term)
                        )
                    )
                )
                .Select(offer => new OfferMatchRow(
                    offer.Id,
                    offer.Title,
                    offer.Status,
                    offer.Validity,
                    offer.CustomExpiryDate,
                    offer.UpdatedAt,
                    offer.RestaurantLocationId
                ))
                .ToListAsync(cancellationToken);

            var today = CatalogOfferStatus.VenueLocalToday(
                DateTime.UtcNow,
                query.UtcOffsetMinutes
            );
            var hits = matched
                .OrderBy(row => RankName(row.Title, term))
                .ThenByDescending(row => row.UpdatedAt)
                .ThenByDescending(row => row.Id)
                .Take(query.Limit)
                .Select(row => ToOfferHit(row, query.LocationNamesById, today))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "offers",
                Hits = hits,
            };
        }

        private async Task<GlobalSearchGroupDto> SearchQrCodesAsync(
            GlobalSearchQuery query,
            CancellationToken cancellationToken
        )
        {
            var trimmed = query.Q.Trim();
            if (trimmed.Length < 2)
            {
                return EmptyGroup("qr-codes");
            }

            if (query.LocationIds.Count == 0)
            {
                return EmptyGroup("qr-codes");
            }

            var locationIdSet = query.LocationIds.ToHashSet();
            var candidates = await _context.QrCodes
                .AsNoTracking()
                .Where(q =>
                    locationIdSet.Contains(q.RestaurantLocationId)
                    && (q.Status == QrCodeStatus.Active
                        || q.Status == QrCodeStatus.Paused)
                )
                .Select(q => new QrMatchRow(
                    q.Id,
                    q.QrType,
                    q.LinkName,
                    q.Status,
                    q.CreatedAt,
                    q.RestaurantLocationId
                ))
                .ToListAsync(cancellationToken);

            var term = trimmed.ToLowerInvariant();
            var hits = candidates
                .Select(row => new
                {
                    Row = row,
                    Title = ResolveQrTitle(row),
                })
                .Where(item =>
                    item.Title.ToLowerInvariant().Contains(term)
                )
                .OrderBy(item => RankName(item.Title, term))
                .ThenByDescending(item => item.Row.CreatedAt)
                .ThenByDescending(item => item.Row.Id)
                .Take(query.Limit)
                .Select(item =>
                    ToQrHit(item.Row, item.Title, query.LocationNamesById)
                )
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "qr-codes",
                Hits = hits,
            };
        }

        /// <summary>
        /// Mirrors FeedbackInboxListService.ApplySearch for comment / guest /
        /// governed tags, plus numeric / FDB- identity matching for Global Search.
        /// </summary>
        private static IQueryable<Feedback> ApplyFeedbackSearch(
            IQueryable<Feedback> query,
            string needle
        )
        {
            var matchingTagKeys = FeedbackSearchTagLabels
                .Where(pair =>
                    pair.Label.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || pair.Key.Contains(needle, StringComparison.OrdinalIgnoreCase)
                )
                .Select(pair => pair.Key)
                .ToList();

            var hasIdentity = TryParseFeedbackIdentity(needle, out var identityId);
            var term = needle.ToLowerInvariant();

            return query.Where(f =>
                (hasIdentity && f.Id == identityId)
                || f.Comment.ToLower().Contains(term)
                || f.GuestName.ToLower().Contains(term)
                || (
                    f.ClassificationStatus == ClassificationStatus.Succeeded
                    && f.DetectedTagsJson != null
                    && (
                        matchingTagKeys.Contains(nameof(DetectedTag.FoodQuality))
                            && f.DetectedTagsJson.Contains("\"FoodQuality\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Service))
                            && f.DetectedTagsJson.Contains("\"Service\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.WaitTime))
                            && f.DetectedTagsJson.Contains("\"WaitTime\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.Cleanliness)
                            )
                            && f.DetectedTagsJson.Contains("\"Cleanliness\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Value))
                            && f.DetectedTagsJson.Contains("\"Value\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.Atmosphere)
                            )
                            && f.DetectedTagsJson.Contains("\"Atmosphere\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Billing))
                            && f.DetectedTagsJson.Contains("\"Billing\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.AllergiesDietary)
                            )
                            && f.DetectedTagsJson.Contains("\"AllergiesDietary\"")
                        || matchingTagKeys.Contains(
                                nameof(DetectedTag.BookingSeating)
                            )
                            && f.DetectedTagsJson.Contains("\"BookingSeating\"")
                        || matchingTagKeys.Contains(nameof(DetectedTag.Other))
                            && f.DetectedTagsJson.Contains("\"Other\"")
                    )
                )
            );
        }

        private static bool TryParseFeedbackIdentity(
            string needle,
            out int identityId
        )
        {
            var trimmed = needle.Trim();
            if (int.TryParse(trimmed, out var numericId) && numericId > 0)
            {
                identityId = numericId;
                return true;
            }

            if (
                trimmed.StartsWith("FDB-", StringComparison.OrdinalIgnoreCase)
                && trimmed.Length > 4
                && int.TryParse(trimmed.AsSpan(4), out var fdbId)
                && fdbId > 0
            )
            {
                identityId = fdbId;
                return true;
            }

            identityId = 0;
            return false;
        }

        private static GlobalSearchGroupDto EmptyGroup(string type)
        {
            return new GlobalSearchGroupDto
            {
                Type = type,
                Hits = [],
            };
        }

        private static int RankName(string name, string termLower)
        {
            var nameLower = name.ToLowerInvariant();
            if (nameLower == termLower)
            {
                return 0;
            }

            if (nameLower.StartsWith(termLower, StringComparison.Ordinal))
            {
                return 1;
            }

            return 2;
        }

        private static int RankFeedback(
            FeedbackMatchRow row,
            string termLower,
            int? identityId
        )
        {
            if (identityId != null && row.Id == identityId.Value)
            {
                return 0;
            }

            var nameLower = row.GuestName.ToLowerInvariant();
            if (nameLower == termLower)
            {
                return 0;
            }

            if (nameLower.StartsWith(termLower, StringComparison.Ordinal))
            {
                return 1;
            }

            return 2;
        }

        private static GlobalSearchHitDto ToGuestHit(
            GuestMatchRow row,
            IReadOnlyDictionary<int, string> locationNamesById
        )
        {
            var subtitle = !string.IsNullOrWhiteSpace(row.Email)
                ? row.Email
                : !string.IsNullOrWhiteSpace(row.Mobile)
                    ? row.Mobile
                    : null;

            var status = LocationGuestProjections.DeriveMarketingStatus(
                row.MarketingPreference,
                row.Email,
                row.Mobile
            );

            return new GlobalSearchHitDto
            {
                Id = row.Id.ToString(),
                EntityType = "guest",
                Title = row.Name,
                Subtitle = subtitle,
                LocationId = row.LocationId,
                LocationName = ResolveLocationName(
                    row.LocationId,
                    locationNamesById
                ),
                Status = status,
            };
        }

        private static string ResolveLocationName(
            int locationId,
            IReadOnlyDictionary<int, string> locationNamesById
        )
        {
            return locationNamesById.TryGetValue(locationId, out var name)
                ? name
                : string.Empty;
        }

        private static GlobalSearchHitDto ToFeedbackHit(
            FeedbackMatchRow row,
            IReadOnlyDictionary<int, string> locationNamesById
        )
        {
            var title = !string.IsNullOrWhiteSpace(row.GuestName)
                ? row.GuestName
                : TruncateComment(row.Comment);
            var locationName = ResolveLocationName(
                row.LocationId,
                locationNamesById
            );

            return new GlobalSearchHitDto
            {
                Id = row.Id.ToString(),
                EntityType = "feedback",
                Title = title,
                Subtitle = TruncateComment(row.Comment),
                LocationId = row.LocationId,
                LocationName = locationName,
                Status = FeedbackWorkflowStatusMapping.ToOperatorLabel(
                    row.WorkflowStatus
                ),
            };
        }

        private static GlobalSearchHitDto ToCampaignHit(
            CampaignMatchRow row,
            IReadOnlyDictionary<int, string> locationNamesById
        )
        {
            return new GlobalSearchHitDto
            {
                Id = row.Id.ToString(),
                EntityType = "campaign",
                Title = row.Name,
                Subtitle = FormatChannel(row.Channel),
                LocationId = row.LocationId,
                LocationName = ResolveLocationName(
                    row.LocationId,
                    locationNamesById
                ),
                Status = FormatCampaignStatus(row.Status),
            };
        }

        private static GlobalSearchHitDto ToOfferHit(
            OfferMatchRow row,
            IReadOnlyDictionary<int, string> locationNamesById,
            DateOnly venueLocalToday
        )
        {
            var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                row.StoredStatus,
                row.Validity,
                row.CustomExpiryDate,
                venueLocalToday
            );
            var locationName = ResolveLocationName(
                row.LocationId,
                locationNamesById
            );

            return new GlobalSearchHitDto
            {
                Id = row.Id.ToString(),
                EntityType = "offer",
                Title = row.Title,
                Subtitle = locationName,
                LocationId = row.LocationId,
                LocationName = locationName,
                Status = FormatOfferStatusLabel(effective),
            };
        }

        private static string ResolveQrTitle(QrMatchRow row)
        {
            return FeedbackQrSourceMapping.ToDisplay(
                    new QrCode
                    {
                        QrType = row.QrType,
                        LinkName = row.LinkName,
                    }
                )
                ?? row.QrType.ToString();
        }

        private static GlobalSearchHitDto ToQrHit(
            QrMatchRow row,
            string title,
            IReadOnlyDictionary<int, string> locationNamesById
        )
        {
            return new GlobalSearchHitDto
            {
                Id = row.Id.ToString(),
                EntityType = "qr-code",
                Title = title,
                Subtitle = null,
                LocationId = row.LocationId,
                LocationName = ResolveLocationName(
                    row.LocationId,
                    locationNamesById
                ),
                Status = row.Status.ToString(),
            };
        }

        private static string TruncateComment(string comment)
        {
            var trimmed = comment.Trim();
            if (trimmed.Length <= CommentExcerptMaxLength)
            {
                return trimmed;
            }

            return trimmed[..CommentExcerptMaxLength].TrimEnd() + "…";
        }

        private static string FormatOfferStatusLabel(string effectiveStatus)
            => effectiveStatus switch
            {
                CatalogOfferStatus.Draft => "Draft",
                CatalogOfferStatus.Active => "Active",
                CatalogOfferStatus.Paused => "Paused",
                CatalogOfferStatus.Expired => "Expired",
                CatalogOfferStatus.Archived => "Archived",
                _ => effectiveStatus,
            };

        private static string? FormatChannel(string? channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
            {
                return null;
            }

            if (string.Equals(channel, "email", StringComparison.OrdinalIgnoreCase))
            {
                return "Email";
            }

            if (string.Equals(channel, "sms", StringComparison.OrdinalIgnoreCase))
            {
                return "SMS";
            }

            return channel;
        }

        /// <summary>
        /// Operator-facing lifecycle labels only — no billing/reserve internals.
        /// </summary>
        private static string FormatCampaignStatus(string status)
        {
            if (string.Equals(status, "partially-sent", StringComparison.Ordinal))
            {
                return "Partially sent";
            }

            if (status.Length == 0)
            {
                return "—";
            }

            return char.ToUpperInvariant(status[0]) + status[1..];
        }

        private sealed record GuestMatchRow(
            int Id,
            string Name,
            string? Email,
            string? Mobile,
            LocationGuestMarketingPreference MarketingPreference,
            DateTime CreatedAt,
            int LocationId
        );

        private sealed record FeedbackMatchRow(
            int Id,
            string GuestName,
            string Comment,
            FeedbackWorkflowStatus WorkflowStatus,
            DateTime CreatedAt,
            int LocationId
        );

        private sealed record CampaignMatchRow(
            int Id,
            string Name,
            string Status,
            string? Channel,
            DateTime UpdatedAt,
            int LocationId
        );

        private sealed record OfferMatchRow(
            int Id,
            string Title,
            string StoredStatus,
            CatalogOfferValidity Validity,
            DateOnly? CustomExpiryDate,
            DateTime UpdatedAt,
            int LocationId
        );

        private sealed record QrMatchRow(
            int Id,
            QrType QrType,
            string? LinkName,
            QrCodeStatus Status,
            DateTime CreatedAt,
            int LocationId
        );
    }
}
