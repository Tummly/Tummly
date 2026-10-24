import { describe, expect, it } from "vitest"

import {
  guestFeedbackDefaultValues,
  guestFeedbackSchema,
  toGuestFeedbackPayload,
} from "@/schemas/guestFeedback"

const validGuestFeedback = {
  guestName: "Alex",
  guestContact: "alex@example.com",
  comment: "A useful visit.",
  acceptsOffers: false,
}

describe("guestFeedbackDefaultValues", () => {
  it("leaves marketing unchecked by default (GF-03 hard opt-in)", () => {
    expect(guestFeedbackDefaultValues.acceptsOffers).toBe(false)
  })
})

describe("guestFeedbackSchema", () => {
  it("rejects two contacts in one field", () => {
    const result = guestFeedbackSchema.safeParse({
      ...validGuestFeedback,
      guestContact: "alex@example.com 07123456789",
    })

    expect(result.success).toBe(false)
  })

  it("requires first name with the guest form message", () => {
    const result = guestFeedbackSchema.safeParse({
      ...validGuestFeedback,
      guestName: "",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe("First name is required.")
    }
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
