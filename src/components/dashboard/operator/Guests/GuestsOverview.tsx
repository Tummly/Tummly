import { Fragment } from "react"

import { GuestsOverviewDateRangeControl } from "@/components/dashboard/operator/Guests/GuestsOverviewDateRangeControl"
import {
  GUESTS_KPI_CELL_CLASS,
  GUESTS_KPI_CONTENT_CLASS,
  GUESTS_KPI_DIVIDER_CLASS,
  GUESTS_KPI_HELPER_CLASS,
  GUESTS_KPI_META_STACK_CLASS,
  GUESTS_KPI_ROW_CLASS,
  GUESTS_KPI_STRIP_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { GuestsOverviewDateRange } from "@/lib/operatorGuests/guestsOverviewDateRange"
import {
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { OperatorGuestOverviewKpi } from "@/types/operatorGuests"

type GuestsOverviewProps = {
  kpis: OperatorGuestOverviewKpi[]
  dateRangeLabel: string
  selectedDateRange: GuestsOverviewDateRange
  onCommitDateRange: (range: GuestsOverviewDateRange) => void
}

/** Guest overview — Feedback-summary KPI strip + overview date-range control. */
export function GuestsOverview({
  kpis,
  dateRangeLabel,
  selectedDateRange,
  onCommitDateRange,
}: GuestsOverviewProps) {
  return (
    <section className={GUESTS_SECTION_CLASS}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <header className="flex flex-col gap-2 leading-[0]">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Guest overview</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            A summary of your guest list, contact eligibility and recovery
            needs.
          </p>
        </header>

        <GuestsOverviewDateRangeControl
          dateRangeLabel={dateRangeLabel}
          selectedRange={selectedDateRange}
          onCommitRange={onCommitDateRange}
        />
      </div>

      <div className={GUESTS_KPI_STRIP_CLASS}>
        <div className={GUESTS_KPI_ROW_CLASS}>
          {kpis.map((kpi, index) => (
            <Fragment key={kpi.id}>
              {index > 0 ? (
                <div aria-hidden className={GUESTS_KPI_DIVIDER_CLASS} />
              ) : null}
              <div className={GUESTS_KPI_CELL_CLASS}>
                <div className={GUESTS_KPI_CONTENT_CLASS}>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                  </div>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_VALUE_CLASS}>
                      {kpi.value ?? 0}
                    </p>
                  </div>
                  <div className={GUESTS_KPI_META_STACK_CLASS}>
                    <p className={GUESTS_KPI_HELPER_CLASS}>{kpi.description}</p>
                  </div>
                </div>
              </div>
            </Fragment>
          ))}
        </div>
      </div>
    </section>
  )
}
