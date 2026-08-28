using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Billing
{
    public sealed class TeamMemberCapGate : ITeamMemberCapGate
    {
        public const string CapReachedCode = "team_member_cap_reached";

        public const string CapReachedMessage = "Team member cap reached.";

        public const string UnavailableMessage = "Plan entitlements are unavailable.";

        private const int ExtraUsersPerLocation = 2;

        private readonly ApplicationDbContext _context;
        private readonly IPricebookCatalog _pricebook;

        public TeamMemberCapGate(
            ApplicationDbContext context,
            IPricebookCatalog pricebook
        )
        {
            _context = context;
            _pricebook = pricebook;
        }

        public async Task<TeamMemberCapDecision> DenyIncrementAsync(
            int restaurantId
        )
        {
            var account = await LockBillingAccountAsync(restaurantId);
            if (account == null)
            {
                return TeamMemberCapDecision.UnavailableNow();
            }

            if (!TryResolveCap(account, out var cap))
            {
                return TeamMemberCapDecision.UnavailableNow();
            }

            var current = await CountUsageAsync(restaurantId);
            if (current >= cap)
            {
                return TeamMemberCapDecision.AtCap(cap, current);
            }

            return TeamMemberCapDecision.Allow(cap, current);
        }

        private async Task<BillingAccount?> LockBillingAccountAsync(
            int restaurantId
        )
        {
            if (_context.Database.IsSqlServer())
            {
                return await _context.BillingAccounts
                    .FromSqlInterpolated(
                        $"SELECT * FROM BillingAccounts WITH (UPDLOCK, ROWLOCK) WHERE RestaurantId = {restaurantId}"
                    )
                    .AsTracking()
                    .FirstOrDefaultAsync();
            }

            return await _context.BillingAccounts.FirstOrDefaultAsync(row =>
                row.RestaurantId == restaurantId
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

            // Ticket 26 owns PaidExtraLocationCount. Until then extras on the row are 0.
            var extraLocations = 0;
            cap = plan.IncludedTeamMembers + (extraLocations * ExtraUsersPerLocation);
            return true;
        }

        private async Task<int> CountUsageAsync(int restaurantId)
        {
            var now = DateTime.UtcNow;
            var active = await _context.RestaurantMemberships.CountAsync(row =>
                row.RestaurantId == restaurantId
                && row.Status == MembershipStatus.Active
            );
            var pending = await _context.TeamInvitations.CountAsync(row =>
                row.RestaurantId == restaurantId && row.ExpiresAt > now
            );
            return active + pending;
        }
    }
}
