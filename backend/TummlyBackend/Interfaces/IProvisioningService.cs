using TummlyBackend.DTOs.Provisioning;
using TummlyBackend.DTOs.Trial;

namespace TummlyBackend.Interfaces
{
    public interface IProvisioningService
    {
        Task<InviteTokenResult> ValidateInviteTokenAsync(string token);

        Task ProvisionAsync(CompleteSetupDto dto);

        /// <summary>
        /// Creates operator User / Restaurant / locations / GuestLoop / billing
        /// from a PendingSignup. Uses existing PasswordHash. Idempotent when
        /// status is already Complete.
        /// </summary>
        Task ProvisionFromPendingAsync(Guid pendingSignupId);

        Task GenerateActivationCodeAsync(string inviteToken);
    }
}
