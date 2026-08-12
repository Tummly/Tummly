import { Fragment } from "react"

import type { OperatorOffersKpi } from "@/lib/operatorOffers/buildOffersPerformanceKpis"
import {
  OFFERS_KPI_HELPER_CLASS,
  OFFERS_KPI_HELPER_ROW_CLASS,
} from "@/lib/operatorOffers/offersPresentation"
import {
  PERFORMANCE_KPI_CELL_CLASS,
  PERFORMANCE_KPI_CONTENT_CLASS,
  PERFORMANCE_KPI_DIVIDER_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_ROW_CLASS,
  PERFORMANCE_KPI_STRIP_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"

type OffersKpiStripProps = {
  kpis: OperatorOffersKpi[]
}

/** Offers Performance KPI strip — five cells with helper text; no trend icons. */
export function OffersKpiStrip({ kpis }: OffersKpiStripProps) {
  return (
    <div className={PERFORMANCE_KPI_STRIP_CLASS}>
      <div className={PERFORMANCE_KPI_ROW_CLASS}>
        {kpis.map((kpi, index) => (
          <Fragment key={kpi.id}>
            {index > 0 ? (
              <div aria-hidden className={PERFORMANCE_KPI_DIVIDER_CLASS} />
            ) : null}
            <div className={PERFORMANCE_KPI_CELL_CLASS}>
              <div className={PERFORMANCE_KPI_CONTENT_CLASS}>
                <div className="leading-[0]">
                  <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                </div>
                <div className="leading-[0]">
                  <p className={PERFORMANCE_KPI_VALUE_CLASS}>
                    {kpi.primaryText}
                  </p>
                </div>
                <div className={OFFERS_KPI_HELPER_ROW_CLASS}>
                  <p className={OFFERS_KPI_HELPER_CLASS}>{kpi.helperText}</p>
                </div>
              </div>
            </div>
          </Fragment>
        ))}
      </div>
    </div>
  )
}
