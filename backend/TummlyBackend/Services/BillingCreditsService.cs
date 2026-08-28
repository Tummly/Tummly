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

            return new BillingCreditsPageDto
            {
                ActorPermissionRole = actorPermissionRole,
                ActorCanManage = actorCanManage,
                PlanSubscription = BuildPilotPlanSnapshot(activeLocations, renewalDateLabel),
            };
        }

        public async Task<CreditsUsageSnapshotDto?> GetUsageAsync(int restaurantId)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(row => row.Id == restaurantId);

            if (restaurant == null)
            {
                return null;
            }

            return BuildPilotUsageSnapshot();
        }

        private static PlanSubscriptionSnapshotDto BuildPilotPlanSnapshot(
            int activeLocations,
            string? renewalDateLabel
        )
        {
            return new PlanSubscriptionSnapshotDto
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
            };
        }

        private static CreditsUsageSnapshotDto BuildPilotUsageSnapshot()
        {
            return new CreditsUsageSnapshotDto
            {
                PeriodLabel = "Account · Pilot allowance",
                StarterKitState = "unused",
                IsPilot = true,
                Channels =
                [
                    new CreditChannelUsageDto
                    {
                        Channel = "email",
                        CombinedRemaining = 500,
                        UsedThisCycle = 0,
                        IncludedThisPeriod = 500,
                        PurchasedRemaining = 0,
                        PurchasedExpiryLabel = null,
                    },
                    new CreditChannelUsageDto
                    {
                        Channel = "sms",
                        CombinedRemaining = 20,
                        UsedThisCycle = 0,
                        IncludedThisPeriod = 20,
                        PurchasedRemaining = 0,
                        PurchasedExpiryLabel = null,
                    },
                    new CreditChannelUsageDto
                    {
                        Channel = "ai",
                        CombinedRemaining = 20,
                        UsedThisCycle = 0,
                        IncludedThisPeriod = 20,
                        PurchasedRemaining = 0,
                        PurchasedExpiryLabel = null,
                    },
                ],
            };
        }

        private static string FormatUkDate(DateTime value)
        {
            return value.ToString("d MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
        }
    }
}
