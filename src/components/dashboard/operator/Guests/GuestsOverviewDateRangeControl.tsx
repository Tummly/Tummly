import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import {
  GUESTS_OVERVIEW_ALL_TIME_LABEL,
  toHomePerformanceDateRange,
  type GuestsOverviewDateRange,
} from "@/lib/operatorGuests/guestsOverviewDateRange"

type GuestsOverviewDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: GuestsOverviewDateRange
  onCommitRange: (range: GuestsOverviewDateRange) => void
}

/** Guest overview date control — All time + Home Performance presets/Custom. */
export function GuestsOverviewDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: GuestsOverviewDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={toHomePerformanceDateRange(selectedRange)}
      onCommitRange={onCommitRange}
      title="Select guest overview date range"
      leadingOptions={[
        {
          id: "all-time",
          label: GUESTS_OVERVIEW_ALL_TIME_LABEL,
          selected: selectedRange.kind === "all-time",
        },
      ]}
      onCommitLeadingOption={() => {
        onCommitRange({ kind: "all-time" })
      }}
    />
  )
}
