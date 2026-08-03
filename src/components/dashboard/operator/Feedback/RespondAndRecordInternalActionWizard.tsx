import { SparklesIcon } from "lucide-react"
import { toast } from "sonner"
import { useEffect, type ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import { GuestPreviewPanel } from "@/components/dashboard/operator/Feedback/GuestPreviewPanel"
import { GuestResponseChooser } from "@/components/dashboard/operator/Feedback/GuestResponseChooser"
import { InternalActionCategoryToggleGroup } from "@/components/dashboard/operator/Feedback/InternalActionCategoryToggleGroup"
import { RecoveryFeedbackSummaryPanel } from "@/components/dashboard/operator/Feedback/RecoveryFeedbackSummaryPanel"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import { ResponseSetupFields } from "@/components/dashboard/operator/Feedback/ResponseSetupFields"
import type { RespondAndRecordSnapshot } from "@/lib/operatorFeedback/createRespondAndRecordInternalActionModule"
import {
  GUEST_RESPONSE_AI_ACTION_METERING_LABEL,
  GUEST_RESPONSE_STEP_DESCRIPTION,
  GUEST_RESPONSE_STEP_HEADING,
  GUEST_RESPONSE_WRITE_MANUAL_STEP_HEADING,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import {
  INTERNAL_ACTION_NOTE_HELPER,
  INTERNAL_ACTION_NOTE_PLACEHOLDER,
  INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL,
  type InternalActionCategoryId,
} from "@/lib/operatorFeedback/internalActionPresentation"
import { recoverySendConfirmCopy } from "@/lib/operatorFeedback/recoverySendConfirmPresentation"
import { RECOVERY_WIZARD_PAGE_TITLE } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import {
  RESPONSE_SETUP_STEP_DESCRIPTION,
  RESPONSE_SETUP_STEP_HEADING,
} from "@/lib/operatorFeedback/responseSetupPresentation"
import {
  type RespondToGuestChannel,
  type RespondToGuestPurposeId,
  type RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"

type RespondAndRecordWizardProps = {
  snapshot: RespondAndRecordSnapshot
  onSaveAndExit: () => void
  onBack: () => void
  onCategoryChange: (category: InternalActionCategoryId) => void
  onNoteChange: (value: string) => void
  onUseConfirmedActionChange: (value: boolean) => void
  onContinueRecorder: () => void
  onEditInternalAction: () => void
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
  { id: "recorder", label: "Internal action" },
  { id: "setup", label: "Response setup" },
  { id: "write", label: "Guest response" },
  { id: "review", label: "Review and send" },
] as const

function stepIndex(step: RespondAndRecordSnapshot["step"]): number {
  if (step === "recorder") return 1
  if (step === "setup") return 2
  if (step === "write") return 3
  if (step === "review" || step === "success") return 4
  return 1
}

function SummaryRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex w-full items-start justify-between gap-4">
      <dt className="shrink-0 text-base font-semibold text-op-text-muted">
        {label}
      </dt>
      <dd className="min-w-0 text-right text-base font-medium text-op-text-primary">
        {children}
      </dd>
    </div>
  )
}

/** Full-screen Respond and record an internal action wizard. */
export function RespondAndRecordInternalActionWizard({
  snapshot,
  onSaveAndExit,
  onBack,
  onCategoryChange,
  onNoteChange,
  onUseConfirmedActionChange,
  onContinueRecorder,
  onEditInternalAction,
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
}: RespondAndRecordWizardProps) {
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
    intent: "respond_and_record_internal_action",
    maskedDestination: snapshot.maskedDestination,
    sendStatus: snapshot.sendStatus,
  })
  const onWriteChooser =
    snapshot.step === "write" && snapshot.writeEntry === "chooser"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"

  const stepHeading = isSuccess
    ? null
    : snapshot.step === "recorder"
      ? "Record internal action"
      : snapshot.step === "setup"
        ? RESPONSE_SETUP_STEP_HEADING
        : snapshot.step === "write"
          ? onWriteChooser
            ? GUEST_RESPONSE_STEP_HEADING
            : GUEST_RESPONSE_WRITE_MANUAL_STEP_HEADING
          : "Review response and internal action"

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
      title={
        isSuccess
          ? "Response sent and internal action recorded"
          : RECOVERY_WIZARD_PAGE_TITLE
      }
      description={
        isSuccess
          ? "The guest response and internal action were recorded. Keep the Feedback in progress or mark recovery resolved."
          : (snapshot.headerSubtitle
            ?? "Prepare a guest response and record an internal action.")
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
            {snapshot.step === "recorder" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueRecorder || locked}
                onClick={onContinueRecorder}
              >
                Continue
              </Button>
            ) : null}
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
                Send response and record action
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
        <div className="flex w-full flex-col gap-10 lg:flex-row lg:items-start">
          <div className="flex w-full max-w-[690px] flex-col gap-6">
            {snapshot.step === "recorder" ? (
              <>
                <div className="flex flex-col gap-3">
                  <p className="text-sm font-medium text-op-text-primary">
                    Category
                  </p>
                  <InternalActionCategoryToggleGroup
                    value={snapshot.category}
                    onValueChange={onCategoryChange}
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="respond-and-record-note"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Internal follow-up note
                  </label>
                  <Textarea
                    id="respond-and-record-note"
                    value={snapshot.note}
                    placeholder={INTERNAL_ACTION_NOTE_PLACEHOLDER}
                    onChange={(event) => {
                      onNoteChange(event.target.value)
                    }}
                    className="min-h-[120px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                  <p className="text-xs font-medium text-op-text-muted">
                    {INTERNAL_ACTION_NOTE_HELPER}
                  </p>
                </div>

                <CheckboxLabel
                  checked={snapshot.useConfirmedActionForGuestResponse}
                  onCheckedChange={(checked) => {
                    onUseConfirmedActionChange(checked)
                  }}
                >
                  {INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL}
                </CheckboxLabel>
              </>
            ) : null}

            {snapshot.step === "setup" ? (
              <ResponseSetupFields
                idPrefix="respond-and-record"
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

            {onWriteChooser || onWriteEditor || snapshot.step === "review" ? (
              <div className="flex flex-col gap-2 rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] p-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <p className="text-xs font-medium text-op-text-muted">
                      Internal follow-up
                    </p>
                    <p className="mt-1 text-sm font-medium text-op-text-primary">
                      {snapshot.summary.categoryLabel ?? "—"}
                    </p>
                    <p className="mt-1 text-sm text-op-text-muted whitespace-pre-wrap">
                      {snapshot.note}
                    </p>
                    {snapshot.step === "review" ? (
                      <div className="mt-3 flex flex-wrap items-center gap-2">
                        <span className="text-xs font-medium text-op-text-muted">
                          Follow-up state
                        </span>
                        <Badge variant="tag">
                          {snapshot.followUpStateLabel}
                        </Badge>
                      </div>
                    ) : null}
                  </div>
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={locked}
                    onClick={onEditInternalAction}
                  >
                    Edit internal action
                  </Button>
                </div>
              </div>
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
                      htmlFor="respond-and-record-subject"
                      className="text-sm font-medium text-op-text-primary"
                    >
                      Subject
                    </label>
                    <Input
                      id="respond-and-record-subject"
                      value={snapshot.subject}
                      disabled={locked}
                      onChange={(event) => {
                        onSubjectChange(event.target.value)
                      }}
                      className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                    />
                  </div>
                ) : null}
                <div className="flex flex-col gap-2">
                  <label
                    htmlFor="respond-and-record-message"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Message
                  </label>
                  <Textarea
                    id="respond-and-record-message"
                    value={snapshot.message}
                    disabled={locked}
                    onChange={(event) => {
                      onMessageChange(event.target.value)
                    }}
                    className="min-h-[220px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                  />
                </div>
                <div className="flex flex-wrap items-center gap-3">
                  <Button
                    type="button"
                    variant="op-secondary"
                    disabled={locked}
                    onClick={onRewriteDraft}
                  >
                    <SparklesIcon className="size-4" aria-hidden />
                    Rewrite with AI
                  </Button>
                  <span className="text-xs font-medium text-op-text-muted">
                    {GUEST_RESPONSE_AI_ACTION_METERING_LABEL}
                  </span>
                  {snapshot.aiDraftStatus === "failed"
                    && snapshot.aiDraftRetryable ? (
                    <Button
                      type="button"
                      variant="op-secondary"
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

            {isSuccess ? (
              <div className="flex flex-col gap-6 rounded-[6px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
                <p className="text-sm font-medium text-op-text-primary">
                  Response sent · Internal action recorded
                </p>
                <dl className="flex flex-col gap-4">
                  <SummaryRow label="Recovery status">
                    <Badge variant="tag">{snapshot.recoveryStatusLabel}</Badge>
                  </SummaryRow>
                  <SummaryRow label="Follow-up status">
                    <Badge variant="tag">{snapshot.followUpStatusLabel}</Badge>
                  </SummaryRow>
                  <SummaryRow label="Workflow status">
                    <Badge variant="tag">{snapshot.workflowStatusLabel}</Badge>
                  </SummaryRow>
                </dl>
              </div>
            ) : null}
          </div>

          {snapshot.step === "review" ? (
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
                ...(snapshot.summary.categoryLabel != null
                  ? [
                      {
                        label: "Internal action:",
                        children: snapshot.summary.categoryLabel,
                      },
                    ]
                  : []),
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
