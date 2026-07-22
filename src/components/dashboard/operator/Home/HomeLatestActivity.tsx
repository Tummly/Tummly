import { useState } from "react"
import {
  CalendarIcon,
  ChevronDownIcon,
  MessageSquareText,
  RefreshCw,
} from "lucide-react"

import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  LATEST_ACTIVITY_EMPTY_SHELL_CLASS,
  LATEST_ACTIVITY_HEADER_CLASS,
  LATEST_ACTIVITY_ROW_CLASS,
  LATEST_ACTIVITY_TAB_TOUCH_CLASS,
  LATEST_ACTIVITY_TABLIST_CLASS,
  LATEST_ACTIVITY_TABLIST_SCROLL_CLASS,
  LATEST_ACTIVITY_TITLE_CLASS,
  OPERATOR_HOME_CARD_STACK_CLASS,
  OPERATOR_HOME_CHROME_BUTTON_CLASS,
  OPERATOR_HOME_CHROME_ICON_CLASS,
  OPERATOR_HOME_EMPTY_COPY_STACK_CLASS,
  OPERATOR_HOME_EMPTY_HELPER_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL } from "@/lib/operatorHome/homePerformanceDateRange"
import {
  PERFORMANCE_DATE_BUTTON_CLASS,
  PERFORMANCE_DATE_BUTTON_DISABLED_CLASS,
  PERFORMANCE_DATE_ICON_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
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

type HomeLatestActivityProps = {
  activityByTab: OperatorHomeViewModel["activityByTab"]
  activityEmpty: OperatorHomeActivityEmpty
  nowMs?: number
  onViewFeedback?: (feedbackId: number) => void
}

function ActivityRow({
  item,
  nowMs,
  onViewFeedback,
}: {
  item: OperatorHomeActivityItem
  nowMs: number
  onViewFeedback?: (feedbackId: number) => void
}) {
  const timestamp = (
    <p className="text-xs font-medium text-muted-foreground">
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
                variant="ghost"
                className="h-auto px-0 py-0 text-sm font-medium text-foreground hover:bg-transparent disabled:opacity-40"
                disabled={!item.canViewGuest}
                aria-disabled={!item.canViewGuest}
                aria-label={
                  item.canViewGuest ? "View guest" : "View guest (unavailable)"
                }
              >
                View guest
              </Button>
              <Button
                type="button"
                variant="ghost"
                className="h-auto px-0 py-0 text-sm font-medium text-foreground hover:bg-transparent disabled:opacity-40"
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
        <div className="flex size-8 shrink-0 items-center justify-center rounded-[14px] bg-[#f4f4f4] dark:bg-white/10">
          <MessageSquareText className="size-4 text-primary" aria-hidden />
        </div>
        <div className="flex min-w-0 flex-col gap-3">
          <div className="flex flex-wrap items-start gap-2">
            <div className="min-w-0">
              <p className="text-sm font-medium text-foreground">
                {item.comment}
              </p>
              <p className="text-sm font-medium text-muted-foreground">
                {item.guestName}
              </p>
            </div>
            {item.sentiment === "positive" ? (
              <Badge variant="positive">Positive</Badge>
            ) : null}
            {item.sentiment === "neutral" ? (
              <Badge variant="neutral">Neutral</Badge>
            ) : null}
            {item.sentiment === "negative" ? (
              <Badge variant="negative">Negative</Badge>
            ) : null}
          </div>
          <div className="flex flex-wrap items-center gap-4">
            <Button
              type="button"
              variant="ghost"
              className="h-auto px-0 py-0 text-sm font-medium text-foreground hover:bg-transparent disabled:opacity-40"
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
              variant="ghost"
              className="h-auto px-0 py-0 text-sm font-medium text-foreground hover:bg-transparent disabled:opacity-40"
              disabled={!item.canViewGuest}
              aria-disabled={!item.canViewGuest}
              aria-label={
                item.canViewGuest ? "View guest" : "View guest (unavailable)"
              }
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

/** Figma Latest activity — tabs + date range + honest empty shell. */
export function HomeLatestActivity({
  activityByTab,
  activityEmpty,
  nowMs = Date.now(),
  onViewFeedback,
}: HomeLatestActivityProps) {
  const [activeTab, setActiveTab] =
    useState<OperatorHomeActivityTabId>("all")
  const items = activityByTab[activeTab]
  const allEmpty = Object.values(activityByTab).every(
    (list) => list.length === 0
  )
  // Unwired date chrome — static label matches the default Home performance preset.
  const dateRangeLabel = HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL

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
        {allEmpty ? (
          <span className={OPERATOR_HOME_CHROME_BUTTON_CLASS} aria-hidden>
            <RefreshCw className={OPERATOR_HOME_CHROME_ICON_CLASS} />
          </span>
        ) : (
          <Button
            type="button"
            variant="outline"
            disabled
            aria-disabled
            aria-label={`${dateRangeLabel} (unavailable)`}
            className={cn(
              PERFORMANCE_DATE_BUTTON_CLASS,
              PERFORMANCE_DATE_BUTTON_DISABLED_CLASS
            )}
          >
            <CalendarIcon
              className={PERFORMANCE_DATE_ICON_CLASS}
              data-icon="inline-start"
              aria-hidden
            />
            {dateRangeLabel}
            <ChevronDownIcon
              className={PERFORMANCE_DATE_ICON_CLASS}
              data-icon="inline-end"
              aria-hidden
            />
          </Button>
        )}
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
                    variant="ghost"
                    role="tab"
                    aria-selected={selected}
                    className={cn(
                      LATEST_ACTIVITY_TAB_TOUCH_CLASS,
                      "rounded-none border-transparent px-3.5 pr-4 pb-2.5 text-sm shadow-none hover:bg-transparent focus-visible:border-transparent focus-visible:ring-0",
                      selected
                        ? "border-b-2 border-b-primary font-semibold text-foreground"
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

      <div role="tabpanel">
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
          items.map((item) => (
            <ActivityRow
              key={item.id}
              item={item}
              nowMs={nowMs}
              onViewFeedback={onViewFeedback}
            />
          ))
        )}
      </div>
    </section>
  )
}
