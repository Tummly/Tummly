/**
 * Portaled Select / Popover menus under Change analysis scope.
 * Dialog must not treat clicks inside these as outside-dismiss of the Dialog.
 */
const CHANGE_SCOPE_NESTED_MENU_SLOTS = [
  "select-content",
  "popover-content",
] as const

type ClosestTarget = {
  closest: (selectors: string) => unknown
}

function asClosestTarget(target: EventTarget | null): ClosestTarget | null {
  if (target == null) {
    return null
  }
  const candidate = target as unknown as ClosestTarget
  if (typeof candidate.closest !== "function") {
    return null
  }
  return candidate
}

/** True when the event target is inside a portaled Select or Popover menu. */
export function isChangeScopeNestedMenuTarget(
  target: EventTarget | null
): boolean {
  const element = asClosestTarget(target)
  if (element == null) {
    return false
  }

  return CHANGE_SCOPE_NESTED_MENU_SLOTS.some(
    (slot) => element.closest(`[data-slot="${slot}"]`) != null
  )
}
