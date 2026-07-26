import { useState } from "react"
import { MessageSquare } from "lucide-react"

import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  LATEST_ACTIVITY_EMPTY_SHELL_CLASS,
  LATEST_ACTIVITY_FOOTER_CLASS,
  LATEST_ACTIVITY_HEADER_CLASS,
  LATEST_ACTIVITY_ROW_CLASS,
  LATEST_ACTIVITY_TAB_TOUCH_CLASS,
  LATEST_ACTIVITY_TABLIST_CLASS,
  LATEST_ACTIVITY_TABLIST_SCROLL_CLASS,
  LATEST_ACTIVITY_TITLE_CLASS,
  LATEST_ACTIVITY_VIEW_ALL_LABEL,
  OPERATOR_HOME_CARD_STACK_CLASS,
  OPERATOR_HOME_EMPTY_COPY_STACK_CLASS,
  OPERATOR_HOME_EMPTY_HELPER_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import { cn } from "@/lib/utils"
import type {
  OperatorHomeActivityEmpty,
  OperatorHomeActivityItem,
  OperatorHomeActivityTabId,
  OperatorHomeViewModel,
} from "@/types/operatorHome"

const TABS: Array<{ id: OperatorHomeActivityTabId; label: string }> = [
  { id: "all", label: "All" },
  { id: "feedback", label: "Feedback" },
  { id: "guests", label: "Guests" },
  { id: "offers", label: "Offers" },
  { id: "campaigns", label: "Campaigns" },
]

/** Row text CTAs — paint/geometry from `op-link`; keep disabled opacity. */
const ACTIVITY_ROW_CTA_CLASS = "font-medium disabled:opacity-40"

type HomeLatestActivityProps = {
  activityByTab: OperatorHomeViewModel["activityByTab"]
  activityEmpty: OperatorHomeActivityEmpty
  nowMs?: number
  onViewFeedback?: (feedbackId: number) => void
  onViewGuest?: (locationGuestId: number) => void
  /** Full activity / Feedback page — unavailable until that surface ships. */
  onViewAllActivity?: () => void
}

function ActivityRow({
  item,
  nowMs,
  onViewFeedback,
  onViewGuest,
}: {
  item: OperatorHomeActivityItem
  nowMs: number
  onViewFeedback?: (feedbackId: number) => void
  onViewGuest?: (locationGuestId: number) => void
}) {
  const timestamp = (
    <p className="text-xs font-medium text-muted-foreground dark:text-[#7c7c7c]">
      {formatRelativeTime(item.createdAt, nowMs)}
    </p>
  )

  if (item.kind === "guest-joined") {
    return (
      <div className={LATEST_ACTIVITY_ROW_CLASS}>
        <div className="flex min-w-0 items-start gap-2">
          <Avatar className="size-8 after:hidden">
            <AvatarFallback className="bg-[#f4f4f4] text-xs font-medium text-[#7c7c7c] dark:bg-[#202020]">
              {item.initials}
            </AvatarFallback>
          </Avatar>
          <div className="flex min-w-0 flex-col gap-3.5">
            <div className="flex min-w-0 flex-col gap-0.5">
              <div className="flex flex-wrap items-start gap-2">
                <p className="text-sm font-medium text-foreground">
                  {item.headline}
                </p>
                <Badge variant="soft">{item.consentLabel}</Badge>
              </div>
              <p className="text-sm font-medium text-muted-foreground">
                {item.joinSourceLabel}
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-4">
              <Button
                type="button"
                variant="op-link"
                size={"link-sm"}
                className={ACTIVITY_ROW_CTA_CLASS}
                disabled={!item.canViewGuest}
                aria-disabled={!item.canViewGuest}
                aria-label={
                  item.canViewGuest ? "View guest" : "View guest (unavailable)"
                }
                onClick={() => {
                  if (!item.canViewGuest || onViewGuest == null) {
                    return
                  }
                  onViewGuest(item.locationGuestId)
                }}
              >
                View guest
              </Button>
              <Button
                type="button"
                variant="op-link"
                size={"link-sm"}
                className={ACTIVITY_ROW_CTA_CLASS}
                disabled={!item.canSendOffer}
                aria-disabled={!item.canSendOffer}
                aria-label={
                  item.canSendOffer ? "Send offer" : "Send offer (unavailable)"
                }
              >
                Send offer
              </Button>
            </div>
            <div className="sm:hidden">{timestamp}</div>
          </div>
        </div>
        <div className="hidden shrink-0 sm:block">{timestamp}</div>
      </div>
    )
  }

  return (
    <div className={LATEST_ACTIVITY_ROW_CLASS}>
      <div className="flex min-w-0 items-start gap-2">
        <div className="flex size-8 shrink-0 items-center justify-center rounded-[14px] bg-[#f4f4f4] dark:bg-[#202020]">
          <MessageSquare className="size-4 text-primary" aria-hidden />
        </div>
        <div className="flex min-w-0 flex-col gap-3.5">
          <div className="flex min-w-0 flex-col gap-0.5">
            <p className="text-sm font-medium text-foreground">
              {item.comment}
              {item.sentiment === "positive" ? (
                <Badge variant="positive" className="ml-2 align-middle">
                  Positive
                </Badge>
              ) : null}
              {item.sentiment === "neutral" ? (
                <Badge variant="neutral" className="ml-2 align-middle">
                  Neutral
                </Badge>
              ) : null}
              {item.sentiment === "negative" ? (
                <Badge variant="negative" className="ml-2 align-middle">
                  Negative
                </Badge>
              ) : null}
            </p>
            <p className="text-sm font-medium text-muted-foreground">
              {item.guestName}
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-4">
            <Button
              type="button"
              variant="op-link"
              size={"link-sm"}
              className={ACTIVITY_ROW_CTA_CLASS}
              disabled={!item.canViewFeedback}
              aria-disabled={!item.canViewFeedback}
              aria-label={
                item.canViewFeedback
                  ? "View feedback"
                  : "View feedback (unavailable)"
              }
              onClick={() => {
                if (!item.canViewFeedback || onViewFeedback == null) {
                  return
                }
                onViewFeedback(item.feedbackId)
              }}
            >
              View feedback
            </Button>
            <Button
              type="button"
              variant="op-link"
              size={"link-sm"}
              className={ACTIVITY_ROW_CTA_CLASS}
              disabled={!item.canViewGuest}
              aria-disabled={!item.canViewGuest}
              aria-label={
                item.canViewGuest ? "View guest" : "View guest (unavailable)"
              }
              onClick={() => {
                if (
                  !item.canViewGuest ||
                  item.locationGuestId == null ||
                  onViewGuest == null
                ) {
                  return
                }
                onViewGuest(item.locationGuestId)
              }}
            >
              View guest
            </Button>
          </div>
          <div className="sm:hidden">{timestamp}</div>
        </div>
      </div>
      <div className="hidden shrink-0 sm:block">{timestamp}</div>
    </div>
  )
}

/** Figma Latest activity — tabs + honest empty shell + View all footer. */
export function HomeLatestActivity({
  activityByTab,
  activityEmpty,
  nowMs = Date.now(),
  onViewFeedback,
  onViewGuest,
  onViewAllActivity,
}: HomeLatestActivityProps) {
  const [activeTab, setActiveTab] =
    useState<OperatorHomeActivityTabId>("all")
  const items = activityByTab[activeTab]
  const allEmpty = Object.values(activityByTab).every(
    (list) => list.length === 0
  )
  const canViewAllActivity = onViewAllActivity != null

  return (
    <section className={OPERATOR_HOME_CARD_STACK_CLASS}>
      <div className={LATEST_ACTIVITY_HEADER_CLASS}>
        <div className={OPERATOR_HOME_HEADER_COPY_CLASS}>
          <h2 className={LATEST_ACTIVITY_TITLE_CLASS}>Latest activity</h2>
          <p className={OPERATOR_HOME_SUBTITLE_CLASS}>
            Recent feedback, guest sign-ups, offer claims, redemptions and
            campaign activity.
          </p>
        </div>
      </div>

      {!allEmpty ? (
        <div className="border-b border-[#e5e5e5] dark:border-[#262626]">
          <div className={LATEST_ACTIVITY_TABLIST_SCROLL_CLASS}>
            <div
              role="tablist"
              aria-label="Latest activity filters"
              className={LATEST_ACTIVITY_TABLIST_CLASS}
            >
              {TABS.map((tab) => {
                const selected = tab.id === activeTab
                return (
                  <Button
                    key={tab.id}
                    type="button"
                    variant="op-ghost"
                    role="tab"
                    aria-selected={selected}
                    className={cn(
                      LATEST_ACTIVITY_TAB_TOUCH_CLASS,
                      // Figma: padding 0 16px 10px 14px; -mb-px sits underline on section border-b
                      "-mb-px border-b-2 border-transparent pt-0 pr-4 pb-2.5 pl-3.5 text-sm focus-visible:border-transparent focus-visible:ring-0",
                      selected
                        ? "border-b-primary font-semibold text-foreground"
                        : "font-medium text-[#a6a6a6]"
                    )}
                    onClick={() => setActiveTab(tab.id)}
                  >
                    {tab.label}
                  </Button>
                )
              })}
            </div>
          </div>
        </div>
      ) : null}

      <div role="tabpanel" className="flex flex-col gap-6">
        {items.length === 0 ? (
          <div
            className={cn(
              LATEST_ACTIVITY_EMPTY_SHELL_CLASS,
              allEmpty ? null : "py-10"
            )}
          >
            <div className={OPERATOR_HOME_EMPTY_COPY_STACK_CLASS}>
              <p className={OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS}>
                {allEmpty
                  ? activityEmpty.emptyCopy
                  : activeTab === "offers" ||
                    activeTab === "campaigns" ||
                    activeTab === "guests"
                    ? `No ${activeTab} activity yet.`
                    : "No recent feedback yet."}
              </p>
              {allEmpty ? (
                <p className={OPERATOR_HOME_EMPTY_HELPER_CLASS}>
                  {activityEmpty.emptyHelper}
                </p>
              ) : null}
            </div>
          </div>
        ) : (
          <>
            <div>
              {items.map((item) => (
                <ActivityRow
                  key={item.id}
                  item={item}
                  nowMs={nowMs}
                  onViewFeedback={onViewFeedback}
                  onViewGuest={onViewGuest}
                />
              ))}
            </div>
            <div className={LATEST_ACTIVITY_FOOTER_CLASS}>
              <Button
                type="button"
                variant="op-secondary"
                disabled={!canViewAllActivity}
                aria-disabled={!canViewAllActivity}
                aria-label={
                  canViewAllActivity
                    ? LATEST_ACTIVITY_VIEW_ALL_LABEL
                    : `${LATEST_ACTIVITY_VIEW_ALL_LABEL} (unavailable)`
                }
                onClick={onViewAllActivity}
              >
                {LATEST_ACTIVITY_VIEW_ALL_LABEL}
              </Button>
            </div>
          </>
        )}
      </div>
    </section>
  )
}
