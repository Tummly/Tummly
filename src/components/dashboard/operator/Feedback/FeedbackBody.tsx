import type { RefObject } from "react"

import { Button } from "@/components/ui/button"
import { FeedbackPageDateRangeControl } from "@/components/dashboard/operator/Feedback/FeedbackPageDateRangeControl"
import { FeedbackPageHeaderActionsMenu } from "@/components/dashboard/operator/Feedback/FeedbackPageHeaderActionsMenu"
import { FeedbackSummarySection } from "@/components/dashboard/operator/Feedback/FeedbackSummarySection"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  FEEDBACK_PAGE_COPY,
  FEEDBACK_PAGE_META_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type { OperatorFeedbackPageViewModel } from "@/types/operatorFeedback"

type FeedbackBodyProps = {
  viewModel: OperatorFeedbackPageViewModel
  selectedDateRange: HomePerformanceDateRange
  onCommitDateRange: (range: HomePerformanceDateRange) => void
  onReviewNeedsAttention: () => void
  onChangePeriod: () => void
  onViewCapture: () => void
  inboxRef: RefObject<HTMLElement | null>
}

/** Feedback page body — header chrome, summary KPIs, minimal inbox shell. */
export function FeedbackBody({
  viewModel,
  selectedDateRange,
  onCommitDateRange,
  onReviewNeedsAttention,
  onChangePeriod,
  onViewCapture,
  inboxRef,
}: FeedbackBodyProps) {
  const copy = FEEDBACK_PAGE_COPY

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
          <p className={FEEDBACK_PAGE_META_CLASS}>
            {viewModel.locationName} · {viewModel.dateRangeLabel}
            {viewModel.updatedRelativeLabel
              ? ` · Updated ${viewModel.updatedRelativeLabel}`
              : null}
          </p>
        </header>

        <div className="flex shrink-0 flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="op-primary"
            className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
            onClick={onReviewNeedsAttention}
          >
            {copy.reviewNeedsAttention(viewModel.needsAttentionCount)}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            disabled
            aria-disabled
            aria-label={`${copy.summariseWithAi} (unavailable)`}
            title={`${copy.summariseWithAi} is unavailable`}
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          >
            {copy.summariseWithAi}
          </Button>
          <FeedbackPageDateRangeControl
            dateRangeLabel={viewModel.dateRangeLabel}
            selectedRange={selectedDateRange}
            onCommitRange={onCommitDateRange}
          />
          <FeedbackPageHeaderActionsMenu
            locationName={viewModel.locationName}
          />
        </div>
      </div>

      <FeedbackSummarySection
        summary={viewModel.summary}
        onChangePeriod={onChangePeriod}
        onViewCapture={onViewCapture}
      />

      <section
        ref={inboxRef}
        id="feedback-inbox"
        className={GUESTS_SECTION_CLASS}
      >
        <header className="flex flex-col gap-2 leading-[0]">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.inbox.title}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.inbox.subtitle}</p>
        </header>
      </section>
    </div>
  )
}
