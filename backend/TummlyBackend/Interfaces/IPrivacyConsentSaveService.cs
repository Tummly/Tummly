using TummlyBackend.DTOs.PrivacyConsent;

namespace TummlyBackend.Interfaces
{
    public interface IPrivacyConsentSaveService
    {
        Task<PrivacyConsentSaveResult> SaveAsync(
            int restaurantId,
            int actorUserId,
            SavePrivacyConsentRequest request
        );
    }

    public abstract record PrivacyConsentSaveResult
    {
        public sealed record Ok(bool PrivacyReady) : PrivacyConsentSaveResult;

        public sealed record NotFound : PrivacyConsentSaveResult;

        public sealed record InvalidRequest(string Message)
            : PrivacyConsentSaveResult;
    }
}
