export type CapturePerformanceFacts = {
  qrScans: number
  qrScansPrevious: number
  feedbackSubmitted: number
  feedbackSubmittedPrevious: number
  marketingOptIns: number
  marketingOptInsPrevious: number
  offerClaims: number
  offerClaimsHasRealData: boolean
}

export type OperatorCaptureKpiId =
  | "qr-scans"
  | "form-starts"
  | "feedback-submitted"
  | "marketing-opt-ins"
  | "offer-claims"

export type OperatorCaptureKpi = {
  id: OperatorCaptureKpiId
  label: string
  /** Display primary — count, or “—” when undefined. */
  primaryText: string
  /**
   * Figma rate secondary — e.g. "50% of scans".
   * Null when the card has no secondary (Guest form opens) or the rate is undefined.
   */
  secondaryText: string | null
  hasRealData: boolean
}

export type CapturePerformanceKpisResult = {
  kpis: OperatorCaptureKpi[]
  /** True when the window has no QR scans and no Feedback submitted. */
  isEmpty: boolean
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

/** Build Capture performance KPI cards from location window facts. */
export function buildCapturePerformanceKpis(
  facts: CapturePerformanceFacts
): CapturePerformanceKpisResult {
  // Form-starts count is not on the snapshot yet — reuse feedback/scans as the
  // "% of scans" rate (same numerator the prior Form starts primary used).
  const formStartsOfScans = ratePercent(
    facts.feedbackSubmitted,
    facts.qrScans
  )
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

  const kpis: OperatorCaptureKpi[] = [
    {
      id: "qr-scans",
      label: "Guest form opens",
      primaryText: String(facts.qrScans),
      secondaryText: null,
      hasRealData: true,
    },
    {
      id: "form-starts",
      label: "Form starts",
      // Form-starts count is not on the snapshot yet. Keep the scans rate as
      // the primary; the Figma secondary repeats it with the suffix.
      primaryText:
        formStartsOfScans == null ? "—" : `${formStartsOfScans}%`,
      secondaryText: formatRateSecondary(formStartsOfScans, "of scans"),
      hasRealData: true,
    },
    {
      id: "feedback-submitted",
      label: "Feedback submitted",
      primaryText: String(facts.feedbackSubmitted),
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
      secondaryText: formatRateSecondary(
        offerClaimsOfSubmissions,
        "of submissions"
      ),
      hasRealData: facts.offerClaimsHasRealData,
    },
  ]

  return {
    kpis,
    isEmpty: facts.qrScans === 0 && facts.feedbackSubmitted === 0,
  }
}
