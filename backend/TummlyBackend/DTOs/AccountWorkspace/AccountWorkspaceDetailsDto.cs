namespace TummlyBackend.DTOs.AccountWorkspace
{
    public sealed class AccountWorkspaceDetailsDto
    {
        public bool Success { get; set; } = true;

        public string WorkspaceName { get; set; } = string.Empty;

        public string AccountStructure { get; set; } = string.Empty;

        public string? BusinessCategory { get; set; }

        public string? BusinessCategoryLabel { get; set; }

        public string MainOperatingCountry { get; set; } = "United Kingdom";

        public string? BrandLogoOperatorUrl { get; set; }

        public string? BrandLogoPublicUrl { get; set; }

        public DateTime? LastSavedAt { get; set; }

        public bool IsAccountOwner { get; set; }

        public int RestaurantId { get; set; }

        public AccountWorkspaceStatusDto Status { get; set; } = new();

        public RestaurantBusinessDetailsDto? BusinessDetails { get; set; }

        public AccountWorkspaceKeyContactsDto? KeyContacts { get; set; }

        public AccountWorkspaceWorkspaceDefaultsDto? WorkspaceDefaults { get; set; }
    }

    public sealed class AccountWorkspaceStatusDto
    {
        public string WorkspaceStatus { get; set; } = "Active";

        public string PlanStatus { get; set; } = "Pilot";

        public string BillingStatus { get; set; } = "Active";

        public DateTime AccountCreatedAt { get; set; }

        public int ActiveLocations { get; set; }

        public int TeamMembers { get; set; } = 1;

        public int GuestProfiles { get; set; }

        public string GuestFormStatus { get; set; } = "Live";

        public DateTime LastAccountUpdateAt { get; set; }
    }
}
