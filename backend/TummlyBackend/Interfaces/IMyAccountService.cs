using TummlyBackend.DTOs.Auth;

namespace TummlyBackend.Interfaces
{
    public interface IMyAccountService
    {
        Task<MyAccountSnapshotDto> GetSnapshotAsync(int userId);

        Task<MyAccountSnapshotDto> UpdateProfileAsync(
            int userId,
            UpdateMyAccountProfileDto dto
        );

        Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}
