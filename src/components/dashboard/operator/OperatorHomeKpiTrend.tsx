import { ArrowDownRight, ArrowUpRight } from "lucide-react"
import type { LucideIcon } from "lucide-react"

import {
  formatKpiTrendPercentValue,
  PERFORMANCE_KPI_TREND_ICON_CLASS,
  PERFORMANCE_KPI_TREND_ROW_CLASS,
  PERFORMANCE_KPI_TREND_SUFFIX,
  PERFORMANCE_KPI_TREND_TEXT_CLASS,
  resolveKpiTrendTextClass,
  resolveKpiTrendTone,
  type KpiTrendTone,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import { cn } from "@/lib/utils"

const TREND_ICONS: Record<KpiTrendTone, LucideIcon> = {
  positive: ArrowUpRight,
  negative: ArrowDownRight,
  neutral: ArrowUpRight,
  unknown: ArrowUpRight,
}

type OperatorHomeKpiTrendProps = {
  trendPercent: number | null
}

/** Figma KPI trend row — icon + “X% vs previous period” in green/red. */
export function OperatorHomeKpiTrend({ trendPercent }: OperatorHomeKpiTrendProps) {
  const tone = resolveKpiTrendTone(trendPercent)
  const textClass = resolveKpiTrendTextClass(tone)
  const TrendIcon = TREND_ICONS[tone]

  return (
    <div className={PERFORMANCE_KPI_TREND_ROW_CLASS}>
      <TrendIcon
        className={cn(PERFORMANCE_KPI_TREND_ICON_CLASS, textClass)}
        aria-hidden
      />
      <div className="leading-[0]">
        <p className={cn(PERFORMANCE_KPI_TREND_TEXT_CLASS, textClass)}>
          {formatKpiTrendPercentValue(trendPercent)}% {PERFORMANCE_KPI_TREND_SUFFIX}
        </p>
      </div>
    </div>
  )
}
