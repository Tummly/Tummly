import type { FeedbackClassificationCorrectionReason } from "@/types/dashboard"

export type { FeedbackClassificationCorrectionReason }

export const FEEDBACK_CLASSIFICATION_CORRECTION_REASONS: readonly {
  value: FeedbackClassificationCorrectionReason
  label: string
}[] = [
  {
    value: "mixed_or_ambiguous",
    label: "The feedback is mixed or ambiguous",
  },
  {
    value: "context_misunderstood",
    label: "Important context was misunderstood",
  },
  {
    value: "language_or_translation",
    label: "Language or translation issue",
  },
  {
    value: "incorrect_ai_classification",
    label: "Incorrect AI classification",
  },
  {
    value: "other",
    label: "Other",
  },
] as const

/** Figma `4481:20220` — Correct classification dialog. */
export const FEEDBACK_CLASSIFICATION_CORRECTION_COPY = {
  title: "Correct classification",
  subtitle:
    "Update the AI classification if it does not accurately represent the guest’s feedback.",
  currentClassificationLabel: "Current classification",
  newClassificationLabel: "New classification",
  reasonLabel: "Reason for correction",
  noteLabel: "Additional note",
  selectPlaceholder: "Select",
  notePlaceholder: "Add any helpful context about this correction…",
  callout:
    "This correction will update Feedback summary figures and classification filters. The original AI classification will remain in the audit history.",
  confirmLabel: "Save correction",
  cancelLabel: "Cancel",
} as const

export function feedbackClassificationCorrectionRequiresNote(
  reason: FeedbackClassificationCorrectionReason | null
): boolean {
  return reason === "other"
}

export function canSaveFeedbackClassificationCorrection(input: {
  currentSentiment: "positive" | "neutral" | "negative" | null
  draftSentiment: "positive" | "neutral" | "negative" | null
  reason: FeedbackClassificationCorrectionReason | null
  noteDraft: string
  saveStatus: "idle" | "saving" | "error"
}): boolean {
  if (input.saveStatus === "saving") {
    return false
  }
  if (input.draftSentiment == null || input.reason == null) {
    return false
  }
  if (
    input.currentSentiment == null
    || input.draftSentiment === input.currentSentiment
  ) {
    return false
  }
  if (
    feedbackClassificationCorrectionRequiresNote(input.reason)
    && input.noteDraft.trim().length === 0
  ) {
    return false
  }
  return true
}

export function formatAiClassifiedMetaLine(absoluteDatetime: string): string {
  const trimmed = absoluteDatetime.trim()
  if (trimmed === "") {
    return "AI classified"
  }
  return `AI classified · ${trimmed}`
}
