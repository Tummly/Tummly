/**
 * Session-scoped leave-dirty gate for Operator dashboard navigations that
 * BrowserRouter cannot block with useBlocker (data-router only).
 */

export type LeaveDirtyGuard = {
  /** True when the active form tab has unsaved edits. */
  isBlocked: () => boolean
  /**
   * Ask the page module to open Unsaved changes for `href`.
   * Returns true when navigation may proceed immediately.
   */
  requestLeave: (href: string) => boolean
}

let activeGuard: LeaveDirtyGuard | null = null

export function registerLeaveDirtyGuard(guard: LeaveDirtyGuard | null): void {
  activeGuard = guard
}

/** Returns false when the click/navigation should be cancelled. */
export function tryLeaveDirtyNavigate(href: string): boolean {
  if (activeGuard == null || !activeGuard.isBlocked()) {
    return true
  }
  return activeGuard.requestLeave(href)
}

export const BROWSER_BACK_HREF = "__browser_back__"
