namespace TummlyBackend.Services
{
    /// <summary>
    /// Server-derived Guest Loop activity checklist for Location detail (PRD §5).
    /// Wire ids match <c>locationDetailPresentation.ts</c>.
    /// </summary>
    public static class LocationDetailGuestActivityChecklistBuilder
    {
        public static Dictionary<string, string> Build(
            int guestsCaptured,
            int optIns,
            int feedback,
            int offersClaimed,
            int offersRedeemed,
            int pendingRecoveryCount
        )
        {
            string CountStatus(int count) => count > 0 ? "complete" : "optional";

            return new Dictionary<string, string>
            {
                ["guestProfilesCreated"] = CountStatus(guestsCaptured),
                ["offerClaims"] = CountStatus(offersClaimed),
                ["consentOptIns"] = CountStatus(optIns),
                ["offerRedemptions"] = CountStatus(offersRedeemed),
                ["feedbackSubmitted"] = CountStatus(feedback),
                ["unsubscribes"] = "optional",
                ["needsRecovery"] = pendingRecoveryCount > 0
                    ? "needs-action"
                    : "complete",
            };
        }
    }
}
