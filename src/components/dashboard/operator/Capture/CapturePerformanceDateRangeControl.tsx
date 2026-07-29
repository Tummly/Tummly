import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

type CapturePerformanceDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
}

/** Capture performance date control — Home presets/Custom; independent committed value. */
export function CapturePerformanceDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: CapturePerformanceDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={selectedRange}
      onCommitRange={onCommitRange}
      title="Select Capture performance date range"
    />
  )
}
