import { describe, expect, it } from "vitest"

import { buildGuestFeedbackUnlockCopy } from "@/lib/guestFeedback/guestFeedbackUnlockPresentation"

describe("buildGuestFeedbackUnlockCopy", () => {
  it("builds the Figma SMS unlock screen copy", () => {
    expect(
      buildGuestFeedbackUnlockCopy({
        restaurantName: "KFC",
        locationName: "Camden High Street",
        offerTitle: "Free dessert",
        channel: "sms",
      })
    ).toEqual({
      thankYouHeading: "Thank you.",
      sharedBody:
        "Your feedback has been shared privately with the team at KFC — Camden High Street.",
      wantHeading: "Want Free dessert?",
      joinBody:
        "Join KFC for occasional offers and updates to unlock your welcome offer.",
      unlockCta: "Yes — text me & unlock Free dessert",
      declineCta: "No thanks",
      optOutNote: "You can opt out at any time.",
    })
  })

  it("builds the email channel CTA and unsubscribe note", () => {
    const copy = buildGuestFeedbackUnlockCopy({
      restaurantName: "Cafe",
      locationName: "Soho",
      offerTitle: "10% off",
      channel: "email",
    })
    expect(copy.unlockCta).toBe("Yes — email me & unlock 10% off")
    expect(copy.optOutNote).toBe("You can unsubscribe at any time.")
  })
})
