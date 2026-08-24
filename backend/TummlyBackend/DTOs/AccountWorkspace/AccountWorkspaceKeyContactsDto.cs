namespace TummlyBackend.DTOs.AccountWorkspace
{
    public sealed class TeamMemberPickerItemDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }

    public sealed class AccountWorkspaceKeyContactsDto
    {
        public TeamMemberPickerItemDto AccountOwner { get; set; } = new();

        public int BillingContactUserId { get; set; }

        public int PrivacyContactUserId { get; set; }

        public int SupportContactUserId { get; set; }

        public List<TeamMemberPickerItemDto> EligibleMembers { get; set; } = [];
    }

    public sealed class UpdateKeyContactsRequest
    {
        public int BillingContactUserId { get; set; }

        public int PrivacyContactUserId { get; set; }

        public int SupportContactUserId { get; set; }

        /// <summary>
        /// Optional. When set to a value other than the current Account owner, reject.
        /// </summary>
        public int? AccountOwnerUserId { get; set; }
    }
}
