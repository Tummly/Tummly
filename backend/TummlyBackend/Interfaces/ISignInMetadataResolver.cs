using TummlyBackend.DTOs.Auth;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ISignInMetadataResolver
    {
        Task<NewDeviceSignInDetails> ResolveAsync(
            User user,
            SignInContext signInContext,
            CancellationToken cancellationToken = default
        );
    }
}
