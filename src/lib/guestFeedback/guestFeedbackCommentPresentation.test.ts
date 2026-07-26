import { describe, expect, it } from "vitest"

import {
  GUEST_FEEDBACK_COMMENT_COPY,
  guestFeedbackCommentPresentation,
} from "./guestFeedbackCommentPresentation"

describe("guestFeedbackCommentPresentation", () => {
  it("idle shows type-or-mic placeholder and is not recording", () => {
    expect(guestFeedbackCommentPresentation("idle")).toEqual({
      placeholder: "Type your feedback or tap the mic to dictate…",
      recordingHint: GUEST_FEEDBACK_COMMENT_COPY.recordingHint,
      isRecording: false,
    })
  })

  it("recording exposes speak-now hint and marks recording", () => {
    expect(guestFeedbackCommentPresentation("recording")).toEqual({
      placeholder: "Type your feedback or tap the mic to dictate…",
      recordingHint: "Speak now. We’ll turn your words into editable text.",
      isRecording: true,
    })
  })

  it("transcribing is not recording so speak-now is off", () => {
    expect(guestFeedbackCommentPresentation("transcribing")).toEqual({
      placeholder: "Type your feedback or tap the mic to dictate…",
      recordingHint: "Speak now. We’ll turn your words into editable text.",
      isRecording: false,
    })
  })
})
