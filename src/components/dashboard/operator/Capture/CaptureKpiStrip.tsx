import {
  FilePenLine,
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
  CAPTURE_KPI_ROW_CLASS,
  CAPTURE_KPI_STRIP_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import type {
  OperatorCaptureKpi,
  OperatorCaptureKpiId,
} from "@/lib/operatorCapture/buildCapturePerformanceKpis"
import {
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_TREND_ROW_CLASS,
  PERFORMANCE_KPI_TREND_TEXT_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  resolveKpiTrendTextClass,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { cn } from "@/lib/utils"

const KPI_ICONS: Record<OperatorCaptureKpiId, LucideIcon> = {
  "qr-scans": QrCode,
  "form-starts": FilePenLine,
  "feedback-submitted": MessageSquareText,
  "marketing-opt-ins": Megaphone,
  "offer-claims": Tag,
}

type CaptureKpiStripProps = {
  kpis: OperatorCaptureKpi[]
}

/** Capture performance KPI strip — five Figma metric cells with PoP trends. */
export function CaptureKpiStrip({ kpis }: CaptureKpiStripProps) {
  return (
    <div className={CAPTURE_KPI_STRIP_CLASS}>
      <div className={CAPTURE_KPI_ROW_CLASS}>
        {kpis.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id]
          return (
            <div key={kpi.id} className={CAPTURE_KPI_CELL_CLASS}>
              <div className={CAPTURE_KPI_CONTENT_CLASS}>
                <div className="leading-[0]">
                  <p className={PERFORMANCE_KPI_LABEL_CLASS}>{kpi.label}</p>
                </div>
                <div className="leading-[0]">
                  <p className={PERFORMANCE_KPI_VALUE_CLASS}>{kpi.primaryText}</p>
                </div>
                {!kpi.hasRealData ? (
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
                ) : (
                  <HomeKpiTrend trendPercent={kpi.trendPercent} />
                )}
              </div>
              <Icon className={PERFORMANCE_KPI_ICON_CLASS} aria-hidden />
            </div>
          )
        })}
      </div>
    </div>
  )
}
