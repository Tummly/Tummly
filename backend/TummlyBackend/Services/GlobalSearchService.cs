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
                return EmptyGuestsGroup();
            }

            var scoped = GuestsListQueryComposer.ScopeToLocations(
                _context.LocationGuests
                    .AsNoTracking()
                    .Include(lg => lg.MasterGuest)
                    .Include(lg => lg.RestaurantLocation),
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
                    lg.RestaurantLocationId,
                    lg.RestaurantLocation!.LocationName
                ))
                .ToListAsync(cancellationToken);

            var term = trimmed.ToLowerInvariant();
            var hits = matched
                .OrderBy(row => RankGuest(row.Name, term))
                .ThenByDescending(row => row.CreatedAt)
                .ThenByDescending(row => row.Id)
                .Take(query.Limit)
                .Select(row => ToGuestHit(row))
                .ToList();

            return new GlobalSearchGroupDto
            {
                Type = "guests",
                Hits = hits,
            };
        }

        private static GlobalSearchGroupDto EmptyGuestsGroup()
        {
            return new GlobalSearchGroupDto
            {
                Type = "guests",
                Hits = [],
            };
        }

        private static int RankGuest(string name, string termLower)
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

        private static GlobalSearchHitDto ToGuestHit(GuestMatchRow row)
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
                LocationName = row.LocationName,
                Status = status,
            };
        }

        private sealed record GuestMatchRow(
            int Id,
            string Name,
            string? Email,
            string? Mobile,
            LocationGuestMarketingPreference MarketingPreference,
            DateTime CreatedAt,
            int LocationId,
            string LocationName
        );
    }
}
