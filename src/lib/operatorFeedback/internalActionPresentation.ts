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
  /** Figma card subtitle — display-only chrome, not persisted. */
  description: string
}

export const INTERNAL_ACTION_CATEGORY_OPTIONS: readonly InternalActionCategoryOption[] =
  [
    {
      id: "team_briefed",
      label: "Team briefed",
      description:
        "The relevant restaurant team has been informed about the feedback.",
    },
    {
      id: "order_or_service_process_reviewed",
      label: "Order or service process reviewed",
      description:
        "The team reviewed a process connected to the guest’s experience.",
    },
    {
      id: "delivery_issue_investigated",
      label: "Delivery issue investigated",
      description:
        "The restaurant reviewed delivery preparation or handoff.",
    },
    {
      id: "product_quality_checked",
      label: "Product quality checked",
      description: "The restaurant reviewed food or product quality.",
    },
    {
      id: "cleaning_issue_addressed",
      label: "Cleaning issue addressed",
      description:
        "The restaurant reviewed or corrected a cleanliness issue.",
    },
    {
      id: "staff_follow_up_completed",
      label: "Staff follow-up completed",
      description:
        "The feedback was discussed with the relevant staff member or team.",
    },
    {
      id: "other_action",
      label: "Other action",
      description: "Record a different operational action.",
    },
  ] as const

export const INTERNAL_ACTION_NOTE_PLACEHOLDER =
  "Describe what the restaurant reviewed, changed or plans to follow up…"

export const INTERNAL_ACTION_NOTE_HELPER =
  "This note is internal and will not be sent to the guest."

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

/**
 * Display-only Review “Follow-up state” chip (U-11 / PRD).
 * Not a persisted recovery-status or follow-up-status enum.
 */
export const INTERNAL_ACTION_FOLLOW_UP_STATE_LABEL = "Mark follow-up complete"

/** Display-only Success “Follow-up status” chip. */
export const INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL = "Complete"

/** Display-only Success “Recovery status” chip after record. */
export const INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL =
  "Internal action recorded"

/** Display-only Success “Workflow status” chip — Feedback stays In progress. */
export const INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL = "In progress"

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
