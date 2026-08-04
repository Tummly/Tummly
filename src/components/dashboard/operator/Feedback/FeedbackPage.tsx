import { useEffect, useMemo, useRef } from "react"
import { useNavigate, useOutletContext } from "react-router-dom"

import { FeedbackBody } from "@/components/dashboard/operator/Feedback/FeedbackBody"
import { FeedbackDetailsDrawer } from "@/components/dashboard/operator/Feedback/FeedbackDetailsDrawer"
import { FeedbackExportDialog } from "@/components/dashboard/operator/Feedback/FeedbackExportDialog"
import { StartRecoveryEntryShell } from "@/components/dashboard/operator/Feedback/StartRecoveryEntryShell"
import { RespondToGuestWizard } from "@/components/dashboard/operator/Feedback/RespondToGuestWizard"
import { RecordInternalActionWizard } from "@/components/dashboard/operator/Feedback/RecordInternalActionWizard"
import { RespondAndRecordInternalActionWizard } from "@/components/dashboard/operator/Feedback/RespondAndRecordInternalActionWizard"
import { RespondWithRecoveryOfferWizard } from "@/components/dashboard/operator/Feedback/RespondWithRecoveryOfferWizard"
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
        onStartInboxRecovery={(feedbackId) => {
          void feedback.startInboxRecovery(feedbackId)
        }}
        onStartInboxMarkResolved={(feedbackId) => {
          void feedback.startInboxMarkResolved(feedbackId)
        }}
        onStartInboxMarkNoActionNeeded={(feedbackId) => {
          void feedback.startInboxMarkNoActionNeeded(feedbackId)
        }}
        onExportFeedback={feedback.openExportDialog}
      />

      <StartRecoveryEntryShell
        snapshot={snapshot.startRecovery}
        onClose={feedback.closeStartRecovery}
        onSelectIntent={(intentId) => {
          feedback.selectStartRecoveryIntent(intentId)
        }}
        onRetry={() => {
          void feedback.retryStartRecovery()
        }}
      />

      <RespondToGuestWizard
        snapshot={snapshot.respondToGuest}
        onSaveAndExit={feedback.saveAndExitRespondToGuest}
        onBack={() => {
          void feedback.backRespondToGuest()
        }}
        onChannelChange={feedback.setRespondToGuestChannel}
        onPurposeChange={feedback.setRespondToGuestPurpose}
        onToneChange={feedback.setRespondToGuestTone}
        onIncludeNotesChange={feedback.setRespondToGuestIncludeNotes}
        onContinueSetup={feedback.continueRespondToGuestSetup}
        onWriteManually={feedback.writeRespondToGuestManually}
        onPrepareDraft={() => {
          void feedback.prepareRespondToGuestDraft()
        }}
        onRewriteDraft={(target) => {
          void feedback.rewriteRespondToGuestDraft(target)
        }}
        onRetryAiDraft={() => {
          void feedback.retryRespondToGuestAiDraft()
        }}
        onDismissPreparingOverlay={
          feedback.dismissRespondToGuestPreparingOverlay
        }
        onSubjectChange={feedback.setRespondToGuestSubject}
        onMessageChange={feedback.setRespondToGuestMessage}
        onContinueWrite={feedback.continueRespondToGuestWrite}
        onEditText={feedback.editRespondToGuestText}
        onOpenGuestPreview={feedback.openRespondToGuestGuestPreview}
        onCloseGuestPreview={feedback.closeRespondToGuestGuestPreview}
        onOpenSendConfirm={feedback.openRespondToGuestSendConfirm}
        onCancelSendConfirm={feedback.cancelRespondToGuestSendConfirm}
        onConfirmSend={() => {
          void feedback.confirmRespondToGuestSend()
        }}
        onKeepInProgress={() => {
          void feedback.keepRespondToGuestInProgress()
        }}
        onMarkResolved={() => {
          void feedback.markRespondToGuestResolved()
        }}
      />

      <RecordInternalActionWizard
        snapshot={snapshot.recordInternalAction}
        onSaveAndExit={feedback.saveAndExitRecordInternalAction}
        onBack={() => {
          void feedback.backRecordInternalAction()
        }}
        onCategoryChange={feedback.setRecordInternalActionCategory}
        onNoteChange={feedback.setRecordInternalActionNote}
        onContinueRecorder={feedback.continueRecordInternalActionRecorder}
        onOpenRecordConfirm={feedback.openRecordInternalActionConfirm}
        onCancelRecordConfirm={feedback.cancelRecordInternalActionConfirm}
        onConfirmRecord={() => {
          void feedback.confirmRecordInternalAction()
        }}
        onKeepInProgress={() => {
          void feedback.keepRecordInternalActionInProgress()
        }}
        onMarkResolved={() => {
          void feedback.markRecordInternalActionResolved()
        }}
      />

      <RespondAndRecordInternalActionWizard
        snapshot={snapshot.respondAndRecord}
        onSaveAndExit={feedback.saveAndExitRespondAndRecord}
        onBack={() => {
          void feedback.backRespondAndRecord()
        }}
        onCategoryChange={feedback.setRespondAndRecordCategory}
        onNoteChange={feedback.setRespondAndRecordNote}
        onUseConfirmedActionChange={feedback.setRespondAndRecordUseConfirmedAction}
        onContinueRecorder={feedback.continueRespondAndRecordRecorder}
        onEditInternalAction={feedback.editRespondAndRecordInternalAction}
        onChannelChange={feedback.setRespondAndRecordChannel}
        onPurposeChange={feedback.setRespondAndRecordPurpose}
        onToneChange={feedback.setRespondAndRecordTone}
        onIncludeNotesChange={feedback.setRespondAndRecordIncludeNotes}
        onContinueSetup={feedback.continueRespondAndRecordSetup}
        onWriteManually={feedback.writeRespondAndRecordManually}
        onPrepareDraft={() => {
          void feedback.prepareRespondAndRecordDraft()
        }}
        onRewriteDraft={(target) => {
          void feedback.rewriteRespondAndRecordDraft(target)
        }}
        onRetryAiDraft={() => {
          void feedback.retryRespondAndRecordAiDraft()
        }}
        onDismissPreparingOverlay={
          feedback.dismissRespondAndRecordPreparingOverlay
        }
        onSubjectChange={feedback.setRespondAndRecordSubject}
        onMessageChange={feedback.setRespondAndRecordMessage}
        onContinueWrite={feedback.continueRespondAndRecordWrite}
        onEditText={feedback.editRespondAndRecordText}
        onOpenGuestPreview={feedback.openRespondAndRecordGuestPreview}
        onCloseGuestPreview={feedback.closeRespondAndRecordGuestPreview}
        onOpenSendConfirm={feedback.openRespondAndRecordSendConfirm}
        onCancelSendConfirm={feedback.cancelRespondAndRecordSendConfirm}
        onConfirmSend={() => {
          void feedback.confirmRespondAndRecordSend()
        }}
        onKeepInProgress={() => {
          void feedback.keepRespondAndRecordInProgress()
        }}
        onMarkResolved={() => {
          void feedback.markRespondAndRecordResolved()
        }}
      />

      <RespondWithRecoveryOfferWizard
        snapshot={snapshot.respondWithRecoveryOffer}
        onSaveAndExit={feedback.saveAndExitRespondWithRecoveryOffer}
        onBack={() => {
          void feedback.backRespondWithRecoveryOffer()
        }}
        onChannelChange={feedback.setRespondWithRecoveryOfferChannel}
        onToneChange={feedback.setRespondWithRecoveryOfferTone}
        onIncludeNotesChange={feedback.setRespondWithRecoveryOfferIncludeNotes}
        onContinueSetup={feedback.continueRespondWithRecoveryOfferSetup}
        onOfferTypeChange={feedback.setRespondWithRecoveryOfferType}
        onDiscountPercentageChange={
          feedback.setRespondWithRecoveryOfferDiscountPercentage
        }
        onDiscountAmountChange={
          feedback.setRespondWithRecoveryOfferDiscountAmount
        }
        onFreeItemTextChange={feedback.setRespondWithRecoveryOfferFreeItemText}
        onPurchaseRequirementChange={
          feedback.setRespondWithRecoveryOfferPurchaseRequirement
        }
        onMinimumSpendChange={
          feedback.setRespondWithRecoveryOfferMinimumSpend
        }
        onAdditionalExclusionsChange={
          feedback.setRespondWithRecoveryOfferAdditionalExclusions
        }
        onReplacementItemTextChange={
          feedback.setRespondWithRecoveryOfferReplacementItemText
        }
        onOfferTitleChange={feedback.setRespondWithRecoveryOfferTitle}
        onOfferDescriptionChange={
          feedback.setRespondWithRecoveryOfferDescription
        }
        onOfferValidityChange={feedback.setRespondWithRecoveryOfferValidity}
        onExpiryDateChange={feedback.setRespondWithRecoveryOfferExpiryDate}
        onStaffInstructionsChange={
          feedback.setRespondWithRecoveryOfferStaffInstructions
        }
        onPrepareOfferDescription={() => {
          void feedback.prepareRespondWithRecoveryOfferDescription()
        }}
        onContinueOffer={feedback.continueRespondWithRecoveryOfferDetails}
        onEditOffer={feedback.editRespondWithRecoveryOffer}
        onWriteManually={feedback.writeRespondWithRecoveryOfferManually}
        onPrepareDraft={() => {
          void feedback.prepareRespondWithRecoveryOfferDraft()
        }}
        onRewriteDraft={(target) => {
          void feedback.rewriteRespondWithRecoveryOfferDraft(target)
        }}
        onRetryAiDraft={() => {
          void feedback.retryRespondWithRecoveryOfferAiDraft()
        }}
        onDismissPreparingOverlay={
          feedback.dismissRespondWithRecoveryOfferPreparingOverlay
        }
        onSubjectChange={feedback.setRespondWithRecoveryOfferSubject}
        onMessageChange={feedback.setRespondWithRecoveryOfferMessage}
        onContinueWrite={feedback.continueRespondWithRecoveryOfferWrite}
        onEditText={feedback.editRespondWithRecoveryOfferText}
        onOpenGuestPreview={feedback.openRespondWithRecoveryOfferGuestPreview}
        onCloseGuestPreview={feedback.closeRespondWithRecoveryOfferGuestPreview}
        onOpenSendConfirm={feedback.openRespondWithRecoveryOfferSendConfirm}
        onCancelSendConfirm={
          feedback.cancelRespondWithRecoveryOfferSendConfirm
        }
        onConfirmSend={() => {
          void feedback.confirmRespondWithRecoveryOfferSend()
        }}
        onKeepInProgress={() => {
          void feedback.keepRespondWithRecoveryOfferInProgress()
        }}
        onMarkResolved={() => {
          void feedback.markRespondWithRecoveryOfferResolved()
        }}
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
        onDraftReasonChange={feedback.setClassificationDraftReason}
        onDraftNoteChange={feedback.setClassificationDraftNote}
        onCancelCorrection={feedback.cancelClassificationCorrection}
        onSaveCorrection={() => {
          void feedback.saveClassificationCorrection()
        }}
        onReopen={() => {
          void feedback.reopenFeedbackDetails()
        }}
        onStartMarkResolved={feedback.startFeedbackMarkResolved}
        onMarkNoActionNeeded={() => {
          feedback.startFeedbackMarkNoActionNeeded()
        }}
        onCancelCloseOut={feedback.cancelFeedbackCloseOut}
        onSetCloseOutReason={feedback.setFeedbackCloseOutReason}
        onSetCloseOutNoteDraft={feedback.setFeedbackCloseOutNoteDraft}
        onSetCloseOutAcknowledged={feedback.setFeedbackCloseOutAcknowledged}
        onConfirmCloseOut={() => {
          void feedback.confirmFeedbackCloseOut()
        }}
        onViewGuestProfile={navigateToGuestProfile}
        onStartRecovery={() => {
          const feedbackId = snapshot.feedbackDetails.feedbackId
          if (feedbackId == null) {
            return
          }
          void feedback.startInboxRecovery(feedbackId)
        }}
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
