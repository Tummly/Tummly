/**
 * Location-wide Offers redemption log — mirrors Details Redemptions chrome
 * (Figma 3527:54982) plus Offer column. Override column hidden for MVP.
 */

export const OFFERS_REDEMPTION_LOG_COPY = {
  title: "Redemption log",
  subtitle: "Staff redemptions across all offers at this location.",
  backToOffers: "Offers",
  loadError: "Could not load the redemption log for this location.",
  emptyTitle: "No redemptions yet",
  emptyHelper:
    "When staff redeem offer passes at this location, they will show here.",
  retry: "Retry",
  columns: {
    dateTime: "Date/time",
    guest: "Guest",
    passReference: "Pass reference",
    location: "Location",
    staffMember: "Staff member",
    outcome: "Outcome",
    reason: "Reason",
    offerVersion: "Offer version",
    offer: "Offer",
    actions: "Actions",
  },
} as const
