import { useEffect, useState, type ReactNode } from "react"
import { XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog"
import { Separator } from "@/components/ui/separator"
import { Spinner } from "@/components/ui/spinner"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import type { StartRecoveryEntrySnapshot } from "@/lib/operatorFeedback/createStartRecoveryEntryModule"
import { formatRecoveryLastSavedLabel } from "@/lib/operatorFeedback/recoveryWizardChromePresentation"
import {
  START_RECOVERY_INTENT_CARD_CLASS,
  START_RECOVERY_INTENT_CARD_DISABLED_CLASS,
  START_RECOVERY_INTENT_CARD_IDLE_CLASS,
  START_RECOVERY_INTENT_CARD_SELECTED_CLASS,
  START_RECOVERY_SUMMARY_CLASS,
  START_RECOVERY_SUMMARY_DIVIDER_CLASS,
  type StartRecoveryIntentId,
} from "@/lib/operatorFeedback/startRecoveryPresentation"
import {
  OPERATOR_WIZARD_SHELL_BODY_CLASS,
  OPERATOR_WIZARD_SHELL_DIALOG_CLASS,
  OPERATOR_WIZARD_SHELL_FOOTER_CLASS,
  OPERATOR_WIZARD_SHELL_HEADER_CLASS,
} from "@/lib/operatorUi/operatorWizardChromePresentation"
import { cn } from "@/lib/utils"

type StartRecoveryEntryShellProps = {
  snapshot: StartRecoveryEntrySnapshot
  onClose: () => void
  onSelectIntent: (intentId: StartRecoveryIntentId) => void
  onRetry: () => void
}

/** Figma Main Bg/Subtitle (#7c7c7c) — preferred over dark `text-op-text-muted`. */
const SUBTITLE_CLASS = "text-[var(--op-color-gray-550)]"

function ClassificationValue({
  status,
  sentiment,
}: {
  status: "Pending" | "Succeeded" | "Failed"
  sentiment: "positive" | "neutral" | "negative" | null
}) {
  if (status === "Pending") {
    return (
      <span className="text-base font-medium text-op-text-primary">
        Pending
      </span>
    )
  }
  if (status === "Failed" || sentiment == null) {
    return (
      <span className="text-base font-medium text-op-text-primary">
        Unavailable
      </span>
    )
  }

  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return <Badge variant={sentiment}>{label}</Badge>
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
      <dt className={cn("shrink-0 text-base font-semibold", SUBTITLE_CLASS)}>
        {label}
      </dt>
      <dd className="min-w-0 text-right text-base font-medium text-op-text-primary">
        {children}
      </dd>
    </div>
  )
}

/** Full-screen Start recovery entry shell — intents left, Feedback summary right. */
export function StartRecoveryEntryShell({
  snapshot,
  onClose,
  onSelectIntent,
  onRetry,
}: StartRecoveryEntryShellProps) {
  const [pendingIntentId, setPendingIntentId] =
    useState<StartRecoveryIntentId | null>(null)
  const [openedAt, setOpenedAt] = useState(() => new Date())

  useEffect(() => {
    if (snapshot.isOpen) {
      setOpenedAt(new Date())
      setPendingIntentId(null)
    }
  }, [snapshot.isOpen, snapshot.feedbackId])

  useEffect(() => {
    setPendingIntentId((current) => {
      if (current == null) {
        return null
      }
      if (snapshot.loadStatus !== "loaded") {
        return null
      }
      const stillValid = snapshot.intents.some(
        (intent) => intent.id === current && intent.enabled
      )
      return stillValid ? current : null
    })
  }, [snapshot.loadStatus, snapshot.intents])

  const lastSavedLabel = formatRecoveryLastSavedLabel(openedAt)
  const canContinue = pendingIntentId != null

  return (
    <Dialog
      open={snapshot.isOpen}
      onOpenChange={(open) => {
        if (!open) {
          onClose()
        }
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={OPERATOR_WIZARD_SHELL_DIALOG_CLASS}
      >
        <div className={OPERATOR_WIZARD_SHELL_HEADER_CLASS}>
          <Button
            type="button"
            variant="op-collapse"
            aria-label="Close"
            onClick={onClose}
          >
            <XIcon aria-hidden />
          </Button>
        </div>

        {/* Full-bleed scroll: content + footer share one track at the screen edge. */}
        <div className={OPERATOR_WIZARD_SHELL_BODY_CLASS}>
          <div className="flex min-h-full flex-col">
            {/* Figma 1728 frame uses 200px side inset; scale down on narrower viewports. */}
            <div className="flex flex-1 flex-col px-4 pb-24 pt-10 sm:px-6 sm:pt-[60px] md:px-[100px] min-[1728px]:px-[200px]">
              <DialogTitle className="pr-0 text-[28px] font-bold leading-normal tracking-normal text-op-text-primary sm:text-[32px]">
                Start recovery
              </DialogTitle>
              <DialogDescription
                className={cn(
                  "mt-2 text-sm font-medium leading-5",
                  SUBTITLE_CLASS,
                  snapshot.headerSubtitle == null && "sr-only"
                )}
              >
                {snapshot.headerSubtitle
                  ?? "Choose how you would like to follow up on this feedback."}
              </DialogDescription>

              {snapshot.loadStatus === "loading" ? (
                <div
                  className="flex flex-1 items-center justify-center py-24"
                  role="status"
                  aria-live="polite"
                  aria-label="Loading recovery"
                >
                  <Spinner size="md" aria-hidden />
                </div>
              ) : null}

              {snapshot.loadStatus === "error" ? (
                <div className="mt-12 flex flex-col items-start gap-3">
                  <p className="text-sm text-destructive" role="alert">
                    {snapshot.loadError ?? "Could not load recovery."}
                  </p>
                  <Button type="button" variant="op-secondary" onClick={onRetry}>
                    Retry
                  </Button>
                </div>
              ) : null}

              {snapshot.loadStatus === "loaded" && snapshot.summary != null ? (
                <div className="mt-10 flex flex-col gap-8 sm:mt-[52px] lg:flex-row lg:items-start lg:gap-[42px]">
                  <div className="flex flex-1 flex-col gap-7">
                    <div className="flex flex-col gap-2">
                      <h2 className="text-xl font-semibold text-op-text-primary sm:text-[22px]">
                        What would you like to do?
                      </h2>
                      <p
                        className={cn(
                          "text-sm font-medium leading-5",
                          SUBTITLE_CLASS
                        )}
                      >
                        Choose how you would like to follow up on this feedback.
                      </p>
                    </div>

                    {snapshot.workflowAdvanceError != null ? (
                      <p className="text-sm text-destructive" role="alert">
                        {snapshot.workflowAdvanceError}
                      </p>
                    ) : null}

                    <TooltipProvider delayDuration={200}>
                      <div
                        className="flex flex-col gap-[18px]"
                        role="radiogroup"
                        aria-label="Recovery follow-up options"
                      >
                        {snapshot.intents.map((intent) => {
                          const selected = pendingIntentId === intent.id
                          const card = (
                            <Button
                              type="button"
                              variant="ghost"
                              role="radio"
                              aria-checked={selected}
                              aria-disabled={!intent.enabled}
                              tabIndex={0}
                              title={
                                !intent.enabled && intent.disableReason != null
                                  ? intent.disableReason
                                  : undefined
                              }
                              className={cn(
                                START_RECOVERY_INTENT_CARD_CLASS,
                                !intent.enabled
                                  && START_RECOVERY_INTENT_CARD_DISABLED_CLASS,
                                intent.enabled
                                  && selected
                                  && START_RECOVERY_INTENT_CARD_SELECTED_CLASS,
                                intent.enabled
                                  && !selected
                                  && START_RECOVERY_INTENT_CARD_IDLE_CLASS
                              )}
                              onClick={() => {
                                if (!intent.enabled) {
                                  return
                                }
                                setPendingIntentId(intent.id)
                              }}
                            >
                              <span className="flex flex-col gap-1">
                                <span className="text-sm font-medium text-op-text-primary">
                                  {intent.title}
                                </span>
                                <span
                                  className={cn(
                                    "text-sm font-medium",
                                    SUBTITLE_CLASS
                                  )}
                                >
                                  {intent.description}
                                </span>
                              </span>
                            </Button>
                          )

                          if (!intent.enabled && intent.disableReason != null) {
                            return (
                              <Tooltip key={intent.id}>
                                <TooltipTrigger asChild>{card}</TooltipTrigger>
                                <TooltipContent
                                  side="top"
                                  className="z-[140] bg-op-toast-background text-op-toast-text"
                                >
                                  {intent.disableReason}
                                </TooltipContent>
                              </Tooltip>
                            )
                          }

                          return <div key={intent.id}>{card}</div>
                        })}
                      </div>
                    </TooltipProvider>
                  </div>

                  <aside className={START_RECOVERY_SUMMARY_CLASS}>
                    <h2 className="text-lg font-semibold text-op-text-primary">
                      Feedback summary
                    </h2>
                    <dl className="flex flex-col gap-3.5">
                      <SummaryRow label="Guest:">
                        {snapshot.summary.guestName}
                      </SummaryRow>
                      <Separator className={START_RECOVERY_SUMMARY_DIVIDER_CLASS} />
                      <SummaryRow label="Classification:">
                        <ClassificationValue
                          status={snapshot.summary.classificationStatus}
                          sentiment={snapshot.summary.classificationSentiment}
                        />
                      </SummaryRow>
                      <Separator className={START_RECOVERY_SUMMARY_DIVIDER_CLASS} />
                      <SummaryRow label="Contact:">
                        {snapshot.summary.contactLabel}
                      </SummaryRow>
                      <Separator className={START_RECOVERY_SUMMARY_DIVIDER_CLASS} />
                      <SummaryRow label="Feedback:">
                        “{snapshot.summary.feedbackComment}”
                      </SummaryRow>
                      <Separator className={START_RECOVERY_SUMMARY_DIVIDER_CLASS} />
                      <SummaryRow label="Issue tags:">
                        {snapshot.summary.issueTagLabels == null ? (
                          <span>—</span>
                        ) : snapshot.summary.issueTagLabels.length === 0 ? (
                          <span>No issues detected.</span>
                        ) : (
                          <ul className="flex flex-wrap justify-end gap-3">
                            {snapshot.summary.issueTagLabels.map((label) => (
                              <li key={label}>
                                <Badge variant="tag">{label}</Badge>
                              </li>
                            ))}
                          </ul>
                        )}
                      </SummaryRow>
                      <Separator className={START_RECOVERY_SUMMARY_DIVIDER_CLASS} />
                    </dl>
                  </aside>
                </div>
              ) : null}
            </div>

            <div className={OPERATOR_WIZARD_SHELL_FOOTER_CLASS}>
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-wrap items-center gap-[18px]">
                  <Button
                    type="button"
                    variant="op-secondary"
                    onClick={onClose}
                  >
                    Cancel
                  </Button>
                  <div
                    className={cn(
                      "flex items-center gap-3 text-sm font-medium",
                      SUBTITLE_CLASS
                    )}
                  >
                    <span
                      aria-hidden
                      className="size-3 shrink-0 rounded-full bg-[var(--op-color-gray-550)]"
                    />
                    <span>{lastSavedLabel}</span>
                  </div>
                </div>
                <Button
                  type="button"
                  variant="op-primary"
                  disabled={!canContinue}
                  aria-disabled={!canContinue}
                  onClick={() => {
                    if (pendingIntentId == null) {
                      return
                    }
                    onSelectIntent(pendingIntentId)
                  }}
                >
                  Continue
                </Button>
              </div>
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
