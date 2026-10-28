import type { LinkFailure } from "@/lib/guestPreferences/guestPreferencesRequestErrors"

/**
 * The add-contact confirm page. Nothing is sent until the guest presses Confirm.
 * A settled call is confirmed, expired (dead link) or error (retry allowed).
 */
export type LinkConfirmState =
  | { status: "idle" }
  | { status: "confirming" }
  | { status: "confirmed" }
  | { status: "expired" }
  | { status: "error" }

export type LinkConfirmEvent =
  | { type: "confirm" }
  | { type: "settled"; outcome: "ok" | LinkFailure }

export const LINK_CONFIRM_INITIAL: LinkConfirmState = { status: "idle" }

export function reduceLinkConfirm(
  state: LinkConfirmState,
  event: LinkConfirmEvent
): LinkConfirmState {
  if (event.type === "confirm") {
    return state.status === "idle" || state.status === "error"
      ? { status: "confirming" }
      : state
  }

  if (state.status !== "confirming") {
    return state
  }
  if (event.outcome === "ok") {
    return { status: "confirmed" }
  }
  return event.outcome === "invalid" ? { status: "expired" } : { status: "error" }
}
