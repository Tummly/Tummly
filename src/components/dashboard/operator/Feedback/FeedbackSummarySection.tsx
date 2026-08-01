import { Button } from "@/components/ui/button"
import {
  GUESTS_KPI_CARD_CLASS,
  GUESTS_KPI_DESCRIPTION_CLASS,
  GUESTS_KPI_GRID_CLASS,
  GUESTS_KPI_LABEL_CLASS,
  GUESTS_KPI_VALUE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_EMPTY_HELPER_CLASS,
  GUESTS_TABLE_EMPTY_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { FEEDBACK_PAGE_COPY } from "@/lib/operatorFeedback/feedbackPresentation"
import type { OperatorFeedbackSummarySection } from "@/types/operatorFeedback"

type FeedbackSummarySectionProps = {
  summary: OperatorFeedbackSummarySection
  onChangePeriod: () => void
  onViewCapture: () => void
}

/** Feedback summary — four-cell KPI strip or empty card (no in-section date control). */
export function FeedbackSummarySection({
  summary,
  onChangePeriod,
  onViewCapture,
}: FeedbackSummarySectionProps) {
  const copy = FEEDBACK_PAGE_COPY.summary

  return (
    <section className={GUESTS_SECTION_CLASS}>
      <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
        <header className="flex flex-col gap-2 leading-[0]">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.title}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </header>
      </div>

      {summary.kind === "empty" ? (
        <div className="flex flex-col items-center gap-4 py-6 text-center">
          <div className="flex flex-col items-center gap-2.5">
            <p className={GUESTS_TABLE_EMPTY_TITLE_CLASS}>{copy.emptyTitle}</p>
            <p className={`${GUESTS_TABLE_EMPTY_HELPER_CLASS} max-w-[450px]`}>
              {copy.emptyHelper}
            </p>
          </div>
          <div className="flex flex-wrap items-center justify-center gap-3">
            <Button
              type="button"
              variant="op-secondary"
              onClick={onChangePeriod}
            >
              {copy.changePeriod}
            </Button>
            <Button type="button" variant="op-tertiary" onClick={onViewCapture}>
              {copy.viewCapture}
            </Button>
          </div>
        </div>
      ) : (
        <div className={GUESTS_KPI_GRID_CLASS}>
          {summary.kpis.map((kpi) => (
            <div key={kpi.id} className={GUESTS_KPI_CARD_CLASS}>
              <div className="flex flex-col gap-0.5">
                <p className={GUESTS_KPI_LABEL_CLASS}>{kpi.label}</p>
                <p className={GUESTS_KPI_VALUE_CLASS}>{kpi.value}</p>
              </div>
              <div className="flex flex-col gap-0.5">
                {kpi.shareLabel != null ? (
                  <p className={GUESTS_KPI_DESCRIPTION_CLASS}>{kpi.shareLabel}</p>
                ) : null}
                {kpi.comparisonLabel != null ? (
                  <p className={GUESTS_KPI_DESCRIPTION_CLASS}>
                    {kpi.comparisonLabel}
                  </p>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
