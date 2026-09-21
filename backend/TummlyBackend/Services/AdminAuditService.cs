using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminAuditService : IAdminAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;

        public AdminAuditService(
            ApplicationDbContext context,
            TimeProvider clock
        )
        {
            _context = context;
            _clock = clock;
        }

        public void Append(AdminAuditAppendRequest request)
        {
            _context.AdminAuditEvents.Add(
                new AdminAuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = _clock.GetUtcNow().UtcDateTime,
                    Action = request.Action,
                    ActorAdminUserId = request.ActorAdminUserId,
                    ActorIdentity = request.ActorIdentity,
                    TargetType = request.TargetType,
                    TargetId = request.TargetId,
                    RestaurantId = request.RestaurantId,
                    DetailJson = request.DetailJson,
                    Succeeded = true,
                }
            );
        }

        public async Task<AdminAuditListResult> ListAsync(
            AdminAuditListQuery query,
            CancellationToken cancellationToken = default
        )
        {
            var take = Math.Clamp(query.Take, 1, 100);
            var skip = Math.Max(0, query.Skip);

            var filtered = _context.AdminAuditEvents.AsQueryable();

            if (!string.IsNullOrEmpty(query.Action))
            {
                // Ordinal equality; EF translates == for Action filter.
                filtered = filtered.Where(e => e.Action == query.Action);
            }

            if (query.RestaurantId is int restaurantId)
            {
                filtered = filtered.Where(e => e.RestaurantId == restaurantId);
            }

            if (query.FromUtc is DateTime fromUtc)
            {
                filtered = filtered.Where(e => e.OccurredAtUtc >= fromUtc);
            }

            if (query.ToUtc is DateTime toUtc)
            {
                filtered = filtered.Where(e => e.OccurredAtUtc <= toUtc);
            }

            var totalCount = await filtered.CountAsync(cancellationToken);

            var items = await filtered
                .OrderByDescending(e => e.OccurredAtUtc)
                .Skip(skip)
                .Take(take)
                .Select(e => new AdminAuditEventDto
                {
                    Id = e.Id,
                    OccurredAtUtc = e.OccurredAtUtc,
                    Action = e.Action,
                    ActorAdminUserId = e.ActorAdminUserId,
                    ActorIdentity = e.ActorIdentity,
                    TargetType = e.TargetType,
                    TargetId = e.TargetId,
                    RestaurantId = e.RestaurantId,
                    DetailJson = e.DetailJson,
                    Succeeded = e.Succeeded,
                })
                .ToListAsync(cancellationToken);

            return new AdminAuditListResult(items, totalCount);
        }
    }
}
