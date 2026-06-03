using TummlyBackend.DTOs.Auth;
using TummlyBackend.DTOs.Trial;
using TummlyBackend.Models;

namespace TummlyBackend.Interfaces
{
    public interface ITrialService
    {
        Task<PendingTrialRequest> CreateTrialRequestAsync(TrialRequestDto dto);

        Task<bool> VerifyOtpAsync(VerifyOtpDto dto);

        Task ResendOtpAsync(string email);

        /*
         =========================================
         VALIDATE SETUP TOKEN
         =========================================
        */

        Task<object> ValidateSetupTokenAsync(
            string token
        );

        /*
         =========================================
         COMPLETE ACCOUNT SETUP
         =========================================
        */

        Task<bool> CompleteAccountSetupAsync(
            CompleteSetupDto dto
        );
    }
}