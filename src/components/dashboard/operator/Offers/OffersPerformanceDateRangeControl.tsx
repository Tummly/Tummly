import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"

type OffersPerformanceDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void | Promise<void>
}

/** Offers Performance date control — Home presets/Custom (not Details' 90-day). */
export function OffersPerformanceDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: OffersPerformanceDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={selectedRange}
      onCommitRange={onCommitRange}
      title="Select Offers performance date range"
    />
  )
}
