using TummlyBackend.DTOs.TeamPermissions;

namespace TummlyBackend.Interfaces
{
    public interface ITeamInvitationAcceptService
    {
        Task<(TeamInvitationPreviewDto? Preview, string? Error)> PreviewAsync(
            string? invite,
            int? sessionUserId
        );

        Task<string?> StoreCredentialsAndSendOtpAsync(
            TeamInvitationCredentialsRequest request
        );

        Task<string?> SignInAndSendOtpAsync(
            TeamInvitationSignInRequest request
        );

        Task<(object? Session, string? Error)> VerifyOtpAndAcceptAsync(
            TeamInvitationVerifyOtpRequest request
        );

        Task<(object? Session, string? Error)> AcceptInPlaceAsync(
            int userId,
            string? invite
        );
    }
}
