import { Fragment } from "react"

import {
  GUESTS_KPI_CELL_CLASS,
  GUESTS_KPI_CONTENT_CLASS,
  GUESTS_KPI_DIVIDER_CLASS,
  GUESTS_KPI_LABEL_CLASS,
  GUESTS_KPI_ROW_CLASS,
  GUESTS_KPI_STRIP_CLASS,
  GUESTS_KPI_VALUE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  LOCATIONS_CARD_CLASS,
  type LocationsKpi,
} from "@/lib/operatorLocations/locationsPresentation"

type LocationsKpiStripProps = {
  kpis: LocationsKpi[]
}

/** Locations summary KPI card — Figma 3753:66479 (flat cells + dividers). */
export function LocationsKpiStrip({ kpis }: LocationsKpiStripProps) {
  return (
    <section className={LOCATIONS_CARD_CLASS} aria-label="Location summary">
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
                    <p className={GUESTS_KPI_LABEL_CLASS}>{kpi.label}</p>
                  </div>
                  <div className="leading-[0]">
                    <p className={GUESTS_KPI_VALUE_CLASS}>{kpi.primaryText}</p>
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
