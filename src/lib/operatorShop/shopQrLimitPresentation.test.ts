import { describe, expect, it } from "vitest"

import { axiosErrorWithBody } from "@/lib/operatorShop/shopApiErrorTestHelpers"
import {
  SHOP_QR_LIMIT_COPY,
  buildShopQrLimitBody,
  readShopQrLimitError,
} from "@/lib/operatorShop/shopQrLimitPresentation"

describe("buildShopQrLimitBody", () => {
  it("uses the limit verbatim in the exact copy", () => {
    expect(buildShopQrLimitBody(3)).toBe(
      "Your plan allows 3 active QR codes on this Workspace. You can use an existing active QR code or upgrade your plan to add another."
    )
  })
})

describe("SHOP_QR_LIMIT_COPY", () => {
  it("has the title and the three button labels", () => {
    expect(SHOP_QR_LIMIT_COPY).toEqual({
      title: "QR code limit reached",
      backToOrder: "Back to order",
      reviewPlanOptions: "Review plan options",
      manageQrCodes: "Manage QR codes",
    })
  })
})

describe("readShopQrLimitError", () => {
  it("returns the limit from a 409 qr_limit_reached body", () => {
    const error = axiosErrorWithBody(409, {
      success: false,
      code: "qr_limit_reached",
      message: "QR limit reached.",
      limit: 2,
    })

    expect(readShopQrLimitError(error)).toBe(2)
  })

  it("returns null for other codes", () => {
    const error = axiosErrorWithBody(409, {
      code: "offer_card_offer_required",
      message: "Attach a card offer first.",
    })

    expect(readShopQrLimitError(error)).toBeNull()
  })

  it("returns null when the limit is missing or not a number", () => {
    expect(
      readShopQrLimitError(axiosErrorWithBody(409, { code: "qr_limit_reached" }))
    ).toBeNull()
    expect(
      readShopQrLimitError(
        axiosErrorWithBody(409, { code: "qr_limit_reached", limit: "2" })
      )
    ).toBeNull()
  })

  it("returns null for errors that are not Axios errors", () => {
    expect(readShopQrLimitError(new Error("qr_limit_reached"))).toBeNull()
  })
})
