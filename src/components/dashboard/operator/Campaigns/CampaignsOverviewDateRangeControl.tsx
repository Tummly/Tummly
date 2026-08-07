import { PerformanceDateRangeControl } from "@/components/dashboard/operator/Home/PerformanceDateRangeControl"
import {
  CAMPAIGNS_OVERVIEW_ALL_TIME_LABEL,
  toCampaignsHomePerformanceDateRange,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"

type CampaignsOverviewDateRangeControlProps = {
  dateRangeLabel: string
  selectedRange: CampaignsOverviewDateRange
  onCommitRange: (range: CampaignsOverviewDateRange) => void
}

/** Campaigns overview date control — All time + Home Performance presets/Custom. */
export function CampaignsOverviewDateRangeControl({
  dateRangeLabel,
  selectedRange,
  onCommitRange,
}: CampaignsOverviewDateRangeControlProps) {
  return (
    <PerformanceDateRangeControl
      dateRangeLabel={dateRangeLabel}
      selectedRange={toCampaignsHomePerformanceDateRange(selectedRange)}
      onCommitRange={onCommitRange}
      title="Select campaigns overview date range"
      leadingOptions={[
        {
          id: "all-time",
          label: CAMPAIGNS_OVERVIEW_ALL_TIME_LABEL,
          selected: selectedRange.kind === "all-time",
        },
      ]}
      onCommitLeadingOption={() => {
        onCommitRange({ kind: "all-time" })
      }}
    />
  )
}
