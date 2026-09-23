using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Signup;

namespace TummlyBackend.Interfaces
{
    public interface IExternalAuthService
    {
        Task<string> BuildStartRedirectAsync(
            string provider,
            string? returnPath,
            CancellationToken cancellationToken = default
        );

        Task<string> HandleCallbackAsync(
            string provider,
            string? code,
            string? state,
            string? error,
            CancellationToken cancellationToken = default
        );

        Task<object> ExchangeSignInAsync(
            ExternalAuthExchangeDto dto,
            CancellationToken cancellationToken = default
        );

        Task<SignupSessionResponse> AcceptTermsAsync(
            ExternalAuthAcceptTermsDto dto,
            CancellationToken cancellationToken = default
        );
    }
}
