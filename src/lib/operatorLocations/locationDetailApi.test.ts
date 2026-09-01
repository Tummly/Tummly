import { describe, expect, it } from "vitest"

import { locationDetailRecoveryFeedbackPath } from "@/lib/operatorLocations/locationDetailApi"

describe("locationDetailRecoveryFeedbackPath", () => {
  it("appends feedbackId when the path has no query string", () => {
    expect(
      locationDetailRecoveryFeedbackPath("/single-dashboard/feedback", 42)
    ).toBe("/single-dashboard/feedback?feedbackId=42")
  })

  it("joins feedbackId with & when the path already has query params", () => {
    expect(
      locationDetailRecoveryFeedbackPath(
        "/single-dashboard/feedback?locationId=9",
        42
      )
    ).toBe("/single-dashboard/feedback?locationId=9&feedbackId=42")
  })
})
