using TummlyBackend.DTOs.Admin;

namespace TummlyBackend.Interfaces
{
    /// <summary>
    /// Admin-only staff management. Every write needs a reason and writes one
    /// audit row. Outcomes carry a stable code the controller maps to HTTP.
    /// </summary>
    public interface IAdminStaffService
    {
        Task<IReadOnlyList<AdminStaffMemberDto>> ListAsync(
            CancellationToken cancellationToken = default
        );

        Task<StaffOutcome<AdminStaffMemberDto>> CreateAsync(
            int actorAdminId,
            AdminStaffCreateRequest request,
            CancellationToken cancellationToken = default
        );

        Task<StaffOutcome<AdminStaffMemberDto>> DisableAsync(
            int actorAdminId,
            int targetId,
            string? reason,
            CancellationToken cancellationToken = default
        );

        Task<StaffOutcome<AdminStaffMemberDto>> EnableAsync(
            int actorAdminId,
            int targetId,
            string? reason,
            CancellationToken cancellationToken = default
        );

        Task<StaffOutcome<AdminStaffMemberDto>> UnlockAsync(
            int actorAdminId,
            int targetId,
            string? reason,
            CancellationToken cancellationToken = default
        );

        Task<StaffOutcome<AdminStaffMemberDto>> ResetPasswordAsync(
            int actorAdminId,
            int targetId,
            string? newPassword,
            string? reason,
            CancellationToken cancellationToken = default
        );
    }

    public sealed record StaffOutcome<T>(
        T? Value,
        string? Code = null,
        string? Message = null
    )
    {
        public bool Succeeded => Code == null;

        public static StaffOutcome<T> Ok(T value) => new(value);

        public static StaffOutcome<T> Fail(string code, string message) =>
            new(default, code, message);
    }
}
