using TummlyBackend.DTOs.Admin;
using TummlyBackend.Models;

public interface IAdminService
{
    Task<List<TrialRequest>>
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


}