import { describe, expect, it } from "vitest"

import {
  guestFeedbackDefaultValues,
  toGuestFeedbackPayload,
} from "@/schemas/guestFeedback"

describe("guestFeedbackDefaultValues", () => {
  it("leaves marketing unchecked by default (GF-03 hard opt-in)", () => {
    expect(guestFeedbackDefaultValues.acceptsOffers).toBe(false)
  })
})

describe("toGuestFeedbackPayload", () => {
  it.each([
    { acceptsOffers: true, expectedOffersOptOut: false },
    { acceptsOffers: false, expectedOffersOptOut: true },
  ])(
    "maps acceptsOffers=$acceptsOffers to offersOptOut=$expectedOffersOptOut",
    ({ acceptsOffers, expectedOffersOptOut }) => {
      expect(
        toGuestFeedbackPayload({
          guestName: "  Alex Guest  ",
          guestContact: "  alex@example.com  ",
          comment: "  A useful visit.  ",
          acceptsOffers,
        })
      ).toEqual({
        guestName: "Alex Guest",
        guestContact: "alex@example.com",
        comment: "A useful visit.",
        offersOptOut: expectedOffersOptOut,
      })
    }
  )
})
