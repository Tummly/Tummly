using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class AdminAuditService : IAdminAuditService
    {
        private const int CorrelationIdMaxLength = 64;

        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public AdminAuditService(
            ApplicationDbContext context,
            TimeProvider clock,
            IHttpContextAccessor? httpContextAccessor = null
        )
        {
            _context = context;
            _clock = clock;
            _httpContextAccessor = httpContextAccessor;
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
                    Succeeded = request.Succeeded,
                    CorrelationId = CurrentCorrelationId(),
                }
            );
        }

        public async Task AppendAndSaveAsync(
            AdminAuditAppendRequest request,
            CancellationToken cancellationToken = default
        )
        {
            Append(request);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Request trace id for the current HTTP request, or null when the
        /// event is written outside a request (background jobs).
        /// </summary>
        private string? CurrentCorrelationId()
        {
            var traceIdentifier = _httpContextAccessor?.HttpContext?.TraceIdentifier;
            if (string.IsNullOrWhiteSpace(traceIdentifier))
            {
                return null;
            }

            var trimmed = traceIdentifier.Trim();
            return trimmed.Length <= CorrelationIdMaxLength
                ? trimmed
                : trimmed[..CorrelationIdMaxLength];
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

            if (!string.IsNullOrWhiteSpace(query.TargetId))
            {
                var targetId = query.TargetId.Trim();
                filtered = filtered.Where(e => e.TargetId == targetId);
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
                    CorrelationId = e.CorrelationId,
                })
                .ToListAsync(cancellationToken);

            return new AdminAuditListResult(items, totalCount);
        }
    }
}
