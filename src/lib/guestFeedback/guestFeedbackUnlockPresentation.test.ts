import { describe, expect, it } from "vitest"

import {
  buildGuestFeedbackSharedPrivatelyBody,
  buildGuestFeedbackUnlockCopy,
} from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"

describe("buildGuestFeedbackUnlockCopy", () => {
  it("builds the Figma SMS unlock screen copy", () => {
    expect(
      buildGuestFeedbackUnlockCopy({
        restaurantName: "KFC",
        locationName: "Camden High Street",
        offerTitle: "14% OFF YOUR NEXT ORDER",
        channel: "sms",
      })
    ).toEqual({
      thankYouHeading: "Thank you.",
      sharedBody:
        "Your feedback has been shared privately with the team at KFC — Camden High Street.",
      wantHeading: "Want 14% OFF YOUR NEXT ORDER?",
      joinBody:
        "Join KFC for occasional offers and updates to get your thank-you offer.",
      unlockCta: "Yes — join by SMS & get 14% OFF YOUR NEXT ORDER",
      declineCta: "No thanks",
      optOutNote: "You can opt out at any time.",
    })
  })

  it("builds the shared privately thank-you body for restaurant and location", () => {
    expect(
      buildGuestFeedbackSharedPrivatelyBody("KFC", "Camden High Street")
    ).toBe(
      "Your feedback has been shared privately with the team at KFC — Camden High Street."
    )
  })

  it("builds the email channel CTA and unsubscribe note", () => {
    const copy = buildGuestFeedbackUnlockCopy({
      restaurantName: "Cafe",
      locationName: "Soho",
      offerTitle: "10% off",
      channel: "email",
    })
    expect(copy.unlockCta).toBe("Yes — join by email & get 10% off")
    expect(copy.optOutNote).toBe("You can unsubscribe at any time.")
  })
})
