import { useEffect, useLayoutEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from "react"
import {
  ArrowUpIcon,
  Maximize2Icon,
  Minimize2Icon,
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
import { AiIcon } from "@/components/ui/ai-icon"
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
  ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS,
  ASSISTANT_COMPOSER_SEND_ICON_CLASS,
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
}


const CHIP_CLASS = [
  "h-auto min-h-10 w-full justify-start gap-2.5 rounded-lg",
  "border border-op-border-default bg-op-card-background px-4 py-2.5",
  "text-left text-sm font-normal whitespace-normal text-op-assistant-list-subtitle hover:text-op-text-primary",
  "shadow-none hover:bg-op-assistant-list-row-hover transition-colors md:min-h-0",
].join(" ")

const ACTION_CARD_CLASS = [
  "h-auto min-h-11 w-full justify-start gap-2 rounded-lg",
  "border border-op-border-default p-4",
  "text-left text-sm font-normal whitespace-normal text-op-assistant-list-subtitle hover:text-op-text-primary",
  "shadow-none hover:bg-op-assistant-list-row-hover transition-colors md:min-h-0",
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
        <div className="max-w-[85%] rounded-lg border border-zinc-800 bg-neutral-900 px-4 py-3.5 text-sm font-normal leading-5 text-neutral-400">
          {message.body}
        </div>
      </div>
    )
  }

  const isWait = message.role === "wait"
  const showHelpful = message.class === "grounded"

  return (
    <div
      className="flex flex-col gap-7"
      data-assistant-thread-row={message.id}
    >
      <div className="flex flex-col gap-3">
        <div className={cn("flex gap-3", isWait ? "items-center" : "items-start")}>
          <AiIcon
            size={24}
            className={cn("shrink-0 mt-0.5", isWait && ASSISTANT_WAIT_ICON_CLASS)}
          />
          <div className="flex min-w-0 flex-1 flex-col gap-4">
            {isWait ? (
              <AssistantWaitLine text={message.body} />
            ) : (
              <>
                <div className="flex flex-col gap-1.5">
                  {message.title ? (
                    <p className="text-sm font-bold leading-5 text-white">
                      {message.title}
                    </p>
                  ) : null}
                  {message.class === "grounded" ? (
                    <GroundedLiveAnswerBody body={message.body} />
                  ) : (
                    <p className="text-sm font-normal leading-5 text-neutral-400">
                      {message.body}
                    </p>
                  )}
                </div>
                {message.meta ? (
                  <p className="text-xs font-normal text-neutral-400">
                    {message.meta}
                  </p>
                ) : null}
              </>
            )}
          </div>
        </div>
        {showHelpful ? (
          <div className="pl-9">
            <HelpfulButtons
              messageId={message.id}
              helpfulFill={helpfulFill}
              onToggleHelpful={onToggleHelpful}
            />
          </div>
        ) : null}
      </div>

      {message.recommendedNextStep ? (
        <div className="flex flex-col gap-3">
          <p className="text-sm font-medium leading-5 text-white">
            Recommended next step
          </p>
          <div className="flex items-center gap-2 rounded-lg border border-op-border-default p-4 text-sm font-normal text-op-assistant-list-subtitle">
            <AiIcon size={16} className="shrink-0 text-op-icon-default" />
            <span className="flex-1">{message.recommendedNextStep}</span>
          </div>
        </div>
      ) : null}

      {message.class === "grounded" && (message.actions?.length ?? 0) > 0 ? (
        <div className="flex flex-col gap-3">
          <p className="text-sm font-medium leading-5 text-white">
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
              <AiIcon size={16} className="shrink-0" />
              <span className="flex-1">{action.label}</span>
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
}: AiAssistantDrawerProps) {
  const [viewportAtLeastLg, setViewportAtLeastLg] = useState(readViewportAtLeastLg)
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
  const placeholder = "Ask AI Assistant..."
  const canSend =
    snapshot.composerDraft.trim().length > 0
    && !snapshot.sendLocked
    && !snapshot.sendBlocked
  const showList =
    !paintExpanded
    && (snapshot.view === "recent" || snapshot.view === "archive")
  const showGreeting =
    !showList && !snapshot.messages.some((message) => message.role === "user")

  const currentConversation =
    snapshot.listRows.find(
      (row) => row.isCurrent || row.id === snapshot.conversationId
    )
    ?? snapshot.recentGroups
      .flatMap((group) => group.rows)
      .find((row) => row.isCurrent || row.id === snapshot.conversationId)
  const activeTitle =
    currentConversation?.title
    ?? snapshot.messages.find((message) => message.role === "user")?.body
    ?? "What are guests complaining about this week?"

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

  return (
    <>
      <Drawer
        open={snapshot.drawerOpen}
        onOpenChange={onOpenChange}
        direction="left"
        modal={false}
        dismissible={true}
        // Vaul captures the pointer on content press for drawers.
        // That traps selection inside one block. Drag only from a handle.
        handleOnly
      >
        <DrawerContent
          className={assistantDrawerContentClass({
            widthMode: snapshot.widthMode,
            viewportAtLeastLg,
            sidebarCollapsed,
          })}
          showOverlay={showOverlay}
          overlayClassName={assistantDrawerOverlayClass()}
          onOpenAutoFocus={(event) => {
            event.preventDefault()
          }}
          data-assistant-width="expanded"
        >
          <div className="flex min-h-0 flex-1" data-vaul-no-drag>
            <AiAssistantConversationList
              snapshot={snapshot}
              expandedSidebar
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
            <div className="flex min-h-0 min-w-0 flex-1 flex-col pt-5">
              <div className="flex shrink-0 items-start justify-between gap-4 px-5 pb-5">
                <div className="flex flex-col items-start gap-3">
                  {!showGreeting && activeTitle ? (
                    <div className="flex items-center gap-3">
                      <h2 className="text-lg font-normal text-white">
                        {activeTitle}
                      </h2>
                      <Badge variant="tag">Setup</Badge>
                    </div>
                  ) : null}
                  <p
                    className="text-sm font-normal text-neutral-500 leading-5"
                    title={snapshot.headerStatusLine}
                    aria-label={snapshot.headerStatusLine}
                  >
                    {snapshot.headerStatusLine || "Mehmet’s Grill · Camden · Last 7 days"}
                  </p>
                  <Button
                    type="button"
                    variant="op-tertiary"
                    className="h-10 px-4 py-2.5 rounded-xs"
                    onClick={onOpenChangeScope}
                  >
                    Change scope
                  </Button>
                </div>

                <div className="flex shrink-0 items-center gap-5">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="hidden size-7 p-0.5 rounded-lg text-op-text-primary hover:bg-transparent lg:inline-flex"
                    aria-label={
                      paintExpanded
                        ? "Collapse AI Assistant"
                        : "Expand AI Assistant"
                    }
                    onClick={paintExpanded ? onLeaveExpand : onExpand}
                  >
                    {paintExpanded ? (
                      <Minimize2Icon className="size-6 text-op-text-primary" aria-hidden />
                    ) : (
                      <Maximize2Icon className="size-6 text-op-text-primary" aria-hidden />
                    )}
                  </Button>
                  <DrawerClose asChild>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="size-8 shrink-0 rounded-xs bg-zinc-800 border border-op-border-default text-white hover:bg-zinc-700"
                      aria-label="Close AI Assistant"
                    >
                      <XIcon className="size-4 text-white" aria-hidden />
                    </Button>
                  </DrawerClose>
                </div>
              </div>

              <DrawerTitle className="sr-only">AI Assistant</DrawerTitle>
              <DrawerDescription className="sr-only">
                Ask about feedback, guests, offers, campaigns or performance.
              </DrawerDescription>

              <div className={assistantConversationStageClass(paintExpanded)}>
                <div
                  ref={threadBodyRef}
                  className={assistantThreadBodyClass(paintExpanded)}
                >
                  <div className={assistantThreadRailClass(paintExpanded)}>
                    {showGreeting ? (
                      <div className="mt-auto flex flex-col items-center gap-3 pb-3">
                        <AiIcon size={36} />
                        <div className="flex flex-col items-center gap-1 text-center">
                          <p className="text-base font-medium text-[var(--op-color-gray-625)]">
                            {snapshot.greeting.hello}
                          </p>
                          <p className="bg-gradient-to-r from-[var(--op-color-green-600)] to-[var(--op-color-blue-600)] bg-clip-text text-2xl leading-7 font-medium text-transparent dark:text-transparent">
                            {snapshot.greeting.headline}
                          </p>
                        </div>
                        <p className="max-w-[490px] text-center text-sm leading-tight font-normal text-op-assistant-list-subtitle">
                          {snapshot.greeting.body}
                        </p>
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

                <div className={assistantComposerDockClass(paintExpanded)}>
                  <div className={assistantComposerRailClass(paintExpanded)}>
                    <div
                      className={cn(
                        assistantComposerShellClass(composerFocused),
                        "shadow-2xl"
                      )}
                    >
                      <div className="overflow-hidden rounded-[8px]">
                        <AiAssistantCreditsBar
                          remainingLine={snapshot.creditsRemainingLine}
                          viewUsageLabel={snapshot.viewUsageLabel}
                          addCreditsLabel={snapshot.addCreditsLabel}
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
                              <Button
                                type="button"
                                variant="op-ghost"
                                size="icon"
                                disabled={!canSend}
                                aria-label="Send"
                                onClick={onSend}
                                className={ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS}
                              >
                                <ArrowUpIcon
                                  className={ASSISTANT_COMPOSER_SEND_ICON_CLASS}
                                  aria-hidden
                                />
                              </Button>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>

                    {snapshot.suggestionChips.length > 0 ? (
                      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 w-full">
                        {snapshot.suggestionChips.map((label, index) => (
                          <Button
                            key={label}
                            type="button"
                            variant="op-ghost"
                            className={cn(
                              CHIP_CLASS,
                              index === 6 && "sm:col-span-2"
                            )}
                            disabled={snapshot.chipsLocked}
                            onClick={() => {
                              onFillComposerFromChip(label)
                              composerRef.current?.focus()
                            }}
                          >
                            <span>{label}</span>
                          </Button>
                        ))}
                      </div>
                    ) : null}
                  </div>
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
