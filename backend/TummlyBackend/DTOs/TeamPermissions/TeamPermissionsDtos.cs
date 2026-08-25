namespace TummlyBackend.DTOs.TeamPermissions
{
    public sealed class TeamPermissionsPageDto
    {
        public bool ActorCanManage { get; set; }

        public string ActorPermissionRole { get; set; } = string.Empty;

        public bool PrivacyConsentHasAccess { get; set; }

        public bool IsSingleLocation { get; set; }

        public TeamPermissionsStatsDto Stats { get; set; } = new();

        public List<TeamPermissionsLocationDto> Locations { get; set; } = [];

        public List<TeamMemberRowDto> Members { get; set; } = [];

        public List<PermissionMatrixAreaDto> Matrix { get; set; } = [];

        public List<TeamInvitationRowDto> Invitations { get; set; } = [];
    }

    public sealed class PermissionMatrixAreaDto
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public Dictionary<string, string> Cells { get; set; } = [];
    }

    public sealed class UpdateAdminMatrixRequest
    {
        public List<AdminMatrixCellDto> AdminCells { get; set; } = [];
    }

    public sealed class AdminMatrixCellDto
    {
        public string AreaId { get; set; } = string.Empty;

        public string Level { get; set; } = string.Empty;
    }

    public sealed class TeamPermissionsStatsDto
    {
        public int ActiveMembers { get; set; }

        public int PendingInvites { get; set; }

        public int LocationManagers { get; set; }

        public int LimitedAccessUsers { get; set; }
    }

    public sealed class TeamPermissionsLocationDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class TeamMemberRowDto
    {
        public int MembershipId { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PermissionRole { get; set; } = string.Empty;

        public string LocationScope { get; set; } = "all";

        public List<int> NamedLocationIds { get; set; } = [];

        public string LocationAccessLabel { get; set; } = string.Empty;

        public string Status { get; set; } = "active";

        public bool IsAccountOwner { get; set; }

        public List<string> Actions { get; set; } = [];
    }

    public sealed class TeamInvitationRowDto
    {
        public int InvitationId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PermissionRole { get; set; } = string.Empty;

        public string LocationAccessLabel { get; set; } = string.Empty;

        public string InvitedBy { get; set; } = string.Empty;

        public string SentLabel { get; set; } = string.Empty;

        public string ExpiresLabel { get; set; } = string.Empty;

        public bool Expired { get; set; }

        public List<string> Actions { get; set; } = [];
    }

    public sealed class SendTeamInvitationRequest
    {
        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PermissionRole { get; set; } = string.Empty;

        public string LocationScope { get; set; } = "all";

        public int[] NamedLocationIds { get; set; } = [];

        public string? Message { get; set; }
    }

    public sealed class UpdateMemberRoleRequest
    {
        public string PermissionRole { get; set; } = string.Empty;
    }

    public sealed class UpdateMemberLocationScopeRequest
    {
        public string LocationScope { get; set; } = "all";

        public int[] NamedLocationIds { get; set; } = [];
    }

    public sealed class AccessActivityListDto
    {
        public List<AccessActivityRowDto> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }
    }

    public sealed class AccessActivityRowDto
    {
        public int Id { get; set; }

        public string Kind { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }

        public string ActorDisplayName { get; set; } = string.Empty;

        public string? TargetDisplayName { get; set; }

        public string? TargetEmail { get; set; }

        public string? FromValue { get; set; }

        public string? ToValue { get; set; }
    }
}
