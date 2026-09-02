import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import {
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"

type ReportsDateRangeControlProps = {
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
  title?: string
}

export function ReportsDateRangeControl({
  selectedRange,
  onCommitRange,
  title = "Select Reports date range",
}: ReportsDateRangeControlProps) {
  const dateRangeLabel = labelForHomePerformanceDateRange(selectedRange)

  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={selectedRange}
      onCommitRange={onCommitRange}
      title={title}
    />
  )
}
