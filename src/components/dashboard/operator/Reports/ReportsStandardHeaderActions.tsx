import { Download } from "lucide-react"

import { ReportsDateRangeControl } from "@/components/dashboard/operator/Reports/ReportsDateRangeControl"
import { AiIcon } from "@/components/ui/ai-icon"
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
  generateBusy?: boolean
  selectedRange: HomePerformanceDateRange
  onCommitRange: (range: HomePerformanceDateRange) => void
  showDateRange?: boolean
}

/** Generate brief + Export + date range — shared across Reports hub and report pages. */
export function ReportsStandardHeaderActions({
  onGenerateBrief,
  onExport,
  exportDisabled = false,
  generateBusy = false,
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
        disabled={generateBusy}
      >
        <AiIcon size={16} />
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
