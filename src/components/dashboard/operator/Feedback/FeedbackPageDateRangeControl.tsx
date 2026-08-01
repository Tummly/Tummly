import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

type FeedbackPageDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
}

/** Feedback page header date control — Home presets/Custom; no All time. */
export function FeedbackPageDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: FeedbackPageDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={selectedRange}
      onCommitRange={onCommitRange}
      title="Select Feedback date range"
    />
  )
}
