import { toast } from "sonner"
import { useEffect } from "react"

import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { GuestPreviewPanel } from "@/components/dashboard/operator/Feedback/GuestPreviewPanel"
import { GuestResponseChooser } from "@/components/dashboard/operator/Feedback/GuestResponseChooser"
import { GuestResponseWriteFields } from "@/components/dashboard/operator/Feedback/GuestResponseWriteFields"
import { RecoveryFeedbackSummaryPanel } from "@/components/dashboard/operator/Feedback/RecoveryFeedbackSummaryPanel"
import { RecoveryReviewSummary } from "@/components/dashboard/operator/Feedback/RecoveryReviewSummary"
import { RecoverySuccessStatusList } from "@/components/dashboard/operator/Feedback/RecoverySuccessStatusList"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import { ResponseSetupFields } from "@/components/dashboard/operator/Feedback/ResponseSetupFields"
import type {
  PrepareRecoveryDraftRewriteTarget,
  RespondToGuestSnapshot,
} from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  GUEST_RESPONSE_STEP_DESCRIPTION,
  GUEST_RESPONSE_STEP_HEADING,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import { recoverySendConfirmCopy } from "@/lib/operatorFeedback/recoverySendConfirmPresentation"
import { recoverySuccessChromeForRespondToGuest } from "@/lib/operatorFeedback/recoverySuccessPresentation"
import { RECOVERY_WIZARD_PAGE_TITLE } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import {
  RESPONSE_SETUP_STEP_DESCRIPTION,
  RESPONSE_SETUP_STEP_HEADING,
} from "@/lib/operatorFeedback/responseSetupPresentation"
import {
  REVIEW_RESPONSE_STEP_DESCRIPTION,
  REVIEW_RESPONSE_STEP_HEADING,
} from "@/lib/operatorFeedback/reviewResponsePresentation"
import {
  type RespondToGuestChannel,
  type RespondToGuestPurposeId,
  type RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"

type RespondToGuestWizardProps = {
  snapshot: RespondToGuestSnapshot
  onSaveAndExit: () => void
  onBack: () => void
  onChannelChange: (channel: RespondToGuestChannel) => void
  onPurposeChange: (purpose: RespondToGuestPurposeId) => void
  onToneChange: (tone: RespondToGuestToneId) => void
  onIncludeNotesChange: (value: string) => void
  onContinueSetup: () => void
  onWriteManually: () => void
  onPrepareDraft: () => void
  onRewriteDraft: (target: PrepareRecoveryDraftRewriteTarget) => void
  onRetryAiDraft: () => void
  onDismissPreparingOverlay: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onContinueWrite: () => void
  onEditText: () => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onOpenSendConfirm: () => void
  onCancelSendConfirm: () => void
  onConfirmSend: () => void
  onKeepInProgress: () => void
  onMarkResolved: () => void
}

const STEP_LABELS = [
  { id: "action", label: "Action" },
  { id: "setup", label: "Response setup" },
  { id: "write", label: "Guest response" },
  { id: "review", label: "Review and send" },
] as const

function stepIndex(step: RespondToGuestSnapshot["step"]): number {
  // Action (entry shell) is always complete once this wizard is open.
  if (step === "setup") return 1
  if (step === "write") return 2
  if (step === "review" || step === "success") return 3
  return 1
}

/** Full-screen Respond to the guest wizard — AI draft + manual path. */
export function RespondToGuestWizard({
  snapshot,
  onSaveAndExit,
  onBack,
  onChannelChange,
  onPurposeChange,
  onToneChange,
  onIncludeNotesChange,
  onContinueSetup,
  onWriteManually,
  onPrepareDraft,
  onRewriteDraft,
  onRetryAiDraft,
  onDismissPreparingOverlay,
  onSubjectChange,
  onMessageChange,
  onContinueWrite,
  onEditText,
  onOpenGuestPreview,
  onCloseGuestPreview,
  onOpenSendConfirm,
  onCancelSendConfirm,
  onConfirmSend,
  onKeepInProgress,
  onMarkResolved,
}: RespondToGuestWizardProps) {
  useEffect(() => {
    if (snapshot.sendStatus === "error" && snapshot.sendError != null) {
      toast.error(snapshot.sendError)
    }
  }, [snapshot.sendStatus, snapshot.sendError])

  useEffect(() => {
    if (
      snapshot.completeStatus === "error"
      && snapshot.completeError != null
    ) {
      toast.error(snapshot.completeError)
    }
  }, [snapshot.completeStatus, snapshot.completeError])

  useEffect(() => {
    if (snapshot.aiDraftStatus === "failed" && snapshot.aiDraftError != null) {
      toast.error(snapshot.aiDraftError)
    }
  }, [snapshot.aiDraftStatus, snapshot.aiDraftError])

  const activeStep = stepIndex(snapshot.step)
  const isSuccess = snapshot.step === "success"
  const sending = snapshot.sendStatus === "saving"
  const completing = snapshot.completeStatus === "saving"
  const locked = snapshot.actionsLocked
  const sendConfirm = recoverySendConfirmCopy({
    intent: "respond_to_guest",
    maskedDestination: snapshot.maskedDestination,
    sendStatus: snapshot.sendStatus,
  })
  const successChrome = isSuccess
    ? recoverySuccessChromeForRespondToGuest({
        maskedDestination:
          snapshot.successReceipt?.maskedDestination
          ?? snapshot.maskedDestination,
        channel: snapshot.successReceipt?.channel ?? snapshot.channel,
        actorDisplayName: snapshot.successReceipt?.actorDisplayName ?? null,
        sentAt: snapshot.successReceipt?.at ?? null,
      })
    : null
  const onWriteStep = snapshot.step === "write"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"

  const stepHeading = isSuccess
    ? null
    : snapshot.step === "setup"
      ? RESPONSE_SETUP_STEP_HEADING
      : snapshot.step === "write"
        ? GUEST_RESPONSE_STEP_HEADING
        : REVIEW_RESPONSE_STEP_HEADING

  const stepDescription = isSuccess
    ? null
    : snapshot.step === "setup"
      ? RESPONSE_SETUP_STEP_DESCRIPTION
      : snapshot.step === "write"
        ? GUEST_RESPONSE_STEP_DESCRIPTION
        : REVIEW_RESPONSE_STEP_DESCRIPTION

  return (
    <RecoveryWizardShell
      isOpen={snapshot.isOpen}
      onRequestClose={isSuccess ? onKeepInProgress : onSaveAndExit}
      closeDisabled={locked && !isSuccess}
      showBackButton={!isSuccess}
      onBack={onBack}
      backDisabled={locked}
      title={isSuccess ? successChrome!.title : RECOVERY_WIZARD_PAGE_TITLE}
      description={
        isSuccess
          ? successChrome!.subtitle
          : (snapshot.headerSubtitle
            ?? "Prepare and send a private response.")
      }
      descriptionSrOnly={snapshot.headerSubtitle == null && !isSuccess}
      stepHeading={stepHeading}
      stepDescription={stepDescription}
      steps={isSuccess ? null : STEP_LABELS}
      activeStepIndex={activeStep}
      isLoading={snapshot.loadStatus === "loading"}
      footerLayout={isSuccess ? "end" : "wizard"}
      onSaveAndExit={isSuccess ? undefined : onSaveAndExit}
      saveAndExitDisabled={locked}
      footer={
        !isSuccess ? (
          <>
            {snapshot.step === "setup" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueSetup || locked}
                onClick={onContinueSetup}
              >
                Continue
              </Button>
            ) : null}
            {onWriteEditor ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueWrite || locked}
                onClick={onContinueWrite}
              >
                Continue
              </Button>
            ) : null}
            {snapshot.step === "review" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={locked}
                onClick={onOpenSendConfirm}
              >
                Send response
              </Button>
            ) : null}
          </>
        ) : (
          <>
            <Button
              type="button"
              variant="op-secondary"
              disabled={completing}
              onClick={onKeepInProgress}
            >
              Keep in progress
            </Button>
            <Button
              type="button"
              variant="op-primary"
              disabled={completing}
              onClick={onMarkResolved}
            >
              {completing ? "Saving…" : "Mark resolved"}
            </Button>
          </>
        )
      }
      preparingOverlay={{
        open: snapshot.preparingOverlayOpen,
        onDismiss: onDismissPreparingOverlay,
        onWriteManually,
        subtitle: snapshot.headerSubtitle,
      }}
      confirmDialog={{
        open: snapshot.sendConfirmOpen,
        busy: sending,
        onCancel: onCancelSendConfirm,
        onConfirm: onConfirmSend,
        title: sendConfirm.title,
        description: sendConfirm.description,
        error: snapshot.sendError,
        confirmLabel: sendConfirm.confirmLabel,
        confirmBusyLabel: "Sending…",
      }}
    >
      {snapshot.loadStatus === "loaded" && snapshot.summary != null ? (
        <div className="flex flex-col gap-10 lg:flex-row lg:items-start lg:gap-[42px]">
          <div className="flex flex-1 flex-col gap-6">
            {snapshot.step === "setup" ? (
              <ResponseSetupFields
                idPrefix="respond-to-guest"
                availableChannels={snapshot.availableChannels}
                channel={snapshot.channel}
                maskedDestination={snapshot.maskedDestination}
                onChannelChange={onChannelChange}
                purpose={snapshot.purpose}
                onPurposeChange={onPurposeChange}
                tone={snapshot.tone}
                onToneChange={onToneChange}
                includeNotes={snapshot.includeNotes}
                onIncludeNotesChange={onIncludeNotesChange}
                disabled={locked}
              />
            ) : null}

            {onWriteStep ? (
              <>
                <GuestResponseChooser
                  disabled={locked}
                  aiDraftFailed={
                    snapshot.aiDraftStatus === "failed"
                    && snapshot.writeEntry === "chooser"
                  }
                  aiDraftRetryable={snapshot.aiDraftRetryable}
                  onPrepareDraft={onPrepareDraft}
                  onWriteManually={onWriteManually}
                  onRetryAiDraft={onRetryAiDraft}
                />
                {onWriteEditor ? (
                  <>
                    <Separator className="bg-op-card-border" />
                    <GuestResponseWriteFields
                      idPrefix="respond"
                      channel={snapshot.channel}
                      subject={snapshot.subject}
                      message={snapshot.message}
                      disabled={locked}
                      aiDraftStatus={snapshot.aiDraftStatus}
                      aiDraftMode={snapshot.aiDraftMode}
                      aiDraftRetryable={snapshot.aiDraftRetryable}
                      onSubjectChange={onSubjectChange}
                      onMessageChange={onMessageChange}
                      onRewriteSubject={() => {
                        onRewriteDraft("subject")
                      }}
                      onRewriteMessage={() => {
                        onRewriteDraft("message")
                      }}
                      onRetryAiDraft={onRetryAiDraft}
                    />
                  </>
                ) : null}
              </>
            ) : null}

            {snapshot.step === "review" ? (
              <RecoveryReviewSummary
                idPrefix="respond-to-guest-review"
                guestName={snapshot.summary.guestName}
                channel={snapshot.channel}
                maskedDestination={snapshot.maskedDestination}
                feedbackComment={snapshot.summary.feedbackComment}
                feedbackId={snapshot.feedbackId}
                issueTagLabels={snapshot.summary.issueTagLabels}
                subject={snapshot.subject}
                message={snapshot.message}
                aiActionCount={snapshot.aiActionCount}
              />
            ) : null}

            {isSuccess && successChrome != null ? (
              <RecoverySuccessStatusList rows={successChrome.rows} />
            ) : null}
          </div>

          {isSuccess ? null : snapshot.step === "review" ? (
            <GuestPreviewPanel
              channel={snapshot.channel}
              subject={snapshot.subject}
              message={snapshot.message}
              locationName={snapshot.locationName}
              locationAddress={snapshot.locationAddress}
              disabled={locked}
              guestPreviewOpen={snapshot.guestPreviewOpen}
              onOpenPreview={onOpenGuestPreview}
              onClosePreview={onCloseGuestPreview}
              onEditText={onEditText}
            />
          ) : (
            <RecoveryFeedbackSummaryPanel
              guestName={snapshot.summary.guestName}
              classificationStatus={snapshot.summary.classificationStatus}
              classificationSentiment={
                snapshot.summary.classificationSentiment
              }
              contactLabel={snapshot.summary.contactLabel}
              feedbackComment={snapshot.summary.feedbackComment}
              issueTagLabels={snapshot.summary.issueTagLabels}
              extraRows={[
                ...(snapshot.summary.purposeLabel != null
                  ? [
                      {
                        label: "Purpose:",
                        children: snapshot.summary.purposeLabel,
                      },
                    ]
                  : []),
                ...(snapshot.summary.toneLabel != null
                  ? [
                      {
                        label: "Tone:",
                        children: snapshot.summary.toneLabel,
                      },
                    ]
                  : []),
              ]}
            />
          )}
        </div>
      ) : null}
    </RecoveryWizardShell>
  )
}
