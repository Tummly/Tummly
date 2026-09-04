import {
  OPERATOR_RIGHT_DRAWER_BODY_CLASS
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
export const OPERATOR_AI_ASSISTANT_LEFT_DRAWER_CLASS = [
  "h-full max-h-dvh overflow-hidden bg-op-assistant-list-background !select-text !touch-pan-y",
  "data-[vaul-drawer-direction=left]:inset-y-0",
  "data-[vaul-drawer-direction=left]:left-0",
  "lg:data-[vaul-drawer-direction=left]:left-[52px]",
  "data-[vaul-drawer-direction=left]:w-full",
  "lg:data-[vaul-drawer-direction=left]:w-[calc(100vw-52px)]",
  "data-[vaul-drawer-direction=left]:max-w-none",
  "data-[vaul-drawer-direction=left]:sm:max-w-none",
  "data-[vaul-drawer-direction=left]:rounded-none",
  "data-[vaul-drawer-direction=left]:border-r-0",
  "data-[vaul-drawer-direction=left]:border-l",
  "data-[vaul-drawer-direction=left]:border-op-border-default",
  "data-[vaul-drawer-direction=right]:w-[calc(100vw-52px)]",
  "data-[vaul-drawer-direction=right]:sm:max-w-none",
  "lg:left-[52px]",
  "lg:w-[calc(100vw-52px)]",
  "[&::after]:!hidden [&::after]:!pointer-events-none [&::after]:!w-0",
].join(" ")

/** Viewport minus 52px collapsed SideNav. */
export const OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS =
  OPERATOR_AI_ASSISTANT_LEFT_DRAWER_CLASS

/** Collapsed Assistant — same full view. */
export const ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS =
  OPERATOR_AI_ASSISTANT_LEFT_DRAWER_CLASS

/** Assistant-only overlay starting beside the 52px rail. */
export function assistantDrawerOverlayClass(): string {
  return "bg-black/40 left-0 lg:left-[52px]"
}

export function paintsAssistantExpand(_input?: {
  widthMode?: OperatorAiAssistantWidthMode
  viewportAtLeastLg?: boolean
}): boolean {
  return true
}

/**
 * Mount Overlay for left drawer only when not expanded on desktop.
 */
export function assistantDrawerMountsOverlay(input?: {
  widthMode?: OperatorAiAssistantWidthMode
  viewportAtLeastLg?: boolean
}): boolean {
  if (input?.widthMode === "expanded" && input.viewportAtLeastLg) {
    return false
  }
  return true
}

/**
 * Assistant Drawer content class — always opens from the left beside the 52px rail.
 */
export function assistantDrawerContentClass(_input?: {
  widthMode?: OperatorAiAssistantWidthMode
  viewportAtLeastLg?: boolean
  sidebarCollapsed?: boolean
}): string {
  return OPERATOR_AI_ASSISTANT_LEFT_DRAWER_CLASS
}

/** Figma 3444:52355 — conversation + composer cap at 800px, centered. */
export const ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS =
  "mx-auto w-full max-w-[800px] px-[30px]"

/**
 * Expand stage is full pane width so the thread scrollbar sits on the
 * right bleed. 30px sits between the thread and the composer. 16px sits
 * under the composer.
 */
export function assistantConversationStageClass(expanded: boolean): string {
  return expanded
    ? "flex min-h-0 flex-1 flex-col gap-4 pb-4"
    : "flex min-h-0 flex-1 flex-col"
}

/**
 * Scrollable thread. Expand is full pane width so the scrollbar sits on
 * the right bleed. Collapsed keeps 30px gutters on the scroll box.
 */
export function assistantThreadBodyClass(expanded: boolean): string {
  return [
    OPERATOR_RIGHT_DRAWER_BODY_CLASS,
    "flex flex-col",
    expanded ? "" : "px-[30px] pb-[30px]",
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
    : "flex w-full shrink-0 flex-col gap-4 px-[30px] pb-4"
}

/** Inner composer rail. Expand matches the 800px chat column. */
export function assistantComposerRailClass(expanded: boolean): string {
  return expanded
    ? `flex w-full shrink-0 flex-col gap-4 ${ASSISTANT_EXPAND_CONVERSATION_RAIL_CLASS}`
    : "contents"
}

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

/** Scroll the thread body so the latest row sits at the end. */
export function stickAssistantThreadToBottom(body: HTMLElement): void {
  body.scrollTop = body.scrollHeight
}
