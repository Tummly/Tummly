import { CaptureOverviewKpiStrip } from "@/components/dashboard/operator/Capture/CaptureOverviewKpiStrip"
import { CaptureMultiCaptureOverviewDateRangeControl } from "@/components/dashboard/operator/Capture/CaptureMultiCaptureOverviewDateRangeControl"
import type { OperatorMultiCaptureOverviewView } from "@/lib/operatorMultiCapture/createOperatorMultiCapturePageModule"
import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS,
  CAPTURE_PERFORMANCE_HEADER_ROW_CLASS,
  CAPTURE_SECTION_CLASS,
  CAPTURE_SECTION_SUBTITLE_CLASS,
  CAPTURE_SECTION_TITLE_CLASS,
  OPERATOR_CAPTURE_MULTI_SECTION_COPY,
} from "@/lib/operatorCapture/capturePresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { PERFORMANCE_HEADER_COPY_CLASS } from "@/lib/operatorHome/performanceOverviewPresentation"

type CaptureOverviewSectionProps = {
  dateRangeLabel: string
  selectedRange: HomePerformanceDateRange
  overview: OperatorMultiCaptureOverviewView
  onCommitRange: (range: HomePerformanceDateRange) => void
}

/** Multi Capture overview section — header + date control + KPI strip or empty. */
export function CaptureOverviewSection({
  dateRangeLabel,
  selectedRange,
  overview,
  onCommitRange,
}: CaptureOverviewSectionProps) {
  const copy = OPERATOR_CAPTURE_MULTI_SECTION_COPY.overview
  const showKpiStrip =
    !overview.isNoLocations && !overview.isLoadError && overview.kpis.length > 0

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

        <CaptureMultiCaptureOverviewDateRangeControl
          dateRangeLabel={dateRangeLabel}
          selectedRange={selectedRange}
          onCommitRange={onCommitRange}
        />
      </div>

      {overview.isNoLocations ? (
        <div className={CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS}>
          <div className="flex flex-col items-center gap-2.5 text-center">
            <p className={CAPTURE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
            <p className={`${CAPTURE_EMPTY_HELPER_CLASS} max-w-[450px]`}>
              {copy.emptyHelper}
            </p>
          </div>
        </div>
      ) : showKpiStrip ? (
        <CaptureOverviewKpiStrip kpis={overview.kpis} />
      ) : (
        <div className={CAPTURE_PERFORMANCE_EMPTY_BODY_CLASS} aria-hidden />
      )}
    </section>
  )
}
