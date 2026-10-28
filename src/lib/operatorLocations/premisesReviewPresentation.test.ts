import { describe, expect, it } from "vitest"

import {
  normalizePremisesReviewWire,
  premisesReviewLabel,
} from "@/lib/operatorLocations/premisesReviewPresentation"

describe("premisesReviewPresentation", () => {
  it("normalizes only the two wire values", () => {
    expect(normalizePremisesReviewWire("pending_review")).toBe("pending_review")
    expect(normalizePremisesReviewWire("rejected")).toBe("rejected")
    expect(normalizePremisesReviewWire("cleared")).toBeNull()
    expect(normalizePremisesReviewWire(undefined)).toBeNull()
    expect(normalizePremisesReviewWire(null)).toBeNull()
  })

  it("labels a pending review with the approved copy", () => {
    expect(premisesReviewLabel("pending_review")).toBe(
      "Saved as Draft for review"
    )
  })

  it("has no label when no review applies", () => {
    expect(premisesReviewLabel(null)).toBeNull()
    expect(premisesReviewLabel(undefined)).toBeNull()
  })
})
