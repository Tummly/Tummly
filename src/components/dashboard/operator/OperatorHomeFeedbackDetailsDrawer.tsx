import type { ReactNode } from "react"
import { SparklesIcon, XIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import type {
  FeedbackClassificationCorrection,
  FeedbackDetailsLoaded,
  FeedbackDetailsSnapshot,
} from "@/lib/operatorHome/createFeedbackDetailsModule"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type { FeedbackSentiment } from "@/types/dashboard"
import { cn } from "@/lib/utils"

type OperatorHomeFeedbackDetailsDrawerProps = {
  snapshot: FeedbackDetailsSnapshot
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
  nowMs?: number
}

const SENTIMENT_OPTIONS: Array<{
  value: FeedbackSentiment
  label: string
}> = [
  { value: "positive", label: "Positive" },
  { value: "neutral", label: "Neutral" },
  { value: "negative", label: "Negative" },
]

function formatSubmittedAbsolute(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  const datePart = date.toLocaleDateString("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  })
  const timePart = date.toLocaleTimeString("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  })
  return `${datePart} at ${timePart}`
}

function formatActivityTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  return date.toLocaleTimeString("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  })
}

function Section({
  title,
  children,
  className,
}: {
  title: string
  children: ReactNode
  className?: string
}) {
  return (
    <section
      className={cn(
        "flex flex-col gap-4 border-t border-[#dedede] px-[22px] py-4 pt-[22px] dark:border-white/10",
        className
      )}
    >
      <h3 className="text-lg font-bold text-foreground">{title}</h3>
      {children}
    </section>
  )
}

function PendingEmpty({ children }: { children: ReactNode }) {
  return (
    <p className="text-sm font-medium text-[#919191] dark:text-white/50">
      {children}
    </p>
  )
}

function SentimentBadge({
  sentiment,
}: {
  sentiment: "positive" | "neutral" | "negative"
}) {
  if (sentiment === "positive") {
    return (
      <span className="rounded-[4px] bg-[#e7f7ec] px-1.5 py-1 text-xs font-medium text-primary">
        Positive
      </span>
    )
  }
  if (sentiment === "neutral") {
    return (
      <span className="rounded-[4px] bg-[#fff4e6] px-1.5 py-1 text-xs font-medium text-[#f99810]">
        Neutral
      </span>
    )
  }
  return (
    <span className="rounded-[4px] bg-[#ffeeec] px-1.5 py-1 text-xs font-medium text-[#da4231]">
      Negative
    </span>
  )
}

function ClassificationSection({
  details,
  correction,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
}: {
  details: FeedbackDetailsLoaded
  correction: FeedbackClassificationCorrection
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
}) {
  const status = details.classificationStatus
  const saving = correction.saveStatus === "saving"

  return (
    <section className="flex flex-col gap-4 border-t border-[#dedede] px-[22px] py-4 pt-[22px] dark:border-white/10">
      <div className="flex items-center gap-3">
        <SparklesIcon className="size-[22px] text-primary" aria-hidden />
        <h3 className="text-lg font-bold text-foreground">
          AI classification
        </h3>
      </div>
      {status === "Pending" ? (
        <PendingEmpty>Classification is not available yet.</PendingEmpty>
      ) : null}
      {status === "Failed" ? (
        <PendingEmpty>Classification unavailable.</PendingEmpty>
      ) : null}
      {status === "Succeeded" && details.sentiment != null ? (
        <div className="flex flex-wrap gap-2">
          <SentimentBadge sentiment={details.sentiment} />
        </div>
      ) : null}
      {correction.isEditing ? (
        <div className="flex w-full flex-col gap-3">
          <FloatingLabelSelect
            label="Change classification"
            options={SENTIMENT_OPTIONS}
            value={correction.draftSentiment ?? undefined}
            onValueChange={(value) => {
              onDraftSentimentChange?.(value as FeedbackSentiment)
            }}
            disabled={saving}
            disableFocusRing
          />
          {correction.saveError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {correction.saveError}
            </p>
          ) : null}
          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="outline"
              disabled={!correction.canSave || saving}
              aria-disabled={!correction.canSave || saving}
              onClick={() => {
                onSaveCorrection?.()
              }}
              className="h-[37px] w-fit rounded-lg border-foreground px-[17px] text-xs font-medium"
            >
              {saving ? "Saving…" : "Save classification"}
            </Button>
            <button
              type="button"
              disabled={saving}
              onClick={() => {
                onCancelCorrection?.()
              }}
              className="flex h-[37px] w-fit items-center justify-center rounded-lg bg-[#ececec] px-4 text-xs font-medium text-foreground hover:bg-[#e2e2e2] disabled:opacity-50"
            >
              Cancel
            </button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          disabled={!details.canCorrectClassification}
          aria-disabled={!details.canCorrectClassification}
          onClick={() => {
            onStartCorrection?.()
          }}
          className="flex w-fit items-center gap-1.5 text-sm font-medium text-primary disabled:opacity-40"
        >
          Correct classification
        </button>
      )}
    </section>
  )
}

function DetectedIssuesSection({ details }: { details: FeedbackDetailsLoaded }) {
  const status = details.classificationStatus

  return (
    <Section title="Detected issues">
      {status === "Pending" ? (
        <PendingEmpty>
          Detected issues will appear when classification is available.
        </PendingEmpty>
      ) : null}
      {status === "Failed" ? (
        <PendingEmpty>Detected issues unavailable.</PendingEmpty>
      ) : null}
      {status === "Succeeded" && details.detectedIssues != null ? (
        details.detectedIssues.length === 0 ? (
          <PendingEmpty>No issues detected.</PendingEmpty>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {details.detectedIssues.map((issue) => (
              <li key={issue.key}>
                <span className="rounded-[4px] bg-[#f4f4f4] px-1.5 py-1 text-xs font-medium text-foreground dark:bg-white/10">
                  {issue.label}
                </span>
              </li>
            ))}
          </ul>
        )
      ) : null}
    </Section>
  )
}

function LoadedBody({
  details,
  correction,
  nowMs,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
}: {
  details: FeedbackDetailsLoaded
  correction: FeedbackClassificationCorrection
  nowMs: number
  onStartCorrection?: () => void
  onDraftSentimentChange?: (sentiment: FeedbackSentiment) => void
  onCancelCorrection?: () => void
  onSaveCorrection?: () => void
}) {
  const relative = formatRelativeTime(details.createdAt, nowMs)

  return (
    <>
      <div className="flex items-start justify-between gap-[22px] px-[22px] pb-[22px]">
        <div className="flex min-w-0 flex-1 flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <DrawerTitle className="text-2xl font-bold text-foreground">
              Feedback details
            </DrawerTitle>
            <DrawerDescription className="text-sm font-medium text-foreground">
              {details.venueLine}
            </DrawerDescription>
            {relative ? (
              <p className="text-xs font-medium text-[#919191]">
                Submitted {relative}
              </p>
            ) : null}
          </div>
          {details.isNew ? (
            <div className="flex gap-3">
              <span className="rounded bg-[#e4e4e4] px-1.5 py-1 text-xs font-medium text-foreground dark:bg-white/15">
                New
              </span>
            </div>
          ) : null}
        </div>
        <DrawerClose asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-[42px] shrink-0 rounded-xl bg-[#f1f1f1] hover:bg-[#e8e8e8] dark:bg-white/10"
            aria-label="Close Feedback details"
          >
            <XIcon className="size-[18px]" aria-hidden />
          </Button>
        </DrawerClose>
      </div>

      <Section title="Guest feedback" className="gap-4">
        <p className="text-base font-medium text-foreground">
          “{details.comment}”
        </p>
      </Section>

      <ClassificationSection
        details={details}
        correction={correction}
        onStartCorrection={onStartCorrection}
        onDraftSentimentChange={onDraftSentimentChange}
        onCancelCorrection={onCancelCorrection}
        onSaveCorrection={onSaveCorrection}
      />

      <DetectedIssuesSection details={details} />

      <section className="flex flex-col gap-5 border-t border-[#dedede] px-[22px] py-4 pt-[22px] dark:border-white/10">
        <h3 className="text-base font-bold text-foreground">Guest</h3>
        <div className="flex flex-col gap-1.5">
          <p className="text-sm font-semibold text-foreground">
            {details.guestName}
          </p>
          <p className="text-sm font-medium text-[#919191]">
            {details.guestContact}
          </p>
        </div>
        <button
          type="button"
          disabled={!details.canViewGuestProfile}
          aria-disabled={!details.canViewGuestProfile}
          className="w-fit text-sm font-medium text-primary disabled:opacity-40"
          aria-label={
            details.canViewGuestProfile
              ? "View guest profile"
              : "View guest profile (unavailable)"
          }
        >
          View guest profile
        </button>
      </section>

      <section className="flex flex-col gap-5 border-t border-[#dedede] px-[22px] py-4 pt-[22px] dark:border-white/10">
        <h3 className="text-lg font-bold text-foreground">
          Submission details
        </h3>
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-bold text-foreground">Location</p>
          <p className="text-sm font-medium text-foreground">
            {details.locationName}
          </p>
        </div>
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-bold text-foreground">Address</p>
          <p className="text-sm font-medium text-foreground">
            {details.address}
          </p>
        </div>
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-bold text-foreground">Submitted</p>
          <p className="text-sm font-medium text-foreground">
            {formatSubmittedAbsolute(details.createdAt)}
          </p>
        </div>
      </section>

      <section className="flex flex-col gap-[22px] border-t border-[#dedede] px-[22px] py-4 pt-[22px] dark:border-white/10">
        <h3 className="text-lg font-bold text-foreground">
          Add an internal note
        </h3>
        <div className="flex flex-col gap-3">
          <textarea
            disabled={!details.canAddInternalNote}
            aria-disabled={!details.canAddInternalNote}
            rows={3}
            placeholder="Add details about the feedback or any action taken…"
            className="w-full resize-none rounded border border-[rgba(74,74,76,0.4)] px-[13px] py-[15px] text-sm text-foreground placeholder:text-[#7d7d7d] disabled:cursor-not-allowed disabled:opacity-60 dark:border-white/20"
          />
          <Button
            type="button"
            variant="outline"
            disabled={!details.canAddInternalNote}
            aria-disabled={!details.canAddInternalNote}
            className="h-[37px] w-fit rounded-lg border-foreground px-[17px] text-xs font-medium"
          >
            Add note
          </Button>
        </div>
      </section>

      <Section title="Activity history" className="pb-[122px]">
        {details.activityHistory.map((event) => (
          <div key={`${event.kind}-${event.at}`} className="flex flex-col gap-1.5">
            <p className="text-sm font-semibold text-foreground">
              {formatActivityTime(event.at)}
            </p>
            <p className="text-xs font-normal text-foreground">
              Feedback received
            </p>
          </div>
        ))}
      </Section>
    </>
  )
}

/** Latest activity Feedback details — modal right Drawer (Figma 2934:4740). */
export function OperatorHomeFeedbackDetailsDrawer({
  snapshot,
  onOpenChange,
  onRetry,
  onStartCorrection,
  onDraftSentimentChange,
  onCancelCorrection,
  onSaveCorrection,
  nowMs = Date.now(),
}: OperatorHomeFeedbackDetailsDrawerProps) {
  return (
    <Drawer
      open={snapshot.isOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent className="h-full max-h-dvh overflow-hidden rounded-tl-lg data-[vaul-drawer-direction=right]:sm:max-w-lg">
        <div className="flex min-h-0 flex-1 flex-col overflow-y-auto pt-[22px]">
          {snapshot.loadStatus === "loading" ||
          snapshot.loadStatus === "idle" ? (
            <div className="flex flex-col gap-4 px-[22px] pb-[22px]">
              <DrawerHeader className="gap-3 p-0">
                <div className="flex items-start justify-between gap-4">
                  <DrawerTitle className="text-2xl font-bold">
                    Feedback details
                  </DrawerTitle>
                  <DrawerClose asChild>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="size-[42px] shrink-0 rounded-xl bg-[#f1f1f1] hover:bg-[#e8e8e8] dark:bg-white/10"
                      aria-label="Close Feedback details"
                    >
                      <XIcon className="size-[18px]" aria-hidden />
                    </Button>
                  </DrawerClose>
                </div>
                <DrawerDescription className="sr-only">
                  Loading Feedback details
                </DrawerDescription>
              </DrawerHeader>
              <div
                className="flex min-h-48 items-center justify-center"
                role="status"
                aria-live="polite"
                aria-label="Loading Feedback details"
              >
                <div
                  className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
                  aria-hidden
                />
              </div>
            </div>
          ) : snapshot.loadStatus === "error" ? (
            <div className="flex flex-col gap-4 px-[22px] pb-[22px]">
              <DrawerHeader className="gap-3 p-0">
                <div className="flex items-start justify-between gap-4">
                  <DrawerTitle className="text-2xl font-bold">
                    Feedback details
                  </DrawerTitle>
                  <DrawerClose asChild>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="size-[42px] shrink-0 rounded-xl bg-[#f1f1f1] hover:bg-[#e8e8e8] dark:bg-white/10"
                      aria-label="Close Feedback details"
                    >
                      <XIcon className="size-[18px]" aria-hidden />
                    </Button>
                  </DrawerClose>
                </div>
                <DrawerDescription className="sr-only">
                  Feedback details failed to load
                </DrawerDescription>
              </DrawerHeader>
              <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
                <p className="text-sm text-destructive" role="alert">
                  {snapshot.loadError ??
                    "Could not load Feedback details. Please try again."}
                </p>
                <button
                  type="button"
                  className="text-sm font-medium text-primary underline"
                  onClick={onRetry}
                >
                  Retry
                </button>
              </div>
            </div>
          ) : snapshot.details != null ? (
            <LoadedBody
              details={snapshot.details}
              correction={snapshot.correction}
              nowMs={nowMs}
              onStartCorrection={onStartCorrection}
              onDraftSentimentChange={onDraftSentimentChange}
              onCancelCorrection={onCancelCorrection}
              onSaveCorrection={onSaveCorrection}
            />
          ) : null}
        </div>
      </DrawerContent>
    </Drawer>
  )
}
