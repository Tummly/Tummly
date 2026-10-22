using TummlyBackend.DTOs.Provisioning;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Interfaces;

namespace TummlyBackend.Tests.Helpers
{
    public sealed class NoOpProvisioningService : IProvisioningService
    {
        public Task<InviteTokenResult> ValidateInviteTokenAsync(string token) =>
            throw new NotImplementedException();

        public Task ProvisionAsync(CompleteSetupDto dto) => Task.CompletedTask;

        public Task ProvisionFromPendingAsync(Guid pendingSignupId) =>
            Task.CompletedTask;

        public Task RetryPaidSignupCheckoutAsync(Guid pendingSignupId) =>
            Task.CompletedTask;

        public Task GenerateActivationCodeAsync(string inviteToken) =>
            Task.CompletedTask;
    }
}
