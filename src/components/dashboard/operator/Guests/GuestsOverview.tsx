import { GuestsOverviewDateRangeControl } from "@/components/dashboard/operator/Guests/GuestsOverviewDateRangeControl"
import {
  GUESTS_KPI_CARD_CLASS,
  GUESTS_KPI_DESCRIPTION_CLASS,
  GUESTS_KPI_GRID_CLASS,
  GUESTS_KPI_LABEL_CLASS,
  GUESTS_KPI_VALUE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { GuestsOverviewDateRange } from "@/lib/operatorGuests/guestsOverviewDateRange"
import type { OperatorGuestOverviewKpi } from "@/types/operatorGuests"

type GuestsOverviewProps = {
  kpis: OperatorGuestOverviewKpi[]
  dateRangeLabel: string
  selectedDateRange: GuestsOverviewDateRange
  onCommitDateRange: (range: GuestsOverviewDateRange) => void
}

/** Figma Guest overview — KPI cards + overview date-range control. */
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

      <div className={GUESTS_KPI_GRID_CLASS}>
        {kpis.map((kpi) => (
          <div key={kpi.id} className={GUESTS_KPI_CARD_CLASS}>
            <div className="flex flex-col gap-0.5">
              <p className={GUESTS_KPI_LABEL_CLASS}>{kpi.label}</p>
              <p className={GUESTS_KPI_VALUE_CLASS}>{kpi.value ?? 0}</p>
            </div>
            <p className={GUESTS_KPI_DESCRIPTION_CLASS}>{kpi.description}</p>
          </div>
        ))}
      </div>
    </section>
  )
}
