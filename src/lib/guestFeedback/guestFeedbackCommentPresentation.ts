import type { GuestMicSttSnapshot } from "@/lib/guestFeedback/createGuestMicSttModule"

export const GUEST_FEEDBACK_COMMENT_COPY = {
  idlePlaceholder: "Type your feedback or tap the mic to dictate…",
  recordingHint: "Speak now. We’ll turn your words into editable text.",
} as const

export type GuestFeedbackCommentPresentation = {
  placeholder: typeof GUEST_FEEDBACK_COMMENT_COPY.idlePlaceholder
  recordingHint: typeof GUEST_FEEDBACK_COMMENT_COPY.recordingHint
  /** True only while the mic is actively recording (not transcribing). */
  isRecording: boolean
}

export function guestFeedbackCommentPresentation(
  phase: GuestMicSttSnapshot["phase"]
): GuestFeedbackCommentPresentation {
  return {
    placeholder: GUEST_FEEDBACK_COMMENT_COPY.idlePlaceholder,
    recordingHint: GUEST_FEEDBACK_COMMENT_COPY.recordingHint,
    isRecording: phase === "recording",
  }
}
