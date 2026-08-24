import {
  ArrowUp,
  Megaphone,
  MessageSquare,
  QrCode,
  Tag,
} from "lucide-react"
import type { LucideIcon } from "lucide-react"
import { Fragment } from "react"

import {
  CAPTURE_KPI_CELL_CLASS,
  CAPTURE_KPI_CONTENT_CLASS,
  CAPTURE_KPI_STRIP_CLASS,
  CAPTURE_OVERVIEW_KPI_DIVIDER_CLASS,
  CAPTURE_OVERVIEW_KPI_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import type {
  OperatorCaptureOverviewKpi,
  OperatorCaptureOverviewKpiId,
} from "@/lib/operatorMultiCapture/buildCaptureOverviewKpis"
import {
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_TREND_ICON_CLASS,
  PERFORMANCE_KPI_TREND_ROW_CLASS,
  PERFORMANCE_KPI_TREND_TEXT_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  PERFORMANCE_KPI_VALUE_ROW_CLASS,
  resolveKpiTrendTextClass,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { cn } from "@/lib/utils"

const KPI_ICONS: Record<OperatorCaptureOverviewKpiId, LucideIcon> = {
  "qr-scans": QrCode,
  "feedback-submitted": MessageSquare,
  "marketing-opt-ins": Megaphone,
  "offer-claims": Tag,
}

type CaptureOverviewKpiStripProps = {
  kpis: OperatorCaptureOverviewKpi[]
}

function OverviewKpiSecondary({ kpi }: { kpi: OperatorCaptureOverviewKpi }) {
  if (kpi.secondaryKind === "none") {
    return (
      <div className={PERFORMANCE_KPI_TREND_ROW_CLASS} aria-hidden>
        <div className="leading-[0]">
          <p className={cn(PERFORMANCE_KPI_TREND_TEXT_CLASS, "invisible")}>
            —
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

  return (
    <div className={PERFORMANCE_KPI_TREND_ROW_CLASS}>
      <ArrowUp
        className={cn(
          PERFORMANCE_KPI_TREND_ICON_CLASS,
          resolveKpiTrendTextClass("positive")
        )}
        aria-hidden
      />
      <div className="leading-[0]">
        <p
          className={cn(
            PERFORMANCE_KPI_TREND_TEXT_CLASS,
            resolveKpiTrendTextClass("positive")
          )}
        >
          {kpi.secondaryText}
        </p>
      </div>
    </div>
  )
}

/** Capture overview KPI strip — four restaurant-wide metric cells (Figma strip chrome). */
export function CaptureOverviewKpiStrip({ kpis }: CaptureOverviewKpiStripProps) {
  return (
    <div className={CAPTURE_KPI_STRIP_CLASS}>
      <div className={CAPTURE_OVERVIEW_KPI_ROW_CLASS}>
        {kpis.map((kpi, index) => {
          const Icon = KPI_ICONS[kpi.id]
          return (
            <Fragment key={kpi.id}>
              {index > 0 ? (
                <div aria-hidden className={CAPTURE_OVERVIEW_KPI_DIVIDER_CLASS} />
              ) : null}
              <div className={CAPTURE_KPI_CELL_CLASS}>
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
            </Fragment>
          )
        })}
      </div>
    </div>
  )
}
