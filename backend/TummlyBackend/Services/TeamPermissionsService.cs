using Microsoft.EntityFrameworkCore;
using TummlyBackend.Data;
using TummlyBackend.DTOs.TeamPermissions;
using TummlyBackend.Helpers;
using TummlyBackend.Helpers.EmailTemplates;
using TummlyBackend.Interfaces;
using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    public sealed class TeamPermissionsService : ITeamPermissionsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRestaurantPermissionHelper _permissions;
        private readonly IEmailService _email;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TeamPermissionsService> _logger;

        public TeamPermissionsService(
            ApplicationDbContext context,
            IRestaurantPermissionHelper permissions,
            IEmailService email,
            IConfiguration configuration,
            ILogger<TeamPermissionsService> logger
        )
        {
            _context = context;
            _permissions = permissions;
            _email = email;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<TeamPermissionsPageDto?> GetPageAsync(
            int actorUserId,
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

            var actorMembership = await ActorMembershipAsync(
                actorUserId,
                restaurantId
            );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => new TeamPermissionsLocationDto
                {
                    Id = row.Id,
                    Name = row.LocationName,
                })
                .ToListAsync();

            var memberships = await _context.RestaurantMemberships
                .AsNoTracking()
                .Include(row => row.User)
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync();

            var namesById = locations.ToDictionary(row => row.Id, row => row.Name);

            var members = memberships
                .Select(row => MapMember(
                    row,
                    restaurant.OwnerUserId,
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    namesById
                ))
                .OrderBy(row => row.Status == "active" ? 0 : 1)
                .ThenBy(row => row.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var invitations = await _context.TeamInvitations
                .AsNoTracking()
                .Include(row => row.InviterUser)
                .Where(row => row.RestaurantId == restaurantId)
                .ToListAsync();
            var now = DateTime.UtcNow;
            var invitationRows = invitations
                .Select(row => MapInvitation(
                    row,
                    actorRole,
                    actorCanManage,
                    namesById,
                    now
                ))
                .OrderByDescending(row => row.Expired ? 1 : 0)
                .ThenByDescending(row => row.InvitationId)
                .ToList();

            var active = memberships
                .Where(row => row.Status == MembershipStatus.Active)
                .ToList();

            return new TeamPermissionsPageDto
            {
                ActorCanManage = actorCanManage,
                ActorPermissionRole = actorRole,
                PrivacyConsentHasAccess = DefaultPermissionMatrix.Meets(
                    DefaultPermissionMatrix.LevelFor(
                        actorRole,
                        OperatorAreaIds.PrivacyConsent
                    ),
                    PermissionLevel.View
                ),
                IsSingleLocation = locations.Count <= 1,
                Stats = new TeamPermissionsStatsDto
                {
                    ActiveMembers = active.Count,
                    PendingInvites = invitations.Count(row => row.ExpiresAt > now),
                    LocationManagers = active.Count(row =>
                        row.PermissionRole == PermissionRoles.LocationManager
                    ),
                    LimitedAccessUsers = active.Count(row =>
                        row.LocationScope == LocationScopeKind.NamedList
                    ),
                },
                Locations = locations,
                Members = members,
                Invitations = invitationRows,
            };
        }

        public async Task<string?> UpdateRoleAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId,
            string permissionRole
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.Status != MembershipStatus.Active)
            {
                return "Role cannot change while the member is deactivated.";
            }

            if (
                !TeamPermissionsActor.MayAssignRole(
                    loaded.ActorRole,
                    permissionRole
                )
            )
            {
                return "That permission role is not allowed.";
            }

            var namedIds = MembershipLocationScope.ParseNamedIds(
                target.NamedLocationIdsJson
            );
            var scopeError = MembershipLocationScope.Validate(
                permissionRole,
                target.LocationScope,
                namedIds
            );
            if (scopeError != null)
            {
                return scopeError;
            }

            if (target.PermissionRole == permissionRole)
            {
                return null;
            }

            var from = target.PermissionRole;
            target.PermissionRole = permissionRole;
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.RoleChanged,
                from,
                permissionRole
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> UpdateLocationScopeAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId,
            IReadOnlyList<int> allowedLocationIds,
            string locationScope,
            int[] namedLocationIds
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.PermissionRole == PermissionRoles.Owner)
            {
                return "Owner location scope is not editable.";
            }

            if (target.Status != MembershipStatus.Active)
            {
                return "Location scope cannot change while the member is deactivated.";
            }

            var kind = locationScope == "named"
                ? LocationScopeKind.NamedList
                : LocationScopeKind.AllLocations;
            var named = namedLocationIds.Distinct().ToArray();
            var scopeError = MembershipLocationScope.Validate(
                target.PermissionRole,
                kind,
                named
            );
            if (scopeError != null)
            {
                return scopeError;
            }

            if (kind == LocationScopeKind.NamedList)
            {
                var namedDecision =
                    await _permissions.AuthorizeNamedLocationIdsAsync(
                            allowedLocationIds,
                            named
                        );
                if (namedDecision.Status != RestaurantPermissionStatus.Allowed)
                {
                    return namedDecision.Message;
                }
            }

            var from = FormatScope(target);
            target.LocationScope = kind;
            target.NamedLocationIdsJson = kind == LocationScopeKind.NamedList
                ? MembershipLocationScope.SerializeNamedIds(named)
                : "[]";
            var to = FormatScope(target);
            if (from == to)
            {
                return null;
            }

            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.LocationScopeChanged,
                from,
                to
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> DeactivateAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.Status != MembershipStatus.Active)
            {
                return null;
            }

            ReassignKeyContacts(loaded.Restaurant!, target.UserId);
            target.Status = MembershipStatus.Deactivated;
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.MemberDeactivated,
                MembershipStatus.Active.ToString(),
                MembershipStatus.Deactivated.ToString()
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> ReactivateAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            if (target.Status != MembershipStatus.Deactivated)
            {
                return null;
            }

            target.Status = MembershipStatus.Active;
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.MemberReactivated,
                MembershipStatus.Deactivated.ToString(),
                MembershipStatus.Active.ToString()
            );
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> RemoveAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var loaded = await LoadWriteTargetAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                membershipId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var target = loaded.Target!;
            ReassignKeyContacts(loaded.Restaurant!, target.UserId);
            AddActivity(
                restaurantId,
                actorUserId,
                target,
                AccessActivityKinds.MemberRemoved,
                target.PermissionRole,
                null
            );
            _context.RestaurantMemberships.Remove(target);
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> SendInviteAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            IReadOnlyList<int> allowedLocationIds,
            SendTeamInvitationRequest request
        )
        {
            var email = (request.Email ?? string.Empty).Trim();
            var fullName = (request.FullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return "Enter a valid email address.";
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "Full name is required.";
            }

            var loaded = await LoadInviteActorAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                request.PermissionRole
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var kind = request.LocationScope == "named"
                ? LocationScopeKind.NamedList
                : LocationScopeKind.AllLocations;
            var named = (request.NamedLocationIds ?? []).Distinct().ToArray();
            if (loaded.IsSingleLocation)
            {
                if (
                    request.PermissionRole == PermissionRoles.AreaManager
                    || request.PermissionRole == PermissionRoles.LocationManager
                )
                {
                    kind = LocationScopeKind.NamedList;
                    named = [loaded.Locations[0].Id];
                }
                else
                {
                    kind = LocationScopeKind.AllLocations;
                    named = [];
                }
            }

            var scopeError = MembershipLocationScope.Validate(
                request.PermissionRole,
                kind,
                named
            );
            if (scopeError != null)
            {
                return scopeError;
            }

            if (kind == LocationScopeKind.NamedList)
            {
                var namedDecision =
                    await _permissions.AuthorizeNamedLocationIdsAsync(
                        allowedLocationIds,
                        named
                    );
                if (namedDecision.Status != RestaurantPermissionStatus.Allowed)
                {
                    return namedDecision.Message;
                }
            }

            var deny = await DenySendEmailAsync(
                restaurantId,
                loaded.Restaurant!,
                email
            );
            if (deny != null)
            {
                return deny;
            }

            var existingUser = await FindUserByEmailAsync(email);
            var invite = new TeamInvitation
            {
                RestaurantId = restaurantId,
                Email = email,
                FullName = fullName,
                PermissionRole = request.PermissionRole,
                LocationScope = kind,
                NamedLocationIdsJson = kind == LocationScopeKind.NamedList
                    ? MembershipLocationScope.SerializeNamedIds(named)
                    : "[]",
                Message = string.IsNullOrWhiteSpace(request.Message)
                    ? null
                    : request.Message.Trim(),
                InviterUserId = actorUserId,
                SentAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(TeamInvitation.LifetimeDays),
                OpaqueReference = TeamInvitationReference.Create(),
            };
            _context.TeamInvitations.Add(invite);
            AddInvitationActivity(
                restaurantId,
                actorUserId,
                loaded.ActorName,
                existingUser?.Id,
                existingUser?.FullName ?? fullName,
                email,
                AccessActivityKinds.InvitationSent,
                null,
                request.PermissionRole
            );
            await _context.SaveChangesAsync();
            await TrySendInvitationEmailAsync(
                invite,
                loaded.Restaurant!.Name,
                loaded.ActorName,
                existingUser?.FullName,
                loaded.Locations
            );
            return null;
        }

        public async Task<string?> ResendInviteAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int invitationId
        )
        {
            var loaded = await LoadInvitationAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                invitationId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var invite = loaded.Invite!;
            invite.OpaqueReference = TeamInvitationReference.Create();
            invite.SentAt = DateTime.UtcNow;
            invite.ExpiresAt = DateTime.UtcNow.AddDays(TeamInvitation.LifetimeDays);
            invite.PendingPasswordHash = null;
            var existingUser = await FindUserByEmailAsync(invite.Email);
            AddInvitationActivity(
                restaurantId,
                actorUserId,
                loaded.ActorName,
                existingUser?.Id,
                existingUser?.FullName ?? invite.FullName,
                invite.Email,
                AccessActivityKinds.InvitationResent,
                null,
                invite.PermissionRole
            );
            await _context.SaveChangesAsync();
            await TrySendInvitationEmailAsync(
                invite,
                loaded.Restaurant!.Name,
                loaded.ActorName,
                existingUser?.FullName,
                loaded.Locations
            );
            return null;
        }

        public async Task<string?> RevokeInviteAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int invitationId
        )
        {
            var loaded = await LoadInvitationAsync(
                actorUserId,
                restaurantId,
                actorCanManage,
                invitationId
            );
            if (loaded.Error != null)
            {
                return loaded.Error;
            }

            var invite = loaded.Invite!;
            var existingUser = await FindUserByEmailAsync(invite.Email);
            AddInvitationActivity(
                restaurantId,
                actorUserId,
                loaded.ActorName,
                existingUser?.Id,
                existingUser?.FullName ?? invite.FullName,
                invite.Email,
                AccessActivityKinds.InvitationRevoked,
                invite.PermissionRole,
                null
            );
            _context.TeamInvitations.Remove(invite);
            await _context.SaveChangesAsync();
            return null;
        }

        private async Task<(
            Restaurant? Restaurant,
            string ActorRole,
            string ActorName,
            bool IsSingleLocation,
            List<TeamPermissionsLocationDto> Locations,
            string? Error
        )> LoadInviteActorAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            string inviteRole
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, PermissionRoles.Owner, "", false, [], "Restaurant not found.");
            }

            var actor = await _context.Users.FirstAsync(row => row.Id == actorUserId);
            var actorMembership = await ActorMembershipAsync(actorUserId, restaurantId);
            var actorRole = actorMembership?.PermissionRole ?? PermissionRoles.Owner;
            if (!TeamPermissionsActor.MayInvite(actorRole, actorCanManage, inviteRole))
            {
                return (restaurant, actorRole, actor.FullName, false, [], "forbidden");
            }

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => new TeamPermissionsLocationDto
                {
                    Id = row.Id,
                    Name = row.LocationName,
                })
                .ToListAsync();
            return (
                restaurant,
                actorRole,
                actor.FullName,
                locations.Count <= 1,
                locations,
                null
            );
        }

        private async Task<(
            TeamInvitation? Invite,
            Restaurant? Restaurant,
            string ActorName,
            List<TeamPermissionsLocationDto> Locations,
            string? Error
        )> LoadInvitationAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int invitationId
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, null, "", [], "Restaurant not found.");
            }

            var actor = await _context.Users.FirstAsync(row => row.Id == actorUserId);
            var actorMembership = await ActorMembershipAsync(actorUserId, restaurantId);
            var actorRole = actorMembership?.PermissionRole ?? PermissionRoles.Owner;
            var invite = await _context.TeamInvitations
                .FirstOrDefaultAsync(row =>
                    row.Id == invitationId && row.RestaurantId == restaurantId
                );
            if (invite == null)
            {
                return (null, restaurant, actor.FullName, [], "Invitation not found.");
            }

            if (
                !TeamPermissionsActor.MayInvite(
                    actorRole,
                    actorCanManage,
                    invite.PermissionRole
                )
            )
            {
                return (null, restaurant, actor.FullName, [], "forbidden");
            }

            var locations = await _context.RestaurantLocations
                .AsNoTracking()
                .Where(row => row.RestaurantId == restaurantId)
                .OrderBy(row => row.Id)
                .Select(row => new TeamPermissionsLocationDto
                {
                    Id = row.Id,
                    Name = row.LocationName,
                })
                .ToListAsync();
            return (invite, restaurant, actor.FullName, locations, null);
        }

        private async Task<string?> DenySendEmailAsync(
            int restaurantId,
            Restaurant restaurant,
            string email
        )
        {
            var lower = email.ToLowerInvariant();
            var owner = await _context.Users.FirstAsync(
                row => row.Id == restaurant.OwnerUserId
            );
            if (owner.Email.ToLower() == lower)
            {
                return "You cannot invite the Account owner.";
            }

            var staff = await _context.Admins.AnyAsync(row =>
                row.Email.ToLower() == lower
                && (row.Role == "Admin" || row.Role == "Support")
            );
            var staffUser = await _context.Users.AnyAsync(row =>
                row.Email.ToLower() == lower
                && (row.Role == "Admin" || row.Role == "Support")
            );
            if (staff || staffUser)
            {
                return "This email belongs to Tummly staff.";
            }

            var openTrial = await _context.TrialRequests.AnyAsync(row =>
                row.Email.ToLower() == lower && !row.IsAccountCreated
            );
            if (openTrial)
            {
                return "This email has an open trial request.";
            }

            var membership = await _context.RestaurantMemberships
                .Include(row => row.User)
                .FirstOrDefaultAsync(row =>
                    row.RestaurantId == restaurantId
                    && row.User.Email.ToLower() == lower
                );
            if (membership != null)
            {
                return membership.Status == MembershipStatus.Deactivated
                    ? "This person is deactivated. Reactivate them instead of inviting."
                    : "This email is already a member of this workspace.";
            }

            var pending = await _context.TeamInvitations.AnyAsync(row =>
                row.RestaurantId == restaurantId && row.Email.ToLower() == lower
            );
            if (pending)
            {
                return "An invitation is already pending for this email.";
            }

            return null;
        }

        private async Task<User?> FindUserByEmailAsync(string email)
        {
            var lower = email.ToLowerInvariant();
            return await _context.Users.FirstOrDefaultAsync(row =>
                row.Email.ToLower() == lower
            );
        }

        private async Task TrySendInvitationEmailAsync(
            TeamInvitation invite,
            string workspaceName,
            string inviterName,
            string? existingUserName,
            IReadOnlyList<TeamPermissionsLocationDto> locations
        )
        {
            var namesById = locations.ToDictionary(row => row.Id, row => row.Name);
            var named = MembershipLocationScope.ParseNamedIds(
                invite.NamedLocationIdsJson
            );
            var locationScope = MembershipLocationScope.FormatAccessLabel(
                invite.LocationScope,
                named,
                namesById
            );
            var greetingSource = existingUserName ?? invite.FullName;
            var firstName = TeamInvitationReference.FirstName(greetingSource);
            var acceptUrl = TeamInvitationReference.AcceptUrl(
                FrontendBaseUrl(),
                invite.OpaqueReference
            );
            var subject = TeamInvitationEmailTemplate.Subject(workspaceName);
            try
            {
                await _email.SendTeamInvitationEmailAsync(
                    invite.Email,
                    subject,
                    acceptUrl,
                    firstName,
                    inviterName,
                    workspaceName,
                    invite.PermissionRole,
                    locationScope,
                    invite.Message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Team invitation email failed for {Email}",
                    invite.Email
                );
            }
        }

        private string FrontendBaseUrl()
        {
            var frontendBaseUrl =
                _configuration["Frontend:BaseUrl"]?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(frontendBaseUrl))
            {
                return "http://localhost:5173";
            }

            return frontendBaseUrl;
        }

        private static TeamInvitationRowDto MapInvitation(
            TeamInvitation row,
            string actorRole,
            bool actorCanManage,
            IReadOnlyDictionary<int, string> namesById,
            DateTime now
        )
        {
            var named = MembershipLocationScope.ParseNamedIds(
                row.NamedLocationIdsJson
            );
            return new TeamInvitationRowDto
            {
                InvitationId = row.Id,
                Email = row.Email,
                PermissionRole = row.PermissionRole,
                LocationAccessLabel = MembershipLocationScope.FormatAccessLabel(
                    row.LocationScope,
                    named,
                    namesById
                ),
                InvitedBy = row.InviterUser.FullName,
                SentLabel = LondonDateFormat.DMmmYyyy(row.SentAt),
                ExpiresLabel = LondonDateFormat.DMmmYyyy(row.ExpiresAt),
                Expired = row.ExpiresAt <= now,
                Actions = TeamPermissionsActor.InvitationActionsFor(
                    actorRole,
                    actorCanManage,
                    row.PermissionRole
                ).ToList(),
            };
        }

        private async Task<(
            RestaurantMembership? Target,
            Restaurant? Restaurant,
            string ActorRole,
            string? Error
        )> LoadWriteTargetAsync(
            int actorUserId,
            int restaurantId,
            bool actorCanManage,
            int membershipId
        )
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(row => row.Id == restaurantId);
            if (restaurant == null)
            {
                return (null, null, PermissionRoles.Owner, "Restaurant not found.");
            }

            var actorMembership = await ActorMembershipAsync(
                actorUserId,
                restaurantId
            );
            var actorRole =
                actorMembership?.PermissionRole ?? PermissionRoles.Owner;

            var target = await _context.RestaurantMemberships
                .FirstOrDefaultAsync(row =>
                    row.Id == membershipId && row.RestaurantId == restaurantId
                );
            if (target == null)
            {
                return (null, restaurant, actorRole, "Member not found.");
            }

            if (
                !TeamPermissionsActor.MayWriteTarget(
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    restaurant.OwnerUserId,
                    target
                )
            )
            {
                return (null, restaurant, actorRole, "forbidden");
            }

            return (target, restaurant, actorRole, null);
        }

        private async Task<RestaurantMembership?> ActorMembershipAsync(
            int actorUserId,
            int restaurantId
        )
        {
            return await _context.RestaurantMemberships
                .AsNoTracking()
                .FirstOrDefaultAsync(row =>
                    row.UserId == actorUserId
                    && row.RestaurantId == restaurantId
                    && row.Status == MembershipStatus.Active
                );
        }

        private static TeamMemberRowDto MapMember(
            RestaurantMembership row,
            int ownerUserId,
            string actorRole,
            bool actorCanManage,
            int actorUserId,
            IReadOnlyDictionary<int, string> namesById
        )
        {
            var named = MembershipLocationScope.ParseNamedIds(
                row.NamedLocationIdsJson
            );
            return new TeamMemberRowDto
            {
                MembershipId = row.Id,
                UserId = row.UserId,
                FullName = row.User.FullName,
                Email = row.User.Email,
                PermissionRole = row.PermissionRole,
                LocationScope =
                    row.LocationScope == LocationScopeKind.NamedList
                        ? "named"
                        : "all",
                NamedLocationIds = named.ToList(),
                LocationAccessLabel = MembershipLocationScope.FormatAccessLabel(
                    row.LocationScope,
                    named,
                    namesById
                ),
                Status =
                    row.Status == MembershipStatus.Active
                        ? "active"
                        : "deactivated",
                IsAccountOwner = row.UserId == ownerUserId,
                Actions = TeamPermissionsActor.ActionsFor(
                    actorRole,
                    actorCanManage,
                    actorUserId,
                    ownerUserId,
                    row
                ).ToList(),
            };
        }

        private void AddInvitationActivity(
            int restaurantId,
            int actorUserId,
            string actorDisplayName,
            int? targetUserId,
            string targetDisplayName,
            string targetEmail,
            string kind,
            string? from,
            string? to
        )
        {
            _context.RestaurantAccessActivities.Add(
                new RestaurantAccessActivity
                {
                    RestaurantId = restaurantId,
                    ActorUserId = actorUserId,
                    ActorDisplayName = actorDisplayName,
                    TargetUserId = targetUserId,
                    TargetDisplayName = targetDisplayName,
                    TargetEmail = targetEmail,
                    Kind = kind,
                    FromValue = from,
                    ToValue = to,
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }

        private static string FormatScope(RestaurantMembership row)
        {
            if (row.LocationScope == LocationScopeKind.AllLocations)
            {
                return "all";
            }

            return row.NamedLocationIdsJson;
        }

        private static void ReassignKeyContacts(Restaurant restaurant, int userId)
        {
            if (restaurant.BillingContactUserId == userId)
            {
                restaurant.BillingContactUserId = restaurant.OwnerUserId;
            }

            if (restaurant.PrivacyContactUserId == userId)
            {
                restaurant.PrivacyContactUserId = restaurant.OwnerUserId;
            }

            if (restaurant.SupportContactUserId == userId)
            {
                restaurant.SupportContactUserId = restaurant.OwnerUserId;
            }
        }

        private void AddActivity(
            int restaurantId,
            int actorUserId,
            RestaurantMembership target,
            string kind,
            string? from,
            string? to
        )
        {
            if (from == to)
            {
                return;
            }

            _context.RestaurantAccessActivities.Add(
                new RestaurantAccessActivity
                {
                    RestaurantId = restaurantId,
                    ActorUserId = actorUserId,
                    TargetUserId = target.UserId,
                    TargetMembershipId = target.Id,
                    Kind = kind,
                    FromValue = from,
                    ToValue = to,
                    OccurredAt = DateTime.UtcNow,
                }
            );
        }
    }
}
