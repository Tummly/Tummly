import { OperatorHomeKpiStrip } from "@/components/dashboard/operator/OperatorHomeKpiStrip"
import type { OperatorHomeKpi } from "@/types/operatorHome"

type OperatorHomePerformanceOverviewProps = {
  kpis: OperatorHomeKpi[]
  feedbackLoading?: boolean
}

/** Figma Performance overview — section chrome + KPI strip (no fake trends). */
export function OperatorHomePerformanceOverview({
  kpis,
  feedbackLoading = false,
}: OperatorHomePerformanceOverviewProps) {
  return (
    <section className="flex flex-col gap-10 rounded-lg border border-[#e5e5e5] bg-white p-6 dark:border-white/10 dark:bg-white/5">
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-bold text-foreground">
          Performance overview
        </h2>
        <p className="text-sm font-medium text-foreground/70">
          See how guests are engaging with Guest Loop.
        </p>
      </div>
      <OperatorHomeKpiStrip kpis={kpis} feedbackLoading={feedbackLoading} />
    </section>
  )
}
