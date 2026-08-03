import { toast } from "sonner"
import { useEffect, type ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { InternalActionCategoryToggleGroup } from "@/components/dashboard/operator/Feedback/InternalActionCategoryToggleGroup"
import { RecoveryWizardShell } from "@/components/dashboard/operator/Feedback/RecoveryWizardShell"
import type { RecordInternalActionSnapshot } from "@/lib/operatorFeedback/createRecordInternalActionModule"
import {
  INTERNAL_ACTION_NOTE_HELPER,
  INTERNAL_ACTION_NOTE_PLACEHOLDER,
  RECORD_INTERNAL_ONLY_REVIEW_PRIMARY_CTA,
  type InternalActionCategoryId,
} from "@/lib/operatorFeedback/internalActionPresentation"
import { recoverySendConfirmCopy } from "@/lib/operatorFeedback/recoverySendConfirmPresentation"
import { RECOVERY_WIZARD_PAGE_TITLE } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"

type RecordInternalActionWizardProps = {
  snapshot: RecordInternalActionSnapshot
  onSaveAndExit: () => void
  onBack: () => void
  onCategoryChange: (category: InternalActionCategoryId) => void
  onNoteChange: (value: string) => void
  onContinueRecorder: () => void
  onOpenRecordConfirm: () => void
  onCancelRecordConfirm: () => void
  onConfirmRecord: () => void
  onKeepInProgress: () => void
  onMarkResolved: () => void
}

const STEP_LABELS = [
  { id: "action", label: "Action" },
  { id: "recorder", label: "Internal action" },
  { id: "review", label: "Review and record" },
] as const

function stepIndex(step: RecordInternalActionSnapshot["step"]): number {
  if (step === "recorder") return 1
  if (step === "review" || step === "success") return 2
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

/** Full-screen Record an internal action only wizard. */
export function RecordInternalActionWizard({
  snapshot,
  onSaveAndExit,
  onBack,
  onCategoryChange,
  onNoteChange,
  onContinueRecorder,
  onOpenRecordConfirm,
  onCancelRecordConfirm,
  onConfirmRecord,
  onKeepInProgress,
  onMarkResolved,
}: RecordInternalActionWizardProps) {
  useEffect(() => {
    if (snapshot.recordStatus === "error" && snapshot.recordError != null) {
      toast.error(snapshot.recordError)
    }
  }, [snapshot.recordStatus, snapshot.recordError])

  useEffect(() => {
    if (
      snapshot.completeStatus === "error"
      && snapshot.completeError != null
    ) {
      toast.error(snapshot.completeError)
    }
  }, [snapshot.completeStatus, snapshot.completeError])

  const activeStep = stepIndex(snapshot.step)
  const isSuccess = snapshot.step === "success"
  const recording = snapshot.recordStatus === "saving"
  const completing = snapshot.completeStatus === "saving"
  const recordConfirm = recoverySendConfirmCopy({
    intent: "record_internal_action_only",
    maskedDestination: null,
    sendStatus: snapshot.recordStatus,
  })

  const stepHeading = isSuccess
    ? null
    : snapshot.step === "recorder"
      ? "Record internal action"
      : "Review internal follow-up"

  return (
    <RecoveryWizardShell
      isOpen={snapshot.isOpen}
      onRequestClose={isSuccess ? onKeepInProgress : onSaveAndExit}
      showBackButton={!isSuccess}
      onBack={onBack}
      title={
        isSuccess ? "Internal follow-up recorded" : RECOVERY_WIZARD_PAGE_TITLE
      }
      description={
        isSuccess
          ? "The internal follow-up was recorded. Keep the Feedback in progress or mark recovery resolved."
          : (snapshot.headerSubtitle
            ?? "Document what the restaurant reviewed or changed.")
      }
      descriptionSrOnly={snapshot.headerSubtitle == null && !isSuccess}
      stepHeading={stepHeading}
      steps={isSuccess ? null : STEP_LABELS}
      activeStepIndex={activeStep}
      isLoading={snapshot.loadStatus === "loading"}
      footerLayout={isSuccess ? "end" : "wizard"}
      onSaveAndExit={isSuccess ? undefined : onSaveAndExit}
      footer={
        !isSuccess ? (
          <>
            {snapshot.step === "recorder" ? (
              <Button
                type="button"
                variant="op-primary"
                disabled={!snapshot.canContinueRecorder}
                onClick={onContinueRecorder}
              >
                Continue
              </Button>
            ) : null}
            {snapshot.step === "review" ? (
              <Button
                type="button"
                variant="op-primary"
                onClick={onOpenRecordConfirm}
              >
                {RECORD_INTERNAL_ONLY_REVIEW_PRIMARY_CTA}
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
      confirmDialog={{
        open: snapshot.recordConfirmOpen,
        busy: recording,
        onCancel: onCancelRecordConfirm,
        onConfirm: onConfirmRecord,
        title: recordConfirm.title,
        description: recordConfirm.description,
        error: snapshot.recordError,
        confirmLabel: recordConfirm.confirmLabel,
        confirmBusyLabel: "Recording…",
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
                    htmlFor="internal-action-note"
                    className="text-sm font-medium text-op-text-primary"
                  >
                    Internal follow-up note
                  </label>
                  <Textarea
                    id="internal-action-note"
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
              </>
            ) : null}

            {snapshot.step === "review" ? (
              <div className="flex flex-col gap-6">
                <p className="rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] px-4 py-3 text-sm font-medium text-op-text-muted">
                  This will not contact the guest. The record is visible only
                  to authorised restaurant users.
                </p>
                <dl className="flex flex-col gap-4 rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
                  <SummaryRow label="Feedback reference">
                    {snapshot.headerSubtitle?.split(" · ")[0]
                      ?? `FDB-${String(snapshot.feedbackId ?? 0).padStart(4, "0")}`}
                  </SummaryRow>
                  <SummaryRow label="Location">
                    {snapshot.summary.locationName}
                  </SummaryRow>
                  <SummaryRow label="Classification">
                    {snapshot.summary.classificationLabel}
                  </SummaryRow>
                  <SummaryRow label="Action">
                    {snapshot.summary.categoryLabel ?? "—"}
                  </SummaryRow>
                  <SummaryRow label="Internal follow-up note">
                    {snapshot.note}
                  </SummaryRow>
                  <SummaryRow label="Follow-up state">
                    <Badge variant="tag">{snapshot.followUpStateLabel}</Badge>
                  </SummaryRow>
                </dl>
              </div>
            ) : null}

            {isSuccess ? (
              <div className="flex w-full max-w-[600px] flex-col gap-6">
                <dl className="flex flex-col gap-6">
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

          <aside className="w-full max-w-[360px] shrink-0 rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
            <h2 className="text-base font-semibold text-op-text-primary">
              Feedback summary
            </h2>
            <dl className="mt-4 flex flex-col gap-3">
              <SummaryRow label="Guest">
                {snapshot.summary.guestName}
              </SummaryRow>
              <SummaryRow label="Classification">
                {snapshot.summary.classificationLabel}
              </SummaryRow>
              <SummaryRow label="Feedback">
                <span className="line-clamp-4 whitespace-pre-wrap">
                  {snapshot.summary.feedbackComment}
                </span>
              </SummaryRow>
            </dl>
          </aside>
        </div>
      ) : null}
    </RecoveryWizardShell>
  )
}
