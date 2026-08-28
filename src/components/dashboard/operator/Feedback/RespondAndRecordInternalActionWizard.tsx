import { toast } from "sonner"
import { useEffect } from "react"
import { Link } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import { GuestPreviewPanel } from "@/components/dashboard/operator/Feedback/GuestPreviewPanel"
import { CampaignSendTestEmailDialog } from "@/components/dashboard/operator/Campaigns/CampaignSendTestEmailDialog"
import { GuestResponseChooser } from "@/components/dashboard/operator/Feedback/GuestResponseChooser"
import { GuestResponseWriteFields } from "@/components/dashboard/operator/Feedback/GuestResponseWriteFields"
import { InternalActionCategoryToggleGroup } from "@/components/dashboard/operator/Feedback/InternalActionCategoryToggleGroup"
import { RecoveryFeedbackSummaryPanel } from "@/components/dashboard/operator/Feedback/RecoveryFeedbackSummaryPanel"
import { RecoveryReviewSummary } from "@/components/dashboard/operator/Feedback/RecoveryReviewSummary"
import { RecoverySuccessStatusList } from "@/components/dashboard/operator/Feedback/RecoverySuccessStatusList"
import { OperatorWizardShell } from "@/components/dashboard/operator/OperatorWizardShell"
import { ResponseSetupFields } from "@/components/dashboard/operator/Feedback/ResponseSetupFields"
import type { RespondAndRecordSnapshot } from "@/lib/operatorFeedback/createRespondAndRecordInternalActionModule"
import type { PrepareRecoveryDraftRewriteTarget } from "@/lib/operatorFeedback/createRespondToGuestModule"
import { GUEST_PREVIEW_SEND_TEST_SUCCESS } from "@/lib/operatorFeedback/guestPreviewPresentation"
import {
  GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
  GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
  GUEST_RESPONSE_STEP_DESCRIPTION,
  GUEST_RESPONSE_STEP_HEADING,
} from "@/lib/operatorFeedback/guestResponseChooserPresentation"
import {
  INTERNAL_ACTION_NOTE_HELPER,
  INTERNAL_ACTION_NOTE_PLACEHOLDER,
  INTERNAL_ACTION_RECORDER_STEP_DESCRIPTION,
  INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL,
  type InternalActionCategoryId,
} from "@/lib/operatorFeedback/internalActionPresentation"
import { recoverySendConfirmCopy } from "@/lib/operatorFeedback/recoverySendConfirmPresentation"
import { recoverySuccessChromeForRespondAndRecord } from "@/lib/operatorFeedback/recoverySuccessPresentation"
import { RECOVERY_WIZARD_PAGE_TITLE } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import {
  FEEDBACK_FIELD_LABEL_CLASS,
  FEEDBACK_TEXTAREA_CLASS,
} from "@/lib/operatorFeedback/feedbackPresentation"
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
import { cn } from "@/lib/utils"

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
  onRewriteDraft: (target: PrepareRecoveryDraftRewriteTarget) => void
  onRetryAiDraft: () => void
  onDismissPreparingOverlay: () => void
  onSubjectChange: (value: string) => void
  onMessageChange: (value: string) => void
  onContinueWrite: () => void
  onEditText: () => void
  onOpenGuestPreview: () => void
  onCloseGuestPreview: () => void
  onOpenSendTest: () => void
  onCloseSendTest: () => void
  onSendTestEmailChange: (value: string) => void
  onConfirmSendTest: () => void
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
  onOpenGuestPreview,
  onCloseGuestPreview,
  onOpenSendTest,
  onCloseSendTest,
  onSendTestEmailChange,
  onConfirmSendTest,
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

  useEffect(() => {
    if (snapshot.sendTestStatus === "error" && snapshot.sendTestError != null) {
      toast.error(snapshot.sendTestError)
    }
    if (snapshot.sendTestStatus === "success") {
      toast.success(GUEST_PREVIEW_SEND_TEST_SUCCESS)
    }
  }, [snapshot.sendTestStatus, snapshot.sendTestError])

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
  const successChrome = isSuccess
    ? recoverySuccessChromeForRespondAndRecord({
        channel: snapshot.successReceipt?.channel ?? snapshot.channel,
      })
    : null
  const onWriteStep = snapshot.step === "write"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"

  const stepHeading = isSuccess
    ? null
    : snapshot.step === "recorder"
      ? "Record internal action"
      : snapshot.step === "setup"
        ? RESPONSE_SETUP_STEP_HEADING
        : snapshot.step === "write"
          ? GUEST_RESPONSE_STEP_HEADING
          : REVIEW_RESPONSE_STEP_HEADING

  const stepDescription = isSuccess
    ? null
    : snapshot.step === "recorder"
      ? INTERNAL_ACTION_RECORDER_STEP_DESCRIPTION
      : snapshot.step === "setup"
        ? RESPONSE_SETUP_STEP_DESCRIPTION
        : snapshot.step === "write"
          ? GUEST_RESPONSE_STEP_DESCRIPTION
          : REVIEW_RESPONSE_STEP_DESCRIPTION

  return (
    <>
    <OperatorWizardShell
      isOpen={snapshot.isOpen}
      onRequestClose={isSuccess ? onKeepInProgress : onSaveAndExit}
      closeDisabled={locked && !isSuccess}
      showBackButton={!isSuccess}
      onBack={onBack}
      backDisabled={locked}
      title={
        isSuccess
          ? successChrome!.title
          : RECOVERY_WIZARD_PAGE_TITLE
      }
      description={
        isSuccess
          ? successChrome!.subtitle
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
              <div className="flex flex-wrap items-center gap-3">
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={locked || snapshot.sendBlocked}
                  onClick={onOpenSendConfirm}
                >
                  Send response and record action
                </Button>
                {snapshot.paidWrite.helperCta != null ? (
                  <Button
                    type="button"
                    variant="op-link"
                    className="h-auto min-h-0 w-fit p-0"
                    asChild
                  >
                    <Link to={snapshot.paidWrite.helperCta.href}>
                      {snapshot.paidWrite.helperCta.label}
                    </Link>
                  </Button>
                ) : null}
              </div>
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
        title: GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
        description: GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
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
          <div
            className={cn(
              "flex flex-1 flex-col",
              snapshot.step === "recorder" ? "gap-7" : "gap-6"
            )}
          >
            {snapshot.step === "recorder" ? (
              <>
                <InternalActionCategoryToggleGroup
                  value={snapshot.category}
                  onValueChange={onCategoryChange}
                />

                <Separator className="bg-op-card-border" />

                <div className="flex flex-col gap-3">
                  <div className="flex flex-col gap-2">
                    <label
                      htmlFor="respond-and-record-note"
                      className={FEEDBACK_FIELD_LABEL_CLASS}
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
                      className={cn(FEEDBACK_TEXTAREA_CLASS, "min-h-[120px]")}
                    />
                  </div>
                  <p className="text-sm font-medium text-[var(--op-color-gray-550)]">
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
                messageBody={snapshot.message}
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

            {onWriteStep || snapshot.step === "review" ? (
              <div className="flex flex-col gap-2 rounded-[4px] border border-op-card-border bg-op-background-secondary p-4">
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

            {onWriteStep ? (
              <>
                <GuestResponseChooser
                  disabled={locked}
                  aiDraftFailed={
                    snapshot.aiDraftStatus === "failed"
                    && snapshot.writeEntry === "chooser"
                  }
                  aiDraftRetryable={snapshot.aiDraftRetryable}
                  aiActionChip={snapshot.aiActionChip}
                  onPrepareDraft={onPrepareDraft}
                  onWriteManually={onWriteManually}
                  onRetryAiDraft={onRetryAiDraft}
                />
                {onWriteEditor ? (
                  <>
                    <Separator className="bg-op-card-border" />
                    <GuestResponseWriteFields
                      idPrefix="respond-and-record"
                      channel={snapshot.channel}
                      subject={snapshot.subject}
                      message={snapshot.message}
                      disabled={locked}
                      rewriteDisabled={!snapshot.aiActionChip.prepareAllowed}
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

            {snapshot.step === "review" && snapshot.smsShortfall.blocked ? (
              <div className="flex w-full flex-col gap-3 rounded-[4px] bg-[var(--op-color-gray-995)] p-[18px]">
                <p className="m-0 text-sm font-medium text-op-text-primary">
                  More SMS credits are required.
                </p>
                <div className="flex flex-wrap items-center gap-3">
                  {snapshot.smsShortfall.buyCta != null ? (
                    <Button
                      type="button"
                      variant="op-link"
                      className="h-auto min-h-0 w-fit p-0"
                      asChild
                    >
                      <Link to={snapshot.smsShortfall.buyCta.href}>
                        {snapshot.smsShortfall.buyCta.label}
                      </Link>
                    </Button>
                  ) : null}
                  {snapshot.smsShortfall.changePlanCta != null ? (
                    <Button
                      type="button"
                      variant="op-link"
                      className="h-auto min-h-0 w-fit p-0"
                      asChild
                    >
                      <Link to={snapshot.smsShortfall.changePlanCta.href}>
                        {snapshot.smsShortfall.changePlanCta.label}
                      </Link>
                    </Button>
                  ) : null}
                </div>
              </div>
            ) : null}

            {snapshot.step === "review" ? (
              <RecoveryReviewSummary
                idPrefix="respond-and-record-review"
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
              onSendTest={onOpenSendTest}
              sendTestBusy={snapshot.sendTestStatus === "sending"}
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
                ...(snapshot.step !== "recorder"
                  && snapshot.summary.categoryLabel != null
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
    </OperatorWizardShell>
      {snapshot.sendTest != null ? (
        <CampaignSendTestEmailDialog
          sendTest={snapshot.sendTest}
          onOpenChange={(open) => {
            if (!open) {
              onCloseSendTest()
            }
          }}
          onEmailChange={onSendTestEmailChange}
          onConfirm={onConfirmSendTest}
        />
      ) : null}
    </>
  )
}
