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

export type AbsoluteCountDeltaKind = "total" | "negative"

function pluralize(count: number, singular: string, plural: string): string {
  return count === 1 ? singular : plural
}

/**
 * Absolute count delta for Total / Negative PoP helpers.
 * Figma: "18 more than the previous period" /
 * "4 more negative submissions than the previous period".
 */
export function formatAbsoluteCountDelta(
  current: number,
  previous: number,
  kind: AbsoluteCountDeltaKind = "total"
): string {
  const delta = current - previous
  const abs = Math.abs(delta)

  if (kind === "negative") {
    if (delta === 0) {
      return "Same number of negative submissions as the previous period"
    }
    const noun = pluralize(abs, "submission", "submissions")
    const direction = delta > 0 ? "more" : "fewer"
    return `${abs} ${direction} negative ${noun} than the previous period`
  }

  if (delta === 0) {
    return "Same as the previous period"
  }
  const direction = delta > 0 ? "more" : "fewer"
  return `${abs} ${direction} than the previous period`
}

/**
 * Percentage-point change of share for Positive / Neutral PoP helpers.
 * Figma: "6 percentage points higher than the previous period".
 * Previous-period empty treats prior share as 0%.
 */
export function formatSharePointDelta(
  currentBucket: number,
  currentTotal: number,
  previousBucket: number,
  previousTotal: number
): string {
  const deltaPp = Math.round(
    shareOf(currentBucket, currentTotal) - shareOf(previousBucket, previousTotal)
  )
  if (deltaPp === 0) {
    return "Same share as the previous period"
  }
  const abs = Math.abs(deltaPp)
  const noun = pluralize(abs, "percentage point", "percentage points")
  const direction = deltaPp > 0 ? "higher" : "lower"
  return `${abs} ${noun} ${direction} than the previous period`
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
        "total"
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
        "negative"
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
