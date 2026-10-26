using Microsoft.Extensions.DependencyInjection;
using TummlyBackend.Data;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Tests.Integration.Support
{
    /// <summary>
    /// Shared WS2b seed: two tenants, an owner, a scoped member, guests and
    /// Feedback with contact at the in-scope Location.
    /// </summary>
    public static class OperatorScopeSeed
    {
        private const string GuestMobile = "07700900987";

        public static async Task<OperatorScopeFixture> SeedAsync(
            TummlyWebApplicationFactory factory,
            string memberRole,
            bool namedInScopeOnly = true
        )
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();
            var jwtService = scope.ServiceProvider
                .GetRequiredService<IJwtService>();

            var owner = new User
            {
                FullName = "Scope Seed Owner",
                Email = $"ws2b-owner-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900211",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(owner);
            await context.SaveChangesAsync();

            var restaurant = new Restaurant
            {
                Name = "Scope Seed Venue",
                AccountType = "Multi",
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(restaurant);
            await context.SaveChangesAsync();

            var inScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "In Scope",
                Address = "1 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScope = new RestaurantLocation
            {
                RestaurantId = restaurant.Id,
                LocationName = "Out Of Scope",
                Address = "2 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.AddRange(inScope, outOfScope);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = owner.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });

            var member = new User
            {
                FullName = "Scope Seed Member",
                Email = $"ws2b-member-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900213",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                SelectedRestaurantId = restaurant.Id,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(member);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = member.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = memberRole,
                LocationScope = namedInScopeOnly
                    ? LocationScopeKind.NamedList
                    : LocationScopeKind.AllLocations,
                NamedLocationIdsJson = namedInScopeOnly
                    ? MembershipLocationScope.SerializeNamedIds(
                        new[] { inScope.Id }
                    )
                    : "[]",
                Status = MembershipStatus.Active,
            });

            var otherOwner = new User
            {
                FullName = "Scope Seed Other Owner",
                Email = $"ws2b-other-{Guid.NewGuid():N}@example.com",
                PasswordHash = "hash",
                PhoneNumber = "07700900311",
                Role = "Owner",
                AccountType = "Multi",
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            context.Users.Add(otherOwner);
            await context.SaveChangesAsync();

            var otherRestaurant = new Restaurant
            {
                Name = "Scope Seed Other Tenant",
                AccountType = "Multi",
                OwnerUserId = otherOwner.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Restaurants.Add(otherRestaurant);
            await context.SaveChangesAsync();

            var otherTenantLocation = new RestaurantLocation
            {
                RestaurantId = otherRestaurant.Id,
                LocationName = "Other Tenant",
                Address = "3 High Street",
                CreatedAt = DateTime.UtcNow,
            };
            context.RestaurantLocations.Add(otherTenantLocation);
            await context.SaveChangesAsync();

            context.RestaurantMemberships.Add(new RestaurantMembership
            {
                UserId = otherOwner.Id,
                RestaurantId = otherRestaurant.Id,
                PermissionRole = PermissionRoles.Owner,
                LocationScope = LocationScopeKind.AllLocations,
                NamedLocationIdsJson = "[]",
                Status = MembershipStatus.Active,
            });
            await context.SaveChangesAsync();

            var guestEmail = $"ws2b-{Guid.NewGuid():N}@example.com";

            var emailMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = guestEmail,
                NormalizedEmail = guestEmail.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
            };
            var phoneMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Mobile = GuestMobile,
                NormalizedPhone = GuestMobile,
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScopeMaster = new MasterGuest
            {
                RestaurantId = restaurant.Id,
                Email = $"ws2b-out-{Guid.NewGuid():N}@example.com",
                CreatedAt = DateTime.UtcNow,
            };
            context.MasterGuests.AddRange(emailMaster, phoneMaster, outOfScopeMaster);
            await context.SaveChangesAsync();

            var emailGuest = new LocationGuest
            {
                MasterGuestId = emailMaster.Id,
                RestaurantLocationId = inScope.Id,
                Name = "Email Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            var phoneGuest = new LocationGuest
            {
                MasterGuestId = phoneMaster.Id,
                RestaurantLocationId = inScope.Id,
                Name = "Phone Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            var outOfScopeGuest = new LocationGuest
            {
                MasterGuestId = outOfScopeMaster.Id,
                RestaurantLocationId = outOfScope.Id,
                Name = "Out Of Scope Guest",
                MarketingPreference = LocationGuestMarketingPreference.Allowed,
                CreatedAt = DateTime.UtcNow,
            };
            context.LocationGuests.AddRange(emailGuest, phoneGuest, outOfScopeGuest);
            await context.SaveChangesAsync();

            var emailFeedback = new Feedback
            {
                RestaurantLocationId = inScope.Id,
                LocationGuestId = emailGuest.Id,
                GuestName = "Email Guest",
                GuestContact = guestEmail,
                ContactType = ContactType.Email,
                Comment = "Lovely service",
                WorkflowStatus = FeedbackWorkflowStatus.New,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
            };
            var phoneFeedback = new Feedback
            {
                RestaurantLocationId = inScope.Id,
                LocationGuestId = phoneGuest.Id,
                GuestName = "Phone Guest",
                GuestContact = GuestMobile,
                ContactType = ContactType.Phone,
                Comment = "Food was cold",
                WorkflowStatus = FeedbackWorkflowStatus.New,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
            };
            context.Feedbacks.AddRange(emailFeedback, phoneFeedback);
            await context.SaveChangesAsync();

            var ownerJwt = jwtService.GenerateToken(
                owner.Id.ToString(),
                owner.Email,
                owner.Role
            );
            var memberJwt = jwtService.GenerateToken(
                member.Id.ToString(),
                member.Email,
                member.Role
            );

            return new OperatorScopeFixture(
                ownerJwt,
                memberJwt,
                owner.Id,
                member.Id,
                restaurant.Id,
                inScope.Id,
                outOfScope.Id,
                otherTenantLocation.Id,
                emailGuest.Id,
                phoneGuest.Id,
                outOfScopeGuest.Id,
                emailFeedback.Id,
                phoneFeedback.Id,
                guestEmail,
                GuestMobile
            );
        }
    }

    public sealed record OperatorScopeFixture(
        string OwnerJwt,
        string MemberJwt,
        int OwnerUserId,
        int MemberUserId,
        int RestaurantId,
        int InScopeLocationId,
        int OutOfScopeLocationId,
        int OtherTenantLocationId,
        int EmailGuestId,
        int PhoneGuestId,
        int OutOfScopeGuestId,
        int EmailFeedbackId,
        int PhoneFeedbackId,
        string GuestEmail,
        string GuestMobile
    );
}
