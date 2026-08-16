export type AssistantSideNavExpandLock = {
  effectiveCollapsed: boolean
  toggleLocked: boolean
}

/**
 * Keep the stored SideNav preference unchanged while Assistant Expand paints
 * the rail. The stored preference becomes effective again after Expand ends.
 */
export function assistantSideNavExpandLock(input: {
  priorCollapsed: boolean
  assistantExpanded: boolean
}): AssistantSideNavExpandLock {
  if (input.assistantExpanded) {
    return {
      effectiveCollapsed: true,
      toggleLocked: true,
    }
  }

  return {
    effectiveCollapsed: input.priorCollapsed,
    toggleLocked: false,
  }
}
