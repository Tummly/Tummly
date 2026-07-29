import { computeKpiTrendPercent } from "@/lib/operatorHome/performanceOverviewPresentation"

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
  /** Display primary — count, rate (“45%”), or “—” when undefined. */
  primaryText: string
  trendPercent: number | null
  hasRealData: boolean
}

export type CapturePerformanceKpisResult = {
  kpis: OperatorCaptureKpi[]
  /** True when the window has no QR scans and no Feedback submitted. */
  isEmpty: boolean
}

function formStartsRatePercent(
  feedbackSubmitted: number,
  qrScans: number
): number | null {
  if (qrScans === 0) {
    return null
  }
  return Math.round((feedbackSubmitted / qrScans) * 100)
}

function formStartsTrendPercent(
  currentRate: number | null,
  previousRate: number | null
): number | null {
  if (currentRate == null || previousRate == null) {
    return null
  }
  return computeKpiTrendPercent(currentRate, previousRate)
}

/** Build Capture performance KPI cards from location window facts. */
export function buildCapturePerformanceKpis(
  facts: CapturePerformanceFacts
): CapturePerformanceKpisResult {
  const formStartsCurrent = formStartsRatePercent(
    facts.feedbackSubmitted,
    facts.qrScans
  )
  const formStartsPrevious = formStartsRatePercent(
    facts.feedbackSubmittedPrevious,
    facts.qrScansPrevious
  )

  const kpis: OperatorCaptureKpi[] = [
    {
      id: "qr-scans",
      label: "QR scans",
      primaryText: String(facts.qrScans),
      trendPercent: computeKpiTrendPercent(
        facts.qrScans,
        facts.qrScansPrevious
      ),
      hasRealData: true,
    },
    {
      id: "form-starts",
      label: "Form starts",
      primaryText:
        formStartsCurrent == null ? "—" : `${formStartsCurrent}%`,
      trendPercent: formStartsTrendPercent(
        formStartsCurrent,
        formStartsPrevious
      ),
      hasRealData: true,
    },
    {
      id: "feedback-submitted",
      label: "Feedback submitted",
      primaryText: String(facts.feedbackSubmitted),
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
      trendPercent: null,
      hasRealData: facts.offerClaimsHasRealData,
    },
  ]

  return {
    kpis,
    isEmpty: facts.qrScans === 0 && facts.feedbackSubmitted === 0,
  }
}
