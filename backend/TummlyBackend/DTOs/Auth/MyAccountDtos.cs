namespace TummlyBackend.DTOs.Auth
{
    public sealed class MyAccountLocationAccessDto
    {
        public string LocationName { get; set; } = string.Empty;

        public string AccessLabel { get; set; } = "Full access";
    }

    public sealed class MyAccountSnapshotDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Organisation { get; set; } = string.Empty;

        public IReadOnlyList<MyAccountLocationAccessDto> LocationAccess { get; set; } =
            [];

        public bool TwoFactorEnabled { get; set; }
    }

    public sealed class UpdateMyAccountProfileDto
    {
        public string FullName { get; set; } = string.Empty;

        public string? JobTitle { get; set; }

        public string? PhoneNumber { get; set; }
    }

    public sealed class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;

        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
