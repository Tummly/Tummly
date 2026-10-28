using TummlyBackend.DTOs.PrivacyConsent;

namespace TummlyBackend.Interfaces
{
    public interface IPrivacyConsentService
    {
        Task<PrivacyConsentGetResult> GetAsync(
            int restaurantId,
            bool actorCanManage,
            bool canViewGuests,
            bool actorIsWorkspaceOwner
        );

        /// <summary>
        /// Workspace Owner: active membership with the Owner role, or no
        /// membership on a restaurant the user owns. Mirrors how
        /// RestaurantPermissionHelper resolves the Owner role.
        /// </summary>
        Task<bool> IsWorkspaceOwnerAsync(int restaurantId, int userId);

        Task<PrivacyConsentPatchResult> PatchTogglesAsync(
            int restaurantId,
            int actorUserId,
            PatchPrivacyConsentTogglesRequest request
        );

        /// <param name="accessibleLocationIds">
        /// Location rows outside this set are hidden. Restaurant-wide rows
        /// (LocationId null) always show. Archived Locations are always
        /// hidden here; see GetArchivedActivityAsync.
        /// </param>
        Task<object> GetActivityAsync(
            int restaurantId,
            IReadOnlyCollection<int> accessibleLocationIds
        );

        /// <summary>
        /// Historical view: privacy activity for archived Locations only.
        /// Caller must already be authorised as the Workspace Owner.
        /// </summary>
        Task<object> GetArchivedActivityAsync(int restaurantId);
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
