using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminPremisesReviewService : IAdminPremisesReviewService
    {
        public const int ReasonMaxLength = 500;
        private const int ListLimit = 200;

        private readonly ApplicationDbContext _context;
        private readonly IPremisesReviewService _premises;
        private readonly IAdminAuditService _audit;

        public AdminPremisesReviewService(
            ApplicationDbContext context,
            IPremisesReviewService premises,
            IAdminAuditService audit
        )
        {
            _context = context;
            _premises = premises;
            _audit = audit;
        }

        public async Task<IReadOnlyList<AdminPremisesReviewItemDto>> ListPendingAsync(
            CancellationToken cancellationToken = default
        )
        {
            var pending = await _context.RestaurantLocations
                .Where(row => row.PremisesReviewStatus == PremisesReviewStatus.PendingReview)
                .OrderBy(row => row.CreatedAt)
                .ThenBy(row => row.Id)
                .Take(ListLimit)
                .Join(
                    _context.Restaurants,
                    location => location.RestaurantId,
                    restaurant => restaurant.Id,
                    (location, restaurant) => new { Location = location, restaurant.Name }
                )
                .ToListAsync(cancellationToken);

            var items = new List<AdminPremisesReviewItemDto>();
            foreach (var row in pending)
            {
                var location = row.Location;
                var check = await _premises.CheckAsync(
                    new PremisesCheckInput(
                        location.RestaurantId,
                        location.Address,
                        location.City,
                        location.Postcode,
                        location.FsaEstablishmentId,
                        ExcludeLocationId: location.Id
                    ),
                    cancellationToken
                );
                items.Add(
                    new AdminPremisesReviewItemDto
                    {
                        LocationId = location.Id,
                        RestaurantId = location.RestaurantId,
                        WorkspaceName = row.Name,
                        LocationName = location.LocationName,
                        Address = location.Address,
                        City = location.City,
                        Postcode = location.Postcode,
                        FsaEstablishmentId = location.FsaEstablishmentId,
                        CreatedAtUtc = location.CreatedAt,
                        Matches = await DescribeMatchesAsync(
                            check.Matches,
                            cancellationToken
                        ),
                    }
                );
            }

            // The check may have filled the key of older rows. Keep it.
            await _context.SaveChangesAsync(cancellationToken);
            return items;
        }

        public async Task<AdminPremisesReviewResolveResult> ResolveAsync(
            int locationId,
            bool clear,
            string? reason,
            int adminUserId,
            string adminIdentity,
            CancellationToken cancellationToken = default
        )
        {
            var trimmed = reason?.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.Length > ReasonMaxLength)
            {
                return new AdminPremisesReviewResolveResult.ReasonRequired();
            }

            var location = await _context.RestaurantLocations.FirstOrDefaultAsync(
                row => row.Id == locationId,
                cancellationToken
            );
            if (location == null)
            {
                return new AdminPremisesReviewResolveResult.NotFound();
            }

            if (location.PremisesReviewStatus != PremisesReviewStatus.PendingReview)
            {
                return new AdminPremisesReviewResolveResult.NotPending();
            }

            location.PremisesReviewStatus = clear
                ? PremisesReviewStatus.Cleared
                : PremisesReviewStatus.Rejected;
            location.PremisesReviewedAtUtc = DateTime.UtcNow;
            location.PremisesReviewedByAdminUserId = adminUserId;
            location.PremisesReviewReason = trimmed;

            _audit.Append(
                new AdminAuditAppendRequest(
                    clear
                        ? AdminAuditActions.LocationPremisesCleared
                        : AdminAuditActions.LocationPremisesRejected,
                    adminIdentity,
                    AdminAuditTargetTypes.RestaurantLocation,
                    location.Id.ToString(),
                    ActorAdminUserId: adminUserId,
                    RestaurantId: location.RestaurantId,
                    DetailJson: JsonSerializer.Serialize(new { reason = trimmed })
                )
            );
            await _context.SaveChangesAsync(cancellationToken);
            return new AdminPremisesReviewResolveResult.Resolved();
        }

        private async Task<IReadOnlyList<AdminPremisesReviewMatchDto>> DescribeMatchesAsync(
            IReadOnlyList<PremisesMatch> matches,
            CancellationToken cancellationToken
        )
        {
            if (matches.Count == 0)
            {
                return [];
            }

            var locationIds = matches.Select(m => m.LocationId).ToList();
            var workspaceIds = matches.Select(m => m.RestaurantId).Distinct().ToList();
            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => locationIds.Contains(row.Id))
                .ToDictionaryAsync(row => row.Id, cancellationToken);
            var workspaces = await _context.Restaurants
                .AsNoTracking()
                .Where(row => workspaceIds.Contains(row.Id))
                .ToDictionaryAsync(row => row.Id, row => row.Name, cancellationToken);

            return matches
                .Select(match =>
                {
                    // A matched Location may be gone (a Starter Kit order keeps
                    // the shipped address). The ids still identify it.
                    locations.TryGetValue(match.LocationId, out var location);
                    return new AdminPremisesReviewMatchDto
                    {
                        LocationId = match.LocationId,
                        RestaurantId = match.RestaurantId,
                        WorkspaceName = workspaces.GetValueOrDefault(match.RestaurantId, string.Empty),
                        LocationName = location?.LocationName,
                        Address = location?.Address,
                        City = location?.City,
                        Postcode = location?.Postcode,
                        LifecycleStatus = location == null
                            ? null
                            : LocationRowWire.ToLifecycleWire(location.LifecycleStatus),
                        SameWorkspace = match.SameWorkspace,
                        PremisesKeyMatch = match.PremisesKeyMatch,
                        FsaMatch = match.FsaMatch,
                        PriorPilot = match.PriorPilot,
                        PriorStarterKit = match.PriorStarterKit,
                    };
                })
                .ToList();
        }
    }
}
