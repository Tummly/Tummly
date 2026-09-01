import { Link } from "react-router-dom"
import { MessageSquareIcon } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  formatLocationGuestActivityChecklistStatus,
  LOCATION_DETAIL_ACTION_BUTTON_CLASS,
  LOCATION_DETAIL_CARD_CLASS,
  LOCATION_DETAIL_LATEST_FEEDBACK_ACTION_CLASS,
  LOCATION_DETAIL_LATEST_FEEDBACK_ACTIONS_CLASS,
  LOCATION_DETAIL_LATEST_FEEDBACK_ICON_CLASS,
  LOCATION_DETAIL_LATEST_FEEDBACK_ROW_CLASS,
  LOCATION_DETAIL_METRIC_DIVIDER_CLASS,
  LOCATION_DETAIL_METRIC_FIELD_CLASS,
  LOCATION_DETAIL_METRIC_LABEL_CLASS,
  LOCATION_DETAIL_METRIC_PAIR_CLASS,
  LOCATION_DETAIL_METRIC_STACK_CLASS,
  LOCATION_DETAIL_METRIC_VALUE_CLASS,
  LOCATION_DETAIL_PAGE_COPY,
  LOCATION_DETAIL_SECTION_SUBTITLE_CLASS,
  LOCATION_DETAIL_SECTION_TITLE_CLASS,
  LOCATION_GUEST_ACTIVITY_CHECKLIST_LABELS,
  LOCATION_GUEST_ACTIVITY_CHECKLIST_ROWS,
  type LocationDetailLatestFeedbackRow,
  type LocationGuestActivityChecklistItemId,
  type LocationGuestActivityChecklistStatusId,
} from "@/lib/operatorLocations/locationDetailPresentation"
import { locationDetailRecoveryFeedbackPath } from "@/lib/operatorLocations/locationDetailApi"
import { cn } from "@/lib/utils"

type LocationDetailGuestActivitySectionProps = {
  guestActivityChecklist: Record<
    LocationGuestActivityChecklistItemId,
    LocationGuestActivityChecklistStatusId
  >
  latestFeedbackRows: LocationDetailLatestFeedbackRow[]
  guestsPath: string
  feedbackPath: string
  redemptionsPath: string
  guestProfilePathFor: (locationGuestId: number) => string
}

function SentimentBadge({
  sentiment,
}: {
  sentiment: LocationDetailLatestFeedbackRow["sentiment"]
}) {
  if (sentiment == null) {
    return null
  }

  const label =
    sentiment === "positive"
      ? "Positive"
      : sentiment === "neutral"
        ? "Neutral"
        : "Negative"

  return (
    <Badge variant={sentiment} className="shrink-0">
      {label}
    </Badge>
  )
}

export function LocationDetailGuestActivitySection({
  guestActivityChecklist,
  latestFeedbackRows,
  guestsPath,
  feedbackPath,
  redemptionsPath,
  guestProfilePathFor,
}: LocationDetailGuestActivitySectionProps) {
  const copy = LOCATION_DETAIL_PAGE_COPY

  return (
    <section
      className={cn(LOCATION_DETAIL_CARD_CLASS, "gap-7")}
      aria-label={copy.guestActivityTitle}
    >
      <div className="flex flex-col gap-2">
        <h2 className={LOCATION_DETAIL_SECTION_TITLE_CLASS}>
          {copy.guestActivityTitle}
        </h2>
        <p className={LOCATION_DETAIL_SECTION_SUBTITLE_CLASS}>
          {copy.guestActivitySubtitle}
        </p>
      </div>

      <div className={LOCATION_DETAIL_METRIC_STACK_CLASS}>
        {LOCATION_GUEST_ACTIVITY_CHECKLIST_ROWS.map(
          ([leftId, rightId], index) => (
            <div key={leftId} className="flex flex-col gap-5">
              <div className={LOCATION_DETAIL_METRIC_PAIR_CLASS}>
                <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                  <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                    {LOCATION_GUEST_ACTIVITY_CHECKLIST_LABELS[leftId]}
                  </p>
                  <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                    {formatLocationGuestActivityChecklistStatus(
                      guestActivityChecklist[leftId]
                    )}
                  </p>
                </div>
                {rightId != null ? (
                  <div className={LOCATION_DETAIL_METRIC_FIELD_CLASS}>
                    <p className={LOCATION_DETAIL_METRIC_LABEL_CLASS}>
                      {LOCATION_GUEST_ACTIVITY_CHECKLIST_LABELS[rightId]}
                    </p>
                    <p className={LOCATION_DETAIL_METRIC_VALUE_CLASS}>
                      {formatLocationGuestActivityChecklistStatus(
                        guestActivityChecklist[rightId]
                      )}
                    </p>
                  </div>
                ) : (
                  <div className="hidden sm:block" aria-hidden />
                )}
              </div>
              {index < LOCATION_GUEST_ACTIVITY_CHECKLIST_ROWS.length - 1 ? (
                <hr className={LOCATION_DETAIL_METRIC_DIVIDER_CLASS} />
              ) : null}
            </div>
          )
        )}
      </div>

      <hr className={LOCATION_DETAIL_METRIC_DIVIDER_CLASS} />

      <div className="flex w-full flex-col gap-0">
        <h3 className="m-0 text-base font-bold text-op-text-primary">
          {copy.latestFeedbackTitle}
        </h3>

        {latestFeedbackRows.length === 0 ? (
          <div className="flex min-h-[120px] flex-col justify-center gap-2 py-6">
            <p className="m-0 text-base font-medium text-op-text-primary">
              {copy.latestFeedbackEmptyTitle}
            </p>
            <p className="m-0 max-w-[480px] text-sm font-medium text-op-text-muted">
              {copy.latestFeedbackEmptyHelper}
            </p>
          </div>
        ) : (
          latestFeedbackRows.map((row) => (
            <article key={row.id} className={LOCATION_DETAIL_LATEST_FEEDBACK_ROW_CLASS}>
              <div className="flex min-w-0 flex-1 items-start gap-2">
                <div className={LOCATION_DETAIL_LATEST_FEEDBACK_ICON_CLASS}>
                  <MessageSquareIcon
                    className="size-4 text-op-text-primary"
                    aria-hidden
                  />
                </div>
                <div className="flex min-w-0 flex-1 flex-col gap-3.5">
                  <div className="flex flex-wrap items-start gap-2">
                    <div className="min-w-0">
                      <p className="m-0 text-sm font-medium text-op-text-primary">
                        {row.comment}
                      </p>
                      <p className="m-0 text-sm font-medium text-op-text-muted">
                        {row.guestName}
                      </p>
                    </div>
                    <SentimentBadge sentiment={row.sentiment} />
                  </div>
                  <div className={LOCATION_DETAIL_LATEST_FEEDBACK_ACTIONS_CLASS}>
                    {row.canStartRecovery ? (
                      <Button
                        type="button"
                        variant="link"
                        className={LOCATION_DETAIL_LATEST_FEEDBACK_ACTION_CLASS}
                        asChild
                      >
                        <Link
                          to={locationDetailRecoveryFeedbackPath(
                            feedbackPath,
                            row.feedbackId
                          )}
                        >
                          {copy.startRecovery}
                        </Link>
                      </Button>
                    ) : null}
                    <Button
                      type="button"
                      variant="link"
                      className={LOCATION_DETAIL_LATEST_FEEDBACK_ACTION_CLASS}
                      asChild
                    >
                      <Link to={feedbackPath}>{copy.viewFeedback}</Link>
                    </Button>
                    {row.locationGuestId != null ? (
                      <Button
                        type="button"
                        variant="link"
                        className={LOCATION_DETAIL_LATEST_FEEDBACK_ACTION_CLASS}
                        asChild
                      >
                        <Link to={guestProfilePathFor(row.locationGuestId)}>
                          {copy.viewGuest}
                        </Link>
                      </Button>
                    ) : null}
                  </div>
                </div>
              </div>
              <p className="m-0 shrink-0 text-xs font-medium text-op-text-muted">
                {row.timeLabel}
              </p>
            </article>
          ))
        )}
      </div>

      <div className="flex flex-wrap gap-3">
        <Button
          type="button"
          variant="op-secondary"
          className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
          asChild
        >
          <Link to={guestsPath}>{copy.viewGuests}</Link>
        </Button>
        <Button
          type="button"
          variant="op-secondary"
          className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
          asChild
        >
          <Link to={feedbackPath}>{copy.viewFeedback}</Link>
        </Button>
        <Button
          type="button"
          variant="op-secondary"
          className={LOCATION_DETAIL_ACTION_BUTTON_CLASS}
          asChild
        >
          <Link to={redemptionsPath}>{copy.viewRedemptions}</Link>
        </Button>
      </div>
    </section>
  )
}
