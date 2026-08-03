import type { ReactNode } from "react"
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
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import type { StartRecoveryEntrySnapshot } from "@/lib/operatorFeedback/createStartRecoveryEntryModule"
import type { StartRecoveryIntentId } from "@/lib/operatorFeedback/startRecoveryPresentation"
import { cn } from "@/lib/utils"

type StartRecoveryEntryShellProps = {
  snapshot: StartRecoveryEntrySnapshot
  onClose: () => void
  onSelectIntent: (intentId: StartRecoveryIntentId) => void
  onRetry: () => void
}

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
      <dt className="shrink-0 text-base font-semibold text-op-text-muted">
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
        className={cn(
          "fixed inset-0 top-0 left-0 z-[130] flex h-dvh max-h-dvh w-screen max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-hidden rounded-none border-0 bg-op-surface-secondary p-0 text-op-text-primary shadow-none sm:max-w-none",
          "data-open:zoom-in-100 data-closed:zoom-out-100"
        )}
      >
        <div className="flex w-full shrink-0 items-center justify-end p-6">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            aria-label="Close"
            className="rounded-[2px] bg-op-button-collapse-background text-op-text-primary hover:bg-op-button-collapse-hover hover:opacity-100"
            onClick={onClose}
          >
            <XIcon className="size-[18px]" aria-hidden />
          </Button>
        </div>

        <div className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-t-[20px] border-t border-op-card-border bg-[var(--op-color-gray-995)]">
          <div className="mx-auto flex min-h-0 w-full max-w-[1440px] flex-1 flex-col overflow-y-auto px-6 pb-24 pt-[60px] md:px-[100px] xl:px-[200px]">
            <DialogTitle className="pr-0 text-[32px] font-bold leading-normal tracking-normal text-op-text-primary">
              Start recovery
            </DialogTitle>
            <DialogDescription
              className={cn(
                "mt-2 max-w-[425px] text-sm font-medium leading-5 text-op-text-muted",
                snapshot.headerSubtitle == null && "sr-only"
              )}
            >
              {snapshot.headerSubtitle
                ?? "Choose how you would like to follow up on this feedback."}
            </DialogDescription>

            {snapshot.loadStatus === "loading" ? (
              <p className="mt-12 text-sm text-op-text-muted">Loading…</p>
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
              <div className="mt-[52px] flex w-full flex-col gap-[42px] lg:flex-row lg:items-start">
                <div className="flex w-full max-w-[690px] flex-col gap-7">
                  <div className="flex flex-col gap-2">
                    <h2 className="text-[22px] font-semibold text-op-text-primary">
                      What would you like to do?
                    </h2>
                    <p className="max-w-[425px] text-sm font-medium leading-5 text-op-text-muted">
                      Choose how you would like to follow up on this feedback.
                    </p>
                  </div>

                  {snapshot.workflowAdvanceError != null ? (
                    <p className="text-sm text-destructive" role="alert">
                      {snapshot.workflowAdvanceError}
                    </p>
                  ) : null}

                  <TooltipProvider delayDuration={200}>
                    <div className="flex flex-col gap-[18px]">
                      {snapshot.intents.map((intent) => {
                        const card = (
                          <Button
                            type="button"
                            variant="ghost"
                            aria-disabled={!intent.enabled}
                            tabIndex={0}
                            title={
                              !intent.enabled && intent.disableReason != null
                                ? intent.disableReason
                                : undefined
                            }
                            className={cn(
                              "h-auto w-full items-center justify-start rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent",
                              intent.enabled
                                ? "border-op-card-border bg-[var(--op-color-gray-990)] hover:border-op-text-muted"
                                : "cursor-not-allowed border-op-card-border bg-[var(--op-color-gray-990)] opacity-50 hover:border-op-card-border"
                            )}
                            onClick={() => {
                              if (!intent.enabled) {
                                return
                              }
                              onSelectIntent(intent.id)
                            }}
                          >
                            <span className="flex min-w-0 flex-col gap-1">
                              <span className="text-sm font-medium text-op-text-primary">
                                {intent.title}
                              </span>
                              <span className="text-sm font-medium text-op-text-muted">
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

                <aside className="flex w-full flex-1 flex-col gap-6 rounded-[6px] bg-[var(--op-color-gray-990)] p-5">
                  <h2 className="text-lg font-semibold text-op-text-primary">
                    Feedback summary
                  </h2>
                  <dl className="flex flex-col gap-3.5">
                    <SummaryRow label="Guest:">
                      {snapshot.summary.guestName}
                    </SummaryRow>
                    <Separator className="bg-op-card-border" />
                    <SummaryRow label="Classification:">
                      <ClassificationValue
                        status={snapshot.summary.classificationStatus}
                        sentiment={snapshot.summary.classificationSentiment}
                      />
                    </SummaryRow>
                    <Separator className="bg-op-card-border" />
                    <SummaryRow label="Contact:">
                      {snapshot.summary.contactLabel}
                    </SummaryRow>
                    <Separator className="bg-op-card-border" />
                    <SummaryRow label="Feedback:">
                      “{snapshot.summary.feedbackComment}”
                    </SummaryRow>
                    <Separator className="bg-op-card-border" />
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
                    <Separator className="bg-op-card-border" />
                  </dl>
                </aside>
              </div>
            ) : null}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
