/**
 * Figma Operator Reports — Offers report sub-page.
 */

export const OFFERS_REPORT_PAGE_COPY = {
  breadcrumbReports: "Reports",
  breadcrumbOffersReport: "Offers report",
  title: "Offers report",
  pageTitle: "Offers report",
  subtitle: "Track offer claims, redemptions, expiry and usage controls.",
  pageSubtitle: "Track offer claims, redemptions, expiry and usage controls.",
  generateBrief: "Generate brief",
  export: "Export",

  // Empty state copy
  emptyTitle: "No offer reports yet",
  emptySubtitle:
    "Create a controlled offer with expiry and redemption rules to track claims and redemptions.",
  createOffer: "Create offer",

  // Section titles
  performanceSectionTitle: "Offer performance",
  recentRedemptionsSectionTitle: "Recent redemptions",
  controlSignalsSectionTitle: "Offer control signals",

  // Table headers
  offerHeader: "Offer",
  sourceHeader: "Source",
  statusHeader: "Status",
  claimsHeader: "Claims",
  redemptionsHeader: "Redemptions",
  rateHeader: "Rate",
  expiredHeader: "Expired",
  invalidHeader: "Invalid",
  dateHeader: "Date",
  guestHeader: "Guest",
  locationHeader: "Location",

  // CTAs
  viewRedemptionLog: "View redemption log",
  reviewOffer: "Review offer",
  viewOverrides: "View overrides",
} as const

export type OffersReportKpi = {
  label: string
  value: string | number
  delta: string
  positive?: boolean | null
}

export type OffersReportPerformanceRow = {
  id: string
  offer: string
  source: string
  status: string
  claims: number
  redemptions: number
  rate: string
  expired: number
  invalid: number
}

export type OffersReportRedemptionRow = {
  id: string
  date: string
  offer: string
  guest: string
  location: string
  status: string
}

export type OffersReportControlSignal = {
  id: string
  title: string
  subtitle: string
  cta: string
  target: "redemption-log" | "offers" | "overrides"
}

export type OffersReportData = {
  kpis: {
    activeOffers: OffersReportKpi
    offerClaims: OffersReportKpi
    redemptions: OffersReportKpi
    redemptionRate: OffersReportKpi
    expiredClaims: OffersReportKpi
    invalidAttempts: OffersReportKpi
  }
  performance: OffersReportPerformanceRow[]
  redemptionsList: OffersReportRedemptionRow[]
  controlSignals: OffersReportControlSignal[]
}

export const mockOffersReportData: OffersReportData = {
  kpis: {
    activeOffers: {
      label: "Active offers",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    offerClaims: {
      label: "Offer claims",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    redemptions: {
      label: "Redemptions",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    redemptionRate: {
      label: "Redemption rate",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    expiredClaims: {
      label: "Expired claims",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
    invalidAttempts: {
      label: "Invalid attempts",
      value: 0,
      delta: "[X]% vs previous period",
      positive: true,
    },
  },
  performance: [
    {
      id: "1",
      offer: "Free side next visit",
      source: "QR flow",
      status: "QR flow",
      claims: 9,
      redemptions: 41,
      rate: "64%",
      expired: 2,
      invalid: 1,
    },
    {
      id: "2",
      offer: "Buy one get one free",
      source: "Email campaign",
      status: "Promo code",
      claims: 12,
      redemptions: 53,
      rate: "58%",
      expired: 3,
      invalid: 2,
    },
    {
      id: "3",
      offer: "Buy one get one free",
      source: "App notification",
      status: "Push message",
      claims: 15,
      redemptions: 62,
      rate: "70%",
      expired: 4,
      invalid: 3,
    },
    {
      id: "4",
      offer: "Buy one get one free",
      source: "Social media ad",
      status: "In-app banner",
      claims: 8,
      redemptions: 47,
      rate: "52%",
      expired: 1,
      invalid: 4,
    },
  ],
  redemptionsList: [
    {
      id: "r1",
      date: "12 Jul",
      offer: "Free side next visit",
      guest: "Sarah",
      location: "Sarah",
      status: "Redeemed",
    },
    {
      id: "r2",
      date: "12 Jul",
      offer: "Free side next visit",
      guest: "Sarah",
      location: "Sarah",
      status: "Redeemed",
    },
    {
      id: "r3",
      date: "12 Jul",
      offer: "Free side next visit",
      guest: "Sarah",
      location: "Sarah",
      status: "Redeemed",
    },
    {
      id: "r4",
      date: "12 Jul",
      offer: "Free side next visit",
      guest: "Sarah",
      location: "Sarah",
      status: "Redeemed",
    },
    {
      id: "r5",
      date: "12 Jul",
      offer: "Free side next visit",
      guest: "Sarah",
      location: "Sarah",
      status: "Redeemed",
    },
  ],
  controlSignals: [
    {
      id: "cs1",
      title: "Repeated invalid attempts",
      subtitle: "2 attempts this period were already-used or expired offers.",
      cta: "View redemption log",
      target: "redemption-log",
    },
    {
      id: "cs2",
      title: "High claims, lower redemptions",
      subtitle: "The New wrap trial offer had 5 claims and 2 redemptions.",
      cta: "Review offer",
      target: "offers",
    },
    {
      id: "cs3",
      title: "Manager overrides",
      subtitle: "1 redemption was approved manually.",
      cta: "View overrides",
      target: "overrides",
    },
  ],
}
