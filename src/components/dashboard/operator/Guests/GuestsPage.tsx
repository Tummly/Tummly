import { useOutletContext } from "react-router-dom"

import { AddTagDialog } from "@/components/dashboard/operator/Guests/AddTagDialog"
import { GuestDetailsDrawer } from "@/components/dashboard/operator/Guests/GuestDetailsDrawer"
import { FeedbackDetailsDrawer } from "@/components/dashboard/operator/Feedback/FeedbackDetailsDrawer"
import { RecoveryWizardsHost } from "@/components/dashboard/operator/Feedback/RecoveryWizardsHost"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { GuestsBody } from "@/components/dashboard/operator/Guests/GuestsBody"
import { useGuestsPageModule } from "@/components/dashboard/operator/Guests/utils/useGuestsPageModule"
import { useDashboardUiStore } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import {
  labelForGuestsOverviewDateRange,
  type GuestsOverviewDateRange,
} from "@/lib/operatorGuests/guestsOverviewDateRange"
import { guestsFilterSheetSchemaForWorkspace } from "@/lib/operatorGuests/guestsFilterSheetSchema"

export function GuestsPage() {
  const guests = useGuestsPageModule()
  const { snapshot } = guests
  const { mode, locations, selectedLocationId } =
    useOutletContext<DashboardOutletContext>()
  const guestsOverviewDateRange = useDashboardUiStore(
    (state) => state.guestsOverviewDateRange
  )
  const setGuestsOverviewDateRange = useDashboardUiStore(
    (state) => state.setGuestsOverviewDateRange
  )

  const handleCommitOverviewDateRange = (range: GuestsOverviewDateRange) => {
    setGuestsOverviewDateRange(range)
    void guests.reloadForOverviewDateRange()
  }

  if (
    snapshot.viewModel == null &&
    (snapshot.loadStatus === "idle" || snapshot.loadStatus === "loading")
  ) {
    return (
      <div
        className="flex flex-1 items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading guests"
      >
        <Spinner />
      </div>
    )
  }

  if (snapshot.viewModel == null && snapshot.loadStatus === "error") {
    return (
      <div className="flex flex-1 flex-col items-center justify-center gap-3 text-center">
        <p className="text-sm text-destructive">
          Could not load guests. Please try again.
        </p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            void guests.retryLoad()
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

  const hasSelection = snapshot.selectedCount > 0

  return (
    <>
      {snapshot.loadStatus === "error" ? (
        <div className="mb-3 flex flex-wrap items-center gap-3">
          <p className="text-sm text-destructive" role="alert">
            Could not refresh guests. Please try again.
          </p>
          <Button
            type="button"
            variant="link"
            size="link-sm"
            className="font-medium underline"
            onClick={() => {
              void guests.retryLoad()
            }}
          >
            Retry
          </Button>
        </div>
      ) : null}
      {snapshot.actionError ? (
        <p className="mb-3 text-sm text-destructive" role="alert">
          {snapshot.actionError}
        </p>
      ) : null}
      <GuestsBody
        viewModel={snapshot.viewModel}
        searchQuery={snapshot.searchQuery}
        sortId={snapshot.sortId}
        bulkSelectionLabel={snapshot.bulkSelectionLabel}
        isAllVisibleSelected={snapshot.isAllVisibleSelected}
        isSomeVisibleSelected={snapshot.isSomeVisibleSelected}
        isGuestSelected={snapshot.isGuestSelected}
        onSmartGroupChange={guests.setActiveSmartGroupId}
        onSearchQueryChange={guests.setSearchQuery}
        onSortChange={guests.setSortId}
        onPreviousPage={guests.goToPreviousPage}
        onNextPage={guests.goToNextPage}
        onToggleGuestSelection={guests.toggleGuestSelection}
        onToggleSelectAllVisibleRows={guests.toggleSelectAllVisibleRows}
        onClearSelection={guests.clearSelection}
        onClearSearchAndFilters={guests.clearSearchAndFilters}
        onAddTag={
          hasSelection
            ? () => {
                void guests.openAddTag()
              }
            : undefined
        }
        onManageGuestTags={(guestId) => {
          void guests.openAddTag([guestId])
        }}
        onViewGuest={(guestId) => {
          void guests.openGuestDetails(Number.parseInt(guestId, 10))
        }}
        onExportCsv={() => {
          void guests.exportCsv()
        }}
        onExportSelected={
          hasSelection
            ? () => {
                void guests.exportSelectedCsv()
              }
            : undefined
        }
        exportBusy={snapshot.exportBusy}
        filterChips={snapshot.filterChips}
        filterChipCount={snapshot.filterChipCount}
        onOpenFilters={() => {
          void guests.openFilters()
        }}
        onRemoveFilterChip={guests.removeFilterChip}
        overviewDateRangeLabel={labelForGuestsOverviewDateRange(
          guestsOverviewDateRange
        )}
        overviewDateRange={guestsOverviewDateRange}
        onCommitOverviewDateRange={handleCommitOverviewDateRange}
      />
      <OperatorFilterSheetDialog
        open={snapshot.filtersSession != null}
        title="Filter guests"
        schema={guestsFilterSheetSchemaForWorkspace({
          locations: locations.map((location) => ({
            id: String(location.id),
            label: location.locationName,
          })),
          tags: snapshot.filterCatalog.map((tag) => ({
            id: tag.id,
            label: tag.name,
          })),
          showLocationFilter: locations.length > 1,
        })}
        session={snapshot.filtersSession}
        chipResolvers={{
          location: (id) =>
            locations.find((location) => String(location.id) === id)
              ?.locationName ?? id,
          tag: (id) =>
            snapshot.filterCatalog.find((tag) => tag.id === id)?.name ?? id,
        }}
        onSessionChange={guests.setFiltersSession}
        onOpenChange={(open) => {
          if (!open) {
            guests.closeFilters()
          }
        }}
        onApply={guests.applyFilters}
      />
      <AddTagDialog
        open={snapshot.addTagSession != null}
        session={snapshot.addTagSession}
        busy={snapshot.addTagBusy}
        onOpenChange={(open) => {
          if (!open) {
            guests.closeAddTag()
          }
        }}
        onStageTag={guests.stageAddTag}
        onUnstageTag={guests.unstageAddTag}
        onSearchChange={guests.setAddTagSearch}
        onCreateOpenChange={guests.setAddTagCreateOpen}
        onCreateNameChange={guests.setAddTagCreateName}
        onCreateTag={() => {
          void guests.createAndStageAddTag()
        }}
        onApply={() => {
          void guests.applyAddTag()
        }}
      />
      <GuestDetailsDrawer
        snapshot={snapshot.guestDetails}
        mode={mode}
        selectedLocationId={selectedLocationId}
        onOpenChange={(open) => {
          if (!open) {
            guests.closeGuestDetails()
          }
        }}
        onRetry={() => {
          void guests.retryGuestDetails()
        }}
        onNoteDraftChange={guests.setGuestDetailsNoteDraft}
        onCreateNote={guests.createGuestDetailsNote}
        onOpenFeedback={(feedbackId) => {
          void guests.openFeedbackDetails(feedbackId)
        }}
        onStartRecovery={(feedbackId) => {
          void guests.startRecovery(feedbackId)
        }}
      />
      <FeedbackDetailsDrawer
        snapshot={snapshot.feedbackDetails}
        onOpenChange={(open) => {
          if (!open) {
            guests.closeFeedbackDetails()
          }
        }}
        onRetry={() => {
          void guests.retryFeedbackDetails()
        }}
        onStartCorrection={guests.startClassificationCorrection}
        onDraftSentimentChange={guests.setClassificationDraftSentiment}
        onCancelCorrection={guests.cancelClassificationCorrection}
        onSaveCorrection={() => {
          void guests.saveClassificationCorrection()
        }}
        onReopen={() => {
          void guests.reopenFeedback()
        }}
        onStartMarkResolved={guests.startFeedbackMarkResolved}
        onMarkNoActionNeeded={() => {
          guests.startFeedbackMarkNoActionNeeded()
        }}
        onCancelCloseOut={guests.cancelFeedbackCloseOut}
        onSetCloseOutReason={guests.setFeedbackCloseOutReason}
        onSetCloseOutNoteDraft={guests.setFeedbackCloseOutNoteDraft}
        onConfirmCloseOut={() => {
          void guests.confirmFeedbackCloseOut()
        }}
        onStartRecovery={() => {
          const feedbackId = snapshot.feedbackDetails.feedbackId
          if (feedbackId == null) {
            return
          }
          void guests.startRecovery(feedbackId)
        }}
        onNoteDraftChange={guests.setFeedbackInternalNoteDraft}
        onCreateNote={() => {
          void guests.createFeedbackInternalNote()
        }}
        onStartNoteEdit={guests.startFeedbackNoteEdit}
        onNoteEditDraftChange={guests.setFeedbackNoteEditDraft}
        onCancelNoteEdit={guests.cancelFeedbackNoteEdit}
        onSaveNoteEdit={() => {
          void guests.saveFeedbackNoteEdit()
        }}
        onStartNoteDelete={guests.startFeedbackNoteDelete}
        onCancelNoteDelete={guests.cancelFeedbackNoteDelete}
        onConfirmNoteDelete={() => {
          void guests.confirmFeedbackNoteDelete()
        }}
      />
      <RecoveryWizardsHost snapshot={snapshot} wizards={guests.recoveryWizards} />
    </>
  )
}
