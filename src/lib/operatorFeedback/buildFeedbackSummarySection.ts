import type { FeedbackSummaryResponse } from "@/types/dashboard"
import type {
  OperatorFeedbackSummaryKpi,
  OperatorFeedbackSummarySection,
} from "@/types/operatorFeedback"

/** Half-up whole percent of Total; 0 when Total is 0. */
export function feedbackSharePercent(
  bucket: number,
  total: number
): number {
  if (total <= 0) {
    return 0
  }
  return Math.round((bucket / total) * 100)
}

function shareOf(bucket: number, total: number): number {
  if (total <= 0) {
    return 0
  }
  return (bucket / total) * 100
}

/** Absolute count delta for Total / Negative PoP helpers. */
export function formatAbsoluteCountDelta(
  current: number,
  previous: number,
  previousTotal: number
): string | null {
  if (previousTotal === 0) {
    return null
  }
  const delta = current - previous
  if (delta > 0) {
    return `+${delta} vs previous period`
  }
  if (delta < 0) {
    return `${delta} vs previous period`
  }
  return `0 vs previous period`
}

/** Percentage-point change of share for Positive / Neutral PoP helpers. */
export function formatSharePointDelta(
  currentBucket: number,
  currentTotal: number,
  previousBucket: number,
  previousTotal: number
): string | null {
  if (previousTotal === 0) {
    return null
  }
  const deltaPp = Math.round(
    shareOf(currentBucket, currentTotal) - shareOf(previousBucket, previousTotal)
  )
  if (deltaPp > 0) {
    return `+${deltaPp}pp vs previous period`
  }
  if (deltaPp < 0) {
    return `${deltaPp}pp vs previous period`
  }
  return `0pp vs previous period`
}

function buildKpis(
  summary: FeedbackSummaryResponse
): OperatorFeedbackSummaryKpi[] {
  const shareDescription = (bucket: number) => {
    const percent = feedbackSharePercent(bucket, summary.total)
    return `${percent}% of feedback`
  }

  return [
    {
      id: "total",
      label: "Total feedback",
      value: summary.total,
      shareLabel: null,
      comparisonLabel: formatAbsoluteCountDelta(
        summary.total,
        summary.totalPrevious,
        summary.totalPrevious
      ),
    },
    {
      id: "positive",
      label: "Positive",
      value: summary.positive,
      shareLabel: shareDescription(summary.positive),
      comparisonLabel: formatSharePointDelta(
        summary.positive,
        summary.total,
        summary.positivePrevious,
        summary.totalPrevious
      ),
    },
    {
      id: "neutral",
      label: "Neutral",
      value: summary.neutral,
      shareLabel: shareDescription(summary.neutral),
      comparisonLabel: formatSharePointDelta(
        summary.neutral,
        summary.total,
        summary.neutralPrevious,
        summary.totalPrevious
      ),
    },
    {
      id: "negative",
      label: "Negative",
      value: summary.negative,
      shareLabel: shareDescription(summary.negative),
      comparisonLabel: formatAbsoluteCountDelta(
        summary.negative,
        summary.negativePrevious,
        summary.totalPrevious
      ),
    },
  ]
}

/** Map summary API counts onto empty card vs four-cell KPI strip. */
export function buildFeedbackSummarySection(
  summary: FeedbackSummaryResponse
): OperatorFeedbackSummarySection {
  if (summary.total === 0) {
    return { kind: "empty" }
  }
  return {
    kind: "kpis",
    kpis: buildKpis(summary),
  }
}
