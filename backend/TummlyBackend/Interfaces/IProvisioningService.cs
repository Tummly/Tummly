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

        /// <summary>
        /// Opens the paid signup checkout again when the first Revolut order
        /// did not start (account still Free, no open pay intent).
        /// </summary>
        Task RetryPaidSignupCheckoutAsync(Guid pendingSignupId);

        /// <summary>
        /// Starts the Pilot for a Complete PendingSignup that chose Pilot, when
        /// no Pilot start has run yet. Idempotent. A refused start (already
        /// used) is not an error.
        /// </summary>
        Task EnsurePilotForPendingAsync(Guid pendingSignupId);

        Task GenerateActivationCodeAsync(string inviteToken);
    }
}
