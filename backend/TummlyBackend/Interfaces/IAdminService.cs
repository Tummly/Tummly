using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;

public interface IAdminService
{
    Task<List<AdminTrialRequestDto>>
        GetAllTrialRequestsAsync();

    Task<bool>
        UpdateTrialStatusAsync(
            UpdateTrialStatusDto dto
        );

    Task<object>
        ApproveTrialRequestAsync(
            int trialRequestId
        );

    Task<object>
        ResendInviteAsync(
            int trialRequestId
        );
    Task<object> DeclineRequestAsync(int trialRequestId);

    Task<object> RequestMoreInfoAsync(int trialRequestId);

    bool IsTrialPurgeEnabled();

    Task<bool> PurgeTrialRequestAsync(int trialRequestId);

    Task<int> ProcessOperatorSetupInvitationRemindersAsync();

    Task<AdminTrialRequestDto?> ExtendActivationAsync(
        int userId,
        ExtendActivationDto dto
    );

    Task<(byte[] Content, string FileName, string ContentType)?>
        GetActivationDownloadAsync(int userId);
}