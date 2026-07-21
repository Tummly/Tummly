import { MessageSquareText, QrCode, Tag, Users } from "lucide-react"
import type { LucideIcon } from "lucide-react"
import { Fragment } from "react"

import { OperatorHomeKpiTrend } from "@/components/dashboard/operator/OperatorHomeKpiTrend"
import {
  PERFORMANCE_KPI_CELL_CLASS,
  PERFORMANCE_KPI_CONTENT_CLASS,
  PERFORMANCE_KPI_DIVIDER_CLASS,
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_ROW_CLASS,
  PERFORMANCE_KPI_STRIP_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { OperatorHomeKpi, OperatorHomeKpiId } from "@/types/operatorHome"

const KPI_ICONS: Record<OperatorHomeKpiId, LucideIcon> = {
  "qr-scans": QrCode,
  feedback: MessageSquareText,
  "guests-joined": Users,
  "offer-redemptions": Tag,
}

type OperatorHomeKpiStripProps = {
  kpis: OperatorHomeKpi[]
  feedbackLoading?: boolean
}

/** Figma KPI strip — four metrics; trend row always visible (API fills trendPercent). */
export function OperatorHomeKpiStrip({
  kpis,
  feedbackLoading = false,
}: OperatorHomeKpiStripProps) {
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
                  <div className="leading-[0]">
                    <p
                      className={PERFORMANCE_KPI_VALUE_CLASS}
                      aria-busy={showPending || showRecountBusy || undefined}
                    >
                      {showPending ? "—" : kpi.value}
                    </p>
                  </div>
                  <OperatorHomeKpiTrend trendPercent={kpi.trendPercent} />
                </div>
                <Icon className={PERFORMANCE_KPI_ICON_CLASS} aria-hidden />
              </div>
            </Fragment>
          )
        })}
      </div>
    </div>
  )
}
