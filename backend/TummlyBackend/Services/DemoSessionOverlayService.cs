using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Session-bound overlay store for public product-demo mutations (§15).
    /// </summary>
    public sealed class DemoSessionOverlayService
    {
        private readonly ApplicationDbContext _db;

        public DemoSessionOverlayService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task UpsertAsync(
            Guid sessionId,
            string entityType,
            string entityKey,
            object payload,
            CancellationToken cancellationToken = default
        )
        {
            var expires = DateTime.UtcNow.Add(DemoSessionClaims.DefaultTtl);
            var json = JsonSerializer.Serialize(payload);
            var existing = await _db.DemoSessionOverlays.FirstOrDefaultAsync(
                row =>
                    row.SessionId == sessionId
                    && row.EntityType == entityType
                    && row.EntityKey == entityKey,
                cancellationToken
            );

            if (existing is null)
            {
                _db.DemoSessionOverlays.Add(
                    new DemoSessionOverlay
                    {
                        SessionId = sessionId,
                        EntityType = entityType,
                        EntityKey = entityKey,
                        PayloadJson = json,
                        CreatedAtUtc = DateTime.UtcNow,
                        ExpiresAtUtc = expires,
                    }
                );
            }
            else
            {
                existing.PayloadJson = json;
                existing.ExpiresAtUtc = expires;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> PurgeExpiredAsync(
            CancellationToken cancellationToken = default
        )
        {
            var now = DateTime.UtcNow;
            var overlays = await _db.DemoSessionOverlays
                .Where(row => row.ExpiresAtUtc < now)
                .ExecuteDeleteAsync(cancellationToken);
            var sessions = await _db.DemoSessionRegistries
                .Where(row => row.ExpiresAtUtc < now)
                .ExecuteDeleteAsync(cancellationToken);
            return overlays + sessions;
        }
    }
}
