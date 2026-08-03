import { ArrowLeftIcon, CheckIcon, Loader2Icon, SparklesIcon, XIcon } from "lucide-react"
import { toast } from "sonner"
import { useEffect, type ReactNode } from "react"

import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import { Textarea } from "@/components/ui/textarea"
import type { RespondToGuestSnapshot } from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  RESPOND_TO_GUEST_PURPOSE_OPTIONS,
  RESPOND_TO_GUEST_TONE_OPTIONS,
  type RespondToGuestChannel,
  type RespondToGuestPurposeId,
  type RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import { cn } from "@/lib/utils"

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
  const onWriteChooser =
    snapshot.step === "write" && snapshot.writeEntry === "chooser"
  const onWriteEditor =
    snapshot.step === "write" && snapshot.writeEntry === "editor"

  const title = isSuccess
    ? "Response sent"
    : snapshot.step === "setup"
      ? "Response setup"
      : snapshot.step === "write"
        ? onWriteChooser
          ? "Guest response"
          : "Write response manually"
        : "Review and send"

  return (
    <>
      <Dialog
        open={snapshot.isOpen}
        onOpenChange={(open) => {
          if (!open) {
            if (locked) {
              return
            }
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
                disabled={locked}
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
              disabled={locked && !isSuccess}
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
                          current
                            ? "text-op-text-primary"
                            : done
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
                  ? "Your response was recorded. Keep the Feedback in progress or mark recovery resolved."
                  : (snapshot.headerSubtitle
                    ?? "Prepare and send a private response.")}
              </DialogDescription>

              {snapshot.loadStatus === "loading" ? (
                <p className="mt-12 text-sm text-op-text-muted">Loading…</p>
              ) : null}

              {snapshot.loadStatus === "loaded" && snapshot.summary != null ? (
                <div className="mt-10 flex w-full flex-col gap-10 lg:flex-row lg:items-start">
                  <div className="flex w-full max-w-[690px] flex-col gap-6">
                    {snapshot.step === "setup" ? (
                      <>
                        {snapshot.availableChannels.length > 1 ? (
                          <FloatingLabelSelect
                            label="Channel"
                            options={snapshot.availableChannels.map(
                              (channel) => ({
                                value: channel,
                                label: channel === "email" ? "Email" : "SMS",
                              })
                            )}
                            value={snapshot.channel ?? undefined}
                            onValueChange={(value) => {
                              onChannelChange(value as RespondToGuestChannel)
                            }}
                            disableFocusRing
                            contentClassName="z-[140]"
                          />
                        ) : (
                          <div className="rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] px-4 py-3">
                            <p className="text-xs font-medium text-op-text-muted">
                              Channel
                            </p>
                            <p className="mt-1 text-sm font-medium text-op-text-primary">
                              {snapshot.channel === "sms" ? "SMS" : "Email"}
                              {snapshot.maskedDestination != null
                                ? ` · ${snapshot.maskedDestination}`
                                : null}
                            </p>
                          </div>
                        )}

                        <FloatingLabelSelect
                          label="Purpose"
                          options={RESPOND_TO_GUEST_PURPOSE_OPTIONS.map(
                            (option) => ({
                              value: option.id,
                              label: option.label,
                            })
                          )}
                          value={snapshot.purpose ?? undefined}
                          onValueChange={(value) => {
                            onPurposeChange(value as RespondToGuestPurposeId)
                          }}
                          disableFocusRing
                          contentClassName="z-[140]"
                        />

                        <FloatingLabelSelect
                          label="Tone"
                          options={RESPOND_TO_GUEST_TONE_OPTIONS.map(
                            (option) => ({
                              value: option.id,
                              label: option.label,
                            })
                          )}
                          value={snapshot.tone ?? undefined}
                          onValueChange={(value) => {
                            onToneChange(value as RespondToGuestToneId)
                          }}
                          disableFocusRing
                          contentClassName="z-[140]"
                        />

                        <div className="flex flex-col gap-2">
                          <label
                            htmlFor="respond-include-notes"
                            className="text-sm font-medium text-op-text-primary"
                          >
                            Anything the response should include? (optional)
                          </label>
                          <Textarea
                            id="respond-include-notes"
                            value={snapshot.includeNotes}
                            onChange={(event) => {
                              onIncludeNotesChange(event.target.value)
                            }}
                            className="min-h-[96px] rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                          />
                        </div>
                      </>
                    ) : null}

                    {onWriteChooser ? (
                      <div className="flex flex-col gap-4">
                        <p className="text-sm font-medium text-op-text-muted">
                          Prepare an AI draft or write the response yourself.
                        </p>
                        {snapshot.aiDraftStatus === "failed" ? (
                          <div className="flex flex-wrap gap-3">
                            {snapshot.aiDraftRetryable ? (
                              <Button
                                type="button"
                                disabled={locked}
                                onClick={onRetryAiDraft}
                              >
                                Try again
                              </Button>
                            ) : null}
                            <Button
                              type="button"
                              variant="op-secondary"
                              disabled={locked}
                              onClick={onWriteManually}
                            >
                              Write manually
                            </Button>
                          </div>
                        ) : (
                          <div className="flex flex-wrap gap-3">
                            <Button
                              type="button"
                              disabled={locked}
                              onClick={onPrepareDraft}
                            >
                              <SparklesIcon className="size-4" aria-hidden />
                              Prepare response draft
                            </Button>
                            <Button
                              type="button"
                              variant="op-secondary"
                              disabled={locked}
                              onClick={onWriteManually}
                            >
                              Write response manually
                            </Button>
                          </div>
                        )}
                      </div>
                    ) : null}

                    {onWriteEditor ? (
                      <>
                        {snapshot.channel === "email" ? (
                          <div className="flex flex-col gap-2">
                            <label
                              htmlFor="respond-subject"
                              className="text-sm font-medium text-op-text-primary"
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
                              className="h-12 rounded-[4px] border-op-card-border bg-[var(--op-color-gray-990)]"
                            />
                          </div>
                        ) : null}
                        <div className="flex flex-col gap-2">
                          <label
                            htmlFor="respond-message"
                            className="text-sm font-medium text-op-text-primary"
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
                            Uses 1 AI action
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
                      <div className="flex flex-col gap-4 rounded-[6px] border border-op-card-border bg-[var(--op-color-gray-990)] p-5">
                        <p className="text-sm font-medium text-op-text-primary">
                          Guest response recorded
                          {snapshot.maskedDestination != null
                            ? ` · ${snapshot.maskedDestination}`
                            : null}
                        </p>
                        <p className="text-sm text-op-text-muted">
                          Feedback stays In progress until you mark recovery
                          resolved.
                        </p>
                      </div>
                    ) : null}
                  </div>

                  <aside className="flex w-full flex-1 flex-col gap-6 rounded-[6px] bg-[var(--op-color-gray-990)] p-5">
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Feedback summary
                    </h2>
                    <dl className="flex flex-col gap-3.5">
                      <SummaryRow label="Guest:">
                        {snapshot.summary.guestName}
                      </SummaryRow>
                      <Separator className="bg-op-card-border" />
                      <SummaryRow label="Feedback:">
                        “{snapshot.summary.feedbackComment}”
                      </SummaryRow>
                      {snapshot.summary.purposeLabel != null ? (
                        <>
                          <Separator className="bg-op-card-border" />
                          <SummaryRow label="Purpose:">
                            {snapshot.summary.purposeLabel}
                          </SummaryRow>
                        </>
                      ) : null}
                      {snapshot.summary.toneLabel != null ? (
                        <>
                          <Separator className="bg-op-card-border" />
                          <SummaryRow label="Tone:">
                            {snapshot.summary.toneLabel}
                          </SummaryRow>
                        </>
                      ) : null}
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
                      disabled={locked}
                      onClick={onSaveAndExit}
                    >
                      Save and exit
                    </Button>
                    {snapshot.step === "setup" ? (
                      <Button
                        type="button"
                        disabled={!snapshot.canContinueSetup || locked}
                        onClick={onContinueSetup}
                      >
                        Continue
                      </Button>
                    ) : null}
                    {onWriteEditor ? (
                      <Button
                        type="button"
                        disabled={!snapshot.canContinueWrite || locked}
                        onClick={onContinueWrite}
                      >
                        Continue
                      </Button>
                    ) : null}
                    {snapshot.step === "review" ? (
                      <Button
                        type="button"
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
        open={snapshot.preparingOverlayOpen}
        onOpenChange={(open) => {
          if (!open) {
            onDismissPreparingOverlay()
          }
        }}
      >
        <DialogContent
          showCloseButton={false}
          className="z-[150] max-w-md border-op-card-border bg-[var(--op-color-gray-995)] text-op-text-primary"
        >
          <div className="absolute top-4 right-4">
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="Dismiss"
              className="rounded-[2px]"
              onClick={onDismissPreparingOverlay}
            >
              <XIcon className="size-[18px]" aria-hidden />
            </Button>
          </div>
          <DialogHeader className="items-center text-center sm:text-center">
            <Loader2Icon
              className="mb-2 size-8 animate-spin text-op-text-primary"
              aria-hidden
            />
            <DialogTitle>Preparing AI Draft</DialogTitle>
            <DialogDescription className="text-op-text-muted">
              We are preparing a draft response. You can write manually instead,
              or dismiss this dialog while preparation continues.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="sm:justify-center">
            <Button
              type="button"
              variant="op-secondary"
              onClick={onWriteManually}
            >
              Write manually
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={snapshot.sendConfirmOpen}
        onOpenChange={(open) => {
          if (!open && !sending) {
            onCancelSendConfirm()
          }
        }}
      >
        <DialogContent className="z-[140] max-w-md border-op-card-border bg-[var(--op-color-gray-995)] text-op-text-primary">
          <DialogHeader>
            <DialogTitle>Send this response?</DialogTitle>
            <DialogDescription className="text-op-text-muted">
              This will be recorded against the Feedback
              {snapshot.maskedDestination != null
                ? ` and sent to ${snapshot.maskedDestination}`
                : null}
              . Channel delivery may be stubbed in this environment.
            </DialogDescription>
          </DialogHeader>
          {snapshot.sendError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {snapshot.sendError}
            </p>
          ) : null}
          <DialogFooter className="gap-2 sm:justify-end">
            <Button
              type="button"
              variant="op-secondary"
              disabled={sending}
              onClick={onCancelSendConfirm}
            >
              Cancel
            </Button>
            <Button
              type="button"
              disabled={sending}
              onClick={onConfirmSend}
            >
              {sending
                ? "Sending…"
                : snapshot.sendStatus === "error"
                  ? "Send again"
                  : "Send"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
