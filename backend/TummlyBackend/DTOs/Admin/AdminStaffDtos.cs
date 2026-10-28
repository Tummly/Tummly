namespace TummlyBackend.DTOs.Admin
{
    /// <summary>One Admins row for the staff table. Never carries the password hash.</summary>
    public sealed record AdminStaffMemberDto(
        int Id,
        string FullName,
        string Email,
        string Role,
        bool IsActive,
        bool IsLocked,
        int FailedLoginAttempts,
        DateTime CreatedAt,
        bool UsesDefaultPassword
    );

    public sealed record AdminStaffCreateRequest(
        string? FullName,
        string? Email,
        string? Role,
        string? InitialPassword,
        string? Reason
    );

    public sealed record AdminStaffReasonRequest(string? Reason);

    public sealed record AdminStaffResetPasswordRequest(
        string? NewPassword,
        string? Reason
    );

    public sealed record StaffChangePasswordRequest(
        string? CurrentPassword,
        string? NewPassword
    );
}
