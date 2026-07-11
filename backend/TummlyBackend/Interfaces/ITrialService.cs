using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ITrialService
    {
        Task<PendingTrialRequest> CreateTrialRequestAsync(TrialRequestDto dto);

        Task<TrialVerifyOtpResult> VerifyOtpAsync(VerifyOtpDto dto);

        Task ResendOtpAsync(string email);
    }
}