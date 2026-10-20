import { describe, expect, it } from "vitest"

import {
  BULK_REVIEW_ORIGINAL_FEEDBACK_COPY,
  formatBulkReviewDestinationLabel,
  formatBulkReviewFeedbackReference,
  formatBulkReviewGuestLabel,
  formatReviewDeliveryUsage,
} from "@/lib/operatorFeedback/reviewResponsePresentation"

describe("bulk review presentation", () => {
  it("formats guest and destination counts", () => {
    expect(formatBulkReviewGuestLabel(12)).toBe("12 guests")
    expect(formatBulkReviewDestinationLabel("email", 12)).toBe(
      "12 email destinations"
    )
    expect(formatBulkReviewFeedbackReference(12)).toBe("12 Feedback")
    expect(formatReviewDeliveryUsage("email", 12)).toBe("12 emails")
    expect(BULK_REVIEW_ORIGINAL_FEEDBACK_COPY.length).toBeGreaterThan(0)
  })
})
