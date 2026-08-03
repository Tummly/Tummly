import type { ReactNode } from "react"
import { XIcon } from "lucide-react"
import { useNavigate } from "react-router-dom"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Textarea } from "@/components/ui/textarea"
import type {
  GuestDetailsLoaded,
  GuestDetailsSnapshot,
} from "@/lib/operatorGuests/createGuestDetailsModule"
import {
  GUEST_PROFILE_ADD_NOTE_LABEL,
  GUEST_PROFILE_EMPTY_COPY,
  GUEST_PROFILE_NOT_PROVIDED,
  GUEST_PROFILE_OPEN_FEEDBACK_LABEL,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import type { GuestProfileLocationHandoff } from "@/lib/operatorGuestProfile/guestProfileLocationHandoff"
import {
  operatorDashboardGuestProfilePath,
  type OperatorDashboardMode,
} from "@/lib/operatorHome/operatorDashboardPaths"
import {
  OPERATOR_DRAWER_PRIMARY_ACTION_CLASS,
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

const DRAWER_SECTION_CLASS =
  "flex flex-col gap-5 border-t border-border p-[22px]"

const PENDING_UNAVAILABLE = "unavailable"

type GuestDetailsDrawerProps = {
  snapshot: GuestDetailsSnapshot
  mode: OperatorDashboardMode
  selectedLocationId: number | null
  onOpenChange: (open: boolean) => void
  onRetry: () => void
  onNoteDraftChange: (value: string) => void
  onCreateNote: () => Promise<boolean>
  onOpenFeedback: (feedbackId: number) => void
  onStartRecovery?: (feedbackId: number) => void
}

function Section({
  title,
  children,
  className,
  headerAction,
}: {
  title: string
  children: ReactNode
  className?: string
  headerAction?: ReactNode
}) {
  return (
    <section className={cn(DRAWER_SECTION_CLASS, className)}>
      <div className="flex items-start justify-between gap-3">
        <h3 className="text-lg font-bold text-foreground">{title}</h3>
        {headerAction}
      </div>
      {children}
    </section>
  )
}

function DetailRow({
  label,
  value,
  href,
  stacked = false,
}: {
  label: string
  value: ReactNode
  href?: string
  stacked?: boolean
}) {
  const valueNode =
    href != null ? (
      <a
        href={href}
        className="text-sm font-medium text-muted-foreground underline"
      >
        {value}
      </a>
    ) : (
      <span className="text-sm font-medium text-muted-foreground">{value}</span>
    )

  if (stacked) {
    return (
      <div className="flex flex-col gap-1.5">
        <p className="text-base font-medium text-foreground">{label}</p>
        {valueNode}
      </div>
    )
  }

  return (
    <div className="flex items-center justify-between gap-3">
      <p className="text-base font-medium text-foreground">{label}</p>
      {valueNode}
    </div>
  )
}

function EmptySectionCopy({
  title,
  helper,
}: {
  title: string
  helper: string
}) {
  return (
    <div className="flex flex-col gap-1">
      <p className="text-sm font-medium text-foreground">{title}</p>
      <p className="text-sm text-muted-foreground">{helper}</p>
    </div>
  )
}

function formatActivityTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return ""
  }

  return date
    .toLocaleTimeString("en-GB", {
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
    })
    .replace(/\b(am|pm)\b/i, (match) => match.toUpperCase())
}

function PendingButton({
  label,
  className,
}: {
  label: string
  className?: string
}) {
  return (
    <Button
      type="button"
      variant="op-tertiary"
      disabled
      aria-disabled
      aria-label={`${label} (${PENDING_UNAVAILABLE})`}
      title={`${label} is unavailable`}
      className={cn("w-fit rounded-[2px]", className)}
    >
      {label}
    </Button>
  )
}

function GuestDetailsDrawerHeader({
  details,
  description,
  onViewFullProfile,
}: {
  details?: GuestDetailsLoaded
  description?: string
  onViewFullProfile?: () => void
}) {
  return (
    <div className="flex shrink-0 items-start justify-between gap-[22px] px-[22px] pb-[22px] pt-8">
      <div className="flex min-w-0 flex-1 flex-col gap-[22px]">
        <div className="flex flex-col gap-3">
          <div className="flex flex-col gap-1.5">
            <DrawerTitle className="text-2xl font-bold text-foreground">
              {details?.name ?? "Guest details"}
            </DrawerTitle>
            {description != null ? (
              <DrawerDescription className="sr-only">
                {description}
              </DrawerDescription>
            ) : details != null ? (
              <DrawerDescription className="text-sm font-medium text-foreground">
                {details.identitySubtitle}
              </DrawerDescription>
            ) : null}
          </div>
          {details != null ? (
            <div className="flex items-start">
              <Badge variant="soft">{details.marketingStatusLabel}</Badge>
            </div>
          ) : null}
        </div>
        {details != null && onViewFullProfile != null ? (
          <div className="flex flex-wrap gap-3">
            <Button
              type="button"
              variant="op-primary"
              className="rounded-[2px]"
              onClick={onViewFullProfile}
            >
              View full profile
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              disabled
              aria-disabled
              aria-label="Create campaign (unavailable)"
              title="Create campaign is unavailable"
              className="rounded-[2px]"
            >
              Create campaign
            </Button>
          </div>
        ) : null}
      </div>
      <DrawerClose asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="size-[42px] shrink-0 rounded-[2px] bg-muted hover:bg-muted/80"
          aria-label="Close Guest details"
        >
          <XIcon className="size-[18px]" aria-hidden />
        </Button>
      </DrawerClose>
    </div>
  )
}

function LoadedBody({
  details,
  noteDraft,
  noteCreateStatus,
  noteCreateError,
  onNoteDraftChange,
  onCreateNote,
  onOpenFeedback,
  onStartRecovery,
  onViewFullActivity,
}: {
  details: GuestDetailsLoaded
  noteDraft: string
  noteCreateStatus: GuestDetailsSnapshot["noteCreateStatus"]
  noteCreateError: string | null
  onNoteDraftChange: (value: string) => void
  onCreateNote: () => void
  onOpenFeedback: (feedbackId: number) => void
  onStartRecovery?: (feedbackId: number) => void
  onViewFullActivity: () => void
}) {
  const feedbackEmpty = GUEST_PROFILE_EMPTY_COPY.overviewLatestFeedback
  const offersEmpty = GUEST_PROFILE_EMPTY_COPY.overviewLatestOffer
  const activityEmpty = GUEST_PROFILE_EMPTY_COPY.activityTab
  const noteBusy = noteCreateStatus === "saving"
  const canAddNote = noteDraft.trim().length > 0 && !noteBusy

  return (
    <>
      <Section title="Contact and permissions">
        <DetailRow
          label="Email:"
          value={details.emailDisplay}
          href={
            details.email != null ? `mailto:${details.email}` : undefined
          }
        />
        <DetailRow label="Mobile:" value={details.mobileDisplay} />
        <DetailRow
          label="Email marketing:"
          value={details.emailMarketingLabel}
        />
        <DetailRow label="SMS marketing:" value={details.smsMarketingLabel} />
        <DetailRow
          label="Consent captured:"
          value={details.consentCapturedDisplay}
        />
      </Section>

      <Section title="Relationship summary">
        <DetailRow
          label="First captured:"
          value={details.firstCapturedDisplay}
          stacked
        />
        <DetailRow label="Location:" value={details.locationName} stacked />
        <DetailRow
          label="Feedback submissions:"
          value={String(details.feedbackSubmissionCount)}
          stacked
        />
        <DetailRow
          label="Offer redemptions:"
          value={details.offerRedemptionsDisplay}
          stacked
        />
        <div className="flex flex-col gap-1.5">
          <p className="text-base font-medium text-foreground">Tags:</p>
          {details.guestTags.length === 0 ? (
            <span className="text-sm font-medium text-muted-foreground">
              {GUEST_PROFILE_NOT_PROVIDED}
            </span>
          ) : (
            <ul className="flex flex-wrap gap-2">
              {details.guestTags.map((tag) => (
                <li key={tag.id}>
                  <Badge variant="soft">{tag.name}</Badge>
                </li>
              ))}
            </ul>
          )}
        </div>
      </Section>

      <Section title="Recent feedback" className="gap-5">
        {details.latestFeedback == null ? (
          <EmptySectionCopy
            title={feedbackEmpty.emptyTitle}
            helper={feedbackEmpty.emptyHelper}
          />
        ) : (
          <>
            <div className="flex flex-wrap gap-3">
              {details.latestFeedback.sentiment != null ? (
                <Badge variant={details.latestFeedback.sentiment}>
                  {details.latestFeedback.sentiment === "positive"
                    ? "Positive"
                    : details.latestFeedback.sentiment === "neutral"
                      ? "Neutral"
                      : "Negative"}
                </Badge>
              ) : null}
              {details.latestFeedback.isNew ? (
                <Badge variant="soft">New</Badge>
              ) : null}
            </div>
            <p className="text-base font-medium text-foreground">
              “{details.latestFeedback.quote}”
            </p>
            <div className="flex flex-wrap gap-3">
              <Button
                type="button"
                variant="op-tertiary"
                className="rounded-[2px]"
                aria-label={GUEST_PROFILE_OPEN_FEEDBACK_LABEL}
                onClick={() => {
                  onOpenFeedback(details.latestFeedback!.id)
                }}
              >
                {GUEST_PROFILE_OPEN_FEEDBACK_LABEL}
              </Button>
              {onStartRecovery != null ? (
                <Button
                  type="button"
                  variant="op-tertiary"
                  className="rounded-[2px]"
                  aria-label="Start recovery"
                  onClick={() => {
                    onStartRecovery(details.latestFeedback!.id)
                  }}
                >
                  Start recovery
                </Button>
              ) : null}
            </div>
          </>
        )}
      </Section>

      <Section title="Offers and campaigns">
        {!details.hasOffersOrCampaigns ? (
          <>
            <EmptySectionCopy
              title={offersEmpty.emptyTitle}
              helper={offersEmpty.emptyHelper}
            />
            <PendingButton label="View engagement history" />
          </>
        ) : null}
      </Section>

      <Section title="Internal notes" className="gap-[22px]">
        {details.recentNotes.length === 0 ? (
          <EmptySectionCopy
            title={GUEST_PROFILE_EMPTY_COPY.overviewRecentNotes.emptyTitle}
            helper={GUEST_PROFILE_EMPTY_COPY.overviewRecentNotes.emptyHelper}
          />
        ) : (
          <ul className="flex flex-col gap-3">
            {details.recentNotes.map((note) => (
              <li key={note.id} className="flex flex-col gap-1">
                <p className="text-sm font-medium text-foreground line-clamp-3">
                  {note.body}
                </p>
                <p className="text-xs text-muted-foreground">
                  {note.authorDisplayName} · {note.createdAtDisplay}
                </p>
              </li>
            ))}
          </ul>
        )}
        <div className="flex flex-col gap-3">
          <Textarea
            value={noteDraft}
            onChange={(event) => {
              onNoteDraftChange(event.target.value)
            }}
            rows={3}
            placeholder="Add a note about this guest or any follow-up taken…"
            disabled={noteBusy}
            className="min-h-0 resize-none rounded-[4px] border-border px-[13px] py-[15px] text-sm placeholder:text-muted-foreground dark:bg-transparent"
          />
          <Button
            type="button"
            variant="op-secondary"
            className="w-fit rounded-[2px]"
            disabled={!canAddNote}
            aria-disabled={!canAddNote}
            onClick={onCreateNote}
          >
            {GUEST_PROFILE_ADD_NOTE_LABEL}
          </Button>
          {noteCreateError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {noteCreateError}
            </p>
          ) : null}
        </div>
      </Section>

      <Section
        title="Recent activity"
        className="pb-[122px]"
        headerAction={
          <Button
            type="button"
            variant="op-tertiary"
            className="rounded-[2px]"
            onClick={onViewFullActivity}
          >
            View full activity
          </Button>
        }
      >
        {details.recentActivity.length === 0 ? (
          <EmptySectionCopy
            title={activityEmpty.emptyTitle}
            helper={activityEmpty.emptyHelper}
          />
        ) : (
          <ul className="flex flex-col gap-4">
            {details.recentActivity.map((event) => (
              <li
                key={`${event.at}-${event.description}`}
                className="flex flex-col gap-1.5"
              >
                <p className="text-sm font-semibold text-foreground">
                  {formatActivityTime(event.at)}
                </p>
                <p className="text-xs font-normal text-foreground">
                  {event.description}
                </p>
              </li>
            ))}
          </ul>
        )}
      </Section>
    </>
  )
}

/** Guest details — modal right Drawer (Figma 3381:84159). */
export function GuestDetailsDrawer({
  snapshot,
  mode,
  selectedLocationId,
  onOpenChange,
  onRetry,
  onNoteDraftChange,
  onCreateNote,
  onOpenFeedback,
  onStartRecovery,
}: GuestDetailsDrawerProps) {
  const navigate = useNavigate()

  const escalateToProfile = (handoff: GuestProfileLocationHandoff = {}) => {
    if (snapshot.guestId == null || selectedLocationId == null) {
      return
    }
    onOpenChange(false)
    navigate(
      operatorDashboardGuestProfilePath(
        mode,
        snapshot.guestId,
        selectedLocationId
      ),
      { state: handoff }
    )
  }

  return (
    <Drawer
      open={snapshot.isOpen}
      onOpenChange={onOpenChange}
      direction="right"
    >
      <DrawerContent
        className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}
      >
        <div className="flex min-h-0 flex-1 flex-col">
          {snapshot.loadStatus === "loading" ||
          snapshot.loadStatus === "idle" ? (
            <>
              <GuestDetailsDrawerHeader description="Loading Guest details" />
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
                  aria-label="Loading Guest details"
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
              <GuestDetailsDrawerHeader description="Guest details failed to load" />
              <div
                className={cn(
                  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                  "px-[22px] pb-[22px]"
                )}
              >
                <div className="flex min-h-48 flex-col items-center justify-center gap-3 text-center">
                  <p className="text-sm text-destructive" role="alert">
                    {snapshot.loadError ??
                      "Could not load Guest details. Please try again."}
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
              <GuestDetailsDrawerHeader
                details={snapshot.details}
                onViewFullProfile={() => {
                  escalateToProfile()
                }}
              />
              <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>
                <LoadedBody
                  details={snapshot.details}
                  noteDraft={snapshot.noteDraft}
                  noteCreateStatus={snapshot.noteCreateStatus}
                  noteCreateError={snapshot.noteCreateError}
                  onNoteDraftChange={onNoteDraftChange}
                  onCreateNote={() => {
                    void onCreateNote()
                  }}
                  onOpenFeedback={onOpenFeedback}
                  onStartRecovery={onStartRecovery}
                  onViewFullActivity={() => {
                    escalateToProfile({ tab: "activity" })
                  }}
                />
              </div>
            </>
          ) : null}
        </div>
      </DrawerContent>
    </Drawer>
  )
}
