/** Figma Offers main page — node 3498:1587 (header + Performance + Needs attention). */

export const OFFERS_PAGE_COPY = {
  title: "Offers",
  subtitle:
    "Create and manage catalog offers for campaigns, recovery, and guest thank-you paths.",
  createOffer: "Create offer",
  openStaffRedeem: "Open staff redeem",
  viewRedemptionLog: "View redemption log",
  loadError: "Could not load offers for this location.",
  performanceAriaLabel: "Performance",
  needsAttentionTitle: "Needs attention",
  needsAttentionSubtitle: "Review issues that may require action.",
  /** Empty list slot — list chrome mounts in a later ticket. */
  listSlotLabel: "Offers list",
} as const

export const OFFERS_PAGE_META_CLASS =
  "m-0 text-sm font-normal leading-5 text-muted-foreground"

/** Helper under KPI value — Figma Main Bg/Subtitle + KPI info size. */
export const OFFERS_KPI_HELPER_CLASS =
  "m-0 text-op-kpi-info-size font-normal leading-normal text-op-card-subtitle-color"

export const OFFERS_KPI_HELPER_ROW_CLASS = "flex items-start pt-3.5"
