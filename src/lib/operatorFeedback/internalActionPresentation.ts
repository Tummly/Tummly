/**
 * Shared internal-action recorder categories and labels (Record-only + Respond and record).
 * Wire keys are snake_case; labels match Figma / PRD.
 */

export type InternalActionCategoryId =
  | "team_briefed"
  | "order_or_service_process_reviewed"
  | "delivery_issue_investigated"
  | "product_quality_checked"
  | "cleaning_issue_addressed"
  | "staff_follow_up_completed"
  | "other_action"

export type InternalActionCategoryOption = {
  id: InternalActionCategoryId
  label: string
}

export const INTERNAL_ACTION_CATEGORY_OPTIONS: readonly InternalActionCategoryOption[] =
  [
    { id: "team_briefed", label: "Team briefed" },
    {
      id: "order_or_service_process_reviewed",
      label: "Order or service process reviewed",
    },
    {
      id: "delivery_issue_investigated",
      label: "Delivery issue investigated",
    },
    {
      id: "product_quality_checked",
      label: "Product quality checked",
    },
    {
      id: "cleaning_issue_addressed",
      label: "Cleaning issue addressed",
    },
    {
      id: "staff_follow_up_completed",
      label: "Staff follow-up completed",
    },
    { id: "other_action", label: "Other action" },
  ] as const

export const INTERNAL_ACTION_NOTE_PLACEHOLDER =
  "Describe what was reviewed or changed…"

export const INTERNAL_ACTION_NOTE_HELPER =
  "Internal only — not sent to the guest."

/** Respond and record — gates Continue until checked (PRD). */
export const INTERNAL_ACTION_USE_FOR_GUEST_RESPONSE_LABEL =
  "Use this confirmed action when preparing the guest response"

/**
 * Record-only Review footer primary (U-02).
 * Not Figma’s guest-send debt string — that path does not contact the guest.
 * Confirm dialog may still use “Send and record”.
 */
export const RECORD_INTERNAL_ONLY_REVIEW_PRIMARY_CTA =
  "Record internal follow-up"

/** Required note max length (matches backend). */
export const INTERNAL_ACTION_NOTE_MAX_LENGTH = 2000

export function labelForInternalActionCategory(
  category: InternalActionCategoryId | null
): string | null {
  if (category == null) {
    return null
  }
  return (
    INTERNAL_ACTION_CATEGORY_OPTIONS.find((option) => option.id === category)
      ?.label ?? null
  )
}

/** Category + non-empty note required. Guest-response checkbox is not part of Record-only. */
export function canContinueInternalActionRecorder(input: {
  category: InternalActionCategoryId | null
  note: string
}): boolean {
  return (
    input.category != null
    && input.note.trim().length > 0
    && input.note.trim().length <= INTERNAL_ACTION_NOTE_MAX_LENGTH
  )
}

/**
 * Respond and record recorder Continue — category, note, and the
 * use-for-guest-response checkbox (default unchecked).
 */
export function canContinueRespondAndRecordRecorder(input: {
  category: InternalActionCategoryId | null
  note: string
  useConfirmedActionForGuestResponse: boolean
}): boolean {
  return (
    canContinueInternalActionRecorder(input)
    && input.useConfirmedActionForGuestResponse
  )
}
