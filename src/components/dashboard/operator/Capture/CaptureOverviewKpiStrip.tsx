import {
  Building2,
  Megaphone,
  MessageSquareText,
  QrCode,
  Tag,
} from "lucide-react"
import type { LucideIcon } from "lucide-react"

import { HomeKpiTrend } from "@/components/dashboard/operator/Home/HomeKpiTrend"
import {
  CAPTURE_KPI_CELL_CLASS,
  CAPTURE_KPI_CONTENT_CLASS,
  CAPTURE_KPI_STRIP_CLASS,
  CAPTURE_OVERVIEW_KPI_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import type {
  OperatorCaptureOverviewKpi,
  OperatorCaptureOverviewKpiId,
} from "@/lib/operatorMultiCapture/buildCaptureOverviewKpis"
import {
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_TREND_ROW_CLASS,
  PERFORMANCE_KPI_TREND_TEXT_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  PERFORMANCE_KPI_VALUE_ROW_CLASS,
  resolveKpiTrendTextClass,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { cn } from "@/lib/utils"

const KPI_ICONS: Record<OperatorCaptureOverviewKpiId, LucideIcon> = {
  "active-locations": Building2,
  "active-qr-placements": QrCode,
  "qr-scans": QrCode,
  "feedback-submitted": MessageSquareText,
  "marketing-opt-ins": Megaphone,
  "offer-claims": Tag,
}

type CaptureOverviewKpiStripProps = {
  kpis: OperatorCaptureOverviewKpi[]
}

function OverviewKpiSecondary({ kpi }: { kpi: OperatorCaptureOverviewKpi }) {
  if (kpi.secondaryKind === "of-total") {
    return (
      <div className={PERFORMANCE_KPI_TREND_ROW_CLASS}>
        <div className="leading-[0]">
          <p
            className={cn(
              PERFORMANCE_KPI_TREND_TEXT_CLASS,
              resolveKpiTrendTextClass("unknown")
            )}
          >
            {kpi.secondaryText}
          </p>
        </div>
      </div>
    )
  }

  if (kpi.secondaryKind === "dash" || !kpi.hasRealData) {
    return (
      <div className={PERFORMANCE_KPI_TREND_ROW_CLASS}>
        <div className="leading-[0]">
          <p
            className={cn(
              PERFORMANCE_KPI_TREND_TEXT_CLASS,
              resolveKpiTrendTextClass("unknown")
            )}
          >
            —
          </p>
        </div>
      </div>
    )
  }

  return <HomeKpiTrend trendPercent={kpi.trendPercent ?? null} />
}

/** Capture overview KPI strip — six restaurant-wide metric cells. */
export function CaptureOverviewKpiStrip({ kpis }: CaptureOverviewKpiStripProps) {
  return (
    <div className={CAPTURE_KPI_STRIP_CLASS}>
      <div className={CAPTURE_OVERVIEW_KPI_ROW_CLASS}>
        {kpis.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id]
          return (
            <div key={kpi.id} className={CAPTURE_KPI_CELL_CLASS}>
              <div className={CAPTURE_KPI_CONTENT_CLASS}>
                <div className="leading-[0]">
                  <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                </div>
                <div className={PERFORMANCE_KPI_VALUE_ROW_CLASS}>
                  <div className="min-w-0 leading-[0]">
                    <p className={PERFORMANCE_KPI_VALUE_CLASS}>
                      {kpi.primaryText}
                    </p>
                  </div>
                  <Icon className={PERFORMANCE_KPI_ICON_CLASS} aria-hidden />
                </div>
                <OverviewKpiSecondary kpi={kpi} />
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
