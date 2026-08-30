using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class LiveRecoverySmsBillingReserve : IRecoverySmsBillingReserve
    {
        private readonly ApplicationDbContext _context;
        private readonly ICreditLedger _ledger;
        private readonly ICreditBalanceSnapshot _snapshot;

        public LiveRecoverySmsBillingReserve(
            ApplicationDbContext context,
            ICreditLedger ledger,
            ICreditBalanceSnapshot snapshot
        )
        {
            _context = context;
            _ledger = ledger;
            _snapshot = snapshot;
        }

        // Ledger reserve/settle/release is live on this branch (campaign + recovery).
        public bool IsLive => true;

        public async Task<RecoverySmsBillingReserveResult> ReserveAsync(
            RecoverySmsBillingReserveRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var restaurantId = await ResolveRestaurantIdAsync(
                request.LocationId,
                cancellationToken
            );
            if (restaurantId == null)
            {
                return new RecoverySmsBillingReserveResult.Failed
                {
                    Code = "location_not_in_account",
                    Remaining = 0,
                    Requested = request.Units,
                };
            }

            var result = await _ledger.ReserveAsync(
                new CreditLedgerReserveRequest
                {
                    RestaurantId = restaurantId.Value,
                    Channel = CreditChannels.Sms,
                    Units = request.Units,
                    LocationId = request.LocationId,
                },
                cancellationToken
            );

            if (result.Succeeded)
            {
                return new RecoverySmsBillingReserveResult.Ok
                {
                    ReservationRef = result.ReservationRef!,
                };
            }

            var remaining = await RemainingSmsAsync(
                restaurantId.Value,
                cancellationToken
            );

            return new RecoverySmsBillingReserveResult.Failed
            {
                Code = result.Code ?? "insufficient_credits",
                Remaining = remaining,
                Requested = request.Units,
            };
        }

        public async Task<RecoverySmsBillingSettleResult> SettleAsync(
            RecoverySmsBillingSettleRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var scope = await ResolveFeedbackBillingScopeAsync(
                request.FeedbackId,
                cancellationToken
            );
            if (scope.ErrorMessage != null)
            {
                return new RecoverySmsBillingSettleResult.Failed
                {
                    Message = scope.ErrorMessage,
                };
            }

            var result = await _ledger.SettleAsync(
                new CreditLedgerSettleRequest
                {
                    RestaurantId = scope.RestaurantId,
                    Channel = CreditChannels.Sms,
                    ReservationRef = request.ReservationRef,
                    AcceptedUnits = request.AcceptedUnits,
                    LocationId = scope.LocationId,
                },
                cancellationToken
            );

            return result.Succeeded
                ? new RecoverySmsBillingSettleResult.Ok()
                : new RecoverySmsBillingSettleResult.Failed
                {
                    Message = result.Code ?? "Settle failed.",
                };
        }

        public async Task<RecoverySmsBillingReleaseResult> ReleaseAsync(
            RecoverySmsBillingReleaseRequest request,
            CancellationToken cancellationToken = default
        )
        {
            var scope = await ResolveFeedbackBillingScopeAsync(
                request.FeedbackId,
                cancellationToken
            );
            if (scope.ErrorMessage != null)
            {
                return new RecoverySmsBillingReleaseResult.Failed
                {
                    Message = scope.ErrorMessage,
                };
            }

            var result = await _ledger.ReleaseAsync(
                new CreditLedgerReleaseRequest
                {
                    RestaurantId = scope.RestaurantId,
                    Channel = CreditChannels.Sms,
                    ReservationRef = request.ReservationRef,
                    LocationId = scope.LocationId,
                },
                cancellationToken
            );

            return result.Succeeded
                ? new RecoverySmsBillingReleaseResult.Ok()
                : new RecoverySmsBillingReleaseResult.Failed
                {
                    Message = result.Code ?? "Release failed.",
                };
        }

        private async Task<FeedbackBillingScope> ResolveFeedbackBillingScopeAsync(
            int feedbackId,
            CancellationToken cancellationToken
        )
        {
            var feedback = await _context.Feedbacks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.Id == feedbackId,
                    cancellationToken
                );
            if (feedback == null)
            {
                return FeedbackBillingScope.Fail("Feedback not found.");
            }

            var restaurantId = await ResolveRestaurantIdAsync(
                feedback.RestaurantLocationId,
                cancellationToken
            );
            if (restaurantId == null)
            {
                return FeedbackBillingScope.Fail("Location not in account.");
            }

            return FeedbackBillingScope.Ok(
                restaurantId.Value,
                feedback.RestaurantLocationId
            );
        }

        private async Task<int> RemainingSmsAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            var account = await _snapshot.GetAccountAsync(
                restaurantId,
                cancellationToken
            );
            return account?.Channels
                .FirstOrDefault(row => row.Channel == CreditChannels.Sms)
                ?.Remaining
                ?? 0;
        }

        private async Task<int?> ResolveRestaurantIdAsync(
            int locationId,
            CancellationToken cancellationToken
        )
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.Id == locationId)
                .Select(row => (int?)row.RestaurantId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private readonly struct FeedbackBillingScope
        {
            public int RestaurantId { get; init; }

            public int LocationId { get; init; }

            public string? ErrorMessage { get; init; }

            public static FeedbackBillingScope Ok(int restaurantId, int locationId)
                => new()
                {
                    RestaurantId = restaurantId,
                    LocationId = locationId,
                };

            public static FeedbackBillingScope Fail(string message)
                => new() { ErrorMessage = message };
        }
    }
}
