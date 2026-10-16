using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using TummlyBackend.Services;

namespace ProductDemoSeed;

internal static class AccountBootstrap
{
    public static async Task<(User User, Restaurant Restaurant, RestaurantLocation Location)>
        EnsureAccountAsync(ApplicationDbContext db)
    {
        var email = DemoSeedConstants.OwnerEmail;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                FullName = DemoSeedConstants.OwnerFullName,
                PhoneNumber = "07700900400",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoOnly!NotForLogin"),
                Role = "Owner",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-60),
                ActivationExpiresAt = DemoSeedConstants.SeedNowUtc.AddYears(1),
                CreatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-90),
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        else
        {
            user.FullName = DemoSeedConstants.OwnerFullName;
            user.AccountType = "Single";
            user.IsEmailVerified = true;
            user.IsApprovedByAdmin = true;
            user.TermsAccepted = true;
            user.HasCompletedFirstSignIn = true;
            user.ActivatedAt ??= DemoSeedConstants.SeedNowUtc.AddDays(-60);
            user.ActivationExpiresAt = DemoSeedConstants.SeedNowUtc.AddYears(1);
            user.IsLocked = false;
        }

        var restaurant = await db.Restaurants
            .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

        if (restaurant is null)
        {
            restaurant = new Restaurant
            {
                Name = DemoSeedConstants.RestaurantName,
                AccountType = "Single",
                OwnerUserId = user.Id,
                BillingContactUserId = user.Id,
                PrivacyContactUserId = user.Id,
                SupportContactUserId = user.Id,
                BusinessCategory = "Casual dining",
                PublicPhoneNumber = "02079460000",
                WorkspaceStatus = WorkspaceStatus.Active,
                CreatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-90),
            };
            db.Restaurants.Add(restaurant);
            await db.SaveChangesAsync();
        }
        else
        {
            restaurant.Name = DemoSeedConstants.RestaurantName;
            restaurant.AccountType = "Single";
            restaurant.BillingContactUserId = user.Id;
            restaurant.PrivacyContactUserId = user.Id;
            restaurant.SupportContactUserId = user.Id;
            restaurant.WorkspaceStatus = WorkspaceStatus.Active;
        }

        user.SelectedRestaurantId = restaurant.Id;
        user.AccountType = "Single";

        var membership = await db.RestaurantMemberships
            .FirstOrDefaultAsync(m =>
                m.UserId == user.Id && m.RestaurantId == restaurant.Id
            );
        if (membership is null)
        {
            db.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = user.Id,
                    RestaurantId = restaurant.Id,
                    PermissionRole = PermissionRoles.Owner,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
        }

        var billing = await db.BillingAccounts
            .FirstOrDefaultAsync(b => b.RestaurantId == restaurant.Id);
        if (billing is null)
        {
            billing = BillingCreditsService.CreateDefaultBillingAccount(
                restaurant.Id,
                DemoSeedConstants.PricebookId
            );
            db.BillingAccounts.Add(billing);
        }

        billing.SubscriptionPlan = BillingSubscriptionPlans.Pilot;
        billing.BillingStatus = BillingStatuses.Pilot;
        billing.ContractedPricebookId = DemoSeedConstants.PricebookId;
        billing.StarterKitState = StarterKitStates.Used;

        await EnsurePilotCreditsAsync(db, restaurant.Id);

        var location = await db.RestaurantLocations
            .Where(l => l.RestaurantId == restaurant.Id)
            .OrderBy(l => l.Id)
            .FirstOrDefaultAsync();

        if (location is null)
        {
            location = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = DemoSeedConstants.LocationName,
                Address = DemoSeedConstants.LocationAddress,
                City = DemoSeedConstants.LocationCity,
                Postcode = DemoSeedConstants.LocationPostcode,
                LifecycleStatus = LocationLifecycleStatus.Active,
                CreatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-90),
            };
            db.RestaurantLocations.Add(location);
            await db.SaveChangesAsync();
        }
        else
        {
            location.LocationName = DemoSeedConstants.LocationName;
            location.Address = DemoSeedConstants.LocationAddress;
            location.City = DemoSeedConstants.LocationCity;
            location.Postcode = DemoSeedConstants.LocationPostcode;
            location.LifecycleStatus = LocationLifecycleStatus.Active;
        }

        user.SelectedLocationId = location.Id;
        await EnsureStaffUserAsync(db, restaurant.Id, location.Id);
        await EnsureAiUsageAsync(db, restaurant.Id);
        await db.SaveChangesAsync();

        return (user, restaurant, location);
    }

    private static async Task EnsureStaffUserAsync(
        ApplicationDbContext db,
        int restaurantId,
        int locationId
    )
    {
        var staff = await db.Users.FirstOrDefaultAsync(u =>
            u.Email == DemoSeedConstants.StaffEmail
        );
        if (staff is null)
        {
            staff = new User
            {
                Email = DemoSeedConstants.StaffEmail,
                FullName = "Mavero Demo Staff",
                PhoneNumber = "07700900401",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoOnly!NotForLogin"),
                Role = "User",
                AccountType = "Single",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                TermsAccepted = true,
                HasCompletedFirstSignIn = true,
                ActivatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-60),
                ActivationExpiresAt = DemoSeedConstants.SeedNowUtc.AddYears(1),
                CreatedAt = DemoSeedConstants.SeedNowUtc.AddDays(-80),
                SelectedRestaurantId = restaurantId,
                SelectedLocationId = locationId,
            };
            db.Users.Add(staff);
            await db.SaveChangesAsync();
        }

        var membership = await db.RestaurantMemberships.FirstOrDefaultAsync(m =>
            m.UserId == staff.Id && m.RestaurantId == restaurantId
        );
        if (membership is null)
        {
            db.RestaurantMemberships.Add(
                new RestaurantMembership
                {
                    UserId = staff.Id,
                    RestaurantId = restaurantId,
                    PermissionRole = PermissionRoles.Staff,
                    LocationScope = LocationScopeKind.AllLocations,
                    NamedLocationIdsJson = "[]",
                    Status = MembershipStatus.Active,
                }
            );
        }
    }

    private static async Task EnsureAiUsageAsync(ApplicationDbContext db, int restaurantId)
    {
        var used = await db.CreditLedgerEntries
            .Where(row =>
                row.RestaurantId == restaurantId
                && row.Channel == CreditChannels.Ai
                && row.EntryType == CreditLedgerEntryTypes.Consumption
            )
            .SumAsync(row => (int?)row.Quantity) ?? 0;

        var need = DemoSeedConstants.TargetAiActionsUsed - used;
        if (need <= 0)
        {
            return;
        }

        var grant = await db.CreditLedgerEntries.FirstOrDefaultAsync(row =>
            row.RestaurantId == restaurantId
            && row.Channel == CreditChannels.Ai
            && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
        );

        for (var i = 0; i < need; i++)
        {
            db.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = CreditChannels.Ai,
                    EntryType = CreditLedgerEntryTypes.Consumption,
                    Quantity = 1,
                    AllocationId = grant?.Id,
                    PricebookVersion = DemoSeedConstants.PricebookId,
                    CreatedAtUtc = DemoSeedConstants.SeedNowUtc.AddDays(-(i % 28)).AddHours(i % 12),
                }
            );
        }
    }

    private static async Task EnsurePilotCreditsAsync(
        ApplicationDbContext db,
        int restaurantId
    )
    {
        var channels = new[]
        {
            ("email", 50_000),
            ("sms", 5_000),
            ("ai", 5_000),
        };

        foreach (var (channel, quantity) in channels)
        {
            var exists = await db.CreditLedgerEntries.AnyAsync(row =>
                row.RestaurantId == restaurantId
                && row.Channel == channel
                && row.EntryType == CreditLedgerEntryTypes.PilotAllocation
            );
            if (exists)
            {
                continue;
            }

            db.CreditLedgerEntries.Add(
                new CreditLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    RestaurantId = restaurantId,
                    Channel = channel,
                    EntryType = CreditLedgerEntryTypes.PilotAllocation,
                    Quantity = quantity,
                    PricebookVersion = DemoSeedConstants.PricebookId,
                    ExpiresAtUtc = null,
                    CreatedAtUtc = DemoSeedConstants.SeedNowUtc.AddDays(-60),
                }
            );
        }
    }
}
