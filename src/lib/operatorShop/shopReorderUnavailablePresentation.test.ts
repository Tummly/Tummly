import { AxiosError, type AxiosResponse } from "axios"
import { describe, expect, it } from "vitest"

import {
  buildShopReorderUnavailableMessage,
  readShopReorderUnavailableLines,
} from "@/lib/operatorShop/shopReorderUnavailablePresentation"

function axiosErrorWithBody(status: number, data: unknown): AxiosError {
  const response = {
    status,
    data,
    headers: {},
    statusText: "",
    config: {},
  } as AxiosResponse
  return new AxiosError("Request failed", "ERR_BAD_REQUEST", undefined, undefined, response)
}

describe("buildShopReorderUnavailableMessage", () => {
  it("lists the unavailable titles joined by comma and space", () => {
    expect(
      buildShopReorderUnavailableMessage([
        { skuId: "table-tents", title: "Table tents" },
        { skuId: "window-stickers", title: "Window stickers" },
      ])
    ).toBe("Some items are no longer available: Table tents, Window stickers")
  })
})

describe("readShopReorderUnavailableLines", () => {
  it("reads unavailableLines from a 409 body", () => {
    const error = axiosErrorWithBody(409, {
      success: false,
      code: "reorder_no_available_lines",
      message: "No items can be reordered.",
      unavailableLines: [{ skuId: "table-tents", title: "Table tents" }],
    })

    expect(readShopReorderUnavailableLines(error)).toEqual([
      { skuId: "table-tents", title: "Table tents" },
    ])
  })

  it("returns an empty list when the body has no unavailableLines", () => {
    expect(
      readShopReorderUnavailableLines(axiosErrorWithBody(409, { code: "x" }))
    ).toEqual([])
    expect(readShopReorderUnavailableLines(new Error("x"))).toEqual([])
  })
})
