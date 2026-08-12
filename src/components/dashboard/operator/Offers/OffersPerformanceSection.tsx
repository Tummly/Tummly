import { OffersKpiStrip } from "@/components/dashboard/operator/Offers/OffersKpiStrip"
import { OffersPerformanceDateRangeControl } from "@/components/dashboard/operator/Offers/OffersPerformanceDateRangeControl"
import type { OperatorOffersPerformanceView } from "@/lib/operatorOffers/createOperatorOffersPageModule"
import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { PERFORMANCE_SECTION_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"

type OffersPerformanceSectionProps = {
  performance: OperatorOffersPerformanceView
  onCommitRange: (range: HomePerformanceDateRange) => void | Promise<void>
}

/** Offers Performance — date control + KPI strip (Figma 3498:1587). */
export function OffersPerformanceSection({
  performance,
  onCommitRange,
}: OffersPerformanceSectionProps) {
  return (
    <section
      className={PERFORMANCE_SECTION_CLASS}
      aria-label={OFFERS_PAGE_COPY.performanceAriaLabel}
    >
      <OffersPerformanceDateRangeControl
        dateRangeLabel={performance.dateRangeLabel}
        selectedRange={performance.selectedRange}
        onCommitRange={onCommitRange}
      />
      <OffersKpiStrip kpis={performance.kpis} />
    </section>
  )
}
