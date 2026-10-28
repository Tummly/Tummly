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

        /// <param name="accessibleLocationIds">
        /// Location rows outside this set are hidden. Restaurant-wide rows
        /// (LocationId null) always show.
        /// </param>
        Task<object> GetActivityAsync(
            int restaurantId,
            IReadOnlyCollection<int> accessibleLocationIds
        );
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
