import { useEffect, useState } from "react"
import {
  ArrowUpIcon,
  HistoryIcon,
  Maximize2Icon,
  Minimize2Icon,
  PlusCircleIcon,
  XIcon,
} from "lucide-react"

import { AiAssistantChangeScopeDialog } from "@/components/dashboard/operator/AiAssistantChangeScopeDialog"
import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Textarea } from "@/components/ui/textarea"
import {
  assistantDrawerContentClass,
  paintsAssistantExpand,
} from "@/lib/operatorAiAssistant/assistantDrawerPresentation"
import type { OperatorAiAssistantSnapshot } from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { OPERATOR_RIGHT_DRAWER_BODY_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type AiAssistantDrawerProps = {
  snapshot: OperatorAiAssistantSnapshot
  sidebarCollapsed: boolean
  onOpenChange: (open: boolean) => void
  onStartNewChat: () => void
  onOpenRecent: () => void
  onExpand: () => void
  onLeaveExpand: () => void
  onOpenChangeScope: () => void
  onChangeScopeOpenChange: (open: boolean) => void
  onChangeScopeDraftLocation: (locationId: number) => void
  onChangeScopeDraftReportingPeriod: (range: HomePerformanceDateRange) => void
  onApplyChangeScope: () => void
}

const HEADER_TEXT_ACTION_CLASS =
  "h-auto min-h-11 gap-1.5 rounded-op-sm px-0 py-0 text-sm font-normal text-op-text-primary hover:bg-transparent md:min-h-0"

const LG_VIEWPORT_QUERY = "(min-width: 1024px)"

function readViewportAtLeastLg(): boolean {
  if (typeof window === "undefined") {
    return true
  }
  return window.matchMedia(LG_VIEWPORT_QUERY).matches
}

/** Operator AI Assistant right Drawer. Figma 3454:56016 / Expand 3428:32355. */
export function AiAssistantDrawer({
  snapshot,
  sidebarCollapsed,
  onOpenChange,
  onStartNewChat,
  onOpenRecent,
  onExpand,
  onLeaveExpand,
  onOpenChangeScope,
  onChangeScopeOpenChange,
  onChangeScopeDraftLocation,
  onChangeScopeDraftReportingPeriod,
  onApplyChangeScope,
}: AiAssistantDrawerProps) {
  const [viewportAtLeastLg, setViewportAtLeastLg] = useState(readViewportAtLeastLg)
  const paintExpanded = paintsAssistantExpand({
    widthMode: snapshot.widthMode,
    viewportAtLeastLg,
  })

  useEffect(() => {
    const media = window.matchMedia(LG_VIEWPORT_QUERY)
    const sync = () => {
      const matches = media.matches
      setViewportAtLeastLg(matches)
      if (!matches) {
        onLeaveExpand()
      }
    }
    sync()
    media.addEventListener("change", sync)
    return () => {
      media.removeEventListener("change", sync)
    }
  }, [onLeaveExpand])

  useEffect(() => {
    if (!snapshot.drawerOpen || !paintExpanded || snapshot.changeScopeDialog.open) {
      return
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return
      }
      event.preventDefault()
      onOpenChange(false)
    }
    window.addEventListener("keydown", onKeyDown)
    return () => {
      window.removeEventListener("keydown", onKeyDown)
    }
  }, [
    onOpenChange,
    paintExpanded,
    snapshot.changeScopeDialog.open,
    snapshot.drawerOpen,
  ])

  return (
    <>
      <Drawer
        open={snapshot.drawerOpen}
        onOpenChange={onOpenChange}
        direction="right"
        modal={!paintExpanded}
        dismissible={!paintExpanded}
      >
        <DrawerContent
          className={assistantDrawerContentClass({
            widthMode: snapshot.widthMode,
            viewportAtLeastLg,
            sidebarCollapsed,
          })}
          overlayClassName={
            paintExpanded ? "hidden pointer-events-none" : undefined
          }
          data-assistant-width={paintExpanded ? "expanded" : "collapsed"}
        >
          <div className="flex min-h-0 flex-1 flex-col pt-[22px]">
            <div className="flex shrink-0 flex-col gap-1.5 px-[22px] pb-[22px]">
              <div className="flex items-start justify-between gap-[22px]">
                <div className="flex min-w-0 flex-wrap items-center gap-[22px]">
                  <Button
                    type="button"
                    variant="op-ghost"
                    className={HEADER_TEXT_ACTION_CLASS}
                    onClick={onStartNewChat}
                  >
                    <PlusCircleIcon className="size-[18px]" aria-hidden />
                    New chat
                  </Button>
                  <Button
                    type="button"
                    variant="op-ghost"
                    className={HEADER_TEXT_ACTION_CLASS}
                    onClick={onOpenRecent}
                  >
                    <HistoryIcon className="size-[18px]" aria-hidden />
                    Recent
                  </Button>
                </div>
                <div className="flex shrink-0 items-center gap-[22px]">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="hidden size-7 rounded-[8px] text-op-text-primary hover:bg-transparent lg:inline-flex"
                    aria-label={
                      paintExpanded
                        ? "Collapse AI Assistant"
                        : "Expand AI Assistant"
                    }
                    onClick={paintExpanded ? onLeaveExpand : onExpand}
                  >
                    {paintExpanded ? (
                      <Minimize2Icon className="size-6" aria-hidden />
                    ) : (
                      <Maximize2Icon className="size-6" aria-hidden />
                    )}
                  </Button>
                  <DrawerClose asChild>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="size-11 shrink-0 rounded-[2px] bg-op-color-gray-70 hover:bg-op-color-gray-85 md:size-[42px] dark:bg-op-color-gray-950 dark:hover:bg-op-color-gray-950"
                      aria-label="Close AI Assistant"
                    >
                      <XIcon className="size-[18px]" aria-hidden />
                    </Button>
                  </DrawerClose>
                </div>
              </div>

              <div className="flex items-center justify-between gap-3">
                <p
                  className="min-w-0 truncate text-sm leading-5 font-normal text-[var(--op-color-gray-550)]"
                  title={snapshot.headerStatusLine}
                  aria-label={snapshot.headerStatusLine}
                >
                  {snapshot.headerStatusLine}
                </p>
                <Button
                  type="button"
                  variant="op-tertiary"
                  className="h-auto min-h-11 shrink-0 px-[17px] md:h-[42px] md:min-h-[42px]"
                  onClick={onOpenChangeScope}
                >
                  Change Scope
                </Button>
              </div>
            </div>

            <DrawerTitle className="sr-only">AI Assistant</DrawerTitle>
            <DrawerDescription className="sr-only">
              Ask about feedback, guests, offers, campaigns or performance.
            </DrawerDescription>

            <div
              className={cn(
                OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                "flex flex-col px-[30px]"
              )}
            >
              <div className="flex flex-1 flex-col items-center justify-center gap-4 py-10">
                <AiIcon size={48} />
                <div className="flex flex-col items-center gap-3 text-center">
                  <p className="text-lg font-medium text-[var(--op-color-gray-625)]">
                    {snapshot.greeting.hello}
                  </p>
                  <p className="bg-gradient-to-r from-[var(--op-color-green-600)] to-[var(--op-color-blue-600)] bg-clip-text text-[26px] leading-8 font-medium text-transparent dark:text-transparent">
                    {snapshot.greeting.headline}
                  </p>
                </div>
                <p className="max-w-[365px] text-center text-base leading-[22px] font-normal text-[var(--op-color-gray-550)]">
                  {snapshot.greeting.body}
                </p>
              </div>
            </div>

            <div className="shrink-0 px-[30px] pb-[22px]">
              <div className="flex flex-col gap-3 rounded-[8px] border border-[var(--op-color-gray-200)] bg-op-surface-secondary p-[21px] dark:border-[var(--op-color-gray-875)] dark:bg-[#141414]">
                <Textarea
                  aria-label="Ask AI Assistant"
                  placeholder="Ask AI Assistant..."
                  className="min-h-[72px] resize-none border-0 bg-transparent p-0 shadow-none focus-visible:ring-0 dark:bg-transparent"
                />
                <div className="flex justify-end">
                  <Button
                    type="button"
                    size="icon"
                    disabled
                    aria-label="Send"
                    className="size-10 min-h-11 min-w-11 rounded-full md:min-h-10 md:min-w-10"
                  >
                    <ArrowUpIcon className="size-6" aria-hidden />
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </DrawerContent>
      </Drawer>
      {snapshot.changeScopeDialog.open ? (
        <AiAssistantChangeScopeDialog
          dialog={snapshot.changeScopeDialog}
          onOpenChange={onChangeScopeOpenChange}
          onDraftLocationChange={onChangeScopeDraftLocation}
          onDraftReportingPeriodChange={onChangeScopeDraftReportingPeriod}
          onApply={onApplyChangeScope}
        />
      ) : null}
    </>
  )
}
