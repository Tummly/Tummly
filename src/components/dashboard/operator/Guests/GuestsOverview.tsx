import { CalendarIcon, ChevronDownIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  PERFORMANCE_DATE_BUTTON_CLASS,
  PERFORMANCE_DATE_BUTTON_DISABLED_CLASS,
  PERFORMANCE_DATE_ICON_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
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
import { cn } from "@/lib/utils"
import type { OperatorGuestOverviewKpi } from "@/types/operatorGuests"

type GuestsOverviewProps = {
  kpis: OperatorGuestOverviewKpi[]
}

/** Figma Guest overview — four display-only KPI cards and disabled date-range chrome. */
export function GuestsOverview({ kpis }: GuestsOverviewProps) {
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

        <Button
          type="button"
          variant="outline"
          disabled
          aria-disabled
          aria-label="Last 7 days (unavailable)"
          className={cn(
            PERFORMANCE_DATE_BUTTON_CLASS,
            PERFORMANCE_DATE_BUTTON_DISABLED_CLASS
          )}
        >
          <CalendarIcon
            className={PERFORMANCE_DATE_ICON_CLASS}
            data-icon="inline-start"
            aria-hidden
          />
          Last 7 days
          <ChevronDownIcon
            className={PERFORMANCE_DATE_ICON_CLASS}
            data-icon="inline-end"
            aria-hidden
          />
        </Button>
      </div>

      <div className={GUESTS_KPI_GRID_CLASS}>
        {kpis.map((kpi) => (
          <div key={kpi.id} className={GUESTS_KPI_CARD_CLASS}>
            <div className="flex flex-col gap-0.5 pb-[4.25px]">
              <p className={GUESTS_KPI_LABEL_CLASS}>{kpi.label}</p>
              <p className={GUESTS_KPI_VALUE_CLASS}>{kpi.value}</p>
              <p className={GUESTS_KPI_DESCRIPTION_CLASS}>{kpi.description}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
