using TummlyBackend.Models;

namespace TummlyBackend.Services
{
    /// <summary>
    /// Server-derived Setup &amp; details checklist for Location detail (PRD §4).
    /// Wire ids match <c>locationDetailPresentation.ts</c>.
    /// </summary>
    public static class LocationDetailSetupChecklistBuilder
    {
        public static Dictionary<string, string> Build(
            LocationLifecycleStatus lifecycle,
            string name,
            string address,
            string? city,
            string? postcode,
            bool hasActiveQr,
            int anyQrCount,
            bool privacyReady,
            int? managerUserId,
            bool hasOffer
        )
        {
            var isDraft = lifecycle == LocationLifecycleStatus.Draft;
            var isActiveOrPaused =
                lifecycle is LocationLifecycleStatus.Active
                    or LocationLifecycleStatus.Paused;
            var hasLocationDetails =
                !string.IsNullOrWhiteSpace(name)
                && !string.IsNullOrWhiteSpace(address)
                && !string.IsNullOrWhiteSpace(city)
                && !string.IsNullOrWhiteSpace(postcode);

            var locationDetailsAdded = isDraft
                ? "not-started"
                : hasLocationDetails
                    ? "complete"
                    : "incomplete";

            var qrCodePublishedLive = isDraft
                ? "not-started"
                : hasActiveQr
                    ? "complete"
                    : "incomplete";

            var guestFormConnected = isDraft
                ? "not-started"
                : isActiveOrPaused
                    ? "complete"
                    : "not-started";

            var guestPrivacyNotice = isDraft
                ? "not-started"
                : privacyReady
                    ? "complete"
                    : "incomplete";

            string atLeastOneQrCreated;
            if (isDraft)
            {
                atLeastOneQrCreated = "not-started";
            }
            else if (anyQrCount > 0)
            {
                atLeastOneQrCreated = "complete";
            }
            else
            {
                atLeastOneQrCreated = "incomplete";
            }

            return new Dictionary<string, string>
            {
                ["locationDetailsAdded"] = locationDetailsAdded,
                ["qrCodePublishedLive"] = qrCodePublishedLive,
                ["guestFormConnected"] = guestFormConnected,
                ["teamAccessAssigned"] = managerUserId.HasValue
                    ? "complete"
                    : "optional",
                ["guestPrivacyNotice"] = guestPrivacyNotice,
                ["firstOfferCreated"] = hasOffer ? "complete" : "optional",
                ["atLeastOneQrCreated"] = atLeastOneQrCreated,
            };
        }
    }
}
