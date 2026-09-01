using TummlyBackend.DTOs.PrivacyConsent;

namespace TummlyBackend.Interfaces
{
    public interface IPrivacyConsentService
    {
        Task<PrivacyConsentGetResult> GetAsync(
            int restaurantId,
            bool actorCanManage,
            bool canViewGuests
        );

        Task<PrivacyConsentPatchResult> PatchTogglesAsync(
            int restaurantId,
            int actorUserId,
            PatchPrivacyConsentTogglesRequest request
        );

        Task<object> GetActivityAsync(int restaurantId);
    }

    public abstract record PrivacyConsentGetResult
    {
        public sealed record Ok(object Payload) : PrivacyConsentGetResult;

        public sealed record NotFound : PrivacyConsentGetResult;
    }

    public abstract record PrivacyConsentPatchResult
    {
        public sealed record Ok : PrivacyConsentPatchResult;

        public sealed record NotFound : PrivacyConsentPatchResult;

        public sealed record InvalidRequest(string Message)
            : PrivacyConsentPatchResult;
    }
}
