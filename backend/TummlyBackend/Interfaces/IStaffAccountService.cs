namespace TummlyBackend.Interfaces
{
    /// <summary>Self-service actions for the signed-in Admin or Support account.</summary>
    public interface IStaffAccountService
    {
        Task<StaffOutcome<bool>> ChangeOwnPasswordAsync(
            int adminId,
            string? currentPassword,
            string? newPassword,
            CancellationToken cancellationToken = default
        );
    }
}
