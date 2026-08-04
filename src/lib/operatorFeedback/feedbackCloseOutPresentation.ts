export type FeedbackCloseOutIntent =
  | "mark_resolved"
  | "mark_no_action_needed"

export type FeedbackCloseOutReason =
  | "positive_no_follow_up"
  | "duplicate_submission"
  | "test_or_invalid"
  | "already_handled_outside"
  | "no_appropriate_follow_up"
  | "other"

export const FEEDBACK_CLOSE_OUT_REASONS: readonly {
  value: FeedbackCloseOutReason
  label: string
}[] = [
  {
    value: "positive_no_follow_up",
    label: "Positive feedback with no follow-up required",
  },
  {
    value: "duplicate_submission",
    label: "Duplicate submission",
  },
  {
    value: "test_or_invalid",
    label: "Test or invalid submission",
  },
  {
    value: "already_handled_outside",
    label: "Already handled outside Tummly",
  },
  {
    value: "no_appropriate_follow_up",
    label: "No appropriate follow-up available",
  },
  {
    value: "other",
    label: "Other",
  },
] as const

/** Reason select placeholder — Figma close-out frames. */
export const feedbackCloseOutReasonPlaceholder = "Select"

/** High-risk callout — Mark no action needed, negative only (Figma `4481:18601`). */
export const feedbackCloseOutHighRiskCallout =
  "This feedback contains a possible high-risk issue. Additional permission and an internal note are required before it can be closed."

/** Confirmation checkbox label — Mark no action needed (Figma `4481:18601`). */
export const FEEDBACK_CLOSE_OUT_ACKNOWLEDGMENT_LABEL =
  "I have reviewed this feedback and confirmed that no further action is required."

export function feedbackCloseOutRequiresAcknowledgment(
  intent: FeedbackCloseOutIntent | null
): boolean {
  return intent === "mark_no_action_needed"
}

/** High-risk callout — Mark no action needed when Feedback sentiment is negative. */
export function feedbackCloseOutShowsHighRiskCallout(
  intent: FeedbackCloseOutIntent | null,
  sentiment: "positive" | "neutral" | "negative" | null | undefined
): boolean {
  return (
    feedbackCloseOutRequiresAcknowledgment(intent) && sentiment === "negative"
  )
}

export function feedbackCloseOutDialogCopy(intent: FeedbackCloseOutIntent): {
  title: string
  subtitle: string
  confirmLabel: string
  notePlaceholder: string
} {
  if (intent === "mark_resolved") {
    return {
      title: "Mark feedback as resolved?",
      subtitle:
        "Confirm how the feedback was handled. The resolution will be recorded in the activity history.",
      confirmLabel: "Mark resolved",
      notePlaceholder: "Explain why this feedback is resolved…",
    }
  }

  return {
    title: "Mark as no action needed?",
    subtitle:
      "Use this when the feedback has been reviewed and no recovery or operational follow-up is required.",
    confirmLabel: "Mark no action needed",
    notePlaceholder: "Explain why no further action is required…",
  }
}

export function feedbackCloseOutReasonLabel(
  reason: FeedbackCloseOutReason
): string {
  return (
    FEEDBACK_CLOSE_OUT_REASONS.find((option) => option.value === reason)
      ?.label ?? reason
  )
}

export function feedbackClosedOutActivityLabel(input: {
  intent: FeedbackCloseOutIntent | null | undefined
  reason?: FeedbackCloseOutReason | null
  fromWorkflowStatus?: "new" | "in_progress" | "resolved" | null
  toWorkflowStatus?: "new" | "in_progress" | "resolved" | null
}): string {
  const intentLabel =
    input.intent === "mark_no_action_needed"
      ? "Marked as no action needed"
      : "Marked as resolved"

  const reasonLabel =
    input.reason != null ? feedbackCloseOutReasonLabel(input.reason) : null

  const from =
    input.fromWorkflowStatus === "new"
      ? "New"
      : input.fromWorkflowStatus === "in_progress"
        ? "In progress"
        : input.fromWorkflowStatus === "resolved"
          ? "Resolved"
          : null
  const to =
    input.toWorkflowStatus === "new"
      ? "New"
      : input.toWorkflowStatus === "in_progress"
        ? "In progress"
        : input.toWorkflowStatus === "resolved"
          ? "Resolved"
          : null

  const parts = [intentLabel]
  if (reasonLabel != null) {
    parts.push(reasonLabel)
  }
  if (from != null && to != null) {
    parts.push(`${from} → ${to}`)
  }
  return parts.join(" · ")
}

export function canConfirmFeedbackCloseOut(input: {
  intent: FeedbackCloseOutIntent | null
  reason: FeedbackCloseOutReason | null
  noteDraft: string
  acknowledged: boolean
}): boolean {
  if (input.reason == null) {
    return false
  }
  if (input.reason === "other" && input.noteDraft.trim().length === 0) {
    return false
  }
  if (
    feedbackCloseOutRequiresAcknowledgment(input.intent)
    && !input.acknowledged
  ) {
    return false
  }
  return true
}
