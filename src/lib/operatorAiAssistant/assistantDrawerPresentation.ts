import { OPERATOR_RIGHT_DRAWER_CONTENT_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"

import type { OperatorAiAssistantWidthMode } from "./createOperatorAiAssistantModule"

/** SideNav open width — Expand starts at this edge. Do not write SideNav width. */
export const OPERATOR_SIDENAV_OPEN_PX = 260

/** SideNav collapsed rail width — Expand follows the rail. */
export const OPERATOR_SIDENAV_COLLAPSED_PX = 52

/**
 * Assistant Expand chrome only. Reuses the shared right-Drawer fill and radius.
 * Does not change `OPERATOR_RIGHT_DRAWER_*`.
 */
const ASSISTANT_EXPAND_CHROME_CLASS =
  "h-full max-h-dvh overflow-hidden bg-op-surface-secondary dark:bg-[#202020] data-[vaul-drawer-direction=right]:rounded-l-[2px]"

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
 * Assistant Drawer content class. Collapsed reuses the shared 620px token.
 * Expand uses a separate width class. Geometry only — do not mount the shell.
 */
export function assistantDrawerContentClass(input: {
  widthMode: OperatorAiAssistantWidthMode
  viewportAtLeastLg: boolean
  sidebarCollapsed: boolean
}): string {
  if (!paintsAssistantExpand(input)) {
    return OPERATOR_RIGHT_DRAWER_CONTENT_CLASS
  }

  const widthClass = input.sidebarCollapsed
    ? OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_RAIL_CLASS
    : OPERATOR_AI_ASSISTANT_EXPAND_WIDTH_OPEN_CLASS

  return `${ASSISTANT_EXPAND_CHROME_CLASS} ${widthClass}`
}
