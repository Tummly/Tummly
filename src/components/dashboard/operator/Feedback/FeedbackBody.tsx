import type { RefObject } from "react"

import { FeedbackInboxSection } from "@/components/dashboard/operator/Feedback/FeedbackInboxSection"
import { FeedbackPageDateRangeControl } from "@/components/dashboard/operator/Feedback/FeedbackPageDateRangeControl"
import { FeedbackPageHeaderActionsMenu } from "@/components/dashboard/operator/Feedback/FeedbackPageHeaderActionsMenu"
import { FeedbackSummarySection } from "@/components/dashboard/operator/Feedback/FeedbackSummarySection"
import type { OperatorTabContentStatus } from "@/components/dashboard/operator/OperatorTableTabPanel"
import { Button } from "@/components/ui/button"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  FEEDBACK_PAGE_COPY,
  FEEDBACK_PAGE_META_CLASS,
  FEEDBACK_PAGE_META_STACK_CLASS,
  FEEDBACK_PAGE_SUBTITLE_CLASS,
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

type FeedbackBodyProps = {
  tabContentStatus: OperatorTabContentStatus
  viewModel: OperatorFeedbackPageViewModel
  activeInboxTabId: OperatorFeedbackInboxTabId
  selectedDateRange: HomePerformanceDateRange
  onCommitDateRange: (range: HomePerformanceDateRange) => void
  onReviewNeedsAttention: () => void
  onSummariseWithAi: () => void
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
  onStartInboxRecovery: (feedbackId: number) => void
  onReopenInboxFeedback: (feedbackId: number) => void
  onStartInboxMarkResolved: (feedbackId: number) => void
  onStartInboxMarkNoActionNeeded: (feedbackId: number) => void
  onExportFeedback: () => void
}

/** Feedback page body — header chrome, summary KPIs, and inbox. */
export function FeedbackBody({
  tabContentStatus,
  viewModel,
  activeInboxTabId,
  selectedDateRange,
  onCommitDateRange,
  onReviewNeedsAttention,
  onSummariseWithAi,
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
  onStartInboxRecovery,
  onReopenInboxFeedback,
  onStartInboxMarkResolved,
  onStartInboxMarkNoActionNeeded,
  onExportFeedback,
}: FeedbackBodyProps) {
  const copy = FEEDBACK_PAGE_COPY

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <header className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={FEEDBACK_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
          <div className={FEEDBACK_PAGE_META_STACK_CLASS}>
            <p className={FEEDBACK_PAGE_META_CLASS}>
              {viewModel.locationName} · {viewModel.dateRangeLabel}
            </p>
            {viewModel.updatedRelativeLabel ? (
              <p className={FEEDBACK_PAGE_META_CLASS}>
                Updated {viewModel.updatedRelativeLabel}
              </p>
            ) : null}
          </div>
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
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onSummariseWithAi}
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
            onExportFeedback={onExportFeedback}
          />
        </div>
      </div>

      <FeedbackSummarySection
        summary={viewModel.summary}
        onChangePeriod={onChangePeriod}
        onViewCapture={onViewCapture}
      />

      <FeedbackInboxSection
        tabContentStatus={tabContentStatus}
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
        onStartInboxRecovery={onStartInboxRecovery}
        onReopenInboxFeedback={onReopenInboxFeedback}
        onStartInboxMarkResolved={onStartInboxMarkResolved}
        onStartInboxMarkNoActionNeeded={onStartInboxMarkNoActionNeeded}
      />
    </div>
  )
}
