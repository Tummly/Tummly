import type { ReactNode } from "react"
import { SparklesIcon, XIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { FloatingLabelSelect } from "@/components/ui/floating-label-select"
import { Textarea } from "@/components/ui/textarea"
import type {
  FeedbackClassificationCorrection,
  FeedbackDetailsLoaded,
  FeedbackDetailsSnapshot,
} from "@/lib/operatorHome/createFeedbackDetailsModule"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import {
  OPERATOR_DRAWER_ACTION_ROW_CLASS,
  OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
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
  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return <Badge variant={sentiment}>{label}</Badge>
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
            contentClassName="z-[120]"
          />
          {correction.saveError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {correction.saveError}
            </p>
          ) : null}
          <div className={OPERATOR_DRAWER_ACTION_ROW_CLASS}>
            <Button
              type="button"
              variant="outline"
              disabled={!correction.canSave || saving}
              aria-disabled={!correction.canSave || saving}
              onClick={() => {
                onSaveCorrection?.()
              }}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "w-fit rounded-lg border border-foreground bg-transparent px-[17px] text-xs font-medium text-foreground hover:bg-black/5 dark:border-foreground dark:bg-transparent dark:hover:bg-white/10"
              )}
            >
              {saving ? "Saving…" : "Save classification"}
            </Button>
            <Button
              type="button"
              variant="muted"
              disabled={saving}
              onClick={() => {
                onCancelCorrection?.()
              }}
              className={cn(
                OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                "w-fit rounded-lg px-4 text-xs font-medium text-foreground"
              )}
            >
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={!details.canCorrectClassification}
          aria-disabled={!details.canCorrectClassification}
          onClick={() => {
            onStartCorrection?.()
          }}
          className="w-fit font-medium disabled:opacity-40"
        >
          Correct classification
        </Button>
      )}
    </section>
  )
}

function DetectedTagsSection({ details }: { details: FeedbackDetailsLoaded }) {
  const status = details.classificationStatus

  return (
    <Section title="Detected tags">
      {status === "Pending" ? (
        <PendingEmpty>
          Detected tags will appear when classification is available.
        </PendingEmpty>
      ) : null}
      {status === "Failed" ? (
        <PendingEmpty>Detected tags unavailable.</PendingEmpty>
      ) : null}
      {status === "Succeeded" && details.detectedTags != null ? (
        details.detectedTags.length === 0 ? (
          <PendingEmpty>No tags detected.</PendingEmpty>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {details.detectedTags.map((tag) => (
              <li key={tag.key}>
                <Badge variant="tag">{tag.label}</Badge>
              </li>
            ))}
          </ul>
        )
      ) : null}
    </Section>
  )
}

function FeedbackDetailsDrawerHeader({
  venueLine,
  relativeSubmitted,
  isNew,
  description,
}: {
  venueLine?: string
  relativeSubmitted?: string
  isNew?: boolean
  description?: string
}) {
  return (
    <div className="flex shrink-0 items-start justify-between gap-[22px] px-[22px] pb-[22px] pt-[22px]">
      <div className="flex min-w-0 flex-1 flex-col gap-3">
        <div className="flex flex-col gap-1.5">
          <DrawerTitle className="text-2xl font-bold text-foreground">
            Feedback details
          </DrawerTitle>
          {description != null ? (
            <DrawerDescription className="sr-only">
              {description}
            </DrawerDescription>
          ) : venueLine != null ? (
            <DrawerDescription className="text-sm font-medium text-foreground">
              {venueLine}
            </DrawerDescription>
          ) : null}
          {relativeSubmitted ? (
            <p className="text-xs font-medium text-[#919191]">
              Submitted {relativeSubmitted}
            </p>
          ) : null}
        </div>
        {isNew ? (
          <div className="flex gap-3">
            <Badge variant="soft">New</Badge>
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
  )
}

function LoadedBody({
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
  return (
    <>
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

      <DetectedTagsSection details={details} />

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
        <Button
          type="button"
          variant="link"
          size="link-sm"
          disabled={!details.canViewGuestProfile}
          aria-disabled={!details.canViewGuestProfile}
          className="w-fit font-medium disabled:opacity-40"
          aria-label={
            details.canViewGuestProfile
              ? "View guest profile"
              : "View guest profile (unavailable)"
          }
        >
          View guest profile
        </Button>
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
          <Textarea
            disabled={!details.canAddInternalNote}
            aria-disabled={!details.canAddInternalNote}
            rows={3}
            placeholder="Add details about the feedback or any action taken…"
            className="min-h-0 resize-none rounded border-[rgba(74,74,76,0.4)] px-[13px] py-[15px] text-sm placeholder:text-[#7d7d7d] disabled:opacity-60 dark:border-white/20 dark:bg-transparent dark:disabled:bg-transparent"
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

      <Section title="Activity history">
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
  const relativeSubmitted =
    snapshot.details != null
      ? formatRelativeTime(snapshot.details.createdAt, nowMs) || undefined
      : undefined

  return (
    <Drawer
      open={snapshot.isOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
        <div className="flex min-h-0 flex-1 flex-col">
          {snapshot.loadStatus === "loading" ||
          snapshot.loadStatus === "idle" ? (
            <>
              <FeedbackDetailsDrawerHeader description="Loading Feedback details" />
              <div
                className={cn(
                  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                  "px-[22px] pb-[22px]"
                )}
              >
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
            </>
          ) : snapshot.loadStatus === "error" ? (
            <>
              <FeedbackDetailsDrawerHeader description="Feedback details failed to load" />
              <div
                className={cn(
                  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                  "px-[22px] pb-[22px]"
                )}
              >
                <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
                  <p className="text-sm text-destructive" role="alert">
                    {snapshot.loadError ??
                      "Could not load Feedback details. Please try again."}
                  </p>
                  <Button
                    type="button"
                    variant="link"
                    size="link-sm"
                    className={cn(
                      OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
                      "font-medium underline"
                    )}
                    onClick={onRetry}
                  >
                    Retry
                  </Button>
                </div>
              </div>
            </>
          ) : snapshot.details != null ? (
            <>
              <FeedbackDetailsDrawerHeader
                venueLine={snapshot.details.venueLine}
                relativeSubmitted={relativeSubmitted}
                isNew={snapshot.details.isNew}
              />
              <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>
                <LoadedBody
                  details={snapshot.details}
                  correction={snapshot.correction}
                  onStartCorrection={onStartCorrection}
                  onDraftSentimentChange={onDraftSentimentChange}
                  onCancelCorrection={onCancelCorrection}
                  onSaveCorrection={onSaveCorrection}
                />
              </div>
            </>
          ) : null}
        </div>
      </DrawerContent>
    </Drawer>
  )
}
