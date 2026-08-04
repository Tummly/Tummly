import { describe, expect, it } from "vitest"

import {
  canSaveFeedbackClassificationCorrection,
  feedbackClassificationCorrectionRequiresNote,
  formatAiClassifiedMetaLine,
} from "./feedbackClassificationCorrectionPresentation"

describe("feedbackClassificationCorrectionPresentation", () => {
  it("gates save until sentiment and reason are set and sentiment changes", () => {
    expect(
      canSaveFeedbackClassificationCorrection({
        currentSentiment: "negative",
        draftSentiment: null,
        reason: "incorrect_ai_classification",
        noteDraft: "",
        saveStatus: "idle",
      })
    ).toBe(false)

    expect(
      canSaveFeedbackClassificationCorrection({
        currentSentiment: "negative",
        draftSentiment: "negative",
        reason: "incorrect_ai_classification",
        noteDraft: "",
        saveStatus: "idle",
      })
    ).toBe(false)

    expect(
      canSaveFeedbackClassificationCorrection({
        currentSentiment: "negative",
        draftSentiment: "positive",
        reason: null,
        noteDraft: "",
        saveStatus: "idle",
      })
    ).toBe(false)

    expect(
      canSaveFeedbackClassificationCorrection({
        currentSentiment: "negative",
        draftSentiment: "positive",
        reason: "incorrect_ai_classification",
        noteDraft: "",
        saveStatus: "idle",
      })
    ).toBe(true)
  })

  it("requires a trimmed note when reason is Other", () => {
    expect(feedbackClassificationCorrectionRequiresNote("other")).toBe(true)
    expect(
      feedbackClassificationCorrectionRequiresNote("mixed_or_ambiguous")
    ).toBe(false)

    expect(
      canSaveFeedbackClassificationCorrection({
        currentSentiment: "negative",
        draftSentiment: "neutral",
        reason: "other",
        noteDraft: "   ",
        saveStatus: "idle",
      })
    ).toBe(false)

    expect(
      canSaveFeedbackClassificationCorrection({
        currentSentiment: "negative",
        draftSentiment: "neutral",
        reason: "other",
        noteDraft: "Tone was sarcastic",
        saveStatus: "idle",
      })
    ).toBe(true)
  })

  it("formats the AI classified meta line", () => {
    expect(formatAiClassifiedMetaLine("13 July 2026 at 11:49 AM")).toBe(
      "AI classified · 13 July 2026 at 11:49 AM"
    )
    expect(formatAiClassifiedMetaLine("")).toBe("AI classified")
  })
})
