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
            if (trimmed.Length < 2)
            {
                return EmptyGroup("guests");
            }

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests
                    .AsNoTracking()
                    .Include(lg => lg.MasterGuest),
                [query.LocationId]
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
                .Select(row => ToGuestHit(row, query.LocationName))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "guests",
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

            var term = trimmed.ToLowerInvariant();
            var matched = await _context.Campaigns
                .AsNoTracking()
                .Where(campaign =>
                    campaign.RestaurantLocationId == query.LocationId
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
                .Select(row => ToCampaignHit(row, query.LocationName))
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

            var term = trimmed.ToLowerInvariant();
            // Same match fields as Offers list: title or attached campaign name.
            var matched = await _context.CatalogOffers
                .AsNoTracking()
                .Where(offer =>
                    offer.RestaurantLocationId == query.LocationId
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
                .Select(row => ToOfferHit(row, query.LocationName, today))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "offers",
                Hits = hits,
            };
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

        private static GlobalSearchHitDto ToGuestHit(
            GuestMatchRow row,
            string locationName
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
                LocationName = locationName,
                Status = status,
            };
        }

        private static GlobalSearchHitDto ToCampaignHit(
            CampaignMatchRow row,
            string locationName
        )
        {
            return new GlobalSearchHitDto
            {
                Id = row.Id.ToString(),
                EntityType = "campaign",
                Title = row.Name,
                Subtitle = FormatChannel(row.Channel),
                LocationId = row.LocationId,
                LocationName = locationName,
                Status = FormatCampaignStatus(row.Status),
            };
        }

        private static GlobalSearchHitDto ToOfferHit(
            OfferMatchRow row,
            string locationName,
            DateOnly venueLocalToday
        )
        {
            var effective = CatalogOfferStatus.ResolveEffectiveStatus(
                row.StoredStatus,
                row.Validity,
                row.CustomExpiryDate,
                venueLocalToday
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
    }
}
