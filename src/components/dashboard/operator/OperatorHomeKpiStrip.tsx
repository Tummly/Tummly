import { MessageSquareText, QrCode, Tag, Users } from "lucide-react"
import type { LucideIcon } from "lucide-react"

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

/** Figma KPI strip — four metrics with honest zeros and no fabricated trends. */
export function OperatorHomeKpiStrip({
  kpis,
  feedbackLoading = false,
}: OperatorHomeKpiStripProps) {
  return (
    <div className="flex flex-col gap-6 sm:flex-row sm:items-stretch sm:gap-0">
      {kpis.map((kpi, index) => {
        const Icon = KPI_ICONS[kpi.id]
        const showPending =
          kpi.id === "feedback" && feedbackLoading && !kpi.hasRealData

        return (
          <div key={kpi.id} className="flex min-w-0 flex-1 items-stretch">
            {index > 0 ? (
              <div
                aria-hidden
                className="mx-[15px] hidden w-0.5 shrink-0 self-center bg-[#dcdcdc] sm:block sm:h-[76px]"
              />
            ) : null}
            <div className="flex min-w-0 flex-1 items-center justify-between gap-3">
              <div className="flex flex-col items-start gap-0.5 pb-1">
                <p className="text-xs font-medium leading-4 text-[#5c697a] dark:text-white/60">
                  {kpi.label}
                </p>
                <p
                  className="text-[30px] leading-9 font-semibold text-foreground"
                  aria-busy={showPending || undefined}
                >
                  {showPending ? "—" : kpi.value}
                </p>
                {kpi.trendPercent != null ? (
                  <p className="pt-0.5 text-[11px] leading-[16.5px] font-semibold text-[#14a946]">
                    {kpi.trendPercent > 0 ? "+" : ""}
                    {kpi.trendPercent}%
                  </p>
                ) : (
                  <span className="block h-[18.5px]" aria-hidden />
                )}
              </div>
              <Icon className="size-4 shrink-0 text-primary" aria-hidden />
            </div>
          </div>
        )
      })}
    </div>
  )
}
