import { OPERATOR_RIGHT_DRAWER_WIDTH_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"

import type { OperatorAiAssistantWidthMode } from "./createOperatorAiAssistantModule"

/** SideNav open width — Expand starts at this edge. Do not write SideNav width. */
export const OPERATOR_SIDENAV_OPEN_PX = 260

/** SideNav collapsed rail width — Expand follows the rail. */
export const OPERATOR_SIDENAV_COLLAPSED_PX = 52

/**
 * Assistant Drawer chrome — same fill as Conversations (`op-assistant-list-background`).
 * Reuses the shared 620px width; does not change `OPERATOR_RIGHT_DRAWER_*`.
 */
const ASSISTANT_DRAWER_CHROME_CLASS =
  "h-full max-h-dvh overflow-hidden bg-op-assistant-list-background data-[vaul-drawer-direction=right]:rounded-l-[2px]"

/** Collapsed Assistant — Conversations fill + shared 620px width. */
export const ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS = `${ASSISTANT_DRAWER_CHROME_CLASS} ${OPERATOR_RIGHT_DRAWER_WIDTH_CLASS}`

/** Viewport minus 260px open SideNav. Overrides Drawer `sm:max-w-sm`. */
export const OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_OPEN_CLASS =
  "data-[vaul-drawer-direction=right]:w-[calc(100vw-260px)] data-[vaul-drawer-direction=right]:sm:max-w-none"

/** Viewport minus 52px collapsed SideNav. */
export const OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS =
  "data-[vaul-drawer-direction=right]:w-[calc(100vw-52px)] data-[vaul-drawer-direction=right]:sm:max-w-none"

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
 * Expand uses the same fill with SideNav-aware width. Geometry only.
 */
export function assistantDrawerContentClass(input: {
  widthMode: OperatorAiAssistantWidthMode
  viewportAtLeastLg: boolean
  sidebarCollapsed: boolean
}): string {
  if (!paintsAssistantExpand(input)) {
    return ASSISTANT_DRAWER_COLLAPSED_CONTENT_CLASS
  }

  const widthClass = input.sidebarCollapsed
    ? OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS
    : OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_OPEN_CLASS

  return `${ASSISTANT_DRAWER_CHROME_CLASS} ${widthClass}`
}
