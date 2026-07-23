import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

type HomePerformanceDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
}

/** Performance overview date control — presets + Custom range calendar. */
export function HomePerformanceDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: HomePerformanceDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={selectedRange}
      onCommitRange={onCommitRange}
      title="Select performance date range"
    />
  )
}
