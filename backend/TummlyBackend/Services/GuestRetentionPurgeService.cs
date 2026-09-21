using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class GuestRetentionPurgeService : IGuestRetentionPurgeService
    {
        private const int BatchCap = 25;
        private const string ActorIdentity = "system:retention";

        private readonly ApplicationDbContext _context;
        private readonly IAdminAuditService _audit;
        private readonly ILogger<GuestRetentionPurgeService> _logger;

        public GuestRetentionPurgeService(
            ApplicationDbContext context,
            IAdminAuditService audit,
            ILogger<GuestRetentionPurgeService> logger
        )
        {
            _context = context;
            _audit = audit;
            _logger = logger;
        }

        public async Task<GuestRetentionPurgeBatchResult> ProcessOnceAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default
        )
        {
            var dueBeforeUtc = nowUtc.AddDays(
                -GuestRetentionEligibility.GuestRetentionDays
            );

            var restaurantIds = await _context.BillingAccounts
                .AsNoTracking()
                .Where(ba =>
                    ba.BillingStatus == BillingStatuses.Dormant
                    && ba.DormantEnteredAt != null
                    && ba.GuestRetentionPurgedAtUtc == null
                    && ba.DormantEnteredAt <= dueBeforeUtc
                )
                .OrderBy(ba => ba.DormantEnteredAt)
                .Select(ba => ba.RestaurantId)
                .Take(BatchCap)
                .ToListAsync(cancellationToken);

            var purged = 0;
            var failed = 0;
            var guestsDeleted = 0;

            foreach (var restaurantId in restaurantIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var deleted = await PurgeRestaurantAsync(
                        restaurantId,
                        nowUtc,
                        cancellationToken
                    );
                    if (deleted < 0)
                    {
                        continue;
                    }

                    purged++;
                    guestsDeleted += deleted;
                }
                catch (Exception ex)
                {
                    failed++;
                    _context.ChangeTracker.Clear();
                    _logger.LogError(
                        ex,
                        "Guest retention purge failed for restaurant {RestaurantId}",
                        restaurantId
                    );
                }
            }

            return new GuestRetentionPurgeBatchResult(
                EligibleFound: restaurantIds.Count,
                Purged: purged,
                Failed: failed,
                GuestsDeleted: guestsDeleted
            );
        }

        /// <summary>
        /// Returns guests deleted, or -1 when the restaurant was skipped
        /// (no longer eligible).
        /// </summary>
        private async Task<int> PurgeRestaurantAsync(
            int restaurantId,
            DateTime nowUtc,
            CancellationToken cancellationToken
        )
        {
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                var ba = await _context.BillingAccounts.FirstOrDefaultAsync(
                    row => row.RestaurantId == restaurantId,
                    cancellationToken
                );

                if (ba == null || !GuestRetentionEligibility.IsEligible(ba, nowUtc))
                {
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }

                    return -1;
                }

                var guestIds = await _context.LocationGuests
                    .Where(lg => lg.RestaurantLocation!.RestaurantId == restaurantId)
                    .Select(lg => lg.Id)
                    .ToListAsync(cancellationToken);

                var deleted = 0;
                foreach (var guestId in guestIds)
                {
                    var guest = await _context.LocationGuests.FirstOrDefaultAsync(
                        lg => lg.Id == guestId,
                        cancellationToken
                    );
                    if (guest == null)
                    {
                        continue;
                    }

                    await LocationGuestHardDelete.ApplyAsync(
                        _context,
                        guest,
                        cancellationToken
                    );
                    deleted++;
                }

                ba.GuestRetentionPurgedAtUtc = nowUtc;

                _audit.Append(
                    new AdminAuditAppendRequest(
                        Action: AdminAuditActions.RetentionGuestPurge,
                        ActorIdentity: ActorIdentity,
                        TargetType: AdminAuditTargetTypes.Restaurant,
                        TargetId: restaurantId.ToString(),
                        RestaurantId: restaurantId,
                        DetailJson: $"{{\"guestsDeleted\":{deleted}}}"
                    )
                );

                await _context.SaveChangesAsync(cancellationToken);

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                return deleted;
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                throw;
            }
        }
    }
}
