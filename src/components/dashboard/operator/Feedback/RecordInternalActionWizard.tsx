import { ArrowLeftIcon, CheckIcon, XIcon } from "lucide-react"
import { toast } from "sonner"
import { useEffect, type ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Textarea } from "@/components/ui/textarea"
import type { RecordInternalActionSnapshot } from "@/lib/operatorFeedback/createRecordInternalActionModule"
import {
  INTERNAL_ACTION_CATEGORY_OPTIONS,
  INTERNAL_ACTION_NOTE_HELPER,
  INTERNAL_ACTION_NOTE_PLACEHOLDER,
  type InternalActionCategoryId,
} from "@/lib/operatorFeedback/internalActionPresentation"
import { cn } from "@/lib/utils"

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

  const title = isSuccess
    ? "Internal follow-up recorded"
    : snapshot.step === "recorder"
      ? "Record internal action"
      : "Review internal follow-up"

  return (
    <>
      <Dialog
        open={snapshot.isOpen}
        onOpenChange={(open) => {
          if (!open) {
            if (isSuccess) {
              onKeepInProgress()
            } else {
              onSaveAndExit()
            }
          }
        }}
      >
        <DialogContent
          showCloseButton={false}
          className={cn(
            "fixed inset-0 top-0 left-0 z-[130] flex h-dvh max-h-dvh w-screen max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-hidden rounded-none border-0 bg-op-surface-secondary p-0 text-op-text-primary shadow-none sm:max-w-none",
            "data-open:zoom-in-100 data-closed:zoom-out-100"
          )}
        >
          <div className="flex w-full shrink-0 items-center justify-between gap-3 p-6">
            {!isSuccess ? (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Back"
                className="rounded-[2px] bg-op-button-collapse-background text-op-text-primary hover:bg-op-button-collapse-hover hover:opacity-100"
                onClick={onBack}
              >
                <ArrowLeftIcon className="size-[18px]" aria-hidden />
              </Button>
            ) : (
              <span className="size-9" />
            )}
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="Close"
              className="rounded-[2px] bg-op-button-collapse-background text-op-text-primary hover:bg-op-button-collapse-hover hover:opacity-100"
              onClick={() => {
                if (isSuccess) {
                  onKeepInProgress()
                } else {
                  onSaveAndExit()
                }
              }}
            >
              <XIcon className="size-[18px]" aria-hidden />
            </Button>
          </div>

          <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-t-[20px] border-t border-op-card-border bg-[var(--op-color-gray-995)]">
            <div className="mx-auto flex min-h-0 w-full max-w-[1440px] flex-1 flex-col overflow-y-auto px-6 pb-28 pt-[40px] md:px-[100px] xl:px-[200px]">
              {!isSuccess ? (
                <ol className="mb-8 flex flex-wrap gap-4">
                  {STEP_LABELS.map((step, index) => {
                    const done = index < activeStep
                    const current = index === activeStep
                    return (
                      <li
                        key={step.id}
                        className={cn(
                          "flex items-center gap-2 text-sm font-medium",
                          current || done
                            ? "text-op-text-primary"
                            : "text-op-text-muted"
                        )}
                      >
                        <span
                          className={cn(
                            "flex size-6 items-center justify-center rounded-full text-xs",
                            current || done
                              ? "bg-op-text-primary text-op-surface-secondary"
                              : "bg-op-card-border text-op-text-muted"
                          )}
                        >
                          {done ? (
                            <CheckIcon className="size-3.5" aria-hidden />
                          ) : (
                            index + 1
                          )}
                        </span>
                        {step.label}
                      </li>
                    )
                  })}
                </ol>
              ) : null}

              <DialogTitle className="text-[32px] font-bold leading-normal tracking-normal text-op-text-primary">
                {title}
              </DialogTitle>
              <DialogDescription
                className={cn(
                  "mt-2 max-w-[520px] text-sm font-medium leading-5 text-op-text-muted",
                  snapshot.headerSubtitle == null && !isSuccess && "sr-only"
                )}
              >
                {isSuccess
                  ? "The internal follow-up was recorded. Keep the Feedback in progress or mark recovery resolved."
                  : (snapshot.headerSubtitle
                    ?? "Document what the restaurant reviewed or changed.")}
              </DialogDescription>

              {snapshot.loadStatus === "loading" ? (
                <p className="mt-12 text-sm text-op-text-muted">Loading…</p>
              ) : null}

              {snapshot.loadStatus === "loaded" && snapshot.summary != null ? (
                <div className="mt-10 flex w-full flex-col gap-10 lg:flex-row lg:items-start">
                  <div className="flex w-full max-w-[690px] flex-col gap-6">
                    {snapshot.step === "recorder" ? (
                      <>
                        <div className="flex flex-col gap-3">
                          <p className="text-sm font-medium text-op-text-primary">
                            Category
                          </p>
                          <div className="grid gap-2 sm:grid-cols-2">
                            {INTERNAL_ACTION_CATEGORY_OPTIONS.map((option) => {
                              const selected = snapshot.category === option.id
                              return (
                                <Button
                                  key={option.id}
                                  type="button"
                                  variant="op-secondary"
                                  className={cn(
                                    "h-auto justify-start rounded-[4px] px-4 py-3 text-left text-sm font-medium whitespace-normal",
                                    selected
                                      && "border-[var(--op-color-gray-500)] ring-1 ring-[var(--op-color-gray-500)]"
                                  )}
                                  onClick={() => {
                                    onCategoryChange(option.id)
                                  }}
                                >
                                  {option.label}
                                </Button>
                              )
                            })}
                          </div>
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
                          This will not contact the guest. The record is visible
                          only to authorised restaurant users.
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
                            <Badge
                              variant="secondary"
                              className="rounded-[2px] font-medium"
                            >
                              {snapshot.followUpStateLabel}
                            </Badge>
                          </SummaryRow>
                        </dl>
                      </div>
                    ) : null}

                    {isSuccess ? (
                      <div className="flex flex-col gap-4">
                        <p className="text-base font-medium text-op-text-muted">
                          Recovery status
                        </p>
                        <Badge
                          variant="secondary"
                          className="w-fit rounded-[2px] font-medium"
                        >
                          {snapshot.recoveryStatusLabel}
                        </Badge>
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
            </div>

            <div className="shrink-0 border-t border-op-card-border bg-[var(--op-color-gray-995)] px-6 py-4 md:px-[100px] xl:px-[200px]">
              <div className="mx-auto flex w-full max-w-[1440px] flex-wrap items-center justify-between gap-3">
                {!isSuccess ? (
                  <>
                    <Button
                      type="button"
                      variant="op-secondary"
                      onClick={onSaveAndExit}
                    >
                      Save and exit
                    </Button>
                    {snapshot.step === "recorder" ? (
                      <Button
                        type="button"
                        disabled={!snapshot.canContinueRecorder}
                        onClick={onContinueRecorder}
                      >
                        Continue
                      </Button>
                    ) : null}
                    {snapshot.step === "review" ? (
                      <Button type="button" onClick={onOpenRecordConfirm}>
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
                      disabled={completing}
                      onClick={onMarkResolved}
                    >
                      {completing ? "Saving…" : "Mark resolved"}
                    </Button>
                  </>
                )}
              </div>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      <Dialog
        open={snapshot.recordConfirmOpen}
        onOpenChange={(open) => {
          if (!open && !recording) {
            onCancelRecordConfirm()
          }
        }}
      >
        <DialogContent className="z-[140] max-w-md border-op-card-border bg-[var(--op-color-gray-995)] text-op-text-primary">
          <DialogHeader>
            <DialogTitle>Record internal follow up?</DialogTitle>
            <DialogDescription className="text-op-text-muted">
              This will record the internal follow-up against this feedback.
            </DialogDescription>
          </DialogHeader>
          {snapshot.recordError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {snapshot.recordError}
            </p>
          ) : null}
          <DialogFooter className="gap-2 sm:justify-end">
            <Button
              type="button"
              variant="op-secondary"
              disabled={recording}
              onClick={onCancelRecordConfirm}
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={recording}
              onClick={onConfirmRecord}
            >
              {recording ? "Recording…" : "Send and record"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
