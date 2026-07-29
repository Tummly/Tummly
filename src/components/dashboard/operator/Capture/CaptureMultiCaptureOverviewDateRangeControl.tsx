import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

type CaptureMultiCaptureOverviewDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
}

/** Multi Capture overview date control — Home presets/Custom; independent committed value. */
export function CaptureMultiCaptureOverviewDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: CaptureMultiCaptureOverviewDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={selectedRange}
      onCommitRange={onCommitRange}
      title="Select Capture overview date range"
    />
  )
}
