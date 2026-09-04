import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import {
  ArrowRight,
  ArrowUpIcon,
  HistoryIcon,
  Maximize2Icon,
  Minimize2Icon,
  PlusCircleIcon,
  Settings,
  ThumbsDownIcon,
  ThumbsUpIcon,
  XIcon,
} from "lucide-react"

import { AiAssistantChangeScopeDialog } from "@/components/dashboard/operator/AiAssistantChangeScopeDialog"
import { AiAssistantConversationList } from "@/components/dashboard/operator/AiAssistantConversationList"
import { AiAssistantCreditsBar } from "@/components/dashboard/operator/AiAssistantCreditsBar"
import { AiAssistantDeleteDialog } from "@/components/dashboard/operator/AiAssistantDeleteDialog"
import { GroundedLiveAnswerBody } from "@/components/dashboard/operator/GroundedLiveAnswerBody"
import { AiAssistantMicChrome } from "@/components/dashboard/operator/AiAssistantMicChrome"
import { useEmptyComposerPlaceholder } from "@/components/dashboard/operator/useEmptyComposerPlaceholder"
import { AiIcon } from "@/components/ui/ai-icon"
import { Button } from "@/components/ui/button"
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Textarea } from "@/components/ui/textarea"
import {
  assistantComposerDockClass,
  assistantComposerRailClass,
  assistantConversationStageClass,
  assistantDrawerContentClass,
  assistantDrawerMountsOverlay,
  assistantDrawerOverlayClass,
  assistantThreadBodyClass,
  assistantThreadRailClass,
  assistantThreadStickAnchor,
  paintsAssistantExpand,
  stickAssistantThreadToBottom,
} from "@/lib/operatorAiAssistant/assistantDrawerPresentation"
import {
  assistantComposerFieldClass,
  assistantComposerShellClass,
  assistantComposerTextareaClass,
} from "@/lib/operatorAiAssistant/assistantCreditsPresentation"
import {
  ASSISTANT_WAIT_ICON_CLASS,
  ASSISTANT_WAIT_TEXT_CLASS,
} from "@/lib/operatorAiAssistant/assistantWaitPresentation"
import type {
  OperatorAiAssistantAction,
  OperatorAiAssistantDraftLocation,
  OperatorAiAssistantHelpfulFill,
  OperatorAiAssistantMessage,
  OperatorAiAssistantSnapshot,
} from "@/lib/operatorAiAssistant/createOperatorAiAssistantModule"
import type { GuestMicAudioLevelSource } from "@/lib/guestFeedback/guestMicAudioLevel"
import type { HomePerformanceDateRange } from "@/lib/operatorHome/homePerformanceDateRange"
import { cn } from "@/lib/utils"

type AiAssistantDrawerProps = {
  snapshot: OperatorAiAssistantSnapshot
  sidebarCollapsed: boolean
  isClosing?: boolean
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
  onChangeScopeDraftLocation: (locationId: OperatorAiAssistantDraftLocation) => void
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
  onViewUsage: () => void
  onAddCredits: () => void
  onFollowRestorationHelper: () => void
}


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

function AssistantWaitLine({ text }: { text: string }) {
  return (
    <p
      className={ASSISTANT_WAIT_TEXT_CLASS}
      role="status"
      aria-live="polite"
      aria-label={text}
    >
      <span key={text} data-assistant-wait-phrase aria-hidden>
        {text}
      </span>
    </p>
  )
}

function HelpfulButtons({
  messageId,
  helpfulFill,
  onToggleHelpful,
}: {
  messageId: string
  helpfulFill?: OperatorAiAssistantHelpfulFill
  onToggleHelpful: (
    messageId: string,
    fill: OperatorAiAssistantHelpfulFill
  ) => void
}) {
  return (
    <div className="flex shrink-0 items-center gap-3 pl-[4px]">
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={HELPFUL_HIT_CLASS}
        aria-label="Helpful"
        aria-pressed={helpfulFill === "helpful"}
        onClick={() => {
          onToggleHelpful(messageId, "helpful")
        }}
      >
        <ThumbsUpIcon
          className={cn("size-4", helpfulFill === "helpful" && "fill-current")}
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
          onToggleHelpful(messageId, "not-helpful")
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
  )
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

  const isWait = message.role === "wait"
  const showHelpful = message.class === "grounded"

  return (
    <div
      className="flex flex-col gap-[30px]"
      data-assistant-thread-row={message.id}
    >
      <div className="flex flex-col gap-[12px]">
        <div className={cn("flex gap-3", isWait ? "items-center" : "items-start")}>
          <AiIcon
            size={26}
            className={isWait ? ASSISTANT_WAIT_ICON_CLASS : undefined}
          />
          <div className="flex min-w-0 flex-1 flex-col gap-2">
            {isWait ? (
              <AssistantWaitLine text={message.body} />
            ) : (
              <>
                {message.title ? (
                  <p className="text-sm leading-5 font-normal text-op-text-primary">
                    {message.title}
                  </p>
                ) : null}
                {message.class === "grounded" ? (
                  <GroundedLiveAnswerBody body={message.body} />
                ) : (
                  <p className="text-sm leading-5 text-[var(--op-color-gray-550)]">
                    {message.body}
                  </p>
                )}
              </>
            )}
          </div>
        </div>
        {showHelpful ? (
          <HelpfulButtons
            messageId={message.id}
            helpfulFill={helpfulFill}
            onToggleHelpful={onToggleHelpful}
          />
        ) : null}
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
              disabled={action.clickable === false}
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
  onViewUsage,
  onAddCredits,
  onFollowRestorationHelper,
}: AiAssistantDrawerProps) {
  const [viewportAtLeastLg, setViewportAtLeastLg] = useState(readViewportAtLeastLg)
  const shouldReduceMotion = useReducedMotion()

  const paintExpanded = paintsAssistantExpand({
    widthMode: snapshot.widthMode,
    viewportAtLeastLg,
  })
  const showOverlay = assistantDrawerMountsOverlay({
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
  const showList =
    !paintExpanded
    && (snapshot.view === "recent" || snapshot.view === "archive")
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
    snapshot.changeScopeDialog.open,
    snapshot.deleteConfirm.open,
    snapshot.drawerOpen,
  ])

  useLayoutEffect(() => {
    if (showList || showGreeting) {
      lastScrolledThreadKeyRef.current = null
      return
    }
    const key = assistantThreadStickAnchor({
      showList,
      showGreeting,
      messages: snapshot.messages,
    })
    if (key == null || lastScrolledThreadKeyRef.current === key) {
      return
    }
    const body = threadBodyRef.current
    if (body == null) {
      return
    }
    lastScrolledThreadKeyRef.current = key
    stickAssistantThreadToBottom(body)
  }, [showGreeting, showList, snapshot.messages])

  const panelContent = (
    <div className="flex min-h-0 flex-1 flex-col" data-vaul-no-drag>
      <div
        className={cn(
          "flex min-h-0 min-w-0 flex-1 flex-col",
          showList ? "" : "pt-5"
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
            <div className="flex shrink-0 flex-col gap-1.5 px-5 pb-5">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-5">
                  <Button
                    type="button"
                    variant="op-ghost"
                    size="icon"
                    className="size-4 min-h-0 min-w-0 p-0 text-white hover:bg-transparent"
                    aria-label="Change analysis scope"
                    onClick={onOpenChangeScope}
                  >
                    <Settings className="size-4 text-white" aria-hidden />
                  </Button>
                  <Button
                    type="button"
                    variant="op-ghost"
                    className="h-auto min-h-0 gap-1.5 p-0 text-sm font-normal text-white hover:bg-transparent"
                    onClick={onStartNewChat}
                  >
                    <PlusCircleIcon
                      className="size-3.5 text-white"
                      aria-hidden
                    />
                    New chat
                  </Button>
                  <Button
                    type="button"
                    variant="op-ghost"
                    className="h-auto min-h-0 gap-1.5 p-0 text-sm font-normal text-white hover:bg-transparent"
                    onClick={onOpenRecent}
                  >
                    <HistoryIcon
                      className="size-3 text-white"
                      aria-hidden
                    />
                    Recent
                  </Button>
                </div>
                <div className="ml-auto flex shrink-0 items-center gap-5">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="hidden size-4 min-h-0 min-w-0 p-0 text-zinc-400 hover:text-white hover:bg-transparent lg:inline-flex"
                    aria-label={
                      paintExpanded
                        ? "Collapse AI Assistant"
                        : "Expand AI Assistant"
                    }
                    onClick={paintExpanded ? onLeaveExpand : onExpand}
                  >
                    {paintExpanded ? (
                      <Minimize2Icon
                        className="size-4 text-zinc-400"
                        aria-hidden
                      />
                    ) : (
                      <Maximize2Icon
                        className="size-4 text-zinc-400"
                        aria-hidden
                      />
                    )}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="size-8 min-h-0 min-w-0 rounded-md bg-[#222222] text-neutral-300 hover:bg-[#2c2c2c] hover:text-white flex items-center justify-center cursor-pointer transition-colors"
                    aria-label="Close AI Assistant"
                    onClick={() => onOpenChange(false)}
                  >
                    <XIcon className="size-4" aria-hidden />
                  </Button>
                </div>
              </div>

              <div className="flex flex-col items-start">
                <button
                  type="button"
                  onClick={onOpenChangeScope}
                  className="mt-1 cursor-pointer text-left text-xs font-normal text-neutral-500 transition-colors hover:text-neutral-300"
                  title={snapshot.headerStatusLine}
                  aria-label={snapshot.headerStatusLine}
                >
                  {snapshot.headerStatusLine}
                </button>
              </div>
            </div>

            <h2 className="sr-only">AI Assistant</h2>
            <p className="sr-only">
              Ask about feedback, guests, offers, campaigns or performance.
            </p>

            {paintExpanded && showGreeting ? (
              <div className="flex-1 w-full flex flex-col justify-center items-center px-7 pb-7 overflow-y-auto">
                <div className="w-[746px] max-w-full flex flex-col justify-center items-center gap-10">
                  {/* 1. Greeting */}
                  <div className="self-stretch flex flex-col justify-start items-center gap-4">
                    <div className="flex flex-col justify-start items-center gap-3">
                      <div className="inline-flex justify-start items-center gap-3">
                        <AiIcon size={28} className="size-7 shrink-0 text-white" />
                        <div className="text-center text-lg font-medium text-neutral-500">
                          {snapshot.greeting.hello}
                        </div>
                      </div>
                      <h2 className="text-center text-2xl font-medium tracking-tight text-white">
                        {snapshot.greeting.headline}
                      </h2>
                    </div>
                    <p className="w-96 max-w-full text-center text-base font-normal leading-5 text-neutral-400">
                      {snapshot.greeting.body}
                    </p>
                  </div>

                  {/* 2. Suggestions: 3 columns */}
                  {snapshot.suggestionChips.length > 0 ? (
                    <div className="self-stretch flex justify-between items-start gap-4">
                      <div className="flex-1 flex flex-col items-start gap-4">
                        {snapshot.suggestionChips.slice(0, 2).map((label) => (
                          <button
                            key={label}
                            type="button"
                            disabled={snapshot.chipsLocked}
                            onClick={() => {
                              onFillComposerFromChip(label)
                              composerRef.current?.focus()
                            }}
                            className="group inline-flex items-center gap-2.5 text-left transition-colors cursor-pointer disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            <ArrowRight
                              className="size-3.5 shrink-0 text-white transition-transform group-hover:translate-x-0.5"
                              aria-hidden
                            />
                            <span className="text-sm font-normal text-white group-hover:text-neutral-300">
                              {label}
                            </span>
                          </button>
                        ))}
                      </div>
                      <div className="flex-1 flex flex-col items-start gap-4">
                        {snapshot.suggestionChips.slice(2, 4).map((label) => (
                          <button
                            key={label}
                            type="button"
                            disabled={snapshot.chipsLocked}
                            onClick={() => {
                              onFillComposerFromChip(label)
                              composerRef.current?.focus()
                            }}
                            className="group inline-flex items-center gap-2.5 text-left transition-colors cursor-pointer disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            <ArrowRight
                              className="size-3.5 shrink-0 text-white transition-transform group-hover:translate-x-0.5"
                              aria-hidden
                            />
                            <span className="text-sm font-normal text-white group-hover:text-neutral-300">
                              {label}
                            </span>
                          </button>
                        ))}
                      </div>
                      <div className="flex-1 flex flex-col items-start gap-4">
                        {snapshot.suggestionChips.slice(4, 6).map((label) => (
                          <button
                            key={label}
                            type="button"
                            disabled={snapshot.chipsLocked}
                            onClick={() => {
                              onFillComposerFromChip(label)
                              composerRef.current?.focus()
                            }}
                            className="group inline-flex items-center gap-2.5 text-left transition-colors cursor-pointer disabled:cursor-not-allowed disabled:opacity-50"
                          >
                            <ArrowRight
                              className="size-3.5 shrink-0 text-white transition-transform group-hover:translate-x-0.5"
                              aria-hidden
                            />
                            <span className="text-sm font-normal text-white group-hover:text-neutral-300">
                              {label}
                            </span>
                          </button>
                        ))}
                      </div>
                    </div>
                  ) : null}

                  {/* 3. Composer */}
                  <div className="self-stretch flex flex-col justify-start items-start gap-4">
                    <div
                      className={cn(
                        assistantComposerShellClass(composerFocused),
                        "self-stretch rounded-lg border border-[#262626] bg-[#141414] overflow-hidden"
                      )}
                    >
                      <AiAssistantCreditsBar
                        remainingLine={snapshot.creditsRemainingLine}
                        viewUsageLabel={snapshot.viewUsageLabel}
                        addCreditsLabel={snapshot.addCreditsLabel}
                        showViewUsage={snapshot.showViewUsage}
                        showAddCredits={snapshot.showAddCredits}
                        onViewUsage={onViewUsage}
                        onAddCredits={onAddCredits}
                      />
                      <div className={assistantComposerFieldClass(snapshot.micChrome)}>
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
                          className={assistantComposerTextareaClass()}
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
                            <div className="flex items-center gap-3">
                              {snapshot.restorationHelper != null ? (
                                <Button
                                  type="button"
                                  variant="op-ghost"
                                  className="h-auto min-h-0 p-0 text-xs font-medium text-neutral-400 hover:bg-transparent hover:text-white transition-colors cursor-pointer"
                                  onClick={onFollowRestorationHelper}
                                >
                                  {snapshot.restorationHelper.label}
                                </Button>
                              ) : null}
                              <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                disabled={!canSend}
                                aria-label="Send"
                                onClick={onSend}
                                className={cn(
                                  "size-8 min-h-8 min-w-8 p-0 transition-colors rounded-full flex items-center justify-center",
                                  canSend
                                    ? "bg-white text-black hover:bg-neutral-200 cursor-pointer"
                                    : "opacity-40 cursor-not-allowed bg-[#222222] text-neutral-400 hover:bg-[#222222]"
                                )}
                              >
                                <ArrowUpIcon
                                  className="size-4"
                                  aria-hidden
                                />
                              </Button>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ) : (
              <div className={assistantConversationStageClass(paintExpanded)}>
                <div
                  ref={threadBodyRef}
                  className={cn(assistantThreadBodyClass(paintExpanded), !paintExpanded && "!px-5 !pb-4")}
                >
                  <div className={assistantThreadRailClass(paintExpanded)}>
                    {showGreeting ? (
                      <div className="flex flex-1 flex-col justify-center gap-7 pb-4">
                        <div className="flex flex-col gap-4">
                          <div className="inline-flex items-center gap-3">
                            <AiIcon size={26} className="size-6 shrink-0 text-white" />
                            <span className="text-base font-normal text-neutral-400">
                              {snapshot.greeting.hello}
                            </span>
                          </div>
                          <div className="flex flex-col gap-2.5">
                            <h2 className="text-2xl font-medium tracking-tight text-white">
                              {snapshot.greeting.headline}
                            </h2>
                            <p className="max-w-[380px] text-sm font-normal leading-relaxed text-neutral-400">
                              {snapshot.greeting.body}
                            </p>
                          </div>
                        </div>

                        {snapshot.suggestionChips.length > 0 ? (
                          <div className="flex flex-col gap-3.5 pt-1">
                            {snapshot.suggestionChips.map((label) => (
                              <button
                                key={label}
                                type="button"
                                disabled={snapshot.chipsLocked}
                                onClick={() => {
                                  onFillComposerFromChip(label)
                                  composerRef.current?.focus()
                                }}
                                className="group inline-flex items-center gap-2.5 text-left transition-colors cursor-pointer disabled:cursor-not-allowed disabled:opacity-50 py-0.5"
                              >
                                <ArrowRight
                                  className="size-3.5 shrink-0 text-white transition-transform group-hover:translate-x-0.5"
                                  aria-hidden
                                />
                                <span className="text-sm font-normal text-white group-hover:text-neutral-300">
                                  {label}
                                </span>
                              </button>
                            ))}
                          </div>
                        ) : null}
                      </div>
                    ) : (
                      <div className="flex flex-1 flex-col gap-[30px] pt-2">
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
                </div>

                <div
                  className={cn(
                    assistantComposerDockClass(paintExpanded),
                    !paintExpanded && "!px-5 !pb-5 !gap-0"
                  )}
                >
                  <div className={assistantComposerRailClass(paintExpanded)}>
                    <div
                      className={cn(
                        assistantComposerShellClass(composerFocused),
                        "rounded-[10px] border-[#262626] bg-[#141414] overflow-hidden",
                        paintExpanded && "w-full max-w-[746px] mx-auto"
                      )}
                    >
                      <AiAssistantCreditsBar
                        remainingLine={snapshot.creditsRemainingLine}
                        viewUsageLabel={snapshot.viewUsageLabel}
                        addCreditsLabel={snapshot.addCreditsLabel}
                        showViewUsage={snapshot.showViewUsage}
                        showAddCredits={snapshot.showAddCredits}
                        onViewUsage={onViewUsage}
                        onAddCredits={onAddCredits}
                      />
                      <div className={assistantComposerFieldClass(snapshot.micChrome)}>
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
                          className={assistantComposerTextareaClass()}
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
                            <div className="flex items-center gap-3">
                              {snapshot.restorationHelper != null ? (
                                <Button
                                  type="button"
                                  variant="op-ghost"
                                  className="h-auto min-h-0 p-0 text-xs font-medium text-neutral-400 hover:bg-transparent hover:text-white transition-colors cursor-pointer"
                                  onClick={onFollowRestorationHelper}
                                >
                                  {snapshot.restorationHelper.label}
                                </Button>
                              ) : null}
                              <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                disabled={!canSend}
                                aria-label="Send"
                                onClick={onSend}
                                className={cn(
                                  "size-8 min-h-8 min-w-8 p-0 transition-colors rounded-full flex items-center justify-center",
                                  canSend
                                    ? "bg-white text-black hover:bg-neutral-200 cursor-pointer"
                                    : "opacity-40 cursor-not-allowed bg-[#222222] text-neutral-400 hover:bg-[#222222]"
                                )}
                              >
                                <ArrowUpIcon
                                  className="size-4"
                                  aria-hidden
                                />
                              </Button>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )

  return (
    <>
      {viewportAtLeastLg ? (
        <AnimatePresence>
          {snapshot.drawerOpen && (
            <motion.aside
              key="ai-assistant-drawer"
              initial={
                paintExpanded
                  ? { opacity: 0, x: "100%" }
                  : { opacity: 0, x: 480, width: 0, marginLeft: 0, marginRight: 0 }
              }
              animate={
                paintExpanded
                  ? { opacity: 1, x: 0, width: "100%", marginLeft: 8, marginRight: 10 }
                  : { opacity: 1, x: 0, width: 480, marginLeft: 8, marginRight: 10 }
              }
              exit={
                paintExpanded
                  ? { opacity: 0, x: "100%" }
                  : { opacity: 0, x: 480, width: 0, marginLeft: 0, marginRight: 0 }
              }
              transition={{
                duration: shouldReduceMotion ? 0 : 0.28,
                ease: [0.32, 0.72, 0, 1],
              }}
              className={cn(
                "relative flex min-h-0 flex-col overflow-hidden",
                "bg-op-assistant-list-background text-op-text-primary",
                "my-2 h-[calc(100%-1rem)]",
                "rounded-tl-[20px] rounded-tr-[20px] rounded-bl-[10px] rounded-br-[10px]",
                "border border-op-border-default shadow-2xl",
                paintExpanded ? "flex-1 min-w-0 shrink" : "shrink-0"
              )}
              data-assistant-width={paintExpanded ? "expanded" : "collapsed"}
              aria-label="AI Assistant"
            >
              <div
                className={cn(
                  "flex min-h-0 flex-1 flex-col",
                  !paintExpanded && "w-[480px] shrink-0"
                )}
              >
                {panelContent}
              </div>
            </motion.aside>
          )}
        </AnimatePresence>
      ) : (
        <Drawer
          open={snapshot.drawerOpen}
          onOpenChange={onOpenChange}
          direction="right"
          modal
          handleOnly
        >
          <DrawerContent
            className={cn(
              assistantDrawerContentClass({
                widthMode: snapshot.widthMode,
                viewportAtLeastLg,
                sidebarCollapsed,
              }),
              "bg-op-assistant-list-background text-op-text-primary"
            )}
            showOverlay={showOverlay}
            overlayClassName={assistantDrawerOverlayClass()}
            onOpenAutoFocus={(event) => {
              event.preventDefault()
            }}
            data-assistant-width="collapsed"
          >
            <DrawerTitle className="sr-only">AI Assistant</DrawerTitle>
            <DrawerDescription className="sr-only">
              Ask about feedback, guests, offers, campaigns or performance.
            </DrawerDescription>
            {panelContent}
          </DrawerContent>
        </Drawer>
      )}
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
