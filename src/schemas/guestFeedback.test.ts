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

  it.each([
    "020 7946 0000",
    "+44 20 7946 0000",
    "02079460000",
  ])("rejects UK landline contact %s", (guestContact) => {
    const result = guestFeedbackSchema.safeParse({
      ...validGuestFeedback,
      guestContact,
    })

    expect(result.success).toBe(false)
  })

  it.each(["07911 123456", "+44 7911 123456"])(
    "accepts UK mobile contact %s",
    (guestContact) => {
      const result = guestFeedbackSchema.safeParse({
        ...validGuestFeedback,
        guestContact,
      })

      expect(result.success).toBe(true)
    }
  )

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
