using TummlyBackend.DTOs.Admin;
using TummlyBackend.Interfaces;
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

    /// <summary>
    /// Admin Pilot extension for the operator's restaurant. Null when the operator owns no restaurant.
    /// </summary>
    Task<PilotExtensionResult?> ExtendPilotAsync(
        int userId,
        ExtendActivationDto dto,
        int? actorAdminUserId,
        string actorIdentity
    );

    Task<AdminPilotAccountDto?> GetPilotAccountAsync(int userId, int? restaurantId);

    Task<IReadOnlyList<AdminPilotAccountDto>> ListPilotAccountsAsync();

    Task<(byte[] Content, string FileName, string ContentType)?>
        GetActivationDownloadAsync(int userId);
}
