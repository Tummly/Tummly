import { useState } from "react"
import { CalendarIcon, ChevronDownIcon, MessageSquareText } from "lucide-react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
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

type OperatorHomeLatestActivityProps = {
  activityByTab: OperatorHomeViewModel["activityByTab"]
  activityEmpty: OperatorHomeActivityEmpty
  dateRangeLabel: string
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
  return (
    <div className="flex items-start justify-between gap-4 border-b border-[#e1e1e1] px-6 py-6 last:border-b-0 dark:border-white/10">
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
              <p className="text-sm font-medium text-[#969595]">
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
          <div className="flex items-center gap-4">
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
        </div>
      </div>
      <p className="shrink-0 text-xs font-medium text-[#969595]">
        {formatRelativeTime(item.createdAt, nowMs)}
      </p>
    </div>
  )
}

/** Figma Latest activity — tabs + date range + honest empty shell. */
export function OperatorHomeLatestActivity({
  activityByTab,
  activityEmpty,
  dateRangeLabel,
  nowMs = Date.now(),
  onViewFeedback,
}: OperatorHomeLatestActivityProps) {
  const [activeTab, setActiveTab] =
    useState<OperatorHomeActivityTabId>("all")
  const items = activityByTab[activeTab]
  const allEmpty = Object.values(activityByTab).every(
    (list) => list.length === 0
  )

  return (
    <section className="flex flex-col gap-6 rounded-[10px] bg-[#f8f8f8] py-5 dark:bg-white/5">
      <div className="flex items-start justify-between gap-4 border-b border-[#e1e1e1] px-6 pb-5 dark:border-white/10">
        <div>
          <h2 className="text-xl font-bold text-foreground">Latest activity</h2>
          <p className="mt-2 text-sm font-medium text-foreground/70">
            Recent feedback, guest sign-ups, offer claims, redemptions and
            campaign activity.
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          disabled
          aria-disabled
          aria-label={`${dateRangeLabel} (unavailable)`}
          className="h-auto min-h-0 shrink-0 gap-1.5 rounded-lg border-[#e6e6e6] p-[11px] text-xs font-medium text-foreground opacity-60 dark:border-white/15 dark:bg-transparent"
        >
          <CalendarIcon data-icon="inline-start" aria-hidden />
          {dateRangeLabel}
          <ChevronDownIcon data-icon="inline-end" aria-hidden />
        </Button>
      </div>

      {!allEmpty ? (
        <div className="border-b border-[#e1e1e1] dark:border-white/10">
          <div
            role="tablist"
            aria-label="Latest activity filters"
            className="flex h-[27px] items-start gap-2.5 px-6"
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
                    "h-full min-h-0 rounded-none border-transparent px-3.5 pr-4 pb-2.5 text-sm shadow-none hover:bg-transparent focus-visible:border-transparent focus-visible:ring-0",
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
      ) : null}

      <div role="tabpanel">
        {items.length === 0 ? (
          <div className="flex min-h-[220px] flex-col items-center justify-center gap-2.5 px-6 py-10 text-center">
            <p className="text-base font-semibold text-[#4b4b4b] dark:text-white/70">
              {allEmpty
                ? activityEmpty.emptyCopy
                : activeTab === "offers" ||
                    activeTab === "campaigns" ||
                    activeTab === "guests"
                  ? `No ${activeTab} activity yet.`
                  : "No recent feedback yet."}
            </p>
            {allEmpty ? (
              <p className="max-w-[324px] text-sm font-medium leading-[18px] text-[#999]">
                {activityEmpty.emptyHelper}
              </p>
            ) : null}
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
