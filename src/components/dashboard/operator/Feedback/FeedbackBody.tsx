import type { RefObject } from "react"

import { FeedbackInboxSection } from "@/components/dashboard/operator/Feedback/FeedbackInboxSection"
import { FeedbackPageDateRangeControl } from "@/components/dashboard/operator/Feedback/FeedbackPageDateRangeControl"
import { FeedbackPageHeaderActionsMenu } from "@/components/dashboard/operator/Feedback/FeedbackPageHeaderActionsMenu"
import { FeedbackSummarySection } from "@/components/dashboard/operator/Feedback/FeedbackSummarySection"
import { Button } from "@/components/ui/button"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  FEEDBACK_PAGE_COPY,
  FEEDBACK_PAGE_META_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import type { FilterChip } from "@/lib/operatorFilterSheet"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  OperatorFeedbackInboxEmptyStateKind,
  OperatorFeedbackInboxSortId,
  OperatorFeedbackInboxTab,
  OperatorFeedbackInboxTabId,
  OperatorFeedbackInboxTableRow,
  OperatorFeedbackPageViewModel,
} from "@/types/operatorFeedback"
import type { FeedbackWorkflowStatus } from "@/types/dashboard"

type FeedbackBodyProps = {
  viewModel: OperatorFeedbackPageViewModel
  activeInboxTabId: OperatorFeedbackInboxTabId
  selectedDateRange: HomePerformanceDateRange
  onCommitDateRange: (range: HomePerformanceDateRange) => void
  onReviewNeedsAttention: () => void
  onChangePeriod: () => void
  onViewCapture: () => void
  inboxRef: RefObject<HTMLElement | null>
  onInboxTabChange: (id: OperatorFeedbackInboxTabId) => void
  inboxTabs: OperatorFeedbackInboxTab[]
  inboxRows: OperatorFeedbackInboxTableRow[]
  inboxSearchQuery: string
  onInboxSearchQueryChange: (query: string) => void
  inboxSortId: OperatorFeedbackInboxSortId
  inboxSortLabel: string
  onInboxSortChange: (id: OperatorFeedbackInboxSortId) => void
  inboxPageRangeLabel: string
  inboxCanGoPrevious: boolean
  inboxCanGoNext: boolean
  onInboxPreviousPage: () => void
  onInboxNextPage: () => void
  inboxTableEmptyState: OperatorFeedbackInboxEmptyStateKind | null
  onInboxClearSearchAndFilters: () => void
  inboxFilterChips: readonly FilterChip[]
  inboxFilterChipCount: number
  onInboxOpenFilters: () => void
  onInboxRemoveFilterChip: (chip: FilterChip) => void
  onOpenFeedbackDetails: (feedbackId: number) => void
  onSetRowWorkflowStatus: (
    feedbackId: number,
    status: FeedbackWorkflowStatus
  ) => void
  onReopenFeedback: (feedbackId: number) => void
  onMarkNoActionNeeded: (feedbackId: number) => void
}

/** Feedback page body — header chrome, summary KPIs, and inbox. */
export function FeedbackBody({
  viewModel,
  activeInboxTabId,
  selectedDateRange,
  onCommitDateRange,
  onReviewNeedsAttention,
  onChangePeriod,
  onViewCapture,
  inboxRef,
  onInboxTabChange,
  inboxTabs,
  inboxRows,
  inboxSearchQuery,
  onInboxSearchQueryChange,
  inboxSortId,
  inboxSortLabel,
  onInboxSortChange,
  inboxPageRangeLabel,
  inboxCanGoPrevious,
  inboxCanGoNext,
  onInboxPreviousPage,
  onInboxNextPage,
  inboxTableEmptyState,
  onInboxClearSearchAndFilters,
  inboxFilterChips,
  inboxFilterChipCount,
  onInboxOpenFilters,
  onInboxRemoveFilterChip,
  onOpenFeedbackDetails,
  onSetRowWorkflowStatus,
  onReopenFeedback,
  onMarkNoActionNeeded,
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

      <FeedbackInboxSection
        inboxRef={inboxRef}
        tabs={inboxTabs}
        activeTabId={activeInboxTabId}
        onTabChange={onInboxTabChange}
        rows={inboxRows}
        searchQuery={inboxSearchQuery}
        onSearchQueryChange={onInboxSearchQueryChange}
        sortId={inboxSortId}
        sortLabel={inboxSortLabel}
        onSortChange={onInboxSortChange}
        pageRangeLabel={inboxPageRangeLabel}
        canGoPrevious={inboxCanGoPrevious}
        canGoNext={inboxCanGoNext}
        onPreviousPage={onInboxPreviousPage}
        onNextPage={onInboxNextPage}
        tableEmptyState={inboxTableEmptyState}
        onClearSearchAndFilters={onInboxClearSearchAndFilters}
        onChangePeriod={onChangePeriod}
        filterChips={inboxFilterChips}
        filterChipCount={inboxFilterChipCount}
        onOpenFilters={onInboxOpenFilters}
        onRemoveFilterChip={onInboxRemoveFilterChip}
        onOpenFeedbackDetails={onOpenFeedbackDetails}
        onSetRowWorkflowStatus={onSetRowWorkflowStatus}
        onReopenFeedback={onReopenFeedback}
        onMarkNoActionNeeded={onMarkNoActionNeeded}
      />
    </div>
  )
}
