import { StartRecoveryEntryShell } from "@/components/dashboard/operator/Feedback/StartRecoveryEntryShell"
import { RespondToGuestWizard } from "@/components/dashboard/operator/Feedback/RespondToGuestWizard"
import { RecordInternalActionWizard } from "@/components/dashboard/operator/Feedback/RecordInternalActionWizard"
import { RespondAndRecordInternalActionWizard } from "@/components/dashboard/operator/Feedback/RespondAndRecordInternalActionWizard"
import { RespondWithRecoveryOfferWizard } from "@/components/dashboard/operator/Feedback/RespondWithRecoveryOfferWizard"
import type {
  RecoveryWizardsModule,
  RecoveryWizardsSnapshot,
} from "@/lib/operatorFeedback/createRecoveryWizardsModule"

/**
 * Mounts the Start recovery entry shell plus the four recovery wizards
 * against a shared `RecoveryWizardsModule`. Reused by Feedback, Guests, and
 * Guest Profile so an operator gets the same recovery path from any of
 * those surfaces (see ticket 21 / AUDIT F-02).
 */
export function RecoveryWizardsHost({
  snapshot,
  wizards,
}: {
  snapshot: RecoveryWizardsSnapshot
  wizards: RecoveryWizardsModule
}) {
  return (
    <>
      <StartRecoveryEntryShell
        snapshot={snapshot.startRecovery}
        onClose={wizards.closeStartRecovery}
        onSelectIntent={(intentId) => {
          wizards.selectStartRecoveryIntent(intentId)
        }}
        onRetry={() => {
          void wizards.retryStartRecovery()
        }}
      />

      <RespondToGuestWizard
        snapshot={snapshot.respondToGuest}
        onSaveAndExit={wizards.respondToGuest.saveAndExit}
        onBack={() => {
          wizards.respondToGuest.back()
        }}
        onChannelChange={wizards.respondToGuest.setChannel}
        onPurposeChange={wizards.respondToGuest.setPurpose}
        onToneChange={wizards.respondToGuest.setTone}
        onIncludeNotesChange={wizards.respondToGuest.setIncludeNotes}
        onContinueSetup={wizards.respondToGuest.continueSetup}
        onWriteManually={wizards.respondToGuest.writeManually}
        onPrepareDraft={() => {
          void wizards.respondToGuest.prepareDraft()
        }}
        onRewriteDraft={(target) => {
          void wizards.respondToGuest.rewriteDraft(target)
        }}
        onRetryAiDraft={() => {
          void wizards.respondToGuest.retryAiDraft()
        }}
        onDismissPreparingOverlay={wizards.respondToGuest.dismissPreparingOverlay}
        onSubjectChange={wizards.respondToGuest.setSubject}
        onMessageChange={wizards.respondToGuest.setMessage}
        onContinueWrite={wizards.respondToGuest.continueWrite}
        onEditText={wizards.respondToGuest.editText}
        onOpenGuestPreview={wizards.respondToGuest.openGuestPreview}
        onCloseGuestPreview={wizards.respondToGuest.closeGuestPreview}
        onSendGuestPreviewTest={wizards.respondToGuest.sendGuestPreviewTest}
        onOpenSendConfirm={wizards.respondToGuest.openSendConfirm}
        onCancelSendConfirm={wizards.respondToGuest.cancelSendConfirm}
        onConfirmSend={() => {
          void wizards.respondToGuest.confirmSend()
        }}
        onKeepInProgress={() => {
          wizards.respondToGuest.keepInProgress()
        }}
        onMarkResolved={() => {
          void wizards.respondToGuest.markResolved()
        }}
      />

      <RecordInternalActionWizard
        snapshot={snapshot.recordInternalAction}
        onSaveAndExit={wizards.recordInternalAction.saveAndExit}
        onBack={() => {
          wizards.recordInternalAction.back()
        }}
        onCategoryChange={wizards.recordInternalAction.setCategory}
        onNoteChange={wizards.recordInternalAction.setNote}
        onContinueRecorder={wizards.recordInternalAction.continueRecorder}
        onOpenRecordConfirm={wizards.recordInternalAction.openRecordConfirm}
        onCancelRecordConfirm={wizards.recordInternalAction.cancelRecordConfirm}
        onConfirmRecord={() => {
          void wizards.recordInternalAction.confirmRecord()
        }}
        onKeepInProgress={() => {
          wizards.recordInternalAction.keepInProgress()
        }}
        onMarkResolved={() => {
          void wizards.recordInternalAction.markResolved()
        }}
      />

      <RespondAndRecordInternalActionWizard
        snapshot={snapshot.respondAndRecord}
        onSaveAndExit={wizards.respondAndRecord.saveAndExit}
        onBack={() => {
          wizards.respondAndRecord.back()
        }}
        onCategoryChange={wizards.respondAndRecord.setCategory}
        onNoteChange={wizards.respondAndRecord.setNote}
        onUseConfirmedActionChange={
          wizards.respondAndRecord.setUseConfirmedActionForGuestResponse
        }
        onContinueRecorder={wizards.respondAndRecord.continueRecorder}
        onEditInternalAction={wizards.respondAndRecord.editInternalAction}
        onChannelChange={wizards.respondAndRecord.setChannel}
        onPurposeChange={wizards.respondAndRecord.setPurpose}
        onToneChange={wizards.respondAndRecord.setTone}
        onIncludeNotesChange={wizards.respondAndRecord.setIncludeNotes}
        onContinueSetup={wizards.respondAndRecord.continueSetup}
        onWriteManually={wizards.respondAndRecord.writeManually}
        onPrepareDraft={() => {
          void wizards.respondAndRecord.prepareDraft()
        }}
        onRewriteDraft={(target) => {
          void wizards.respondAndRecord.rewriteDraft(target)
        }}
        onRetryAiDraft={() => {
          void wizards.respondAndRecord.retryAiDraft()
        }}
        onDismissPreparingOverlay={
          wizards.respondAndRecord.dismissPreparingOverlay
        }
        onSubjectChange={wizards.respondAndRecord.setSubject}
        onMessageChange={wizards.respondAndRecord.setMessage}
        onContinueWrite={wizards.respondAndRecord.continueWrite}
        onEditText={wizards.respondAndRecord.editText}
        onOpenGuestPreview={wizards.respondAndRecord.openGuestPreview}
        onCloseGuestPreview={wizards.respondAndRecord.closeGuestPreview}
        onSendGuestPreviewTest={wizards.respondAndRecord.sendGuestPreviewTest}
        onOpenSendConfirm={wizards.respondAndRecord.openSendConfirm}
        onCancelSendConfirm={wizards.respondAndRecord.cancelSendConfirm}
        onConfirmSend={() => {
          void wizards.respondAndRecord.confirmSend()
        }}
        onKeepInProgress={() => {
          wizards.respondAndRecord.keepInProgress()
        }}
        onMarkResolved={() => {
          void wizards.respondAndRecord.markResolved()
        }}
      />

      <RespondWithRecoveryOfferWizard
        snapshot={snapshot.respondWithRecoveryOffer}
        onSaveAndExit={wizards.respondWithRecoveryOffer.saveAndExit}
        onBack={() => {
          wizards.respondWithRecoveryOffer.back()
        }}
        onChannelChange={wizards.respondWithRecoveryOffer.setChannel}
        onToneChange={wizards.respondWithRecoveryOffer.setTone}
        onIncludeNotesChange={wizards.respondWithRecoveryOffer.setIncludeNotes}
        onContinueSetup={wizards.respondWithRecoveryOffer.continueSetup}
        onOfferTypeChange={wizards.respondWithRecoveryOffer.setOfferType}
        onDiscountPercentageChange={
          wizards.respondWithRecoveryOffer.setDiscountPercentage
        }
        onDiscountAmountChange={
          wizards.respondWithRecoveryOffer.setDiscountAmount
        }
        onFreeItemTextChange={wizards.respondWithRecoveryOffer.setFreeItemText}
        onPurchaseRequirementChange={
          wizards.respondWithRecoveryOffer.setPurchaseRequirement
        }
        onMinimumSpendChange={wizards.respondWithRecoveryOffer.setMinimumSpend}
        onAdditionalExclusionsChange={
          wizards.respondWithRecoveryOffer.setAdditionalExclusions
        }
        onReplacementItemTextChange={
          wizards.respondWithRecoveryOffer.setReplacementItemText
        }
        onOfferTitleChange={wizards.respondWithRecoveryOffer.setOfferTitle}
        onOfferDescriptionChange={
          wizards.respondWithRecoveryOffer.setOfferDescription
        }
        onOfferValidityChange={wizards.respondWithRecoveryOffer.setOfferValidity}
        onExpiryDateChange={wizards.respondWithRecoveryOffer.setExpiryDate}
        onStaffInstructionsChange={
          wizards.respondWithRecoveryOffer.setStaffInstructions
        }
        onPrepareOfferDescription={() => {
          void wizards.respondWithRecoveryOffer.prepareOfferDescription()
        }}
        onContinueOffer={wizards.respondWithRecoveryOffer.continueOffer}
        onEditOffer={wizards.respondWithRecoveryOffer.editOffer}
        onWriteManually={wizards.respondWithRecoveryOffer.writeManually}
        onPrepareDraft={() => {
          void wizards.respondWithRecoveryOffer.prepareDraft()
        }}
        onRewriteDraft={(target) => {
          void wizards.respondWithRecoveryOffer.rewriteDraft(target)
        }}
        onRetryAiDraft={() => {
          void wizards.respondWithRecoveryOffer.retryAiDraft()
        }}
        onDismissPreparingOverlay={
          wizards.respondWithRecoveryOffer.dismissPreparingOverlay
        }
        onSubjectChange={wizards.respondWithRecoveryOffer.setSubject}
        onMessageChange={wizards.respondWithRecoveryOffer.setMessage}
        onContinueWrite={wizards.respondWithRecoveryOffer.continueWrite}
        onEditText={wizards.respondWithRecoveryOffer.editText}
        onOpenGuestPreview={wizards.respondWithRecoveryOffer.openGuestPreview}
        onCloseGuestPreview={wizards.respondWithRecoveryOffer.closeGuestPreview}
        onSendGuestPreviewTest={
          wizards.respondWithRecoveryOffer.sendGuestPreviewTest
        }
        onOpenSendConfirm={wizards.respondWithRecoveryOffer.openSendConfirm}
        onCancelSendConfirm={wizards.respondWithRecoveryOffer.cancelSendConfirm}
        onConfirmSend={() => {
          void wizards.respondWithRecoveryOffer.confirmSend()
        }}
        onKeepInProgress={() => {
          wizards.respondWithRecoveryOffer.keepInProgress()
        }}
        onMarkResolved={() => {
          void wizards.respondWithRecoveryOffer.markResolved()
        }}
      />
    </>
  )
}
