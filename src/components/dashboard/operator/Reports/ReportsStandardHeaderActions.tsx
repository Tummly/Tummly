import { Download } from "lucide-react"

import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import { ReportsDateRangeControl } from "@/components/dashboard/operator/Reports/ReportsDateRangeControl"
import { Button } from "@/components/ui/button"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  REPORTS_PAGE_ACTION_BUTTON_CLASS,
  REPORTS_STANDARD_ACTIONS_COPY,
} from "@/lib/operatorReports/reportsPresentation"

type ReportsStandardHeaderActionsProps = {
  onGenerateBrief: () => void
  onExport: () => void
  exportDisabled?: boolean
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
  showDateRange?: boolean
}

/** Generate brief + Export + date range — shared across Reports hub and report pages. */
export function ReportsStandardHeaderActions({
  onGenerateBrief,
  onExport,
  exportDisabled = false,
  selectedRange,
  onCommitRange,
  showDateRange = true,
}: ReportsStandardHeaderActionsProps) {
  return (
    <>
      <Button
        type="button"
        variant="op-secondary"
        className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
        onClick={onGenerateBrief}
      >
        <img
          src={aiIconPng}
          alt=""
          className="size-4 shrink-0 brightness-0 invert"
        />
        <span>{REPORTS_STANDARD_ACTIONS_COPY.generateBrief}</span>
      </Button>

      <Button
        type="button"
        variant="op-secondary"
        className={REPORTS_PAGE_ACTION_BUTTON_CLASS}
        onClick={onExport}
        disabled={exportDisabled}
      >
        <Download className="size-4" aria-hidden />
        <span>{REPORTS_STANDARD_ACTIONS_COPY.export}</span>
      </Button>

      {showDateRange ? (
        <ReportsDateRangeControl
          selectedRange={selectedRange}
          onCommitRange={onCommitRange}
        />
      ) : null}
    </>
  )
}
