import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from "react"
import {
  ArrowUpIcon,
  HistoryIcon,
  Maximize2Icon,
  Minimize2Icon,
  PlusCircleIcon,
  ThumbsDownIcon,
  ThumbsUpIcon,
  XIcon,
} from "lucide-react"

import { AiAssistantChangeScopeDialog } from "@/components/dashboard/operator/AiAssistantChangeScopeDialog"
import { AiAssistantConversationList } from "@/components/dashboard/operator/AiAssistantConversationList"
import { AiAssistantDeleteDialog } from "@/components/dashboard/operator/AiAssistantDeleteDialog"
import { AiAssistantMicChrome } from "@/components/dashboard/operator/AiAssistantMicChrome"
import { useEmptyComposerPlaceholder } from "@/components/dashboard/operator/useEmptyComposerPlaceholder"
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
import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantHelpfulFill,
  OperatorAiAssistantMessage,
  OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { GuestMicAudioLevelSource } from "@/lib/guestFeedback/guestMicAudioLevel"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { OPERATOR_RIGHT_DRAWER_BODY_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"
import { cn } from "@/lib/utils"

type AiAssistantDrawerProps = {
  snapshot: OperatorAiAssistantSnapshot
  sidebarCollapsed: boolean
  onOpenChange: (open: boolean) => void
  onStartNewChat: () => void
  onOpenRecent: () => void
  onOpenArchive: () => void
  onBackToConversation: () => void
  onSearchQueryChange: (query: string) => void
  onOpenConversation: (conversationId: string) => void
  onArchiveConversation: (conversationId: string) => void
  onUnarchiveConversation: (conversationId: string) => void
  onRequestDelete: (conversationId: string) => void
  onCancelDelete: () => void
  onConfirmDelete: () => void
  onRetryList: () => void
  onRetryBody: () => void
  onExpand: () => void
  onLeaveExpand: () => void
  onOpenChangeScope: () => void
  onChangeScopeOpenChange: (open: boolean) => void
  onChangeScopeDraftLocation: (locationId: number) => void
  onChangeScopeDraftReportingPeriod: (range: HomePerformanceDateRange) => void
  onApplyChangeScope: () => void
  onSetComposerDraft: (text: string) => void
  onFillComposerFromChip: (label: string) => void
  onSend: () => void
  onStartMic: () => void
  onConfirmMic: () => void
  onCancelMic: () => void
  onDismissMicError: () => void
  micAudioLevelSource: GuestMicAudioLevelSource
  onRetry: () => void
  onToggleHelpful: (
    messageId: string,
    fill: OperatorAiAssistantHelpfulFill
  ) => void
  onActivateAction: (action: OperatorAiAssistantAction) => void
  onDismissFromEscape: () => void
}

const HEADER_TEXT_ACTION_CLASS =
  "h-auto min-h-11 gap-1.5 rounded-op-sm px-0 py-0 text-sm font-normal text-op-text-primary hover:bg-transparent md:min-h-0"

const CHIP_CLASS = [
  "h-auto !min-h-11 flex-[1_1_calc(50%-6px)] justify-start gap-2 rounded-[8px]",
  "border border-op-border-default bg-transparent px-[18px] py-[18px]",
  "text-left text-sm font-normal whitespace-normal text-[var(--op-color-gray-550)]",
  "shadow-none hover:bg-transparent md:!min-h-0",
].join(" ")

const ACTION_CARD_CLASS = [
  "h-auto min-h-11 justify-start gap-2 rounded-[8px]",
  "border border-op-border-default px-[18px] py-[18px]",
  "text-left text-sm font-normal whitespace-normal text-[var(--op-color-gray-550)]",
  "shadow-none hover:bg-transparent md:min-h-0",
].join(" ")

const HELPFUL_HIT_CLASS =
  "size-11 min-h-11 min-w-11 p-0 text-op-text-primary hover:bg-transparent md:size-4 md:min-h-0 md:min-w-0"

const LG_VIEWPORT_QUERY = "(min-width: 1024px)"

function readViewportAtLeastLg(): boolean {
  if (typeof window === "undefined") {
    return true
  }
  return window.matchMedia(LG_VIEWPORT_QUERY).matches
}

function ThreadMessage({
  message,
  retryVisible,
  helpfulFill,
  onRetry,
  onToggleHelpful,
  onActivateAction,
}: {
  message: OperatorAiAssistantMessage
  retryVisible: boolean
  helpfulFill?: OperatorAiAssistantHelpfulFill
  onRetry: () => void
  onToggleHelpful: (
    messageId: string,
    fill: OperatorAiAssistantHelpfulFill
  ) => void
  onActivateAction: (action: OperatorAiAssistantAction) => void
}) {
  if (message.role === "user") {
    return (
      <div
        className="flex justify-end"
        data-assistant-thread-row={message.id}
      >
        <div className="max-w-[85%] rounded-[8px] border border-[var(--op-color-gray-85)] bg-op-surface-primary px-[18px] py-[14px] text-sm leading-5 text-[var(--op-color-gray-550)] dark:border-[#2a2a2a] dark:bg-[#141414]">
          {message.body}
        </div>
      </div>
    )
  }

  return (
    <div
      className="flex flex-col gap-3"
      data-assistant-thread-row={message.id}
    >
      <div className="flex items-start gap-3">
        <AiIcon size={26} />
        <div className="flex min-w-0 flex-1 flex-col gap-2">
          {message.role === "wait" ? (
            <p className="text-sm leading-5 text-[var(--op-color-gray-550)]">
              {message.body}
            </p>
          ) : (
            <>
              {message.title ? (
                <p className="text-sm leading-5 font-normal text-op-text-primary">
                  {message.title}
                </p>
              ) : null}
              <p className="text-sm leading-5 text-[var(--op-color-gray-550)]">
                {message.body}
              </p>
              {message.class === "grounded" ? (
                <div className="flex items-center gap-3">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className={HELPFUL_HIT_CLASS}
                    aria-label="Helpful"
                    aria-pressed={helpfulFill === "helpful"}
                    onClick={() => {
                      onToggleHelpful(message.id, "helpful")
                    }}
                  >
                    <ThumbsUpIcon
                      className={cn(
                        "size-4",
                        helpfulFill === "helpful" && "fill-current"
                      )}
                      aria-hidden
                    />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className={HELPFUL_HIT_CLASS}
                    aria-label="Not helpful"
                    aria-pressed={helpfulFill === "not-helpful"}
                    onClick={() => {
                      onToggleHelpful(message.id, "not-helpful")
                    }}
                  >
                    <ThumbsDownIcon
                      className={cn(
                        "size-4",
                        helpfulFill === "not-helpful" && "fill-current"
                      )}
                      aria-hidden
                    />
                  </Button>
                </div>
              ) : null}
            </>
          )}
        </div>
      </div>
      {message.class === "grounded" && (message.actions?.length ?? 0) > 0 ? (
        <div className="flex flex-col gap-3">
          <p className="text-sm leading-5 font-medium text-op-text-primary">
            Actions
          </p>
          {message.actions?.map((action) => (
            <Button
              key={action.type}
              type="button"
              variant="op-ghost"
              className={ACTION_CARD_CLASS}
              onClick={() => {
                onActivateAction(action)
              }}
            >
              <AiIcon size={16} />
              {action.label}
            </Button>
          ))}
        </div>
      ) : null}
      {message.class === "failure" && retryVisible ? (
        <Button
          type="button"
          variant="op-tertiary"
          className="h-[42px] self-start px-[17px]"
          onClick={onRetry}
        >
          Retry
        </Button>
      ) : null}
    </div>
  )
}

/** Operator AI Assistant right Drawer. Figma 3454:56016 / Expand 3428:32355 / 3310:30861. */
export function AiAssistantDrawer({
  snapshot,
  sidebarCollapsed,
  onOpenChange,
  onStartNewChat,
  onOpenRecent,
  onOpenArchive,
  onBackToConversation,
  onSearchQueryChange,
  onOpenConversation,
  onArchiveConversation,
  onUnarchiveConversation,
  onRequestDelete,
  onCancelDelete,
  onConfirmDelete,
  onRetryList,
  onRetryBody,
  onExpand,
  onLeaveExpand,
  onOpenChangeScope,
  onChangeScopeOpenChange,
  onChangeScopeDraftLocation,
  onChangeScopeDraftReportingPeriod,
  onApplyChangeScope,
  onSetComposerDraft,
  onFillComposerFromChip,
  onSend,
  onStartMic,
  onConfirmMic,
  onCancelMic,
  onDismissMicError,
  micAudioLevelSource,
  onRetry,
  onToggleHelpful,
  onActivateAction,
  onDismissFromEscape,
}: AiAssistantDrawerProps) {
  const [viewportAtLeastLg, setViewportAtLeastLg] = useState(readViewportAtLeastLg)
  const paintExpanded = paintsAssistantExpand({
    widthMode: snapshot.widthMode,
    viewportAtLeastLg,
  })
  const composerRef = useRef<HTMLTextAreaElement>(null)
  const threadBodyRef = useRef<HTMLDivElement>(null)
  const lastScrolledThreadKeyRef = useRef<string | null>(null)
  const [composerFocused, setComposerFocused] = useState(false)
  const animatedPlaceholder = useEmptyComposerPlaceholder({
    placeholders: snapshot.composerPlaceholders,
    cycleGeneration: snapshot.placeholderCycleGeneration,
    isPaused:
      composerFocused
      || snapshot.composerDraft.length > 0
      || snapshot.micPhase !== "idle",
  })
  const placeholder =
    snapshot.composerPlaceholders.length > 0
      ? animatedPlaceholder
      : snapshot.composerPlaceholder || "Ask AI Assistant..."
  const canSend =
    snapshot.composerDraft.trim().length > 0
    && !snapshot.sendLocked
    && !snapshot.sendBlocked
  const showList = snapshot.view === "recent" || snapshot.view === "archive"
  const showGreeting =
    !showList && !snapshot.messages.some((message) => message.role === "user")

  const handleComposerKeyDown = (event: ReactKeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault()
      if (canSend) {
        onSend()
      }
    }
  }

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
    if (
      !snapshot.drawerOpen
      || !paintExpanded
      || snapshot.changeScopeDialog.open
      || snapshot.deleteConfirm.open
    ) {
      return
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape") {
        return
      }
      event.preventDefault()
      onDismissFromEscape()
    }
    window.addEventListener("keydown", onKeyDown)
    return () => {
      window.removeEventListener("keydown", onKeyDown)
    }
  }, [
    onDismissFromEscape,
    paintExpanded,
    snapshot.changeScopeDialog.open,
    snapshot.deleteConfirm.open,
    snapshot.drawerOpen,
  ])

  useEffect(() => {
    if (showList || showGreeting) {
      return
    }
    const last = snapshot.messages.at(-1)
    if (last == null) {
      return
    }
    const key = `${snapshot.messages.length}:${last.id}:${last.role}`
    if (lastScrolledThreadKeyRef.current === key) {
      return
    }
    const previousWasWait =
      lastScrolledThreadKeyRef.current?.endsWith(":wait") === true
    const shouldScroll =
      last.role === "wait" || (last.role === "assistant" && previousWasWait)
    lastScrolledThreadKeyRef.current = key
    if (!shouldScroll) {
      return
    }
    const row = threadBodyRef.current?.querySelector(
      `[data-assistant-thread-row="${CSS.escape(last.id)}"]`
    )
    if (row instanceof HTMLElement) {
      row.scrollIntoView({ block: "nearest", inline: "nearest" })
    }
  }, [showGreeting, showList, snapshot.messages])

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
          <div
            className={cn(
              "flex min-h-0 flex-1 flex-col",
              showList ? "" : "pt-[22px]"
            )}
          >
            {showList ? (
              <AiAssistantConversationList
                snapshot={snapshot}
                onBackToConversation={onBackToConversation}
                onSearchQueryChange={onSearchQueryChange}
                onOpenConversation={onOpenConversation}
                onArchive={onArchiveConversation}
                onUnarchive={onUnarchiveConversation}
                onRequestDelete={onRequestDelete}
                onOpenArchive={onOpenArchive}
                onStartConversation={onStartNewChat}
                onRetry={
                  snapshot.listChromeKind === "body-error"
                    ? onRetryBody
                    : onRetryList
                }
              />
            ) : (
              <>
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

              <div className="flex flex-col items-start gap-[18px]">
                <p
                  className="min-w-0 max-w-full truncate text-sm leading-5 font-normal text-[var(--op-color-gray-550)]"
                  title={snapshot.headerStatusLine}
                  aria-label={snapshot.headerStatusLine}
                >
                  {snapshot.headerStatusLine}
                </p>
                <Button
                  type="button"
                  variant="op-tertiary"
                  className="h-auto min-h-11 shrink-0 whitespace-nowrap px-[17px] md:h-[42px] md:min-h-[42px]"
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
              ref={threadBodyRef}
              className={cn(
                OPERATOR_RIGHT_DRAWER_BODY_CLASS,
                "flex flex-col px-[30px]"
              )}
            >
              {showGreeting ? (
                <div className="mt-auto flex flex-col items-center gap-4 pb-[60px]">
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
              ) : (
                <div className="flex flex-1 flex-col gap-[30px] py-2">
                  {snapshot.messages.map((message) => (
                    <ThreadMessage
                      key={message.id}
                      message={message}
                      retryVisible={snapshot.retryVisible}
                      helpfulFill={snapshot.helpfulFills[message.id]}
                      onRetry={onRetry}
                      onToggleHelpful={onToggleHelpful}
                      onActivateAction={onActivateAction}
                    />
                  ))}
                </div>
              )}
            </div>

            <div className="flex w-full shrink-0 flex-col gap-8 px-[30px] pb-[30px]">
              <div className="overflow-hidden rounded-[8px] border border-op-border-default">
                <div className="flex min-h-[144px] flex-col justify-between bg-[var(--op-color-black)] p-[21px]">
                  <Textarea
                    ref={composerRef}
                    id="ai-assistant-composer"
                    value={snapshot.composerDraft}
                    placeholder={placeholder}
                    disabled={snapshot.composerLocked}
                    onChange={(event) => {
                      onSetComposerDraft(event.target.value)
                    }}
                    onFocus={() => {
                      setComposerFocused(true)
                    }}
                    onBlur={() => {
                      setComposerFocused(false)
                    }}
                    onKeyDown={handleComposerKeyDown}
                    className="min-h-0 flex-1 resize-none rounded-none border-0 bg-transparent p-0 text-base text-[var(--op-color-gray-550)] shadow-none placeholder:text-[var(--op-color-gray-550)] focus-visible:border-0 focus-visible:ring-0 dark:bg-transparent"
                    aria-label="Ask AI Assistant"
                  />
                  {snapshot.micError ? (
                    <div
                      role="alert"
                      className="flex items-start justify-between gap-2"
                    >
                      <p className="text-sm text-destructive">
                        {snapshot.micError.message}
                      </p>
                      <Button
                        type="button"
                        variant="op-ghost"
                        size="icon"
                        aria-label="Dismiss"
                        onClick={onDismissMicError}
                        className="size-6 min-h-6 min-w-6 shrink-0 text-[var(--op-color-gray-550)] hover:bg-transparent"
                      >
                        <XIcon className="size-4" aria-hidden />
                      </Button>
                    </div>
                  ) : null}
                  <div
                    className={cn(
                      "flex items-center",
                      snapshot.micChrome === "tick_cancel"
                        ? "w-full"
                        : "justify-end gap-4"
                    )}
                  >
                    <AiAssistantMicChrome
                      chrome={snapshot.micChrome}
                      micAvailable={snapshot.micAvailable}
                      micLocked={snapshot.micLocked || snapshot.sendBlocked}
                      levelSource={micAudioLevelSource}
                      onStart={onStartMic}
                      onConfirm={onConfirmMic}
                      onCancel={onCancelMic}
                    />
                    {snapshot.micChrome === "tick_cancel" ? null : (
                      <Button
                        type="button"
                        variant="op-ghost"
                        size="icon"
                        disabled={!canSend}
                        aria-label="Send"
                        onClick={onSend}
                        className="size-10 min-h-11 min-w-11 rounded-full bg-[var(--op-color-gray-1000)] text-[var(--op-color-gray-550)] hover:bg-[var(--op-color-gray-1000)] md:min-h-10 md:min-w-10"
                      >
                        <ArrowUpIcon className="size-6" aria-hidden />
                      </Button>
                    )}
                  </div>
                </div>
              </div>

              {snapshot.suggestionChips.length > 0 ? (
                <div className="flex flex-wrap gap-3 overflow-x-hidden">
                  {snapshot.suggestionChips.map((label) => (
                    <Button
                      key={label}
                      type="button"
                      variant="op-ghost"
                      className={CHIP_CLASS}
                      disabled={snapshot.chipsLocked}
                      onClick={() => {
                        onFillComposerFromChip(label)
                        composerRef.current?.focus()
                      }}
                    >
                      <AiIcon size={16} />
                      {label}
                    </Button>
                  ))}
                </div>
              ) : null}
            </div>
              </>
            )}
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
      <AiAssistantDeleteDialog
        open={snapshot.deleteConfirm.open}
        onOpenChange={(open) => {
          if (!open) {
            onCancelDelete()
          }
        }}
        onConfirm={onConfirmDelete}
      />
    </>
  )
}
