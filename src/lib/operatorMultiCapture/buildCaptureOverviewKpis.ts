import { computeKpiTrendPercent } from "@/lib/operatorHome/performanceOverviewPresentation"

export type CaptureOverviewFacts = {
  activeLocations: number
  totalLocations: number
  activeQrPlacements: number
  qrScans: number
  qrScansPrevious: number
  feedbackSubmitted: number
  feedbackSubmittedPrevious: number
  marketingOptIns: number
  marketingOptInsPrevious: number
  offerClaims: number
  offerClaimsHasRealData: boolean
}

export type OperatorCaptureOverviewKpiId =
  | "active-locations"
  | "active-qr-placements"
  | "qr-scans"
  | "feedback-submitted"
  | "marketing-opt-ins"
  | "offer-claims"

export type OperatorCaptureOverviewKpiSecondaryKind =
  | "of-total"
  | "dash"
  | "pop"

export type OperatorCaptureOverviewKpi = {
  id: OperatorCaptureOverviewKpiId
  label: string
  primaryText: string
  secondaryKind: OperatorCaptureOverviewKpiSecondaryKind
  /** Static secondary for of-total cards — e.g. "of 5". */
  secondaryText?: string
  trendPercent?: number | null
  hasRealData: boolean
}

export type CaptureOverviewKpisResult = {
  kpis: OperatorCaptureOverviewKpi[]
}

/** Build Capture overview KPI cards from restaurant-wide facts. */
export function buildCaptureOverviewKpis(
  facts: CaptureOverviewFacts
): CaptureOverviewKpisResult {
  const kpis: OperatorCaptureOverviewKpi[] = [
    {
      id: "active-locations",
      label: "Active locations",
      primaryText: String(facts.activeLocations),
      secondaryKind: "of-total",
      secondaryText: `of ${facts.totalLocations}`,
      hasRealData: true,
    },
    {
      id: "active-qr-placements",
      label: "Active QR placements",
      primaryText: String(facts.activeQrPlacements),
      secondaryKind: "dash",
      // Primary is live; hasRealData stays true so chrome does not treat the
      // card like offer-claims. Secondary remains dash (no PoP yet).
      hasRealData: true,
    },
    {
      id: "qr-scans",
      label: "Guest form opens",
      primaryText: String(facts.qrScans),
      secondaryKind: "pop",
      trendPercent: computeKpiTrendPercent(
        facts.qrScans,
        facts.qrScansPrevious
      ),
      hasRealData: true,
    },
    {
      id: "feedback-submitted",
      label: "Feedback submitted",
      primaryText: String(facts.feedbackSubmitted),
      secondaryKind: "pop",
      trendPercent: computeKpiTrendPercent(
        facts.feedbackSubmitted,
        facts.feedbackSubmittedPrevious
      ),
      hasRealData: true,
    },
    {
      id: "marketing-opt-ins",
      label: "Marketing opt-ins",
      primaryText: String(facts.marketingOptIns),
      secondaryKind: "pop",
      trendPercent: computeKpiTrendPercent(
        facts.marketingOptIns,
        facts.marketingOptInsPrevious
      ),
      hasRealData: true,
    },
    {
      id: "offer-claims",
      label: "Offer claims",
      primaryText: String(facts.offerClaims),
      secondaryKind: "dash",
      hasRealData: facts.offerClaimsHasRealData,
    },
  ]

  return { kpis }
}
