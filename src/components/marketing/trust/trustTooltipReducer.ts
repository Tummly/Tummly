/**
 * Open-state rules for the trust indicator toggletip. Pure, so the timing and
 * source rules can be unit-tested in node. The component owns the 120 ms timer:
 * it runs while `closePending` is true and sends `close-timer` when it fires.
 */

export type TrustTooltipSource = "hover" | "focus" | "click"

export type TrustTooltipState = {
  open: boolean
  /** What opened the tooltip. Only a `hover` open closes on pointer leave. */
  source: TrustTooltipSource | null
  /** A mouse left the trigger or the content; the close timer is running. */
  closePending: boolean
}

export type TrustTooltipEvent =
  /** Mouse pointerenter on the trigger. */
  | { type: "hover-enter" }
  /** Keyboard focus on the trigger with `:focus-visible`. */
  | { type: "focus-visible" }
  /** Trigger click, tap, Enter or Space. Toggles. */
  | { type: "click" }
  /** Mouse pointerleave from the trigger or the content. */
  | { type: "pointer-leave" }
  /** Mouse pointerenter on the content. */
  | { type: "content-enter" }
  /** The 120 ms close timer fired. */
  | { type: "close-timer" }
  /** Escape, an outside pointer, or focus left both trigger and content. */
  | { type: "close" }

export const TRUST_TOOLTIP_CLOSE_DELAY_MS = 120

export const TRUST_TOOLTIP_INITIAL_STATE: TrustTooltipState = {
  open: false,
  source: null,
  closePending: false,
}

const CLOSED_STATE: TrustTooltipState = TRUST_TOOLTIP_INITIAL_STATE

export function trustTooltipReducer(
  state: TrustTooltipState,
  event: TrustTooltipEvent,
): TrustTooltipState {
  switch (event.type) {
    case "hover-enter":
      if (!state.open) return { open: true, source: "hover", closePending: false }
      return state.closePending ? { ...state, closePending: false } : state

    case "focus-visible":
      return state.open ? state : { open: true, source: "focus", closePending: false }

    case "click":
      return state.open ? CLOSED_STATE : { open: true, source: "click", closePending: false }

    case "pointer-leave":
      return state.open && state.source === "hover" ? { ...state, closePending: true } : state

    case "content-enter":
      return state.closePending ? { ...state, closePending: false } : state

    case "close-timer":
      return state.closePending ? CLOSED_STATE : state

    case "close":
      return state.open ? CLOSED_STATE : state
  }
}
