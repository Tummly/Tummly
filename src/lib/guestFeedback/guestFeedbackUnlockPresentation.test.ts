import { describe, expect, it } from "vitest"

import { buildGuestFeedbackSharedPrivatelyBody } from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"

describe("buildGuestFeedbackSharedPrivatelyBody", () => {
  it("builds the shared privately thank-you body for restaurant and location", () => {
    expect(
      buildGuestFeedbackSharedPrivatelyBody("KFC", "Camden High Street")
    ).toBe(
      "Your feedback has been shared privately with the team at KFC — Camden High Street."
    )
  })
})
