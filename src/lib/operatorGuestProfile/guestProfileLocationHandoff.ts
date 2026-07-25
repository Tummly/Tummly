import type { OperatorGuestProfileTabId } from "@/types/operatorGuestProfile"

/** One-shot router location state from Guest details → Guest Profile. */
export type GuestProfileLocationHandoff = {
  tab?: OperatorGuestProfileTabId
  openFeedbackId?: number
}

const PROFILE_TABS: ReadonlySet<OperatorGuestProfileTabId> = new Set([
  "overview",
  "feedbacks",
  "offers",
  "campaigns",
  "activity",
  "notes",
])

/**
 * Reads Guest Profile handoff intent from router location state.
 * Invalid / missing fields are ignored (defaults apply at the consumer).
 */
export function readGuestProfileLocationHandoff(
  state: unknown
): GuestProfileLocationHandoff {
  if (state == null || typeof state !== "object") {
    return {}
  }

  const raw = state as Record<string, unknown>
  const handoff: GuestProfileLocationHandoff = {}

  if (
    typeof raw.tab === "string"
    && PROFILE_TABS.has(raw.tab as OperatorGuestProfileTabId)
  ) {
    handoff.tab = raw.tab as OperatorGuestProfileTabId
  }

  if (
    typeof raw.openFeedbackId === "number"
    && Number.isFinite(raw.openFeedbackId)
    && raw.openFeedbackId > 0
  ) {
    handoff.openFeedbackId = raw.openFeedbackId
  }

  return handoff
}

export function guestProfileHandoffHasIntent(
  handoff: GuestProfileLocationHandoff
): boolean {
  return handoff.tab != null || handoff.openFeedbackId != null
}
