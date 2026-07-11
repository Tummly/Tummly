using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;

public interface IAdminService
{
    Task<List<AdminTrialRequestDto>>
        GetAllTrialRequestsAsync();

    bool IsTrialPurgeEnabled();

    Task<bool> PurgeTrialRequestAsync(int trialRequestId);

    Task<OperatorSetupReminderBatchResult>
        ProcessOperatorSetupInvitationRemindersAsync();

    Task<AdminTrialRequestDto?> ExtendActivationAsync(
        int userId,
        ExtendActivationDto dto
    );

    Task<(byte[] Content, string FileName, string ContentType)?>
        GetActivationDownloadAsync(int userId);
}