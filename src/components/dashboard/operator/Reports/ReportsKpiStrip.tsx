import { Fragment, type ReactNode } from "react"
import { ArrowDown, ArrowUp } from "lucide-react"

import {
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_TREND_ICON_CLASS,
  PERFORMANCE_KPI_TREND_ROW_CLASS,
  PERFORMANCE_KPI_TREND_TEXT_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  resolveKpiTrendTextClass,
  type KpiTrendTone,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import {
  REPORTS_KPI_CELL_CLASS,
  REPORTS_KPI_CONTENT_CLASS,
  REPORTS_KPI_DIVIDER_CLASS,
  REPORTS_KPI_ROW_CLASS,
  REPORTS_KPI_STRIP_CLASS,
} from "@/lib/operatorReports/reportsPresentation"
import { cn } from "@/lib/utils"

export type ReportsKpiItem = {
  label: string
  value: string | number
  delta?: string
  /** true = up / positive, false = down, null/undefined = neutral or plain text */
  positive?: boolean | null
}

type ReportsKpiStripProps = {
  items: readonly ReportsKpiItem[]
  className?: string
}

function toneForKpi(positive: boolean | null | undefined): KpiTrendTone {
  if (positive === true) {
    return "positive"
  }
  if (positive === false) {
    return "negative"
  }
  return "neutral"
}

/** Shared Reports KPI strip — Capture / Home Performance chrome. */
export function ReportsKpiStrip({ items, className }: ReportsKpiStripProps) {
  return (
    <div className={cn(REPORTS_KPI_STRIP_CLASS, className)}>
      <div className={REPORTS_KPI_ROW_CLASS}>
        {items.map((kpi, index) => {
          const tone = toneForKpi(kpi.positive)
          const TrendIcon =
            kpi.positive === false
              ? ArrowDown
              : kpi.positive === true
                ? ArrowUp
                : null

          return (
            <Fragment key={kpi.label}>
              {index > 0 ? (
                <div aria-hidden className={REPORTS_KPI_DIVIDER_CLASS} />
              ) : null}
              <div className={REPORTS_KPI_CELL_CLASS}>
                <div className={REPORTS_KPI_CONTENT_CLASS}>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                  </div>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_VALUE_CLASS}>{kpi.value}</p>
                  </div>
                  {kpi.delta != null && kpi.delta.length > 0 ? (
                    <div className={PERFORMANCE_KPI_TREND_ROW_CLASS}>
                      {TrendIcon != null ? (
                        <TrendIcon
                          className={cn(
                            PERFORMANCE_KPI_TREND_ICON_CLASS,
                            resolveKpiTrendTextClass(tone)
                          )}
                          aria-hidden
                        />
                      ) : null}
                      <div className="leading-[0]">
                        <p
                          className={cn(
                            PERFORMANCE_KPI_TREND_TEXT_CLASS,
                            resolveKpiTrendTextClass(tone)
                          )}
                        >
                          {kpi.delta}
                        </p>
                      </div>
                    </div>
                  ) : null}
                </div>
              </div>
            </Fragment>
          )
        })}
      </div>
    </div>
  )
}

/** Optional typed helper for pages that build KPI lists. */
export type ReportsKpiStripChildren = ReactNode
