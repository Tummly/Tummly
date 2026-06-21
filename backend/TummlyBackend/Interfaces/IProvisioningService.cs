using TummlyBackend.DTOs.Provisioning;
using TummlyBackend.DTOs.Trial;

namespace TummlyBackend.Interfaces
{
    public interface IProvisioningService
    {
        Task<InviteTokenResult> ValidateInviteTokenAsync(string token);

        Task ProvisionAsync(CompleteSetupDto dto);
    }
}
