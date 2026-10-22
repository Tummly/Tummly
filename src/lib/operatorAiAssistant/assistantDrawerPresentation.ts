import {
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_WIDTH_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"

import type { OperatorAiAssistantWidthMode } from "./createOperatorAiAssistantModule"

/** SideNav collapsed rail width — Expand follows the rail. */
export const OPERATOR_SIDENAV_COLLAPSED_PX = 52

/**
 * Assistant Drawer chrome — same fill as Conversations (`op-assistant-list-background`).
 * Reuses the shared 620px width; does not change `OPERATOR_RIGHT_DRAWER_*`.
 * `!select-text` and `!touch-pan-y` override Vaul's desktop `user-select: none`
 * and `touch-action: none` so operators can select and copy across answer blocks.
 */
const ASSISTANT_DRAWER_CHROME_CLASS =
  "h-full max-h-dvh overflow-hidden rounded-none bg-op-assistant-list-background !select-text !touch-pan-y data-[vaul-drawer-direction=right]:rounded-none"

/** Collapsed Assistant — Conversations fill + shared 620px width. */
export const ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS = `${ASSISTANT_DRAWER_CHROME_CLASS} ${OPERATOR_RIGHT_DRAWER_WIDTH_CLASS}`

/** Viewport minus 52px collapsed SideNav. */
export const OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS =
  "data-[vaul-drawer-direction=right]:w-[calc(100vw-52px)] data-[vaul-drawer-direction=right]:sm:max-w-none"

/** Assistant-only overlay. Shared Drawer overlay defaults stay unchanged. */
export function assistantDrawerOverlayClass(): string {
  return "bg-op-assistant-overlay supports-backdrop-filter:backdrop-blur-none"
}

export function paintsAssistantExpand(input: {
  widthMode: OperatorAiAssistantWidthMode
  viewportAtLeastLg: boolean
}): boolean {
  return input.widthMode === "expanded" && input.viewportAtLeastLg
}

/**
 * Vaul Overlay returns before a hook when `modal` is false.
 * Unmount Overlay in Expand so the hook count does not change.
 */
export function assistantDrawerMountsOverlay(input: {
  widthMode: OperatorAiAssistantWidthMode
  viewportAtLeastLg: boolean
}): boolean {
  return !paintsAssistantExpand(input)
}

/**
 * Assistant Drawer content class. Collapsed uses Conversations fill + 620px.
 * Expand uses the same fill and always follows the collapsed SideNav rail.
 */
export function assistantDrawerContentClass(input: {
  widthMode: OperatorAiAssistantWidthMode
  viewportAtLeastLg: boolean
  sidebarCollapsed: boolean
}): string {
  if (!paintsAssistantExpand(input)) {
    return ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS
  }

  return `${ASSISTANT_DRAWER_CHROME_CLASS} ${OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS}`
}

/** Figma 3444:52355 — conversation + composer cap at 800px, centered. */
export const ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS =
  "mx-auto w-full max-w-[800px] px-[30px]"

/**
 * Stage is full pane width so the thread scrollbar sits on the right bleed.
 * Tight gap keeps more answer height above the composer.
 */
export function assistantConversationStageClass(expanded: boolean): string {
  return expanded
    ? "flex min-h-0 flex-1 flex-col gap-1.5 pb-1.5"
    : "flex min-h-0 flex-1 flex-col gap-1.5"
}

/** Relative shell that hosts the thread plus top/bottom edge fades. */
export const ASSISTANT_THREAD_EDGE_SHELL_CLASS =
  "relative flex min-h-0 flex-1 flex-col"

/**
 * Thin edge soften (16px): solid fill at the clip line, then a short fade.
 * Full `from-*` (not /55) so the hard cut is covered; height stays small.
 * Pointer-events none so the thread still scrolls under it.
 */
export const ASSISTANT_THREAD_TOP_EDGE_FADE_CLASS = [
  "pointer-events-none absolute inset-x-0 top-0 z-10 h-4",
  "bg-gradient-to-b from-op-assistant-list-background to-transparent",
].join(" ")

export const ASSISTANT_THREAD_BOTTOM_EDGE_FADE_CLASS = [
  "pointer-events-none absolute inset-x-0 bottom-0 z-10 h-4",
  "bg-gradient-to-t from-op-assistant-list-background to-transparent",
].join(" ")

/**
 * Scrollable thread. Expand is full pane width so the scrollbar sits on
 * the right bleed. Collapsed keeps side gutters.
 * Edge fades hide at the live edges, so only a thin py is needed.
 */
export function assistantThreadBodyClass(expanded: boolean): string {
  return [
    OPERATOR_RIGHT_DRAWER_BODY_CLASS,
    "flex flex-col py-1.5",
    expanded ? "" : "px-[30px]",
  ]
    .filter((part) => part.length > 0)
    .join(" ")
}

/** Inner chat rail. Expand centers 800px; collapsed is full thread width. */
export function assistantThreadRailClass(expanded: boolean): string {
  return expanded
    ? `flex min-h-full w-full flex-col ${ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS}`
    : "flex min-h-full w-full flex-1 flex-col"
}

/** Composer + chips dock. Expand inherits stage padding. */
export function assistantComposerDockClass(expanded: boolean): string {
  return expanded
    ? "flex w-full shrink-0 flex-col"
    : "flex w-full shrink-0 flex-col gap-4 px-[30px] pb-1.5"
}

/** Inner composer rail. Expand matches the 800px chat column. */
export function assistantComposerRailClass(expanded: boolean): string {
  return expanded
    ? `flex w-full shrink-0 flex-col gap-4 ${ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS}`
    : "contents"
}

/** Collapsed conversation header — tight so more thread height shows. */
export const ASSISTANT_COLLAPSED_HEADER_CLASS =
  "flex shrink-0 flex-col gap-1 px-5 pb-2"

/** Expand conversation header — tight top padding + room for the top fade. */
export const ASSISTANT_EXPAND_HEADER_CLASS =
  "flex shrink-0 flex-col gap-1 px-7 pt-3 pb-1.5"

/**
 * Stick-to-end key for the conversation thread.
 * Null while the list or empty greeting is painted.
 * Wait body is in the key so progress lines keep the end in view.
 */
export function assistantThreadStickAnchor(input: {
  showList: boolean
  showGreeting: boolean
  messages: readonly { id: string; role: string; body: string }[]
}): string | null {
  if (input.showList || input.showGreeting) {
    return null
  }
  const last = input.messages.at(-1)
  if (last == null) {
    return null
  }
  return `${input.messages.length}:${last.id}:${last.role}:${last.body}`
}

/** Slack from the true bottom that still counts as following the stream. */
export const ASSISTANT_THREAD_NEAR_BOTTOM_PX = 80

/** Slack from the true top that still counts as parked at the start. */
export const ASSISTANT_THREAD_NEAR_TOP_PX = 2

/** True when the thread is parked at (or near) the end. */
export function assistantThreadIsNearBottom(
  body: Pick<HTMLElement, "scrollTop" | "scrollHeight" | "clientHeight">,
  thresholdPx: number = ASSISTANT_THREAD_NEAR_BOTTOM_PX
): boolean {
  return body.scrollHeight - body.scrollTop - body.clientHeight <= thresholdPx
}

/** True when the thread is parked at (or near) the start. */
export function assistantThreadIsNearTop(
  body: Pick<HTMLElement, "scrollTop">,
  thresholdPx: number = ASSISTANT_THREAD_NEAR_TOP_PX
): boolean {
  return body.scrollTop <= thresholdPx
}

/**
 * Edge fades only while content is clipped past that edge.
 * At the end of the chat the bottom fade stays off so last lines stay sharp.
 */
export function assistantThreadEdgeFades(input: {
  scrollTop: number
  scrollHeight: number
  clientHeight: number
}): { top: boolean; bottom: boolean } {
  const canScroll = input.scrollHeight - input.clientHeight > 1
  if (!canScroll) {
    return { top: false, bottom: false }
  }
  return {
    top: !assistantThreadIsNearTop(input),
    bottom: !assistantThreadIsNearBottom(input, ASSISTANT_THREAD_NEAR_TOP_PX),
  }
}

/**
 * True when a new user turn should re-pin follow after the operator scrolled away.
 */
export function assistantThreadShouldForceFollow(input: {
  previousKey: string | null
  lastMessage: { id: string; role: string } | null | undefined
}): boolean {
  const last = input.lastMessage
  if (last == null || last.role !== "user") {
    return false
  }
  const previousKey = input.previousKey
  if (previousKey == null) {
    return true
  }
  return !previousKey.includes(`:${last.id}:`)
}

/** Scroll the thread body so the latest row sits at the end. */
export function stickAssistantThreadToBottom(
  body: Pick<HTMLElement, "scrollTop" | "scrollHeight">
): void {
  body.scrollTop = body.scrollHeight
}
