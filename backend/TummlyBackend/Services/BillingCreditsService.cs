using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.BillingCredits;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public class BillingCreditsService : IBillingCreditsService
    {
        private readonly ApplicationDbContext _context;

        public BillingCreditsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BillingCreditsPageDto?> GetPageAsync(
            int userId,
            int restaurantId,
            bool actorCanManage
        )
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == userId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorPermissionRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var owner = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurant.OwnerUserId);

            var activeLocations = await _context.RestaurantLocations
                .AsNoTracking()
                .CountAsync(row => row.RestaurantId == restaurantId);

            var pilotEndsAt = owner?.ActivationExpiresAt;
            var renewalDateLabel = pilotEndsAt == null
                ? null
                : $"Pilot ends {FormatUkDate(pilotEndsAt.Value)}";

            var billingAccount = await LoadOrCreateBillingAccountAsync(restaurantId);
            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);

            return new BillingCreditsPageDto
            {
                ActorPermissionRole = actorPermissionRole,
                ActorCanManage = actorCanManage,
                ActorCanPersistBillingContacts =
                    actorCanManage
                    && actorPermissionRole == PermissionRoles.Owner,
                PlanSubscription = new PlanSubscriptionSnapshotDto
                {
                    SubscriptionPlan = "Pilot",
                    BillingStatus = "Pilot",
                    RenewalDateLabel = renewalDateLabel,
                    EmailCreditsRemaining = 500,
                    SmsCreditsRemaining = 20,
                    AiCreditsRemaining = 20,
                    BillingCycle = null,
                    PlanPriceNet = "£0",
                    IncludedLocations = 1,
                    ActiveLocations = activeLocations,
                    IncludedEmailCreditsLabel = "500 once",
                    IncludedSmsCreditsLabel = "20 once",
                    IncludedAiCreditsLabel = "20 once",
                    StarterKitState = "unused",
                    PricebookId = "guest-loop-mvp-2026-07",
                    ScheduledChangeLine = null,
                    IsPilot = true,
                },
                BillingContacts = MapBillingContacts(
                    restaurant,
                    billingAccount,
                    eligibleMembers
                ),
            };
        }

        public async Task<(
            UpdateBillingContactsResponseDto? Response,
            string? Error,
            int StatusCode
        )> UpdateBillingContactsAsync(
            int actorUserId,
            int restaurantId,
            UpdateBillingContactsRequest request
        )
        {
            var actorMembership = await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == actorUserId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
            var actorPermissionRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;
            if (actorPermissionRole != PermissionRoles.Owner)
            {
                return (
                    null,
                    "Only the account owner may update billing contacts.",
                    StatusCodes.Status403Forbidden
                );
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return (null, "Restaurant not found.", StatusCodes.Status404NotFound);
            }

            if (request.BillingContactUserId <= 0)
            {
                return (
                    null,
                    "Billing contact is required.",
                    StatusCodes.Status400BadRequest
                );
            }

            var eligibleIds = await GetEligibleMemberIdsAsync(restaurantId);
            if (!eligibleIds.Contains(request.BillingContactUserId))
            {
                return (
                    null,
                    "Billing contact must be an eligible team member.",
                    StatusCodes.Status400BadRequest
                );
            }

            var billingEmail = NormalizeBillingEmail(request.BillingEmail);
            if (billingEmail != null && !IsValidEmail(billingEmail))
            {
                return (
                    null,
                    "Billing email is not valid.",
                    StatusCodes.Status400BadRequest
                );
            }

            restaurant.BillingContactUserId = request.BillingContactUserId;

            var billingAccount = await LoadOrCreateBillingAccountAsync(restaurantId, tracked: true);
            billingAccount.BillingEmail = billingEmail;
            billingAccount.LowCreditAlertOwner = request.LowCreditAlerts.Owner;
            billingAccount.LowCreditAlertAdmin = request.LowCreditAlerts.Admin;
            billingAccount.LowCreditAlertBillingContact =
                request.LowCreditAlerts.BillingContact;
            billingAccount.PaymentFailureAlertOwner =
                request.PaymentFailureAlerts.Owner;
            billingAccount.PaymentFailureAlertBillingContact =
                request.PaymentFailureAlerts.BillingContact;

            await _context.SaveChangesAsync();

            var eligibleMembers = await LoadEligibleMembersAsync(restaurantId);
            return (
                new UpdateBillingContactsResponseDto
                {
                    BillingContacts = MapBillingContacts(
                        restaurant,
                        billingAccount,
                        eligibleMembers
                    ),
                },
                null,
                StatusCodes.Status200OK
            );
        }

        private async Task<BillingAccount> LoadOrCreateBillingAccountAsync(
            int restaurantId,
            bool tracked = false
        )
        {
            var query = tracked
                ? _context.BillingAccounts
                : _context.BillingAccounts.AsNoTracking();

            var existing = await query
                .FirstOrDefaultAsync(row => row.RestaurantId == restaurantId);

            if (existing != null)
            {
                return existing;
            }

            var created = CreateDefaultBillingAccount(restaurantId);
            if (tracked)
            {
                _context.BillingAccounts.Add(created);
            }

            return created;
        }

        public static BillingAccount CreateDefaultBillingAccount(int restaurantId)
        {
            return new BillingAccount
            {
                RestaurantId = restaurantId,
                LowCreditAlertOwner = true,
                LowCreditAlertAdmin = false,
                LowCreditAlertBillingContact = true,
                PaymentFailureAlertOwner = true,
                PaymentFailureAlertBillingContact = true,
            };
        }

        private async Task<List<BillingContactPickerItemDto>> LoadEligibleMembersAsync(
            int restaurantId
        )
        {
            var memberIds = await GetEligibleMemberIdsAsync(restaurantId);
            var members = await _context.Users
                .AsNoTracking()
                .Where(user => memberIds.Contains(user.Id))
                .OrderBy(user => user.FullName)
                .ToListAsync();

            if (members.Count == 0)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId)
                    .Select(row => row.OwnerUserId)
                    .FirstAsync();
                var owner = await _context.Users
                    .AsNoTracking()
                    .FirstAsync(user => user.Id == ownerId);
                members = [owner];
            }

            return members.Select(MapPickerItem).ToList();
        }

        private async Task<HashSet<int>> GetEligibleMemberIdsAsync(int restaurantId)
        {
            var ids = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(m =>
                    m.RestaurantId == restaurantId
                    && m.Status == MembershipStatus.Active
                )
                .Select(m => m.UserId)
                .ToListAsync();

            if (ids.Count == 0)
            {
                var ownerId = await _context.Restaurants
                    .AsNoTracking()
                    .Where(r => r.Id == restaurantId)
                    .Select(r => r.OwnerUserId)
                    .FirstAsync();
                ids.Add(ownerId);
            }

            return ids.ToHashSet();
        }

        private static BillingContactsSnapshotDto MapBillingContacts(
            Restaurant restaurant,
            BillingAccount billingAccount,
            IReadOnlyList<BillingContactPickerItemDto> eligibleMembers
        )
        {
            return new BillingContactsSnapshotDto
            {
                BillingContactUserId = restaurant.BillingContactUserId,
                BillingEmail = billingAccount.BillingEmail,
                EligibleMembers = eligibleMembers.ToList(),
                LowCreditAlerts = new BillingAlertRoleFlagsDto
                {
                    Owner = billingAccount.LowCreditAlertOwner,
                    Admin = billingAccount.LowCreditAlertAdmin,
                    BillingContact = billingAccount.LowCreditAlertBillingContact,
                },
                PaymentFailureAlerts = new BillingPaymentFailureAlertFlagsDto
                {
                    Owner = billingAccount.PaymentFailureAlertOwner,
                    BillingContact = billingAccount.PaymentFailureAlertBillingContact,
                },
            };
        }

        private static BillingContactPickerItemDto MapPickerItem(User user)
        {
            return new BillingContactPickerItemDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
            };
        }

        private static string? NormalizeBillingEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static bool IsValidEmail(string value)
        {
            try
            {
                var address = new System.Net.Mail.MailAddress(value);
                return string.Equals(
                    address.Address,
                    value,
                    StringComparison.OrdinalIgnoreCase
                );
            }
            catch
            {
                return false;
            }
        }

        private static string FormatUkDate(DateTime value)
        {
            return value.ToString(
                "d MMMM yyyy",
                System.Globalization.CultureInfo.GetCultureInfo("en-GB")
            );
        }
    }
}
