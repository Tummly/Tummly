/**
 * Open-state rules for the trust indicator toggletip. Pure, so the timing and
 * source rules can be unit-tested in node. The component owns the 120 ms timer:
 * it runs while `closePending` is true and sends `close-timer` when it fires.
 */

export type TrustTooltipSource = "hover" | "focus" | "click"

export type TrustTooltipState = {
  open: boolean
  /** What opened the tooltip, or what last took it over (see `click` and `focus-visible`). */
  source: TrustTooltipSource | null
  /** A mouse left the trigger or the content; the close timer is running. */
  closePending: boolean
  /**
   * Set by `dismiss` (Escape or an outside pointer). Radix then returns focus to the
   * trigger, and that focus must not reopen the tooltip. Cleared by the next `focus-visible`
   * or by `input`.
   */
  suppressFocus: boolean
}

export type TrustTooltipEvent =
  /** Mouse pointerenter on the trigger. */
  | { type: "hover-enter" }
  /** Keyboard focus on the trigger with `:focus-visible`. */
  | { type: "focus-visible" }
  /** Trigger click, tap, Enter or Space. */
  | { type: "click" }
  /** Mouse pointerleave from the trigger or the content. */
  | { type: "pointer-leave" }
  /** Mouse pointerenter on the content. */
  | { type: "content-enter" }
  /** The 120 ms close timer fired. */
  | { type: "close-timer" }
  /** Focus left both trigger and content. */
  | { type: "close" }
  /** Escape or an outside pointer. Closes and suppresses the focus Radix returns. */
  | { type: "dismiss" }
  /** Next pointer or key press anywhere. Ends the focus suppression. */
  | { type: "input" }

export const TRUST_TOOLTIP_CLOSE_DELAY_MS = 120

export const TRUST_TOOLTIP_INITIAL_STATE: TrustTooltipState = {
  open: false,
  source: null,
  closePending: false,
  suppressFocus: false,
}

const CLOSED_STATE: TrustTooltipState = TRUST_TOOLTIP_INITIAL_STATE

function openedBy(source: TrustTooltipSource): TrustTooltipState {
  return { open: true, source, closePending: false, suppressFocus: false }
}

export function trustTooltipReducer(
  state: TrustTooltipState,
  event: TrustTooltipEvent,
): TrustTooltipState {
  switch (event.type) {
    case "hover-enter":
      if (!state.open) return openedBy("hover")
      return state.closePending ? { ...state, closePending: false } : state

    case "focus-visible":
      if (state.suppressFocus) return { ...state, suppressFocus: false }
      if (!state.open) return openedBy("focus")
      // A keyboard focus takes over a mouse open, so the mouse leave no longer closes it.
      if (state.source === "hover") return { ...state, source: "focus", closePending: false }
      return state

    case "click":
      // Ruling 21: a click on a click-opened tooltip closes it. A click on a hover-
      // or focus-opened one keeps it open and makes the click its source.
      if (!state.open) return openedBy("click")
      if (state.source === "click") return CLOSED_STATE
      return { ...state, source: "click", closePending: false }

    case "pointer-leave":
      return state.open && state.source === "hover" ? { ...state, closePending: true } : state

    case "content-enter":
      return state.closePending ? { ...state, closePending: false } : state

    case "close-timer":
      return state.closePending ? CLOSED_STATE : state

    case "close":
      return state.open ? CLOSED_STATE : state

    case "dismiss":
      return { ...CLOSED_STATE, suppressFocus: true }

    case "input":
      return state.suppressFocus ? { ...state, suppressFocus: false } : state
  }
}
