import { useRef } from "react"

/**
 * Keep the last non-null value while an overlay exits after `open` becomes false.
 * Prevents empty Dialog/Drawer shells during Radix Presence close animations when
 * callers clear payload in the same update as closing.
 */
export function useHeldForExit<T>(open: boolean, value: T | null): T | null {
  const heldRef = useRef<T | null>(value)
  if (value != null) {
    heldRef.current = value
  }
  if (open) {
    return value
  }
  return value ?? heldRef.current
}
