import { toast } from "sonner"
import { useEffect } from "react"

import { AiAssistantIcon } from "@/components/ui/ai-assistant-icon"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import { GuestPreviewPanel } from "@/components/dashboard/operator/Feedback/GuestPreviewPanel"
import { GuestResponseChooser } from "@/components/dashboard/operator/Feedback/GuestResponseChooser"
import { RecoveryFeedbackSummaryPanel } from "@/components/dashboard/operator/Feedback/RecoveryFeedbackSummaryPanel"
import { RecoverySuccessStatusList } from "@/components/dashboard/operator/Feedback/RecoverySuccessStatusList"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import { ResponseSetupFields } from "@/components/dashboard/operator/Feedback/ResponseSetupFields"
import type { RespondToGuestSnapshot } from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  GUEST_RESPONSE_AI_ACTION_METERING_LABEL,
  GUEST_RESPONSE_STEP_DESCRIPTION,
  GUEST_RESPONSE_STEP_HEADING,
  GUEST_RESPONSE_WRITE_MANUAL_STEP_HEADING,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import { recoverySendConfirmCopy } from "@/lib/operatorFeedback/recoverySendConfirmPresentation"
import { recoverySuccessChromeForRespondToGuest } from "@/lib/operatorFeedback/recoverySuccessPresentation"
import { RECOVERY_WIZARD_PAGE_TITLE } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import {
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_INPUT_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
import {
  RESPONSE_SETUP_STEP_DESCRIPTION,
  RESPONSE_SETUP_STEP_HEADING,
} from "@/lib/operatorFeedback/responseSetupPresentation"
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
  onRewriteDraft: () => void
  onRetryAiDraft: () => void
  onDismissPreparingOverlay: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onContinueWrite: () => void
  onEditText: () => void
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
  const onWriteChooser =
    snapshot.step === "write" && snapshot.writeEntry === "chooser"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"

  const stepHeading = isSuccess
    ? null
    : snapshot.step === "setup"
      ? RESPONSE_SETUP_STEP_HEADING
      : snapshot.step === "write"
        ? onWriteChooser
          ? GUEST_RESPONSE_STEP_HEADING
          : GUEST_RESPONSE_WRITE_MANUAL_STEP_HEADING
        : "Review and send"

  const stepDescription = isSuccess
    ? null
    : snapshot.step === "setup"
      ? RESPONSE_SETUP_STEP_DESCRIPTION
      : onWriteChooser
        ? GUEST_RESPONSE_STEP_DESCRIPTION
        : null

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

            {onWriteChooser ? (
              <GuestResponseChooser
                disabled={locked}
                aiDraftFailed={snapshot.aiDraftStatus === "failed"}
                aiDraftRetryable={snapshot.aiDraftRetryable}
                onPrepareDraft={onPrepareDraft}
                onWriteManually={onWriteManually}
                onRetryAiDraft={onRetryAiDraft}
              />
            ) : null}

            {onWriteEditor ? (
              <>
                {snapshot.channel === "email" ? (
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="respond-subject"
                      className={FEEDBACK_FIELD_LABEL_CLASS}
                    >
                      Subject
                    </label>
                    <Input
                      id="respond-subject"
                      value={snapshot.subject}
                      disabled={locked}
                      onChange={(event) => {
                        onSubjectChange(event.target.value)
                      }}
                      className={`${FEEDBACK_INPUT_CLASS} h-12`}
                    />
                  </div>
                ) : null}
                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="respond-message"
                    className={FEEDBACK_FIELD_LABEL_CLASS}
                  >
                    Message
                  </label>
                  <Textarea
                    id="respond-message"
                    value={snapshot.message}
                    disabled={locked}
                    onChange={(event) => {
                      onMessageChange(event.target.value)
                    }}
                    className={`${FEEDBACK_TEXTAREA_CLASS} min-h-[220px]`}
                  />
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={locked}
                    onClick={onRewriteDraft}
                  >
                    <AiAssistantIcon size={18} />
                    Rewrite with AI
                  </Button>
                  <span className="text-xs font-medium text-op-text-muted">
                    {GUEST_RESPONSE_AI_ACTION_METERING_LABEL}
                  </span>
                  {snapshot.aiDraftStatus === "failed"
                    && snapshot.aiDraftRetryable ? (
                    <Button
                      type="button"
                      variant="op-primary"
                      disabled={locked}
                      onClick={onRetryAiDraft}
                    >
                      Try again
                    </Button>
                  ) : null}
                </div>
              </>
            ) : null}

            {snapshot.step === "review" ? (
              <div className="flex flex-col gap-4 rounded-[6px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
                {snapshot.channel === "email" ? (
                  <>
                    <div>
                      <p className="text-xs font-medium text-op-text-muted">
                        Subject
                      </p>
                      <p className="mt-1 text-sm font-medium text-op-text-primary">
                        {snapshot.subject}
                      </p>
                    </div>
                    <Separator className="bg-op-card-border" />
                  </>
                ) : null}
                <div>
                  <p className="text-xs font-medium text-op-text-muted">
                    Message
                  </p>
                  <p className="mt-1 whitespace-pre-wrap text-sm font-medium text-op-text-primary">
                    {snapshot.message}
                  </p>
                </div>
                <Separator className="bg-op-card-border" />
                <div>
                  <p className="text-xs font-medium text-op-text-muted">
                    Destination
                  </p>
                  <p className="mt-1 text-sm font-medium text-op-text-primary">
                    {snapshot.channel === "sms" ? "SMS" : "Email"}
                    {snapshot.maskedDestination != null
                      ? ` · ${snapshot.maskedDestination}`
                      : null}
                  </p>
                </div>
              </div>
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
              disabled={locked}
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
