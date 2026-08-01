import { useEffect, useMemo, useRef } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"

import { FeedbackBody } from "@/components/dashboard/operator/Feedback/FeedbackBody"
import { FeedbackDetailsDrawer } from "@/components/dashboard/operator/Feedback/FeedbackDetailsDrawer"
import { FeedbackExportDialog } from "@/components/dashboard/operator/Feedback/FeedbackExportDialog"
import { useFeedbackPageModule } from "@/components/dashboard/operator/Feedback/utils/useFeedbackPageModule"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { FEEDBACK_LOAD_ERROR_MESSAGE } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import {
  FEEDBACK_INBOX_CATALOG_QR_SOURCE_OPTIONS,
  feedbackInboxDigitalLinkOptionId,
  feedbackInboxFilterSheetSchema,
} from "@/lib/operatorFeedback/feedbackInboxFilterSheetSchema"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  operatorDashboardCaptureLocationPath,
  operatorDashboardGuestProfilePath,
  operatorDashboardNavPath,
} from "@/lib/operatorHome/operatorDashboardPaths"

export function FeedbackPage() {
  const feedback = useFeedbackPageModule()
  const { snapshot } = feedback
  const { mode, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const navigate = useNavigate()
  const inboxRef = useRef<HTMLElement | null>(null)

  const feedbackPageDateRange = useDashboardUiStore(
    (state) => state.feedbackPageDateRange
  )
  const setFeedbackPageDateRange = useDashboardUiStore(
    (state) => state.setFeedbackPageDateRange
  )

  const handleCommitDateRange = (range: HomePerformanceDateRange) => {
    setFeedbackPageDateRange(range)
    void feedback.reloadForFeedbackPageDateRange()
  }

  const filterSheetSchema = useMemo(() => {
    const digitalGuestLinks =
      snapshot.viewModel?.inbox.digitalGuestLinks.map((link) => ({
        id: feedbackInboxDigitalLinkOptionId(link.id),
        label: link.linkName,
      })) ?? []

    return feedbackInboxFilterSheetSchema({ digitalGuestLinks })
  }, [snapshot.viewModel?.inbox.digitalGuestLinks])

  useEffect(() => {
    if (snapshot.scrollToInboxRequestId === 0) {
      return
    }
    inboxRef.current?.scrollIntoView({ behavior: "smooth", block: "start" })
  }, [snapshot.scrollToInboxRequestId])

  useEffect(() => {
    if (snapshot.openDateRangeRequestId === 0) {
      return
    }
    document
      .querySelector<HTMLButtonElement>(
        '[aria-label="' + (snapshot.viewModel?.dateRangeLabel ?? "") + '"]'
      )
      ?.click()
  }, [snapshot.openDateRangeRequestId, snapshot.viewModel?.dateRangeLabel])

  if (
    snapshot.viewModel == null
    && (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading feedback"
      >
        <Spinner />
      </div>
    )
  }

  if (snapshot.loadStatus === "error" && snapshot.viewModel == null) {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3">
        <p className="m-0 text-sm text-muted-foreground">
          {FEEDBACK_LOAD_ERROR_MESSAGE}
        </p>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void feedback.retryLoad()
          }}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (snapshot.viewModel == null) {
    return null
  }

  const viewModel = snapshot.viewModel
  const inbox = viewModel.inbox

  const navigateToGuestProfile = (locationGuestId: number) => {
    if (selectedLocationId == null) {
      return
    }
    navigate(
      operatorDashboardGuestProfilePath(
        mode,
        locationGuestId,
        selectedLocationId
      )
    )
  }

  return (
    <>
      <FeedbackBody
        viewModel={viewModel}
        activeInboxTabId={snapshot.activeInboxTabId}
        selectedDateRange={feedbackPageDateRange}
        onCommitDateRange={handleCommitDateRange}
        onReviewNeedsAttention={() => {
          feedback.reviewNeedsAttention()
        }}
        onChangePeriod={() => {
          feedback.requestOpenDateRange()
        }}
        onViewCapture={() => {
          if (selectedLocationId == null) {
            return
          }
          if (mode === "multi") {
            navigate(operatorDashboardCaptureLocationPath(selectedLocationId))
            return
          }
          navigate(
            operatorDashboardNavPath("single", "capture", selectedLocationId)
          )
        }}
        inboxRef={inboxRef}
        onInboxTabChange={feedback.setActiveInboxTabId}
        inboxTabs={inbox.tabs}
        inboxRows={inbox.tableRows}
        inboxSearchQuery={inbox.searchQuery}
        onInboxSearchQueryChange={feedback.setSearchQuery}
        inboxSortId={inbox.sortId}
        inboxSortLabel={inbox.sortLabel}
        onInboxSortChange={feedback.setSortId}
        inboxPageRangeLabel={inbox.pageRangeLabel}
        inboxCanGoPrevious={inbox.canGoPrevious}
        inboxCanGoNext={inbox.canGoNext}
        onInboxPreviousPage={feedback.goToPreviousPage}
        onInboxNextPage={feedback.goToNextPage}
        inboxTableEmptyState={inbox.tableEmptyState}
        onInboxClearSearchAndFilters={feedback.clearSearchAndFilters}
        inboxFilterChips={inbox.filterChips}
        inboxFilterChipCount={inbox.filterChipCount}
        onInboxOpenFilters={feedback.openFilters}
        onInboxRemoveFilterChip={feedback.removeFilterChip}
        onOpenFeedbackDetails={(feedbackId) => {
          void feedback.openFeedbackDetails(feedbackId)
        }}
        onSetRowWorkflowStatus={(feedbackId, status) => {
          void feedback.setRowWorkflowStatus(feedbackId, status)
        }}
        onReopenFeedback={(feedbackId) => {
          void feedback.reopenFeedback(feedbackId)
        }}
        onMarkNoActionNeeded={(feedbackId) => {
          void feedback.markFeedbackNoActionNeeded(feedbackId)
        }}
        onExportFeedback={feedback.openExportDialog}
      />

      {snapshot.exportDialog != null ? (
        <FeedbackExportDialog
          dialog={snapshot.exportDialog}
          onOpenChange={(open) => {
            if (!open) {
              feedback.closeExportDialog()
            }
          }}
          onScopeChange={feedback.setExportScope}
          onFormatChange={feedback.setExportFormat}
          onIncludeGuestContactChange={feedback.setExportIncludeGuestContact}
          onDownload={() => {
            void feedback.downloadExport()
          }}
        />
      ) : null}

      <OperatorFilterSheetDialog
        open={snapshot.filtersSession != null}
        title="Filter feedback"
        schema={filterSheetSchema}
        session={snapshot.filtersSession}
        chipResolvers={{
          qrSource: (id) => {
            if (id.startsWith("dgl:")) {
              const linkId = Number.parseInt(id.slice(4), 10)
              return (
                inbox.digitalGuestLinks.find((link) => link.id === linkId)
                  ?.linkName ?? id
              )
            }
            return (
              FEEDBACK_INBOX_CATALOG_QR_SOURCE_OPTIONS.find(
                (option) => option.id === id
              )?.label ?? id
            )
          },
        }}
        onSessionChange={feedback.setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            feedback.closeFilters()
          }
        }}
        onApply={feedback.applyFilters}
      />

      <FeedbackDetailsDrawer
        snapshot={snapshot.feedbackDetails}
        onOpenChange={(open) => {
          if (!open) {
            feedback.closeFeedbackDetails()
          }
        }}
        onRetry={() => {
          void feedback.retryFeedbackDetails()
        }}
        onStartCorrection={feedback.startClassificationCorrection}
        onDraftSentimentChange={feedback.setClassificationDraftSentiment}
        onCancelCorrection={feedback.cancelClassificationCorrection}
        onSaveCorrection={() => {
          void feedback.saveClassificationCorrection()
        }}
        onWorkflowStatusChange={(status) => {
          void feedback.setFeedbackWorkflowStatus(status)
        }}
        onReopen={() => {
          void feedback.reopenFeedbackDetails()
        }}
        onMarkNoActionNeeded={() => {
          void feedback.markFeedbackDetailsNoActionNeeded()
        }}
        onViewGuestProfile={navigateToGuestProfile}
        onNoteDraftChange={feedback.setFeedbackInternalNoteDraft}
        onCreateNote={() => {
          void feedback.createFeedbackInternalNote()
        }}
        onStartNoteEdit={feedback.startFeedbackNoteEdit}
        onNoteEditDraftChange={feedback.setFeedbackNoteEditDraft}
        onCancelNoteEdit={feedback.cancelFeedbackNoteEdit}
        onSaveNoteEdit={() => feedback.saveFeedbackNoteEdit()}
        onStartNoteDelete={feedback.startFeedbackNoteDelete}
        onCancelNoteDelete={feedback.cancelFeedbackNoteDelete}
        onConfirmNoteDelete={() => {
          void feedback.confirmFeedbackNoteDelete()
        }}
        canGoPrevious={snapshot.canGoPreviousFeedback}
        canGoNext={snapshot.canGoNextFeedback}
        onPrevious={() => {
          void feedback.openPreviousFeedback()
        }}
        onNext={() => {
          void feedback.openNextFeedback()
        }}
      />
    </>
  )
}
