import { describe, expect, it } from "vitest"

import { axiosErrorWithBody } from "@/lib/operatorShop/shopApiErrorTestHelpers"
import { readShopApiErrorCode } from "@/lib/operatorShop/readShopApiErrorCode"

describe("readShopApiErrorCode", () => {
  it("returns the body code from a 409 Axios error", () => {
    const error = axiosErrorWithBody(409, {
      success: false,
      code: "offer_card_offer_required",
      message: "Attach a card offer first.",
    })

    expect(readShopApiErrorCode(error)).toBe("offer_card_offer_required")
  })

  it("returns null when the Axios error has no response body code", () => {
    expect(readShopApiErrorCode(axiosErrorWithBody(500, { message: "boom" }))).toBeNull()
    expect(readShopApiErrorCode(axiosErrorWithBody(500, null))).toBeNull()
  })

  it("returns null for a non-string code", () => {
    expect(readShopApiErrorCode(axiosErrorWithBody(409, { code: 409 }))).toBeNull()
  })

  it("returns null for errors that are not Axios errors", () => {
    expect(readShopApiErrorCode(new Error("offer_card_offer_required"))).toBeNull()
    expect(readShopApiErrorCode("offer_card_offer_required")).toBeNull()
    expect(readShopApiErrorCode(undefined)).toBeNull()
  })
})
