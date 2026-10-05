using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Notifications;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class MyAccountService : IMyAccountService
    {
        private const string FullAccessLabel = "Full access";

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IOperatorNotificationsService _notifications;
        private readonly ILogger<MyAccountService> _logger;

        public MyAccountService(
            ApplicationDbContext context,
            IEmailService emailService,
            IOperatorNotificationsService notifications,
            ILogger<MyAccountService>? logger = null
        )
        {
            _context = context;
            _emailService = emailService;
            _notifications = notifications;
            _logger =
                logger
                ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MyAccountService>.Instance;
        }

        public async Task<MyAccountSnapshotDto> GetSnapshotAsync(int userId)
        {
            var user = await LoadUserAsync(userId);
            return await BuildSnapshotAsync(user);
        }

        public async Task<MyAccountSnapshotDto> UpdateProfileAsync(
            int userId,
            UpdateMyAccountProfileDto dto
        )
        {
            var user = await LoadUserAsync(userId);

            var fullName = (dto.FullName ?? string.Empty).Trim();
            if (fullName.Length == 0)
            {
                throw new ArgumentException("Full name is required.");
            }

            if (fullName.Length > 150)
            {
                throw new ArgumentException("Full name must be 150 characters or fewer.");
            }

            var jobTitle = string.IsNullOrWhiteSpace(dto.JobTitle)
                ? null
                : dto.JobTitle.Trim();
            if (jobTitle != null && jobTitle.Length > 150)
            {
                throw new ArgumentException("Job title must be 150 characters or fewer.");
            }

            string phoneNumber = string.Empty;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                if (
                    !PhoneNumberHelper.TryNormalizeToE164(
                        dto.PhoneNumber,
                        PhoneNumberHelper.DefaultRegion,
                        out var e164
                    )
                    || e164 == null
                )
                {
                    throw new ArgumentException(
                        "Please enter a valid UK phone number."
                    );
                }

                phoneNumber = e164;
            }

            user.FullName = fullName;
            user.JobTitle = jobTitle;
            user.PhoneNumber = phoneNumber;

            await _context.SaveChangesAsync();

            return await BuildSnapshotAsync(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await LoadUserAsync(userId);

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new InvalidOperationException(
                    "This account uses Google or Microsoft Sign-in. Continue with Google or Microsoft instead of changing a password."
                );
            }

            if (dto.NewPassword != dto.ConfirmNewPassword)
            {
                throw new ArgumentException("Passwords do not match.");
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ArgumentException("New password is required.");
            }

            if (
                !BCrypt.Net.BCrypt.Verify(
                    dto.CurrentPassword ?? string.Empty,
                    user.PasswordHash
                )
            )
            {
                throw new ArgumentException("Current password is incorrect.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            var firstName = SignInMetadataResolver.ExtractFirstName(user.FullName);

            await _emailService.SendPasswordChangedEmailAsync(user.Email, firstName);

            await TryProduceAccountNoticeAsync(
                user.Id,
                type: "password-changed",
                title: "Your Tummly password was changed",
                body: "Your Tummly password was changed. If this wasn't you, reset your password and contact support."
            );
        }

        private async Task<User> LoadUserAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(row => row.Id == userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            return user;
        }

        private async Task<MyAccountSnapshotDto> BuildSnapshotAsync(User user)
        {
            var restaurantId = await ResolveRestaurantIdAsync(user);
            string organisation = string.Empty;
            string role = user.Role;
            IReadOnlyList<MyAccountLocationAccessDto> locationAccess = [];

            if (restaurantId != null)
            {
                var restaurant = await _context.Restaurants
                    .AsNoTracking()
                    .Where(row => row.Id == restaurantId.Value)
                    .Select(row => new { row.Id, row.Name, row.OwnerUserId })
                    .FirstOrDefaultAsync();

                if (restaurant != null)
                {
                    organisation = restaurant.Name;

                    var membership = await _context.RestaurantMemberships
                        .AsNoTracking()
                        .FirstOrDefaultAsync(row =>
                            row.UserId == user.Id
                            && row.RestaurantId == restaurant.Id
                            && row.Status == MembershipStatus.Active
                        );

                    if (membership != null)
                    {
                        role = membership.PermissionRole;
                    }
                    else if (restaurant.OwnerUserId == user.Id)
                    {
                        role = PermissionRoles.Owner;
                    }

                    locationAccess = await ListLocationAccessAsync(
                        restaurant.Id,
                        membership
                    );
                }
            }

            return new MyAccountSnapshotDto
            {
                FullName = user.FullName,
                Email = user.Email,
                JobTitle = user.JobTitle,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = role,
                Organisation = organisation,
                LocationAccess = locationAccess,
                TwoFactorEnabled = false,
            };
        }

        private async Task<int?> ResolveRestaurantIdAsync(User user)
        {
            var activeRestaurantIds = await _context.RestaurantMemberships
                .AsNoTracking()
                .Where(row =>
                    row.UserId == user.Id && row.Status == MembershipStatus.Active
                )
                .Select(row => row.RestaurantId)
                .ToListAsync();

            if (
                user.SelectedRestaurantId is int selected
                && activeRestaurantIds.Contains(selected)
            )
            {
                return selected;
            }

            if (activeRestaurantIds.Count > 0)
            {
                return activeRestaurantIds.OrderBy(id => id).First();
            }

            var ownedIds = await _context.Restaurants
                .AsNoTracking()
                .Where(row => row.OwnerUserId == user.Id)
                .Select(row => row.Id)
                .ToListAsync();

            if (
                user.SelectedRestaurantId is int ownedSelected
                && ownedIds.Contains(ownedSelected)
            )
            {
                return ownedSelected;
            }

            return ownedIds.OrderBy(id => id).Cast<int?>().FirstOrDefault();
        }

        private async Task<IReadOnlyList<MyAccountLocationAccessDto>> ListLocationAccessAsync(
            int restaurantId,
            RestaurantMembership? membership
        )
        {
            var live = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.LocationName)
                .Select(row => new { row.Id, row.LocationName })
                .ToListAsync();

            if (
                membership != null
                && membership.LocationScope == LocationScopeKind.NamedList
            )
            {
                var named = MembershipLocationScope
                    .ParseNamedIds(membership.NamedLocationIdsJson)
                    .ToHashSet();

                live = live.Where(row => named.Contains(row.Id)).ToList();
            }

            return live
                .Select(row => new MyAccountLocationAccessDto
                {
                    LocationName = row.LocationName,
                    AccessLabel = FullAccessLabel,
                })
                .ToList();
        }

        private async Task TryProduceAccountNoticeAsync(
            int userId,
            string type,
            string title,
            string body
        )
        {
            try
            {
                await _notifications.ProduceAsync(
                    new ProduceNotificationRequest
                    {
                        UserId = userId,
                        Type = type,
                        Title = title,
                        Body = body,
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to produce {NotificationType} notice for user {UserId}",
                    type,
                    userId
                );
            }
        }
    }
}
