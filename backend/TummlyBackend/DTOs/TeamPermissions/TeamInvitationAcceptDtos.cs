namespace TummlyBackend.DTOs.TeamPermissions
{
    public sealed class TeamInvitationPreviewDto
    {
        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string WorkspaceName { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string LocationScope { get; set; } = string.Empty;

        public bool ExistingUser { get; set; }

        public string Session { get; set; } = "logged-out";

        public string OwnerActivation { get; set; } = "ok";
    }

    public sealed class TeamInvitationCredentialsRequest
    {
        public string Invite { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public sealed class TeamInvitationSignInRequest
    {
        public string Invite { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public sealed class TeamInvitationVerifyOtpRequest
    {
        public string Invite { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string OtpCode { get; set; } = string.Empty;
    }
}
