using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.TeamPermissions;
using TummlyBackend.Helpers;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class TeamInvitationAcceptService : ITeamInvitationAcceptService
    {
        public const string InvalidInviteMessage = "This invitation is not valid.";

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;
        private readonly IJwtService _jwt;

        public TeamInvitationAcceptService(
            ApplicationDbContext context,
            IEmailService email,
            IJwtService jwt
        )
        {
            _context = context;
            _email = email;
            _jwt = jwt;
        }

        public async Task<(TeamInvitationPreviewDto? Preview, string? Error)> PreviewAsync(
            string? invite,
            int? sessionUserId
        )
        {
            var loaded = await LoadLiveInviteAsync(invite);
            if (loaded.Error != null)
            {
                return (null, loaded.Error);
            }

            var row = loaded.Invite!;
            var existing = await FindUserByEmailAsync(row.Email);
            var session = "logged-out";
            if (sessionUserId != null)
            {
                var sessionUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user => user.Id == sessionUserId.Value);
                if (sessionUser != null)
                {
                    session = string.Equals(
                        sessionUser.Email,
                        row.Email,
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "invited-email"
                        : "wrong-email";
                }
            }

            var namesById = await LocationNamesAsync(row.RestaurantId);
            var named = MembershipLocationScope.ParseNamedIds(row.NamedLocationIdsJson);
            return (
                new TeamInvitationPreviewDto
                {
                    Email = row.Email,
                    FullName = existing?.FullName ?? row.FullName,
                    WorkspaceName = loaded.Restaurant!.Name,
                    RoleName = row.PermissionRole,
                    LocationScope = MembershipLocationScope.FormatAccessLabel(
                        row.LocationScope,
                        named,
                        namesById
                    ),
                    ExistingUser = existing != null,
                    Session = session,
                    OwnerActivation = OwnerActivationStatus(loaded.Owner!),
                },
                null
            );
        }

        public async Task<string?> StoreCredentialsAndSendOtpAsync(
            TeamInvitationCredentialsRequest request
        )
        {
            var loaded = await LoadLiveInviteAsync(request.Invite);
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            if (await FindUserByEmailAsync(loaded.Invite!.Email) != null)
            {
                return "Sign in with the invited email to accept.";
            }

            var fullName = (request.FullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Full name is required.";
            }

            if (!PasswordStrengthHelper.IsAtLeastGood(request.Password))
            {
                return PasswordStrengthHelper.GetValidationMessage(request.Password);
            }

            loaded.Invite.FullName = fullName;
            loaded.Invite.PendingPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                request.Password
            );
            await SendEmailOtpAsync(loaded.Invite.Email);
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> SignInAndSendOtpAsync(
            TeamInvitationSignInRequest request
        )
        {
            var loaded = await LoadLiveInviteAsync(request.Invite);
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var user = await FindUserByEmailAsync(loaded.Invite!.Email);
            if (user == null)
            {
                return "Create your account with the invited email.";
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return "Invalid email or password.";
            }

            await SendEmailOtpAsync(user.Email, user.Id);
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<(object? Session, string? Error)> VerifyOtpAndAcceptAsync(
            TeamInvitationVerifyOtpRequest request
        )
        {
            var loaded = await LoadLiveInviteAsync(request.Invite);
            if (loaded.Error != null)
            {
                return (null, loaded.Error);
            }

            var email = (request.Email ?? string.Empty).Trim();
            if (!string.Equals(email, loaded.Invite!.Email, StringComparison.OrdinalIgnoreCase))
            {
                return (null, InvalidInviteMessage);
            }

            var otp = await _context.OtpVerifications
                .Where(row =>
                    row.Email.ToLower() == email.ToLower()
                    && !row.IsUsed
                    && row.ExpiresAt > DateTime.UtcNow
                )
                .OrderByDescending(row => row.CreatedAt)
                .FirstOrDefaultAsync();
            if (otp == null || otp.OtpCode != (request.OtpCode ?? string.Empty).Trim())
            {
                return (null, "Invalid OTP.");
            }

            otp.IsUsed = true;
            var user = await FindUserByEmailAsync(email);
            if (user == null)
            {
                if (string.IsNullOrWhiteSpace(loaded.Invite.PendingPasswordHash))
                {
                    return (null, "Create your account before verifying.");
                }

                user = await CreateInviteeUserAsync(loaded.Invite, loaded.Restaurant!, loaded.Owner!);
            }

            var error = await CompleteMembershipAsync(
                loaded.Invite,
                loaded.Restaurant!,
                loaded.Owner!,
                user
            );
            if (error != null)
            {
                return (null, error);
            }

            return (await IssueSessionAsync(user, loaded.Owner!), null);
        }

        public async Task<(object? Session, string? Error)> AcceptInPlaceAsync(
            int userId,
            string? invite
        )
        {
            var loaded = await LoadLiveInviteAsync(invite);
            if (loaded.Error != null)
            {
                return (null, loaded.Error);
            }

            var user = await _context.Users.FirstAsync(row => row.Id == userId);
            if (!string.Equals(user.Email, loaded.Invite!.Email, StringComparison.OrdinalIgnoreCase))
            {
                return (null, "Sign out and continue with the invited email.");
            }

            var error = await CompleteMembershipAsync(
                loaded.Invite,
                loaded.Restaurant!,
                loaded.Owner!,
                user
            );
            if (error != null)
            {
                return (null, error);
            }

            return (await IssueSessionAsync(user, loaded.Owner!), null);
        }

        private async Task<(
            TeamInvitation? Invite,
            Restaurant? Restaurant,
            User? Owner,
            string? Error
        )> LoadLiveInviteAsync(string? invite)
        {
            if (string.IsNullOrWhiteSpace(invite))
            {
                return (null, null, null, InvalidInviteMessage);
            }

            var row = await _context.TeamInvitations
                .FirstOrDefaultAsync(item => item.OpaqueReference == invite.Trim());
            if (row == null || row.ExpiresAt <= DateTime.UtcNow)
            {
                return (null, null, null, InvalidInviteMessage);
            }

            var restaurant = await _context.Restaurants
                .FirstAsync(item => item.Id == row.RestaurantId);
            var owner = await _context.Users.FirstAsync(
                item => item.Id == restaurant.OwnerUserId
            );
            return (row, restaurant, owner, null);
        }

        private async Task<User?> FindUserByEmailAsync(string email)
        {
            var lower = email.ToLowerInvariant();
            return await _context.Users.FirstOrDefaultAsync(row =>
                row.Email.ToLower() == lower
            );
        }

        private async Task SendEmailOtpAsync(string email, int? userId = null)
        {
            var old = await _context.OtpVerifications
                .Where(row => row.Email == email && !row.IsUsed)
                .ToListAsync();
            if (old.Count > 0)
            {
                _context.OtpVerifications.RemoveRange(old);
            }

            var otpCode = Random.Shared.Next(100000, 999999).ToString();
            _context.OtpVerifications.Add(
                new OtpVerification
                {
                    UserId = userId,
                    Email = email,
                    OtpCode = otpCode,
                    Channel = OtpVerification.ChannelEmail,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                }
            );
            await _email.SendOtpEmailAsync(email, otpCode);
        }

        private async Task<User> CreateInviteeUserAsync(
            TeamInvitation invite,
            Restaurant restaurant,
            User owner
        )
        {
            var firstLocation = await _context.RestaurantLocations
                .Where(row => row.RestaurantId == restaurant.Id)
                .OrderBy(row => row.Id)
                .Select(row => (int?)row.Id)
                .FirstOrDefaultAsync();

            var user = new User
            {
                FullName = invite.FullName,
                Email = invite.Email,
                PasswordHash = invite.PendingPasswordHash!,
                PhoneNumber = string.Empty,
                Role = "Owner",
                AccountType = restaurant.AccountType,
                IsEmailVerified = true,
                IsApprovedByAdmin = true,
                CreatedAt = DateTime.UtcNow,
                HasCompletedFirstSignIn = true,
                TermsAccepted = true,
                SelectedRestaurantId = restaurant.Id,
                SelectedLocationId = firstLocation,
                ActivatedAt = DateTime.UtcNow,
                ActivationExpiresAt = owner.ActivationExpiresAt
                    ?? DateTime.UtcNow.AddDays(30),
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        private async Task<string?> CompleteMembershipAsync(
            TeamInvitation invite,
            Restaurant restaurant,
            User owner,
            User invitee
        )
        {
            var already = await _context.RestaurantMemberships.AnyAsync(row =>
                row.UserId == invitee.Id && row.RestaurantId == restaurant.Id
            );
            if (already)
            {
                _context.TeamInvitations.Remove(invite);
                await _context.SaveChangesAsync();
                return null;
            }

            invitee.SelectedRestaurantId = restaurant.Id;
            invitee.HasCompletedFirstSignIn = true;
            if (invitee.ActivatedAt == null)
            {
                invitee.ActivatedAt = DateTime.UtcNow;
                invitee.ActivationExpiresAt = owner.ActivationExpiresAt
                    ?? DateTime.UtcNow.AddDays(30);
            }

            var membership = new RestaurantMembership
            {
                UserId = invitee.Id,
                RestaurantId = restaurant.Id,
                PermissionRole = invite.PermissionRole,
                LocationScope = invite.LocationScope,
                NamedLocationIdsJson = invite.NamedLocationIdsJson,
                Status = MembershipStatus.Active,
            };
            _context.RestaurantMemberships.Add(membership);
            _context.RestaurantAccessActivities.Add(
                new RestaurantAccessActivity
                {
                    RestaurantId = restaurant.Id,
                    ActorUserId = invitee.Id,
                    ActorDisplayName = invitee.FullName,
                    TargetUserId = invitee.Id,
                    TargetDisplayName = invitee.FullName,
                    TargetEmail = invitee.Email,
                    Kind = AccessActivityKinds.InvitationAccepted,
                    ToValue = invite.PermissionRole,
                    OccurredAt = DateTime.UtcNow,
                }
            );
            _context.TeamInvitations.Remove(invite);
            await _context.SaveChangesAsync();
            return null;
        }

        private async Task<object> IssueSessionAsync(User user, User owner)
        {
            var refresh = await RefreshTokenHelper.IssueAsync(_context, user.Id);
            await _context.SaveChangesAsync();
            var workspaceCount = await _context.RestaurantMemberships.CountAsync(row =>
                row.UserId == user.Id && row.Status == MembershipStatus.Active
            );
            var token = _jwt.GenerateToken(
                user.Id.ToString(),
                user.Email,
                user.Role
            );
            return new
            {
                token,
                refreshToken = refresh,
                accountType = user.AccountType,
                workspaceCount,
                restaurantId = user.SelectedRestaurantId,
                selectedLocationId = user.SelectedLocationId,
                activationRequired = false,
                ownerActivation = OwnerActivationStatus(owner),
            };
        }

        private static string OwnerActivationStatus(User owner)
        {
            var subject = ActivationSubject.FromUser(owner);
            if (ActivationState.RequiresActivation(subject))
            {
                return "pending";
            }

            if (ActivationState.IsActivationExpired(subject))
            {
                return "expired";
            }

            return "ok";
        }

        private async Task<Dictionary<int, string>> LocationNamesAsync(int restaurantId)
        {
            return await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .ToDictionaryAsync(row => row.Id, row => row.LocationName);
        }
    }
}
