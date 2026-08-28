using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Billing
{
    public sealed class ActiveOfferCapGate : IActiveOfferCapGate
    {
        public const string CapReachedCode = "active_offer_cap_reached";

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;

        public ActiveOfferCapGate(
            ApplicationDbContext context,
            IPricebookCatalog pricebook
        )
        {
            _context = context;
            _pricebook = pricebook;
        }

        public async Task<ActiveOfferCapDecision> DenyIncrementAsync(
            int restaurantId,
            CancellationToken cancellationToken = default
        )
        {
            var account = await LockBillingAccountAsync(
                restaurantId,
                cancellationToken
            );
            if (account == null)
            {
                return ActiveOfferCapDecision.UnavailableNow();
            }

            if (!TryResolveCap(account, out var cap))
            {
                return ActiveOfferCapDecision.UnavailableNow();
            }

            var current = await CountActiveOffersAsync(
                restaurantId,
                cancellationToken
            );
            if (current >= cap)
            {
                return ActiveOfferCapDecision.AtCap(cap, current);
            }

            return ActiveOfferCapDecision.Allow(cap, current);
        }

        private async Task<BillingAccount?> LockBillingAccountAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            if (_context.Database.IsSqlServer())
            {
                return await _context.BillingAccounts
                    .FromSqlInterpolated(
                        $"SELECT * FROM BillingAccounts WITH (UPDLOCK, ROWLOCK) WHERE RestaurantId = {restaurantId}"
                    )
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(
                row => row.RestaurantId == restaurantId,
                cancellationToken
            );
        }

        private bool TryResolveCap(BillingAccount account, out int cap)
        {
            cap = 0;
            Pricebook.PricebookSnapshot book;
            try
            {
                book = _pricebook.GetRequired(account.ContractedPricebookId);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var key = account.SubscriptionPlan.Trim().ToLowerInvariant();
            if (!book.Plans.TryGetValue(key, out var plan))
            {
                return false;
            }

            if (plan.ActiveOffersAccount < 1)
            {
                return false;
            }

            cap = plan.ActiveOffersAccount;
            return true;
        }

        private async Task<int> CountActiveOffersAsync(
            int restaurantId,
            CancellationToken cancellationToken
        )
        {
            return await (
                from offer in _context.CatalogOffers
                join location in _context.RestaurantLocations
                    on offer.RestaurantLocationId equals location.Id
                where location.RestaurantId == restaurantId
                    && offer.Status == CatalogOfferStatus.Active
                select offer.Id
            ).CountAsync(cancellationToken);
        }
    }
}
