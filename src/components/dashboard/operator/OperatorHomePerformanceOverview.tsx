import { CalendarIcon, ChevronDownIcon } from "lucide-react"

import { OperatorHomeKpiStrip } from "@/components/dashboard/operator/OperatorHomeKpiStrip"
import { Button } from "@/components/ui/button"
import {
  PERFORMANCE_DATE_BUTTON_CLASS,
  PERFORMANCE_DATE_ICON_CLASS,
  PERFORMANCE_HEADER_COPY_CLASS,
  PERFORMANCE_HEADER_ROW_CLASS,
  PERFORMANCE_SECTION_CLASS,
  PERFORMANCE_SUBTITLE_CLASS,
  PERFORMANCE_TITLE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { OperatorHomeKpi } from "@/types/operatorHome"

type OperatorHomePerformanceOverviewProps = {
  kpis: OperatorHomeKpi[]
  dateRangeLabel: string
  feedbackLoading?: boolean
}

/** Figma Performance overview — section chrome + KPI strip (no fake trends). */
export function OperatorHomePerformanceOverview({
  kpis,
  dateRangeLabel,
  feedbackLoading = false,
}: OperatorHomePerformanceOverviewProps) {
  return (
    <section className={PERFORMANCE_SECTION_CLASS}>
      <div className={PERFORMANCE_HEADER_ROW_CLASS}>
        <div className={PERFORMANCE_HEADER_COPY_CLASS}>
          <div className="leading-[0]">
            <h2 className={PERFORMANCE_TITLE_CLASS}>Performance overview</h2>
          </div>
          <div className="leading-[0]">
            <p className={PERFORMANCE_SUBTITLE_CLASS}>
              See how guests are engaging with Guest Loop.
            </p>
          </div>
        </div>

        <Button
          type="button"
          variant="outline"
          disabled
          aria-disabled
          aria-label={`${dateRangeLabel} (unavailable)`}
          className={PERFORMANCE_DATE_BUTTON_CLASS}
        >
          <CalendarIcon className={PERFORMANCE_DATE_ICON_CLASS} aria-hidden />
          {dateRangeLabel}
          <ChevronDownIcon className={PERFORMANCE_DATE_ICON_CLASS} aria-hidden />
        </Button>
      </div>

      <OperatorHomeKpiStrip kpis={kpis} feedbackLoading={feedbackLoading} />
    </section>
  )
}
