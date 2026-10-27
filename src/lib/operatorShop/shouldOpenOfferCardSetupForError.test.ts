import { describe, expect, it } from "vitest"

import { axiosErrorWithBody } from "@/lib/operatorShop/shopApiErrorTestHelpers"
import { shouldOpenOfferCardSetupForError } from "@/lib/operatorShop/shouldOpenOfferCardSetupForError"

describe("shouldOpenOfferCardSetupForError", () => {
  it("is true for a 409 offer_card_offer_required body", () => {
    const error = axiosErrorWithBody(409, {
      success: false,
      code: "offer_card_offer_required",
      message: "Attach a card offer first.",
    })

    expect(shouldOpenOfferCardSetupForError(error)).toBe(true)
  })

  it("is false for other Shop error codes", () => {
    expect(
      shouldOpenOfferCardSetupForError(
        axiosErrorWithBody(409, { code: "qr_limit_reached", limit: 2 })
      )
    ).toBe(false)
    expect(
      shouldOpenOfferCardSetupForError(
        axiosErrorWithBody(409, { code: "sku_not_available" })
      )
    ).toBe(false)
  })

  it("is false when the Axios body has no code", () => {
    expect(
      shouldOpenOfferCardSetupForError(axiosErrorWithBody(500, { message: "boom" }))
    ).toBe(false)
  })

  it("is false for errors that are not Axios errors", () => {
    expect(
      shouldOpenOfferCardSetupForError(new Error("offer_card_offer_required"))
    ).toBe(false)
    expect(shouldOpenOfferCardSetupForError("offer_card_offer_required")).toBe(false)
    expect(shouldOpenOfferCardSetupForError(undefined)).toBe(false)
  })
})
