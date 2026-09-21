using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;

public interface IAdminService
{
    Task<List<AdminTrialRequestDto>>
        GetAllTrialRequestsAsync();

    bool IsTrialPurgeEnabled();

    Task<bool> PurgeTrialRequestAsync(
        int trialRequestId,
        int? actorAdminUserId,
        string actorIdentity
    );

    Task<OperatorSetupReminderBatchResult>
        ProcessOperatorSetupInvitationRemindersAsync();

    Task<AdminTrialRequestDto?> ExtendActivationAsync(
        int userId,
        ExtendActivationDto dto,
        int? actorAdminUserId,
        string actorIdentity
    );

    Task<(byte[] Content, string FileName, string ContentType)?>
        GetActivationDownloadAsync(int userId);
}
