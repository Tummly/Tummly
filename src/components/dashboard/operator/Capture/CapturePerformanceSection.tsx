import { CaptureKpiStrip } from "@/components/dashboard/operator/Capture/CaptureKpiStrip"
import { CapturePerformanceDateRangeControl } from "@/components/dashboard/operator/Capture/CapturePerformanceDateRangeControl"
import type { OperatorCapturePerformanceView } from "@/lib/operatorCapture/createOperatorCapturePageModule"
import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS,
  CAPTURE_PERFORMANCE_HEADER_ROW_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { PERFORMANCE_HEADER_COPY_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"

type CapturePerformanceSectionProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  performance: OperatorCapturePerformanceView
  onCommitRange: (range: HomePerformanceDateRange) => void
}

/** Capture performance section — header + date control + KPI strip or empty. */
export function CapturePerformanceSection({
  dateRangeLabel,
  selectedRange,
  performance,
  onCommitRange,
}: CapturePerformanceSectionProps) {
  const copy = OPERATOR_CAPTURE_SECTION_COPY.performance

  return (
    <section className={CAPTURE_SECTION_CLASS}>
      <div className={CAPTURE_PERFORMANCE_HEADER_ROW_CLASS}>
        <div className={PERFORMANCE_HEADER_COPY_CLASS}>
          <div className="leading-[0]">
            <h2 className={CAPTURE_SECTION_TITLE_CLASS}>{copy.title}</h2>
          </div>
          <div className="leading-[0]">
            <p className={CAPTURE_SECTION_SUBTITLE_CLASS}>{copy.description}</p>
          </div>
        </div>

        <CapturePerformanceDateRangeControl
          dateRangeLabel={dateRangeLabel}
          selectedRange={selectedRange}
          onCommitRange={onCommitRange}
        />
      </div>

      {performance.isEmpty ? (
        <div className={CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS}>
          <div className="flex flex-col items-center gap-2.5 text-center">
            <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
            <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[292px]`}>
              {copy.emptyHelper}
            </p>
          </div>
        </div>
      ) : (
        <CaptureKpiStrip kpis={performance.kpis} />
      )}
    </section>
  )
}
