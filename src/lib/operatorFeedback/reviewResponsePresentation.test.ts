import { describe, expect, it } from "vitest"

import {
  formatReviewAiUsage,
  formatReviewDeliveryUsage,
  formatReviewFeedbackReference,
  labelForReviewChannel,
  REVIEW_RESPONSE_EMPTY_VALUE,
} from "./reviewResponsePresentation"

describe("reviewResponsePresentation", () => {
  it("formats AI usage as em dash for 0, singular for 1, plural for N", () => {
    expect(formatReviewAiUsage(0)).toBe(REVIEW_RESPONSE_EMPTY_VALUE)
    expect(formatReviewAiUsage(1)).toBe("1 AI action used")
    expect(formatReviewAiUsage(2)).toBe("2 AI actions used")
    expect(formatReviewAiUsage(7)).toBe("7 AI actions used")
  })

  it("formats Delivery usage from channel, or em dash when none", () => {
    expect(formatReviewDeliveryUsage("email")).toBe("1 email")
    expect(formatReviewDeliveryUsage("sms")).toBe("1 SMS")
    expect(formatReviewDeliveryUsage(null)).toBe(REVIEW_RESPONSE_EMPTY_VALUE)
  })

  it("labels Review channel rows as Email, SMS, or em dash", () => {
    expect(labelForReviewChannel("email")).toBe("Email")
    expect(labelForReviewChannel("sms")).toBe("SMS")
    expect(labelForReviewChannel(null)).toBe(REVIEW_RESPONSE_EMPTY_VALUE)
  })

  it("formats Feedback reference as FDB-padded id", () => {
    expect(formatReviewFeedbackReference(2418)).toBe("FDB-002418")
    expect(formatReviewFeedbackReference(7)).toBe("FDB-000007")
  })
})
