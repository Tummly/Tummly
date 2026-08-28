using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class BillingAccountLifecycleService : IBillingAccountLifecycle
    {
        public const string PilotRefuseReason = "pilot";

        public const string ChargebackRefuseReason = "chargeback";

        public const int PilotDormantHours = 15 * 24;

        public const int DunningSoftLockHours = 10 * 24;

        public const int DunningDormantHours = 24 * 24;

        public static readonly int[] DunningDaySteps = [0, 3, 7, 10, 24];

        private readonly ApplicationDbContext _context;
        private readonly IBillingAccountNoticeNotifier _notifier;

        public BillingAccountLifecycleService(
            ApplicationDbContext context,
            IBillingAccountNoticeNotifier notifier
        )
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task TickAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        )
        {
            var pending = await MutateAsync(
                restaurantId,
                cancellationToken,
                async account =>
                {
                    await EnsurePilotPeriodEndAsync(account, cancellationToken);
                    var events = new List<LifecycleEvent>();
                    AdvanceUnpaidPilot(account, now, events);
                    AdvanceDunning(account, now, events);
                    return events;
                }
            );

            await DispatchAsync(restaurantId, pending, cancellationToken);
        }

        public async Task<BillingLifecycleCommandResult> StartDunningEpisodeAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        )
        {
            BillingLifecycleCommandResult? result = null;
            var pending = await MutateAsync(
                restaurantId,
                cancellationToken,
                account =>
                {
                    if (IsPilotPlan(account))
                    {
                        result = BillingLifecycleCommandResult.Refuse(PilotRefuseReason);
                        return Task.FromResult(new List<LifecycleEvent>());
                    }

                    if (account.ChargebackRestricted)
                    {
                        result = BillingLifecycleCommandResult.Refuse(ChargebackRefuseReason);
                        return Task.FromResult(new List<LifecycleEvent>());
                    }

                    if (account.DunningEpisodeStartedAt != null)
                    {
                        result = BillingLifecycleCommandResult.NoOp();
                        return Task.FromResult(new List<LifecycleEvent>());
                    }

                    account.BillingStatus = BillingStatuses.PastDue;
                    account.DunningEpisodeStartedAt = now;
                    var fired = new HashSet<int>();
                    var events = new List<LifecycleEvent>();
                    FireDunningStep(account, 0, fired, events);
                    account.DunningFiredSteps = EncodeFired(fired);
                    result = BillingLifecycleCommandResult.Ok();
                    return Task.FromResult(events);
                }
            );

            await DispatchAsync(restaurantId, pending, cancellationToken);
            return result ?? BillingLifecycleCommandResult.NoOp();
        }

        public async Task RecoverDunningAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        )
        {
            await MutateAsync(
                restaurantId,
                cancellationToken,
                account =>
                {
                    account.BillingStatus = BillingStatuses.Active;
                    account.DunningEpisodeStartedAt = null;
                    account.DunningFiredSteps = null;
                    account.SoftLockEnteredAt = null;
                    account.DormantEnteredAt = null;
                    return Task.FromResult(new List<LifecycleEvent>());
                }
            );
        }

        public async Task ActivatePaidPlanAsync(
            int restaurantId,
            DateTime now,
            CancellationToken cancellationToken = default
        )
        {
            await MutateAsync(
                restaurantId,
                cancellationToken,
                account =>
                {
                    account.BillingStatus = BillingStatuses.Active;
                    account.PilotPeriodEnd = null;
                    account.SoftLockEnteredAt = null;
                    account.DormantEnteredAt = null;
                    account.PilotSoftLockNotified = false;
                    account.PilotDormantNotified = false;
                    return Task.FromResult(new List<LifecycleEvent>());
                }
            );
        }

        public async Task<BillingLifecycleCommandResult> ExtendPilotActivationAsync(
            int restaurantId,
            DateTime newPeriodEnd,
            DateTime now,
            CancellationToken cancellationToken = default
        )
        {
            BillingLifecycleCommandResult? result = null;
            await MutateAsync(
                restaurantId,
                cancellationToken,
                account =>
                {
                    var lockedUnpaidPilot =
                        IsPilotPlan(account)
                        && (
                            account.BillingStatus == BillingStatuses.SoftLock
                            || account.BillingStatus == BillingStatuses.Dormant
                        );

                    if (!lockedUnpaidPilot)
                    {
                        result = BillingLifecycleCommandResult.Refuse("not-extendable");
                        return Task.FromResult(new List<LifecycleEvent>());
                    }

                    account.PilotPeriodEnd = newPeriodEnd;
                    account.BillingStatus = BillingStatuses.Pilot;
                    account.SoftLockEnteredAt = null;
                    account.DormantEnteredAt = null;
                    account.PilotSoftLockNotified = false;
                    account.PilotDormantNotified = false;
                    result = BillingLifecycleCommandResult.Ok();
                    return Task.FromResult(new List<LifecycleEvent>());
                }
            );

            return result ?? BillingLifecycleCommandResult.NoOp();
        }

        public async Task SetChargebackRestrictionAsync(
            int restaurantId,
            bool restricted,
            CancellationToken cancellationToken = default
        )
        {
            await MutateAsync(
                restaurantId,
                cancellationToken,
                account =>
                {
                    account.ChargebackRestricted = restricted;
                    return Task.FromResult(new List<LifecycleEvent>());
                }
            );
        }

        private async Task<List<LifecycleEvent>> MutateAsync(
            int restaurantId,
            CancellationToken cancellationToken,
            Func<BillingAccount, Task<List<LifecycleEvent>>> mutate
        )
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var account = await LoadForUpdateAsync(restaurantId, cancellationToken);
                if (account == null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return [];
                }

                var events = await mutate(account);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return events;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<BillingAccount?> LoadForUpdateAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.IsSqlServer())
            {
                return await _context.BillingAccounts
                    .FromSql(
                        $"""
                        SELECT * FROM BillingAccounts WITH (UPDLOCK, ROWLOCK)
                        WHERE RestaurantId = {restaurantId}
                        """
                    )
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(
                row => row.RestaurantId == restaurantId,
                cancellationToken
            );
        }

        private async Task EnsurePilotPeriodEndAsync(
            BillingAccount account,
            CancellationToken cancellationToken
        )
        {
            if (!IsPilotPlan(account) || account.PilotPeriodEnd != null)
            {
                return;
            }

            var ownerExpires = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.Id == account.RestaurantId)
                .Join(
                    _context.Users.AsNoTracking(),
                    restaurant => restaurant.OwnerUserId,
                    user => user.Id,
                    (_, user) => user.ActivationExpiresAt
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (ownerExpires != null)
            {
                account.PilotPeriodEnd = ownerExpires;
            }
        }

        private static void AdvanceUnpaidPilot(
            BillingAccount account,
            DateTime now,
            List<LifecycleEvent> events
        )
        {
            if (!IsPilotPlan(account) || account.PilotPeriodEnd == null)
            {
                return;
            }

            var periodEnd = account.PilotPeriodEnd.Value;
            if (now < periodEnd)
            {
                return;
            }

            var dormantAt = periodEnd.AddHours(PilotDormantHours);
            if (now >= dormantAt)
            {
                if (account.SoftLockEnteredAt == null)
                {
                    account.SoftLockEnteredAt = periodEnd;
                }

                if (account.DormantEnteredAt == null)
                {
                    account.DormantEnteredAt = dormantAt;
                }

                account.BillingStatus = BillingStatuses.Dormant;
                EmitPilotLock(account, events);
                EmitPilotDormant(account, events);
                return;
            }

            if (account.SoftLockEnteredAt == null)
            {
                account.SoftLockEnteredAt = periodEnd;
            }

            account.BillingStatus = BillingStatuses.SoftLock;
            EmitPilotLock(account, events);
        }

        private static void AdvanceDunning(
            BillingAccount account,
            DateTime now,
            List<LifecycleEvent> events
        )
        {
            if (account.DunningEpisodeStartedAt == null)
            {
                return;
            }

            var start = account.DunningEpisodeStartedAt.Value;
            var elapsedHours = (now - start).TotalHours;
            var fired = ParseFired(account.DunningFiredSteps);

            foreach (var step in DunningDaySteps)
            {
                if (elapsedHours + 0.0001 >= step * 24)
                {
                    FireDunningStep(account, step, fired, events);
                }
            }

            account.DunningFiredSteps = EncodeFired(fired);

            var dormantAt = start.AddHours(DunningDormantHours);
            var softLockAt = start.AddHours(DunningSoftLockHours);
            if (now >= dormantAt)
            {
                if (account.SoftLockEnteredAt == null)
                {
                    account.SoftLockEnteredAt = softLockAt;
                }

                if (account.DormantEnteredAt == null)
                {
                    account.DormantEnteredAt = dormantAt;
                }

                account.BillingStatus = BillingStatuses.Dormant;
                return;
            }

            if (now >= softLockAt)
            {
                if (account.SoftLockEnteredAt == null)
                {
                    account.SoftLockEnteredAt = softLockAt;
                }

                account.BillingStatus = BillingStatuses.SoftLock;
            }
        }

        private static void FireDunningStep(
            BillingAccount account,
            int step,
            HashSet<int> fired,
            List<LifecycleEvent> events
        )
        {
            if (!fired.Add(step) || account.DunningEpisodeStartedAt == null)
            {
                return;
            }

            var episodeId =
                $"{account.RestaurantId}:{account.DunningEpisodeStartedAt.Value.Ticks}";
            events.Add(new LifecycleEvent.DunningDay(account.RestaurantId, step, episodeId));
        }

        private static void EmitPilotLock(
            BillingAccount account,
            List<LifecycleEvent> events
        )
        {
            if (account.PilotSoftLockNotified || account.PilotPeriodEnd == null)
            {
                return;
            }

            account.PilotSoftLockNotified = true;
            events.Add(
                new LifecycleEvent.PilotSoftLock(
                    account.RestaurantId,
                    PilotEpisodeKey(account)
                )
            );
        }

        private static void EmitPilotDormant(
            BillingAccount account,
            List<LifecycleEvent> events
        )
        {
            if (account.PilotDormantNotified || account.PilotPeriodEnd == null)
            {
                return;
            }

            account.PilotDormantNotified = true;
            events.Add(
                new LifecycleEvent.PilotDormant(
                    account.RestaurantId,
                    PilotEpisodeKey(account)
                )
            );
        }

        private static string PilotEpisodeKey(BillingAccount account) =>
            $"{account.RestaurantId}:pilot:{account.PilotPeriodEnd!.Value.Ticks}";

        private async Task DispatchAsync(
            int restaurantId,
            List<LifecycleEvent> events,
            CancellationToken cancellationToken
        )
        {
            foreach (var item in events)
            {
                switch (item)
                {
                    case LifecycleEvent.DunningDay dunning:
                        await _notifier.NotifyPaymentFailureDayStepAsync(
                            restaurantId,
                            dunning.DayStep,
                            dunning.EpisodeId,
                            cancellationToken
                        );
                        break;
                    case LifecycleEvent.PilotSoftLock softLock:
                        await _notifier.NotifyUnpaidPilotLockEnterAsync(
                            restaurantId,
                            softLock.EpisodeKey,
                            cancellationToken
                        );
                        break;
                    case LifecycleEvent.PilotDormant dormant:
                        await _notifier.NotifyUnpaidPilotDormantEnterAsync(
                            restaurantId,
                            dormant.EpisodeKey,
                            cancellationToken
                        );
                        break;
                }
            }
        }

        private static bool IsPilotPlan(BillingAccount account) =>
            string.Equals(
                account.SubscriptionPlan,
                BillingSubscriptionPlans.Pilot,
                StringComparison.Ordinal
            );

        private static HashSet<int> ParseFired(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return [];
            }

            return value
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(int.Parse)
                .ToHashSet();
        }

        private static string EncodeFired(IEnumerable<int> steps) =>
            string.Join(',', steps.OrderBy(step => step));

        private abstract record LifecycleEvent
        {
            public sealed record DunningDay(
                int RestaurantId,
                int DayStep,
                string EpisodeId
            ) : LifecycleEvent;

            public sealed record PilotSoftLock(int RestaurantId, string EpisodeKey)
                : LifecycleEvent;

            public sealed record PilotDormant(int RestaurantId, string EpisodeKey)
                : LifecycleEvent;
        }
    }
}
