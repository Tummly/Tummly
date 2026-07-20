import { describe, expect, it } from "vitest"

import { toGuestFeedbackPayload } from "@/schemas/guestFeedback"

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
