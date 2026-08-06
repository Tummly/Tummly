import { MessageSquare, QrCode, Tag, Users } from "lucide-react"
import type { LucideIcon } from "lucide-react"
import { Fragment } from "react"

import { HomeKpiTrend } from "@/components/dashboard/operator/Home/HomeKpiTrend"
import {
  PERFORMANCE_KPI_CELL_CLASS,
  PERFORMANCE_KPI_CONTENT_CLASS,
  PERFORMANCE_KPI_DIVIDER_CLASS,
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_ROW_CLASS,
  PERFORMANCE_KPI_STRIP_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  PERFORMANCE_KPI_VALUE_ROW_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { OperatorHomeKpi, OperatorHomeKpiId } from "@/types/operatorHome"

const KPI_ICONS: Record<OperatorHomeKpiId, LucideIcon> = {
  "qr-scans": QrCode,
  feedback: MessageSquare,
  "guests-joined": Users,
  "offer-redemptions": Tag,
}

type HomeKpiStripProps = {
  kpis: OperatorHomeKpi[]
  feedbackLoading?: boolean
}

/** Figma KPI strip — four metrics; trend row always visible (API fills trendPercent). */
export function HomeKpiStrip({
  kpis,
  feedbackLoading = false,
}: HomeKpiStripProps) {
  return (
    <div className={PERFORMANCE_KPI_STRIP_CLASS}>
      <div className={PERFORMANCE_KPI_ROW_CLASS}>
        {kpis.map((kpi, index) => {
          const Icon = KPI_ICONS[kpi.id]
          const isFeedbackKpi = kpi.id === "feedback"
          const showPending =
            isFeedbackKpi && feedbackLoading && !kpi.hasRealData
          const showRecountBusy =
            isFeedbackKpi && feedbackLoading && kpi.hasRealData

          return (
            <Fragment key={kpi.id}>
              {index > 0 ? (
                <div aria-hidden className={PERFORMANCE_KPI_DIVIDER_CLASS} />
              ) : null}
              <div className={PERFORMANCE_KPI_CELL_CLASS}>
                <div className={PERFORMANCE_KPI_CONTENT_CLASS}>
                  <div className="leading-[0]">
                    <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                  </div>
                  <div className={PERFORMANCE_KPI_VALUE_ROW_CLASS}>
                    <div className="min-w-0 leading-[0]">
                      <p
                        className={PERFORMANCE_KPI_VALUE_CLASS}
                        aria-busy={showPending || showRecountBusy || undefined}
                      >
                        {showPending ? "—" : kpi.value}
                      </p>
                    </div>
                    <Icon className={PERFORMANCE_KPI_ICON_CLASS} aria-hidden />
                  </div>
                  <HomeKpiTrend trendPercent={kpi.trendPercent} />
                </div>
              </div>
            </Fragment>
          )
        })}
      </div>
    </div>
  )
}
