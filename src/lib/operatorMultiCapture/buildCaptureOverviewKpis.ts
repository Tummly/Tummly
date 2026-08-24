export type CaptureOverviewFacts = {
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
  | "qr-scans"
  | "feedback-submitted"
  | "marketing-opt-ins"
  | "offer-claims"

export type OperatorCaptureOverviewKpiSecondaryKind =
  | "dash"
  | "rate"
  | "none"

export type OperatorCaptureOverviewKpi = {
  id: OperatorCaptureOverviewKpiId
  label: string
  primaryText: string
  secondaryKind: OperatorCaptureOverviewKpiSecondaryKind
  /** Secondary copy — rate (“50% completion rate”). */
  secondaryText?: string | null
  hasRealData: boolean
}

export type CaptureOverviewKpisResult = {
  kpis: OperatorCaptureOverviewKpi[]
}

function ratePercent(numerator: number, denominator: number): number | null {
  if (denominator === 0) {
    return null
  }
  return Math.round((numerator / denominator) * 100)
}

function formatRateSecondary(
  rate: number | null,
  suffix: string
): string | null {
  if (rate == null) {
    return null
  }
  return `${rate}% ${suffix}`
}

/** Build Capture overview KPI cards from restaurant-wide facts. */
export function buildCaptureOverviewKpis(
  facts: CaptureOverviewFacts
): CaptureOverviewKpisResult {
  const feedbackCompletionRate = ratePercent(
    facts.feedbackSubmitted,
    facts.qrScans
  )
  const marketingOfSubmissions = ratePercent(
    facts.marketingOptIns,
    facts.feedbackSubmitted
  )
  const offerClaimsOfSubmissions = facts.offerClaimsHasRealData
    ? ratePercent(facts.offerClaims, facts.feedbackSubmitted)
    : null

  const kpis: OperatorCaptureOverviewKpi[] = [
    {
      id: "qr-scans",
      label: "Form starts",
      primaryText: String(facts.qrScans),
      secondaryKind: "none",
      secondaryText: null,
      hasRealData: true,
    },
    {
      id: "feedback-submitted",
      label: "Feedback submitted",
      primaryText: String(facts.feedbackSubmitted),
      secondaryKind: feedbackCompletionRate == null ? "dash" : "rate",
      secondaryText: formatRateSecondary(
        feedbackCompletionRate,
        "completion rate"
      ),
      hasRealData: true,
    },
    {
      id: "marketing-opt-ins",
      label: "Marketing opt-ins",
      primaryText: String(facts.marketingOptIns),
      secondaryKind: marketingOfSubmissions == null ? "dash" : "rate",
      secondaryText: formatRateSecondary(
        marketingOfSubmissions,
        "of submissions"
      ),
      hasRealData: true,
    },
    {
      id: "offer-claims",
      label: "Offer claims",
      primaryText: String(facts.offerClaims),
      secondaryKind:
        !facts.offerClaimsHasRealData || offerClaimsOfSubmissions == null
          ? "dash"
          : "rate",
      secondaryText: formatRateSecondary(
        offerClaimsOfSubmissions,
        "of submissions"
      ),
      hasRealData: facts.offerClaimsHasRealData,
    },
  ]

  return { kpis }
}
