namespace TummlyBackend.Models
{
    /// <summary>
    /// Aggregate metrics bag for Home recommendation domain routing (ticket 02).
    /// Counts only — no guest PII, no feedback body text, no setup-checklist fields.
    /// Ticket 03 loads these from the Owned location + Home performance window.
    /// </summary>
    /// <remarks>
    /// Field list:
    /// <list type="bullet">
    /// <item><description><see cref="OpenFeedbackCount"/> — unresolved feedback in window (New + In progress).</description></item>
    /// <item><description><see cref="NeedsAttentionCount"/> — Needs attention feedback in window.</description></item>
    /// <item><description><see cref="GuestsJoinedInWindow"/> — guests captured in the Home performance window.</description></item>
    /// <item><description><see cref="MarketingEligible"/> — marketing-eligible guests (Campaigns signal; also Home-native thank/follow).</description></item>
    /// <item><description><see cref="ActiveOffers"/> — Active catalog offers at the location (echo / presence).</description></item>
    /// <item><description><see cref="HasNoActiveOffers"/> — loader-confirmed zero Active offers (create-offer signal). False when unknown.</description></item>
    /// <item><description><see cref="OfferNeedsAttentionCount"/> — offer health attention rows (expiring / open void, etc.).</description></item>
    /// <item><description><see cref="NewGuests"/> — Campaign Smart Group: new guests.</description></item>
    /// <item><description><see cref="PositiveFeedback"/> — Campaign Smart Group: positive feedback guests.</description></item>
    /// <item><description><see cref="DormantGuests"/> — Campaign Smart Group: dormant guests.</description></item>
    /// <item><description><see cref="NeedsRecovery"/> — Campaign Smart Group: needs recovery guests.</description></item>
    /// </list>
    /// </remarks>
    public sealed record HomeRecommendationMetrics(
        int OpenFeedbackCount,
        int NeedsAttentionCount,
        int GuestsJoinedInWindow,
        int MarketingEligible,
        int ActiveOffers,
        bool HasNoActiveOffers,
        int OfferNeedsAttentionCount,
        int NewGuests,
        int PositiveFeedback,
        int DormantGuests,
        int NeedsRecovery
    );
}
