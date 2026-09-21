import { assistantSideNavExpandLock } from "./assistantSideNavExpandLock"

/**
 * Desktop Side-nav rail: hover peeks open; hamburger click pins that open
 * state. Assistant Expand still forces the rail closed and locks the control.
 */
export function resolveDesktopSidebarRail(input: {
  preferenceCollapsed: boolean
  hoverExpanded: boolean
  assistantExpanded: boolean
}): {
  effectiveCollapsed: boolean
  toggleLocked: boolean
} {
  const lock = assistantSideNavExpandLock({
    priorCollapsed: input.preferenceCollapsed,
    assistantExpanded: input.assistantExpanded,
  })

  if (lock.toggleLocked) {
    return lock
  }

  const peekOpen = input.preferenceCollapsed && input.hoverExpanded
  return {
    effectiveCollapsed: input.preferenceCollapsed && !peekOpen,
    toggleLocked: false,
  }
}
